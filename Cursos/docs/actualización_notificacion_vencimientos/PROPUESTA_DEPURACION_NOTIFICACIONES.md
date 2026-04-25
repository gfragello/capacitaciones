# Propuesta de Depuración de Datos – NotificacionesVencimientos

## Contexto

Con el nuevo refactor, la lógica de notificaciones cambió significativamente:

1. **Antes:** Las notificaciones se creaban manualmente o mediante el proceso batch `ActualizarNotificacionesVencimientos`. No había limpieza automática de notificaciones obsoletas. Los cursos excluidos se filtraban por ID hardcodeado (`CursoId NOT IN (2, 4, 5)`).

2. **Ahora:** Las notificaciones se crean automáticamente al aprobar un registro (`CambiarEstado → EjecutarAccionesAlAprobar`). Se limpian automáticamente las notificaciones obsoletas cuando un capacitado renueva su certificación. Se usa el flag `NotificarVencimiento` por curso en lugar de IDs hardcodeados.

**Consecuencia:** Muchos registros en `NotificacionesVencimientos` son datos heredados que ya no tienen sentido bajo la nueva metodología y deben ser depurados.

---

## Diagnóstico: Tipos de datos obsoletos

### Tipo 1 – Notificaciones de cursos que ya no deberían notificarse

Notificaciones creadas para cursos que ahora tendrán `NotificarVencimiento = false` (ej: Refresh, Induction, Nursery – los antiguos IDs 2, 4, 5).

```sql
-- DIAGNÓSTICO: Contar notificaciones de cursos que no deberían notificar
SELECT 
    cu.CursoID,
    cu.Descripcion AS Curso,
    COUNT(*) AS TotalNotificaciones,
    SUM(CASE WHEN nv.Estado = 0 THEN 1 ELSE 0 END) AS Pendientes,
    SUM(CASE WHEN nv.Estado = 1 THEN 1 ELSE 0 END) AS Notificadas,
    SUM(CASE WHEN nv.Estado = 2 THEN 1 ELSE 0 END) AS NoNotificar,
    SUM(CASE WHEN nv.Estado = 3 THEN 1 ELSE 0 END) AS YaActualizado
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
WHERE cu.NotificarVencimiento = 0
GROUP BY cu.CursoID, cu.Descripcion
ORDER BY TotalNotificaciones DESC;
```

### Tipo 2 – Notificaciones pendientes para capacitados que ya renovaron

Notificaciones en estado `Pendiente` (0) donde el capacitado ya tiene una aprobación más reciente del mismo curso. Estas deberían haber sido marcadas como `NoNotificarYaActualizado` (3), pero fueron creadas antes del refactor.

```sql
-- DIAGNÓSTICO: Notificaciones pendientes que deberían estar marcadas como obsoletas
SELECT 
    nv.NotificacionVencimientoID,
    c.Documento,
    (c.Apellido + ', ' + c.Nombre) AS Capacitado,
    cu.Descripcion AS Curso,
    CONVERT(varchar, j.Fecha, 103) AS FechaJornadaNotificada,
    CONVERT(varchar, rc.FechaVencimiento, 103) AS FechaVencimiento,
    (
        SELECT TOP 1 CONVERT(varchar, j2.Fecha, 103)
        FROM dbo.RegistrosCapacitaciones rc2
        INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
        WHERE rc2.CapacitadoID = rc.CapacitadoID
          AND j2.CursoId = j.CursoId
          AND j2.Fecha > j.Fecha
          AND rc2.Estado = 1
        ORDER BY j2.Fecha DESC
    ) AS FechaJornadaPosterior
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
INNER JOIN dbo.Capacitados c ON rc.CapacitadoID = c.CapacitadoID
WHERE nv.Estado = 0  -- Pendiente
  AND EXISTS (
    SELECT 1
    FROM dbo.RegistrosCapacitaciones rc2
    INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
    WHERE rc2.CapacitadoID = rc.CapacitadoID
      AND j2.CursoId = j.CursoId
      AND j2.Fecha > j.Fecha
      AND rc2.Estado = 1
  )
ORDER BY cu.Descripcion, c.Apellido;
```

### Tipo 3 – Notificaciones para registros no aprobados

Notificaciones asociadas a registros que no están en estado Aprobado. Bajo la nueva lógica, solo se crean notificaciones para registros aprobados.

```sql
-- DIAGNÓSTICO: Notificaciones para registros que no están aprobados
SELECT 
    nv.NotificacionVencimientoID,
    c.Documento,
    (c.Apellido + ', ' + c.Nombre) AS Capacitado,
    cu.Descripcion AS Curso,
    CASE rc.Estado
        WHEN 0 THEN 'Inscripto'
        WHEN 1 THEN 'Aprobado'
        WHEN 2 THEN 'No Aprobado'
    END AS EstadoRegistro,
    CASE nv.Estado
        WHEN 0 THEN 'Pendiente'
        WHEN 1 THEN 'Notificado'
        WHEN 2 THEN 'No Notificar'
        WHEN 3 THEN 'Ya Actualizado'
    END AS EstadoNotificacion
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
INNER JOIN dbo.Capacitados c ON rc.CapacitadoID = c.CapacitadoID
WHERE rc.Estado != 1  -- No aprobado
ORDER BY cu.Descripcion, c.Apellido;
```

### Tipo 4 – Notificaciones duplicadas

Múltiples notificaciones para el mismo registro de capacitación. El nuevo sistema previene duplicados, pero podrían existir datos históricos.

```sql
-- DIAGNÓSTICO: Registros con más de una notificación
SELECT 
    rc.RegistroCapacitacionID,
    c.Documento,
    (c.Apellido + ', ' + c.Nombre) AS Capacitado,
    cu.Descripcion AS Curso,
    COUNT(nv.NotificacionVencimientoID) AS CantidadNotificaciones
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
INNER JOIN dbo.Capacitados c ON rc.CapacitadoID = c.CapacitadoID
GROUP BY rc.RegistroCapacitacionID, c.Documento, c.Apellido, c.Nombre, cu.Descripcion
HAVING COUNT(nv.NotificacionVencimientoID) > 1
ORDER BY CantidadNotificaciones DESC;
```

---

## Plan de depuración

### Paso 0 – Backup (OBLIGATORIO)

```sql
-- Crear tabla de backup antes de cualquier cambio
SELECT *
INTO dbo.NotificacionesVencimientos_BACKUP_DEPURACION
FROM dbo.NotificacionesVencimientos;

-- Verificar que el backup está completo
SELECT 
    'Original' AS Tabla, COUNT(*) AS Registros FROM dbo.NotificacionesVencimientos
UNION ALL
SELECT 
    'Backup' AS Tabla, COUNT(*) AS Registros FROM dbo.NotificacionesVencimientos_BACKUP_DEPURACION;
```

### Paso 1 – Configurar flag NotificarVencimiento en cursos

Antes de depurar, hay que configurar qué cursos deben notificar vencimiento.

```sql
-- Primero, ver todos los cursos y decidir cuáles deben notificar
SELECT 
    CursoID, 
    Descripcion, 
    NotificarVencimiento,
    Vigencia,
    Activo
FROM dbo.Cursos
ORDER BY CursoID;

-- Activar NotificarVencimiento para los cursos que corresponda
-- (AJUSTAR SEGÚN TU CRITERIO - ejemplo: activar todos menos 2, 4, 5)
UPDATE dbo.Cursos
SET NotificarVencimiento = 1
WHERE CursoID NOT IN (2, 4, 5)  -- Ajustar según corresponda
  AND Vigencia > 0               -- Solo cursos con vigencia
  AND Activo = 1;                -- Solo cursos activos

-- Verificar
SELECT CursoID, Descripcion, NotificarVencimiento, Vigencia
FROM dbo.Cursos
ORDER BY NotificarVencimiento DESC, CursoID;
```

### Paso 2 – Marcar notificaciones de cursos no notificables

```sql
-- Contar antes de actualizar
SELECT COUNT(*) AS RegistrosAActualizar
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
WHERE cu.NotificarVencimiento = 0
  AND nv.Estado IN (0, 1);  -- Pendientes y Notificadas

-- Actualizar: marcar como NoNotificar
UPDATE nv
SET nv.Estado = 2  -- NoNotificar
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
WHERE cu.NotificarVencimiento = 0
  AND nv.Estado IN (0, 1);
```

### Paso 3 – Marcar notificaciones obsoletas (capacitado renovó)

```sql
-- Contar antes de actualizar
SELECT COUNT(*) AS RegistrosAActualizar
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
WHERE nv.Estado = 0  -- Solo pendientes
  AND EXISTS (
    SELECT 1
    FROM dbo.RegistrosCapacitaciones rc2
    INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
    WHERE rc2.CapacitadoID = rc.CapacitadoID
      AND j2.CursoId = j.CursoId
      AND j2.Fecha > j.Fecha
      AND rc2.Estado = 1  -- Aprobado
  );

-- Actualizar: marcar como Ya Actualizado
UPDATE nv
SET nv.Estado = 3  -- NoNotificarYaActualizado
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
WHERE nv.Estado = 0
  AND EXISTS (
    SELECT 1
    FROM dbo.RegistrosCapacitaciones rc2
    INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
    WHERE rc2.CapacitadoID = rc.CapacitadoID
      AND j2.CursoId = j.CursoId
      AND j2.Fecha > j.Fecha
      AND rc2.Estado = 1
  );
```

### Paso 4 – Marcar notificaciones de registros no aprobados

```sql
-- Contar antes de actualizar
SELECT COUNT(*) AS RegistrosAActualizar
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
WHERE rc.Estado != 1  -- No aprobados
  AND nv.Estado IN (0, 1);  -- Pendientes o Notificadas

-- Actualizar: marcar como NoNotificar
UPDATE nv
SET nv.Estado = 2  -- NoNotificar
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
WHERE rc.Estado != 1
  AND nv.Estado IN (0, 1);
```

### Paso 5 – Eliminar notificaciones duplicadas (conservar la más reciente)

```sql
-- Contar duplicadas
SELECT COUNT(*) AS NotificacionesDuplicadasAEliminar
FROM dbo.NotificacionesVencimientos nv
WHERE EXISTS (
    SELECT 1
    FROM dbo.NotificacionesVencimientos nv2
    WHERE nv2.RegistroCapacitacionID = nv.RegistroCapacitacionID
      AND nv2.NotificacionVencimientoID > nv.NotificacionVencimientoID
);

-- Eliminar duplicadas (conservar la de mayor ID = la más reciente)
DELETE nv
FROM dbo.NotificacionesVencimientos nv
WHERE EXISTS (
    SELECT 1
    FROM dbo.NotificacionesVencimientos nv2
    WHERE nv2.RegistroCapacitacionID = nv.RegistroCapacitacionID
      AND nv2.NotificacionVencimientoID > nv.NotificacionVencimientoID
);
```

### Paso 6 – Verificación final

```sql
-- Distribución final por estado
SELECT 
    CASE Estado
        WHEN 0 THEN 'Pendiente'
        WHEN 1 THEN 'Notificado'
        WHEN 2 THEN 'No Notificar'
        WHEN 3 THEN 'Ya Actualizado'
    END AS Estado,
    COUNT(*) AS Cantidad
FROM dbo.NotificacionesVencimientos
GROUP BY Estado
ORDER BY Estado;

-- Comparar antes/después
SELECT 
    'Antes (Backup)' AS Momento, 
    COUNT(*) AS Total,
    SUM(CASE WHEN Estado = 0 THEN 1 ELSE 0 END) AS Pendientes,
    SUM(CASE WHEN Estado = 1 THEN 1 ELSE 0 END) AS Notificadas,
    SUM(CASE WHEN Estado = 2 THEN 1 ELSE 0 END) AS NoNotificar,
    SUM(CASE WHEN Estado = 3 THEN 1 ELSE 0 END) AS YaActualizado
FROM dbo.NotificacionesVencimientos_BACKUP_DEPURACION
UNION ALL
SELECT 
    'Después' AS Momento, 
    COUNT(*) AS Total,
    SUM(CASE WHEN Estado = 0 THEN 1 ELSE 0 END) AS Pendientes,
    SUM(CASE WHEN Estado = 1 THEN 1 ELSE 0 END) AS Notificadas,
    SUM(CASE WHEN Estado = 2 THEN 1 ELSE 0 END) AS NoNotificar,
    SUM(CASE WHEN Estado = 3 THEN 1 ELSE 0 END) AS YaActualizado
FROM dbo.NotificacionesVencimientos;

-- Verificar que no hay notificaciones pendientes obsoletas
SELECT COUNT(*) AS NotificacionesPendientesObsoletas
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
WHERE nv.Estado = 0
  AND EXISTS (
    SELECT 1
    FROM dbo.RegistrosCapacitaciones rc2
    INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
    WHERE rc2.CapacitadoID = rc.CapacitadoID
      AND j2.CursoId = j.CursoId
      AND j2.Fecha > j.Fecha
      AND rc2.Estado = 1
  );
-- Resultado esperado: 0
```

---

## Opción alternativa: Eliminar registros que ya no aportan valor

Si preferís una limpieza más agresiva, podés eliminar los registros que ya no tienen sentido en lugar de solo cambiar su estado. **Esta opción es más riesgosa pero deja la tabla más limpia.**

### Opción A – Eliminar notificaciones de cursos no notificables

```sql
-- Solo si estás seguro de que nunca vas a necesitar el histórico
DELETE nv
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
WHERE cu.NotificarVencimiento = 0;
```

### Opción B – Eliminar todas las notificaciones obsoletas y dejar solo las vigentes

```sql
-- Conservar solo: Pendientes y Notificadas de cursos con NotificarVencimiento = true
-- Eliminar todo lo demás
DELETE nv
FROM dbo.NotificacionesVencimientos nv
WHERE nv.Estado IN (2, 3)  -- NoNotificar y YaActualizado
   OR nv.NotificacionVencimientoID IN (
      SELECT nv2.NotificacionVencimientoID
      FROM dbo.NotificacionesVencimientos nv2
      INNER JOIN dbo.RegistrosCapacitaciones rc ON nv2.RegistroCapacitacionID = rc.RegistroCapacitacionID
      INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
      INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
      WHERE cu.NotificarVencimiento = 0
   );
```

---

## Recomendación

Recomiendo el enfoque conservador (Pasos 1 a 6) que **marca** los registros como obsoletos en lugar de eliminarlos:

| Criterio | Enfoque conservador | Enfoque agresivo |
|---|---|---|
| **Riesgo** | Bajo – no se eliminan datos | Medio – se eliminan registros |
| **Reversibilidad** | Alta – se puede revertir cambiando el estado | Baja – requiere restaurar backup |
| **Limpieza** | Buena – los registros obsoletos se filtran en la aplicación | Excelente – tabla más liviana |
| **Historial** | Se conserva historial completo | Se pierde historial de notificaciones obsoletas |
| **Rendimiento** | Aceptable – un poco más lento por más registros | Óptimo – menos registros en la tabla |

**Mi recomendación:** Ejecutar el enfoque conservador primero. Si después de unos meses de operación con la nueva lógica todo funciona correctamente, se puede ejecutar el enfoque agresivo para limpiar los registros marcados.

---

## Orden de ejecución sugerido

1. **En preproducción:**
   - [ ] Ejecutar Paso 0 (Backup)
   - [ ] Ejecutar diagnósticos (Tipos 1-4) para entender la magnitud
   - [ ] Ejecutar Paso 1 (Configurar cursos)
   - [ ] Ejecutar Pasos 2-5 (Depuración)
   - [ ] Ejecutar Paso 6 (Verificación)
   - [ ] Ejecutar tests de preproducción (ver `PLAN_TESTING_PREPRODUCCION.md`)

2. **En producción (después de validar en preprod):**
   - [ ] Tomar backup completo de la base de datos
   - [ ] Ejecutar Paso 0 (Backup de tabla)
   - [ ] Ejecutar Paso 1 (Configurar cursos) – **verificar con el equipo qué cursos deben notificar**
   - [ ] Ejecutar Pasos 2-5 (Depuración)
   - [ ] Ejecutar Paso 6 (Verificación)
   - [ ] Monitorear la aplicación durante las primeras 24-48 horas

3. **Después de 30 días estable:**
   - [ ] Evaluar si ejecutar limpieza agresiva (Opciones A/B)
   - [ ] Eliminar tabla de backup si no se necesita: `DROP TABLE dbo.NotificacionesVencimientos_BACKUP_DEPURACION`
