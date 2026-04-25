/*
==============================================================================
EJEMPLOS DE VERIFICACIÓN DE CAMBIOS - SISTEMA DE NOTIFICACIONES
==============================================================================

Este script contiene queries útiles para verificar el correcto funcionamiento
de las actualizaciones automáticas de notificaciones de vencimiento.

Fecha: 14 de octubre de 2025
==============================================================================
*/

-- ============================================================================
-- 1. VERIFICAR NOTIFICACIONES POR ESTADO
-- ============================================================================
-- Muestra la distribución de notificaciones por estado
SELECT 
    CASE Estado
        WHEN 0 THEN 'Pendiente'
        WHEN 1 THEN 'Notificado'
        WHEN 2 THEN 'No Notificar'
        WHEN 3 THEN 'No Notificar (Ya Actualizado)'
        ELSE 'Desconocido'
    END AS Estado,
    COUNT(*) AS Cantidad
FROM dbo.NotificacionesVencimientos
GROUP BY Estado
ORDER BY Estado;


-- ============================================================================
-- 2. NOTIFICACIONES ACTUALIZADAS RECIENTEMENTE
-- ============================================================================
-- Muestra notificaciones que fueron marcadas como "Ya Actualizado" en las últimas 24 horas
SELECT TOP 20
    nv.NotificacionVencimientoID,
    nv.Fecha AS [Fecha Notificación],
    c.Documento,
    (c.Apellido + ', ' + c.Nombre) AS Capacitado,
    cu.Descripcion AS Curso,
    CONVERT(varchar, rc.FechaVencimiento, 103) AS [Fecha Vencimiento],
    CONVERT(varchar, j.Fecha, 103) AS [Fecha Jornada Original]
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
INNER JOIN dbo.Capacitados c ON rc.CapacitadoID = c.CapacitadoID
WHERE nv.Estado = 3 -- NoNotificarYaActualizado
  AND nv.Fecha >= DATEADD(day, -1, GETDATE())
ORDER BY nv.Fecha DESC;


-- ============================================================================
-- 3. VERIFICAR CASO ESPECÍFICO DE CAPACITADO
-- ============================================================================
-- Busca todas las notificaciones de un capacitado específico (cambiar documento)
DECLARE @Documento VARCHAR(20) = '12345678'; -- CAMBIAR AQUÍ

SELECT 
    nv.NotificacionVencimientoID,
    CASE nv.Estado
        WHEN 0 THEN 'Pendiente'
        WHEN 1 THEN 'Notificado'
        WHEN 2 THEN 'No Notificar'
        WHEN 3 THEN 'Ya Actualizado'
    END AS Estado,
    cu.Descripcion AS Curso,
    CONVERT(varchar, j.Fecha, 103) AS [Fecha Jornada],
    CONVERT(varchar, rc.FechaVencimiento, 103) AS [Fecha Vencimiento],
    CASE rc.Estado
        WHEN 0 THEN 'Inscripto'
        WHEN 1 THEN 'Aprobado'
        WHEN 2 THEN 'No Aprobado'
    END AS [Estado Registro]
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
INNER JOIN dbo.Capacitados c ON rc.CapacitadoID = c.CapacitadoID
WHERE c.Documento = @Documento
ORDER BY cu.Descripcion, j.Fecha DESC;


-- ============================================================================
-- 4. CAPACITADOS CON MÚLTIPLES REGISTROS DEL MISMO CURSO
-- ============================================================================
-- Identifica capacitados que tienen más de un registro del mismo curso
-- (útil para verificar el comportamiento del sistema)
SELECT 
    c.Documento,
    (c.Apellido + ', ' + c.Nombre) AS Capacitado,
    cu.Descripcion AS Curso,
    COUNT(*) AS [Cantidad Registros],
    MIN(j.Fecha) AS [Primera Jornada],
    MAX(j.Fecha) AS [Última Jornada]
FROM dbo.RegistrosCapacitaciones rc
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
INNER JOIN dbo.Capacitados c ON rc.CapacitadoID = c.CapacitadoID
WHERE rc.Estado IN (1, 2) -- Aprobado o No Aprobado (excluye Inscriptos)
GROUP BY c.CapacitadoID, c.Documento, c.Apellido, c.Nombre, cu.CursoID, cu.Descripcion
HAVING COUNT(*) > 1
ORDER BY cu.Descripcion, c.Apellido;


-- ============================================================================
-- 5. DETALLE DE REGISTROS POSTERIORES PARA UNA NOTIFICACIÓN
-- ============================================================================
-- Para una notificación específica, muestra si hay registros posteriores
-- (cambiar el NotificacionVencimientoID)
DECLARE @NotificacionID INT = 1; -- CAMBIAR AQUÍ

SELECT 
    'Notificación Original' AS Tipo,
    rc.RegistroCapacitacionID,
    CONVERT(varchar, j.Fecha, 103) AS Fecha,
    CASE rc.Estado
        WHEN 0 THEN 'Inscripto'
        WHEN 1 THEN 'Aprobado'
        WHEN 2 THEN 'No Aprobado'
    END AS Estado,
    rc.Nota
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
WHERE nv.NotificacionVencimientoID = @NotificacionID

UNION ALL

SELECT 
    'Registros Posteriores' AS Tipo,
    rc2.RegistroCapacitacionID,
    CONVERT(varchar, j2.Fecha, 103) AS Fecha,
    CASE rc2.Estado
        WHEN 0 THEN 'Inscripto'
        WHEN 1 THEN 'Aprobado'
        WHEN 2 THEN 'No Aprobado'
    END AS Estado,
    rc2.Nota
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.RegistrosCapacitaciones rc2 ON rc2.CapacitadoID = rc.CapacitadoID
INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
WHERE nv.NotificacionVencimientoID = @NotificacionID
  AND j2.CursoId = j.CursoId
  AND j2.Fecha > j.Fecha
ORDER BY Tipo, Fecha;


-- ============================================================================
-- 6. NOTIFICACIONES PENDIENTES QUE PODRÍAN SER ACTUALIZADAS
-- ============================================================================
-- Identifica notificaciones pendientes que deberían ser marcadas como "Ya Actualizado"
-- (útil para verificar que el sistema esté funcionando correctamente)
SELECT 
    nv.NotificacionVencimientoID,
    c.Documento,
    (c.Apellido + ', ' + c.Nombre) AS Capacitado,
    cu.Descripcion AS Curso,
    CONVERT(varchar, j.Fecha, 103) AS [Fecha Jornada Notificación],
    CONVERT(varchar, rc.FechaVencimiento, 103) AS [Fecha Vencimiento],
    (
        SELECT TOP 1 CONVERT(varchar, j2.Fecha, 103)
        FROM dbo.RegistrosCapacitaciones rc2
        INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
        WHERE rc2.CapacitadoID = rc.CapacitadoID
          AND j2.CursoId = j.CursoId
          AND j2.Fecha > j.Fecha
          AND rc2.Estado = 1 -- Aprobado
        ORDER BY j2.Fecha DESC
    ) AS [Jornada Posterior Aprobada]
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
INNER JOIN dbo.Capacitados c ON rc.CapacitadoID = c.CapacitadoID
WHERE nv.Estado = 0 -- Pendiente
  AND rc.FechaVencimiento > '2025-01-01'
  AND j.CursoId NOT IN (2, 4, 5)
  AND EXISTS (
    SELECT 1
    FROM dbo.RegistrosCapacitaciones rc2
    INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
    WHERE rc2.CapacitadoID = rc.CapacitadoID
      AND j2.CursoId = j.CursoId
      AND j2.Fecha > j.Fecha
      AND rc2.Estado = 1 -- Aprobado
);


-- ============================================================================
-- 7. ESTADÍSTICAS DE CURSOS EXCLUIDOS
-- ============================================================================
-- Muestra notificaciones de cursos excluidos (2, 4, 5)
SELECT 
    cu.CursoID,
    cu.Descripcion AS Curso,
    COUNT(*) AS [Total Notificaciones],
    SUM(CASE WHEN nv.Estado = 0 THEN 1 ELSE 0 END) AS Pendientes,
    SUM(CASE WHEN nv.Estado = 1 THEN 1 ELSE 0 END) AS Notificados,
    SUM(CASE WHEN nv.Estado = 2 THEN 1 ELSE 0 END) AS [No Notificar],
    SUM(CASE WHEN nv.Estado = 3 THEN 1 ELSE 0 END) AS [Ya Actualizado]
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
WHERE cu.CursoID IN (2, 4, 5)
GROUP BY cu.CursoID, cu.Descripcion
ORDER BY cu.CursoID;


-- ============================================================================
-- 8. AUDITORÍA: CAMBIOS DE ESTADO EN LAS ÚLTIMAS HORAS
-- ============================================================================
-- Si tienes una tabla de auditoría o log, puedes agregar una query aquí
-- Este es un ejemplo conceptual:
/*
SELECT 
    LogFecha,
    LogUsuario,
    LogAccion,
    NotificacionVencimientoID,
    EstadoAnterior,
    EstadoNuevo
FROM dbo.AuditoriaNotificaciones
WHERE LogFecha >= DATEADD(hour, -24, GETDATE())
ORDER BY LogFecha DESC;
*/


-- ============================================================================
-- 9. TEST: SIMULAR APROBACIÓN Y VERIFICAR IMPACTO
-- ============================================================================
-- Query de solo lectura para simular qué notificaciones se actualizarían
-- si se aprobara un registro específico (cambiar valores)
DECLARE @CapacitadoID INT = 123; -- CAMBIAR
DECLARE @CursoID INT = 1; -- CAMBIAR
DECLARE @FechaJornadaAprobada DATETIME = '2025-10-14'; -- CAMBIAR

SELECT 
    nv.NotificacionVencimientoID,
    'Sería actualizada a: Ya Actualizado' AS [Acción],
    c.Documento,
    (c.Apellido + ', ' + c.Nombre) AS Capacitado,
    cu.Descripcion AS Curso,
    CONVERT(varchar, j.Fecha, 103) AS [Fecha Jornada Antigua],
    CONVERT(varchar, rc.FechaVencimiento, 103) AS [Fecha Vencimiento]
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
INNER JOIN dbo.Capacitados c ON rc.CapacitadoID = c.CapacitadoID
WHERE nv.Estado = 0 -- Pendiente
  AND rc.CapacitadoID = @CapacitadoID
  AND j.CursoId = @CursoID
  AND j.Fecha < @FechaJornadaAprobada
  AND rc.FechaVencimiento IS NOT NULL
  AND @CursoID NOT IN (2, 4, 5);


-- ============================================================================
-- 10. RESUMEN GENERAL DEL SISTEMA
-- ============================================================================
SELECT 
    'Total Notificaciones' AS Métrica,
    COUNT(*) AS Valor
FROM dbo.NotificacionesVencimientos

UNION ALL

SELECT 
    'Pendientes de Notificar',
    COUNT(*)
FROM dbo.NotificacionesVencimientos
WHERE Estado = 0

UNION ALL

SELECT 
    'Ya Notificadas',
    COUNT(*)
FROM dbo.NotificacionesVencimientos
WHERE Estado = 1

UNION ALL

SELECT 
    'Marcadas No Notificar',
    COUNT(*)
FROM dbo.NotificacionesVencimientos
WHERE Estado = 2

UNION ALL

SELECT 
    'Ya Actualizadas (Automático)',
    COUNT(*)
FROM dbo.NotificacionesVencimientos
WHERE Estado = 3

UNION ALL

SELECT 
    'Registros con Vencimiento',
    COUNT(*)
FROM dbo.RegistrosCapacitaciones
WHERE FechaVencimiento IS NOT NULL

UNION ALL

SELECT 
    'Registros sin Notificación Asociada',
    COUNT(*)
FROM dbo.RegistrosCapacitaciones rc
WHERE rc.FechaVencimiento IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 
    FROM dbo.NotificacionesVencimientos nv 
    WHERE nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
  );

/*
==============================================================================
NOTAS DE USO:
==============================================================================

1. Ejecutar estas queries después de aprobar registros para verificar que
   las notificaciones se actualicen correctamente.

2. La query #6 es especialmente útil para verificar que no haya 
   notificaciones pendientes que deberían estar marcadas como "Ya Actualizado".
   Si esta query devuelve resultados, indica que el sistema de limpieza
   preventiva debería procesarlas.

3. Usar la query #9 antes de aprobar un registro para predecir el impacto.

4. La query #10 da una visión general rápida del estado del sistema.

==============================================================================
*/
