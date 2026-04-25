# Propuesta de Depuración – Tabla `NotificacionesVencimientos`

> Documento de liberación. Acompaña a [scripts.sql](scripts.sql) y se ejecuta una única vez en producción, **después** de impactar las migraciones y desplegar el nuevo binario.

---

## 1. Contexto

La liberación incorpora tres cambios estructurales sobre el dominio de notificaciones de vencimiento:

1. **Nueva columna `Cursos.NotificarVencimiento`** (migración `202510170342127_Curso_NotificarVencimiento`). Reemplaza el filtro histórico por IDs hardcodeados (`CursoID NOT IN (2, 4, 5)`) repartido por el código y por scripts SQL externos.
2. **Creación automática de la notificación al aprobar** (`CrearNotificacionVencimiento` invocado desde `CambiarEstado` / `CalificarRegistro` / `CalificarRegistroNota`). Antes la notificación se creaba por un batch (`ActualizarNotificacionesVencimientos`) que recorría registros aprobados sin notificación, sin validar duplicados ni estado del curso.
3. **Limpieza automática de notificaciones obsoletas** (`ActualizarNotificacionesObsoletasPorCapacitado` al aprobar + `LimpiarNotificacionesObsoletas` al cargar la vista). Antes había que correr manualmente [`No Notificar Registros Ya Actualizados.sql`](../No%20Notificar%20Registros%20Ya%20Actualizados.sql).

Bajo la nueva lógica, las invariantes esperadas sobre la tabla son:

- (I1) Toda fila corresponde a un `RegistroCapacitacion` en estado `Aprobado` con `FechaVencimiento` no nula.
- (I2) Toda fila corresponde a una `Jornada` cuyo `Curso.NotificarVencimiento = 1`.
- (I3) Existe a lo sumo una fila por `RegistroCapacitacionID`.
- (I4) Si el capacitado aprobó una jornada posterior del mismo curso, la notificación está en estado `3` (`NoNotificarYaActualizado`), nunca en `0` (`NotificacionPendiente`).

## 2. Diagnóstico sobre copia local de producción

Mediciones ejecutadas sobre la copia de la base de producción traída localmente:

| Indicador | Cantidad | % del total |
|---|---:|---:|
| Total de filas en `NotificacionesVencimientos` | **71.463** | 100,0 % |
| Filas con `Estado = 0` (Pendiente) | 40.772 | 57,1 % |
| Filas con `Estado = 1` (Notificado) | 15.947 | 22,3 % |
| Filas con `Estado = 2` (NoNotificar) | 14.347 | 20,1 % |
| Filas con `Estado = 3` (YaActualizado) | 397 | 0,6 % |

Distribución de las filas problemáticas:

| Categoría | Cantidad | Invariante violada |
|---|---:|---|
| Notificaciones de cursos que tendrán `NotificarVencimiento = 0` (IDs 2, 4, 5 y `SinVigencia`) | **36.490** | I2 |
| Notificaciones de registros **no aprobados** (`RegistrosCapacitaciones.Estado ≠ 1`) | 96 | I1 |
| `RegistroCapacitacionID` con más de una notificación | 1.414 | I3 |
| Notificaciones pendientes (`Estado = 0`) con registro posterior aprobado del mismo curso | 2.773 | I4 |

Desglose de (I2) por curso (ordenado por impacto):

| CursoID | Descripción | Total notif. | Pend. | Notif. | NoNotif. | YaAct. |
|---:|---|---:|---:|---:|---:|---:|
| 4 | IN – Inducción | 21.652 | 19.946 | 7 | 1.699 | 0 |
| 2 | RF – Refresh | 13.817 | 8.457 | 0 | 5.349 | 11 |
| 5 | VI – Viveros | 980 | 794 | 0 | 186 | 0 |
| 10 | HM – Herramientas manuales (`SinVigencia=1`) | 41 | 41 | 0 | 0 | 0 |
| **Total** | | **36.490** | | | | |

## 3. Justificación de la depuración

### 3.1 Eliminar (no marcar) las 36.490 notificaciones de cursos sin vencimiento

- **No tienen uso actual ni futuro.** La consulta que alimenta la vista filtra con `n.RegistroCapacitacion.Jornada.Curso.NotificarVencimiento` (ver [NotificacionesVencimientosController.cs L306](../../../Controllers/NotificacionesVencimientosController.cs#L306)). Ninguna de estas filas es visible en la aplicación después del deploy.
- **No tienen uso histórico.** En la lógica vieja estos cursos ya estaban excluidos explícitamente (`CursoID NOT IN (2, 4, 5)`) del proceso batch; la razón por la que hoy existen filas es un bug histórico: el batch las creaba igual porque el filtro se aplicaba sólo al paso de marcarlas como obsoletas, no al paso de crearlas. Esa deuda técnica queda cerrada con el nuevo diseño.
- **Alternativa descartada — `UPDATE Estado = 2`.** Marcarlas como `NoNotificar` deja 36.490 filas basura que engordan la tabla, confunden en reportes ad‑hoc y distorsionan las métricas por estado. No aporta valor: los registros no se van a auditar ni recuperar.
- **Reversibilidad.** Se resguarda toda la tabla antes de eliminar ([Paso 0 de scripts.sql](scripts.sql)).

### 3.2 Eliminar las 96 notificaciones de registros no aprobados

Rompen I1 y corresponden a estados inconsistentes (probablemente registros que pasaron de `Aprobado` a `NoAprobado` sin limpiar la notificación). La nueva lógica no las recrea y la UI nunca las muestra con sentido. Eliminarlas es seguro.

### 3.3 Eliminar duplicados conservando el de mayor `NotificacionVencimientoID`

Hay 1.414 `RegistroCapacitacionID` con más de una notificación. El nuevo método `CrearNotificacionVencimiento` valida explícitamente la preexistencia, por lo que no se van a regenerar. Conservar la fila más reciente (mayor ID) es equivalente a conservar el último estado persistido por la lógica vieja.

### 3.4 Marcar (no eliminar) las 2.773 pendientes obsoletas

Estas filas **sí cumplen I1 y I2** — son notificaciones válidas que simplemente quedaron en estado desactualizado porque la limpieza automática no existía. La forma de normalizarlas es llevarlas a `Estado = 3` (`NoNotificarYaActualizado`), idéntico resultado al que produciría `LimpiarNotificacionesObsoletas()` la primera vez que un usuario abra la vista. Ejecutarlo en el script garantiza consistencia inmediata y evita que el primer acceso post‑deploy haga un `UPDATE` masivo dentro del request.

### 3.5 Resumen de impacto

| Acción | Filas afectadas | Resultado sobre el total |
|---|---:|---:|
| DELETE duplicados | ~1.414 | 71.463 → ~70.049 |
| DELETE cursos `NotificarVencimiento = 0` | ~36.490 | → ~33.559 |
| DELETE registros no aprobados | ~96 | → ~33.463 |
| UPDATE pendientes obsoletas → `Estado = 3` | ~2.773 | (sin cambio en total) |
| **Reducción total estimada** | **~38.000 filas (~53 %)** | |

Los órdenes de magnitud pueden variar ligeramente entre preproducción y producción; el script imprime la cantidad exacta afectada en cada paso.

## 4. Script y orden de ejecución

El archivo [scripts.sql](scripts.sql) contiene:

1. **Paso 1 – Configuración de `NotificarVencimiento`** (ya estaba en el archivo). Réplica exacta del comportamiento anterior: `0` para IDs 2, 4, 5 y `SinVigencia=1`; `1` para el resto.
2. **Paso 2 – Backup.** `SELECT … INTO dbo.NotificacionesVencimientos_BACKUP_<AAAAMMDD>`.
3. **Paso 3 – Diagnóstico previo.** Conteo de cada categoría. Útil para comparar antes/después.
4. **Paso 4 – Eliminación de duplicados** (antes que los demás `DELETE` para no depender del orden de los otros filtros).
5. **Paso 5 – Eliminación de notificaciones de cursos no notificables.**
6. **Paso 6 – Eliminación de notificaciones de registros no aprobados.**
7. **Paso 7 – Marcado de pendientes obsoletas** como `NoNotificarYaActualizado`.
8. **Paso 8 – Verificación final.** Comparativa backup vs tabla, chequeo de que las cuatro invariantes se cumplen (conteos esperados en `0`) y cierre transaccional automático.

**Transacción:** todo el bloque de depuración (pasos 4 a 7) se ejecuta dentro de una transacción. Al final del Paso 8, el script:

- hace `COMMIT` automáticamente si las cuatro invariantes dan `0`;
- hace `ROLLBACK` automáticamente y termina con error si alguna invariante falla.

## 5. Orden operativo sugerido en producción

1. Ventana de mantenimiento acordada.
2. Backup **completo** de la base (responsabilidad del DBA / plataforma, fuera de este script).
3. Deploy del binario nuevo y ejecución de migraciones EF (incluye `Curso_NotificarVencimiento`).
4. Ejecutar [scripts.sql](scripts.sql) completo en una sola sesión de SSMS o mediante `sqlcmd`.
5. Validar los conteos del Paso 8. Si todos son `0`, el script deja los cambios persistidos automáticamente. Si no, revierte automáticamente y termina con error.
6. Smoke test en la app: abrir *Notificaciones de vencimiento*, verificar que el listado muestra sólo cursos con `NotificarVencimiento = 1` y sin filas obsoletas.
7. Dejar la tabla de backup durante al menos 30 días. Al cabo de ese período, si no hubo incidentes, ejecutar:

    ```sql
    DROP TABLE dbo.NotificacionesVencimientos_BACKUP_<AAAAMMDD>;
    ```

## 6. Plan de rollback

En el improbable caso de que, **después del `COMMIT`**, se detecte un problema de datos:

```sql
-- Restaurar contenido completo desde el backup
TRUNCATE TABLE dbo.NotificacionesVencimientos;

SET IDENTITY_INSERT dbo.NotificacionesVencimientos ON;

INSERT INTO dbo.NotificacionesVencimientos
    (NotificacionVencimientoID, Fecha, Estado, MailNotificacionVencimiento, RegistroCapacitacionID)
SELECT
    NotificacionVencimientoID, Fecha, Estado, MailNotificacionVencimiento, RegistroCapacitacionID
FROM dbo.NotificacionesVencimientos_BACKUP_<AAAAMMDD>;

SET IDENTITY_INSERT dbo.NotificacionesVencimientos OFF;
```

> Ajustar las columnas al esquema real que devuelva `sp_help 'dbo.NotificacionesVencimientos'` en producción en el momento del deploy; el listado anterior refleja el modelo EF actual.

---

**Autor:** propuesta generada a partir del análisis estático del código post‑refactor y de mediciones sobre la copia de producción local.
