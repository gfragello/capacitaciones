# Plan de Testing en Preproducción

## Objetivo

Este plan describe los casos de prueba manuales que se deben ejecutar en el ambiente de preproducción para validar el refactor de notificaciones de vencimiento antes de llevar los cambios a producción.

---

## Prerrequisitos

### Base de datos

- [ ] Se ejecutaron las migraciones de Entity Framework pendientes:
  - `202510170342127_Curso_NotificarVencimiento` (agrega columna `NotificarVencimiento` a `Cursos`)
  - `202601201708142_RegistroCapacitacion_EliminarAprobado` (elimina columna `Aprobado` de `RegistrosCapacitaciones`)
- [ ] Se verificó que la columna `Aprobado` ya no existe en `RegistrosCapacitaciones`
- [ ] Se verificó que la columna `NotificarVencimiento` existe en `Cursos`

### Datos de prueba

- [ ] Al menos 2 cursos configurados:
  - Curso A: con `NotificarVencimiento = true`, vigencia > 0 días
  - Curso B: con `NotificarVencimiento = false`
- [ ] Al menos 3 capacitados de prueba
- [ ] Al menos 2 jornadas por curso (una antigua y una reciente)
- [ ] Al menos 1 empresa con email configurado

### Verificación de deploy

- [ ] La aplicación web inicia sin errores
- [ ] El WebJob `EnviarNotificacionesVencimiento` se ejecuta sin errores de conexión
- [ ] No hay errores de migración de Entity Framework al iniciar

---

## Módulo 1: Gestión de cursos – Flag NotificarVencimiento

### TEST-PRE-001: Crear curso con NotificarVencimiento activado

| Campo | Valor |
|---|---|
| **Precondición** | Usuario autenticado con permisos de administrador |
| **Pasos** | 1. Ir a Cursos → Crear nuevo<br>2. Completar datos obligatorios<br>3. Marcar checkbox "Notificar vencimiento"<br>4. Guardar |
| **Resultado esperado** | El curso se crea con `NotificarVencimiento = true`. Se puede verificar en la vista de edición. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

### TEST-PRE-002: Editar curso y cambiar NotificarVencimiento

| Campo | Valor |
|---|---|
| **Precondición** | Curso existente con `NotificarVencimiento = true` |
| **Pasos** | 1. Ir a Cursos → Editar el curso<br>2. Desmarcar checkbox "Notificar vencimiento"<br>3. Guardar<br>4. Volver a editar y verificar |
| **Resultado esperado** | El checkbox se desmarca y se guarda correctamente. Al volver a editar, el checkbox aparece desmarcado. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

### TEST-PRE-003: Curso sin NotificarVencimiento no genera notificaciones

| Campo | Valor |
|---|---|
| **Precondición** | Curso B con `NotificarVencimiento = false` |
| **Pasos** | 1. Crear jornada para Curso B<br>2. Inscribir un capacitado<br>3. Aprobar el registro de capacitación<br>4. Verificar en la tabla `NotificacionesVencimientos` |
| **Resultado esperado** | No se crea ningún registro en `NotificacionesVencimientos` para este registro de capacitación. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

---

## Módulo 2: Aprobación individual de registros

### TEST-PRE-004: Aprobar registro individual genera notificación

| Campo | Valor |
|---|---|
| **Precondición** | Curso A con `NotificarVencimiento = true`. Capacitado inscripto en una jornada de ese curso. |
| **Pasos** | 1. Ir a Registros de Capacitación<br>2. Editar el registro del capacitado<br>3. Calificar con nota aprobatoria (o cambiar estado a Aprobado)<br>4. Guardar<br>5. Verificar en base de datos: `SELECT * FROM NotificacionesVencimientos WHERE RegistroCapacitacionID = <id>` |
| **Resultado esperado** | Se crea un registro en `NotificacionesVencimientos` con `Estado = 0` (NotificacionPendiente). |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

### TEST-PRE-005: Aprobar registro no genera notificación duplicada

| Campo | Valor |
|---|---|
| **Precondición** | Registro ya aprobado con notificación existente (del TEST-PRE-004) |
| **Pasos** | 1. Editar el mismo registro<br>2. Cambiar la nota pero mantener estado Aprobado<br>3. Guardar<br>4. Verificar que no se duplicó la notificación |
| **Resultado esperado** | Sigue existiendo solo 1 registro en `NotificacionesVencimientos` para ese registro de capacitación. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

### TEST-PRE-006: Cambiar estado de Aprobado a NoAprobado

| Campo | Valor |
|---|---|
| **Precondición** | Registro aprobado con notificación existente |
| **Pasos** | 1. Editar el registro<br>2. Cambiar estado a "No Aprobado"<br>3. Guardar<br>4. Verificar estado de la notificación en BD |
| **Resultado esperado** | El estado del registro cambia. La notificación existente puede permanecer (será limpiada por el proceso preventivo). |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

---

## Módulo 3: Aprobación masiva (por jornada)

### TEST-PRE-007: Calificar jornada completa – todos aprueban

| Campo | Valor |
|---|---|
| **Precondición** | Jornada de Curso A con 3+ capacitados inscriptos. Curso A tiene `NotificarVencimiento = true`. |
| **Pasos** | 1. Ir a Jornadas → Seleccionar la jornada<br>2. Calificar todos los registros con nota aprobatoria<br>3. Guardar calificaciones<br>4. Verificar en BD que se crearon notificaciones para cada registro aprobado |
| **Resultado esperado** | Se crean N registros en `NotificacionesVencimientos` (uno por cada registro aprobado), todos con `Estado = 0`. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

### TEST-PRE-008: Calificar jornada – aprobación parcial

| Campo | Valor |
|---|---|
| **Precondición** | Jornada de Curso A con 3+ capacitados inscriptos |
| **Pasos** | 1. Calificar: 2 con nota aprobatoria, 1 con nota reprobatoria<br>2. Guardar<br>3. Verificar que solo se crearon notificaciones para los 2 aprobados |
| **Resultado esperado** | Solo 2 registros nuevos en `NotificacionesVencimientos`. El reprobado no tiene notificación. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

---

## Módulo 4: Limpieza automática de notificaciones obsoletas

### TEST-PRE-009: Nueva aprobación marca notificación anterior como obsoleta

| Campo | Valor |
|---|---|
| **Precondición** | Capacitado X ya tiene una NotificacionVencimiento pendiente para Curso A (Jornada antigua). Existe una Jornada nueva de Curso A con fecha posterior. |
| **Pasos** | 1. Inscribir Capacitado X en la Jornada nueva de Curso A<br>2. Aprobar el registro<br>3. Verificar en BD:<br>&nbsp;&nbsp;`SELECT * FROM NotificacionesVencimientos nv`<br>&nbsp;&nbsp;`JOIN RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID`<br>&nbsp;&nbsp;`WHERE rc.CapacitadoID = <X>` |
| **Resultado esperado** | La notificación antigua cambia a `Estado = 3` (NoNotificarYaActualizado). La nueva notificación tiene `Estado = 0` (NotificacionPendiente). |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

### TEST-PRE-010: Limpieza preventiva al consultar notificaciones

| Campo | Valor |
|---|---|
| **Precondición** | Insertar manualmente en BD una notificación obsoleta (NotificacionPendiente para un registro cuyo capacitado tiene un registro aprobado más reciente del mismo curso) |
| **Pasos** | 1. Ir a la vista de Notificaciones de Vencimiento<br>2. Verificar que la notificación insertada manualmente fue marcada como obsoleta |
| **Resultado esperado** | Al acceder a la vista, el sistema ejecuta `LimpiarNotificacionesObsoletas()` y marca la notificación como `NoNotificarYaActualizado`. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

---

## Módulo 5: Vista de detalle de capacitados

### TEST-PRE-011: Detalle de capacitado muestra estado de notificación

| Campo | Valor |
|---|---|
| **Precondición** | Capacitado con registro aprobado y notificación existente |
| **Pasos** | 1. Ir a Capacitados → Ver detalle del capacitado<br>2. Observar la sección de registros de capacitación |
| **Resultado esperado** | Se muestra un badge con el estado de la notificación (ej: "Pendiente", "Notificado", "Ya Actualizado") junto al registro correspondiente. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

### TEST-PRE-012: Detalle de capacitado sin notificaciones

| Campo | Valor |
|---|---|
| **Precondición** | Capacitado con registros de un curso con `NotificarVencimiento = false` |
| **Pasos** | 1. Ir a Capacitados → Ver detalle del capacitado |
| **Resultado esperado** | No se muestra badge de notificación para esos registros. La vista se renderiza sin errores. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

---

## Módulo 6: Vista de administración de notificaciones

### TEST-PRE-013: Listar notificaciones pendientes

| Campo | Valor |
|---|---|
| **Precondición** | Existen notificaciones en distintos estados |
| **Pasos** | 1. Ir a Notificaciones de Vencimiento<br>2. Verificar que se listan solo las relevantes (cursos con `NotificarVencimiento = true`)<br>3. Verificar que las obsoletas (`NoNotificarYaActualizado`) no aparecen como pendientes |
| **Resultado esperado** | La lista muestra correctamente las notificaciones pendientes y notificadas. Las obsoletas están filtradas o marcadas diferente. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

### TEST-PRE-014: Cambiar estado de notificación manualmente

| Campo | Valor |
|---|---|
| **Precondición** | Notificación en estado Pendiente |
| **Pasos** | 1. En la vista de notificaciones, cambiar el estado a "No Notificar"<br>2. Guardar<br>3. Verificar el cambio |
| **Resultado esperado** | El estado se actualiza correctamente a `NoNotificar` (Estado = 2). |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

---

## Módulo 7: Importación de datos (Excel)

### TEST-PRE-015: Importar registros desde Excel con campo Aprobado

| Campo | Valor |
|---|---|
| **Precondición** | Archivo Excel con columnas que incluyen "Aprobado" = "Si" / "No" |
| **Pasos** | 1. Ir a Herramientas → Importar datos<br>2. Seleccionar archivo Excel<br>3. Ejecutar importación<br>4. Verificar que los registros tienen el Estado correcto |
| **Resultado esperado** | Los registros con "Si" en Aprobado tienen `Estado = Aprobado`. Los registros con "No" tienen `Estado = NoAprobado`. No se busca la propiedad `Aprobado` (ya eliminada). |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

---

## Módulo 8: Envío de emails (WebJob)

### TEST-PRE-016: Ejecución del WebJob genera emails para notificaciones pendientes

| Campo | Valor |
|---|---|
| **Precondición** | Notificaciones pendientes para cursos vencidos. Empresa con email válido. SMTP configurado. |
| **Pasos** | 1. Ejecutar `EnviarNotificacionesVencimiento.exe` manualmente<br>2. Verificar logs de ejecución<br>3. Verificar que las notificaciones pasaron a estado `Notificado`<br>4. Verificar que se enviaron los emails (o revisar logs de SMTP) |
| **Resultado esperado** | Los emails se envían correctamente. Las notificaciones pasan de `NotificacionPendiente` a `Notificado`. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

### TEST-PRE-017: WebJob no envía emails para notificaciones obsoletas

| Campo | Valor |
|---|---|
| **Precondición** | Notificaciones en estado `NoNotificarYaActualizado` |
| **Pasos** | 1. Ejecutar `EnviarNotificacionesVencimiento.exe`<br>2. Verificar que NO se enviaron emails para esas notificaciones |
| **Resultado esperado** | Solo se procesan las notificaciones con `Estado = NotificacionPendiente`. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

---

## Módulo 9: Regresiones generales

### TEST-PRE-018: Navegación general de la aplicación

| Campo | Valor |
|---|---|
| **Precondición** | Aplicación desplegada en preproducción |
| **Pasos** | 1. Navegar por todas las secciones principales: Cursos, Jornadas, Capacitados, Empresas, Registros<br>2. Verificar que no hay errores de carga<br>3. Verificar que los menús y enlaces funcionan |
| **Resultado esperado** | Navegación sin errores 500 ni páginas en blanco. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

### TEST-PRE-019: Generación de certificados

| Campo | Valor |
|---|---|
| **Precondición** | Registro de capacitación aprobado |
| **Pasos** | 1. Generar certificado para un registro aprobado<br>2. Verificar que el PDF se genera correctamente |
| **Resultado esperado** | El certificado se genera sin depender de la propiedad `Aprobado` eliminada. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

### TEST-PRE-020: Búsqueda y filtrado de capacitados

| Campo | Valor |
|---|---|
| **Precondición** | Capacitados con distintos estados de registro |
| **Pasos** | 1. Buscar capacitados por nombre<br>2. Filtrar por empresa<br>3. Ver detalles de varios capacitados |
| **Resultado esperado** | Búsqueda y filtrado funcionan correctamente. No hay errores relacionados con la propiedad `Aprobado`. |
| **Estado** | ☐ Pasó ☐ Falló |
| **Notas** | |

---

## Resumen de ejecución

| Módulo | Tests | Pasaron | Fallaron |
|---|---|---|---|
| 1. Cursos – NotificarVencimiento | 3 | | |
| 2. Aprobación individual | 3 | | |
| 3. Aprobación masiva | 2 | | |
| 4. Limpieza automática | 2 | | |
| 5. Vista de capacitados | 2 | | |
| 6. Admin notificaciones | 2 | | |
| 7. Importación Excel | 1 | | |
| 8. Envío de emails | 2 | | |
| 9. Regresiones generales | 3 | | |
| **Total** | **20** | | |

---

## Criterios de paso a producción

- [ ] Todos los tests pasaron (20/20)
- [ ] No se encontraron errores bloqueantes
- [ ] Los errores menores están documentados y priorizados
- [ ] Se ejecutó el script de depuración de datos de notificaciones (ver `PROPUESTA_DEPURACION_NOTIFICACIONES.md`)
- [ ] Se verificó la configuración de email en el ambiente de producción
- [ ] Se tomó backup de la base de datos de producción antes de aplicar migraciones
