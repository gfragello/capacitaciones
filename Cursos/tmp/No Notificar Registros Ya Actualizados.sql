/*
-----------------------------------------------------------------------------------
-- Descripción del script:
-- 
-- Este script selecciona las notificaciones de vencimiento pendientes para registros 
-- de capacitación cuyo vencimiento es posterior al 01/01/2025 y en los que el capacitado 
-- ya ha cursado una jornada más reciente del mismo curso, haciendo innecesaria la notificación.
--
-- El resultado incluye:
--   - NotificacionVencimientoID: Identificador de la notificación
--   - RegistroCapacitacionID: Identificador del registro de capacitación
--   - Documento: Cédula del capacitado
--   - Nombre: Apellido y nombre del capacitado en una sola columna
--   - Curso: Descripción del curso
--   - Fecha Vencimiento: Fecha de vencimiento de la certificación (formato dd/mm/yyyy)
--   - Fecha Jornada Actualización: Fecha en que se realizó la jornada más reciente que hace innecesaria la notificación (formato dd/mm/yyyy)
--   - Link Capacitado: Enlace al detalle del capacitado en el sistema
--
-- Se excluyen los cursos con ID 2, 4 y 5.
-----------------------------------------------------------------------------------
*/

SELECT 
    nv.NotificacionVencimientoID AS NotificacionVencimientoID,
    rc.RegistroCapacitacionID AS RegistroCapacitacionID,
    c.Documento AS Documento,
    (c.Apellido + ', ' + c.Nombre) AS Nombre,
    cu.Descripcion AS Curso,
    CONVERT(varchar, rc.FechaVencimiento, 103) AS [Fecha Vencimiento],
    CONVERT(varchar, (
        SELECT TOP 1 j2.Fecha
        FROM dbo.RegistrosCapacitaciones rc2
        INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
        WHERE rc2.CapacitadoID = rc.CapacitadoID
          AND j2.CursoId = j.CursoId
          AND rc2.RegistroCapacitacionID <> rc.RegistroCapacitacionID
          AND j2.Fecha > j.Fecha
        ORDER BY j2.Fecha DESC
    ), 103) AS [Fecha Jornada Actualización],
    'https://certificaciones.csl.uy/es-UY/Capacitados/Details/' + CAST(rc.CapacitadoID AS varchar) AS [Link Capacitado]
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
INNER JOIN dbo.Cursos cu ON j.CursoId = cu.CursoID
INNER JOIN dbo.Capacitados c ON rc.CapacitadoID = c.CapacitadoID
WHERE nv.Estado = 0 -- Pendiente de notificación
  AND rc.FechaVencimiento > '2025-01-01'
  AND j.CursoId NOT IN (2, 4, 5)
  AND EXISTS (
    SELECT 1
    FROM dbo.RegistrosCapacitaciones rc2
    INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
    WHERE rc2.CapacitadoID = rc.CapacitadoID
      AND j2.CursoId = j.CursoId
      AND rc2.RegistroCapacitacionID <> rc.RegistroCapacitacionID
      AND j2.Fecha > j.Fecha
)

/*
-----------------------------------------------------------------------------------
-- Descripción del script:
--
-- Este script actualiza el estado de las notificaciones de vencimiento (tabla NotificacionesVencimientos)
-- cuyo registro cumple con las siguientes condiciones:
--   - La notificación está pendiente (Estado = 0)
--   - La fecha de vencimiento de la capacitación es posterior al 01/01/2025
--   - El capacitado ya cursó una jornada más reciente del mismo curso, haciendo innecesaria la notificación
--   - El curso no es ninguno de los siguientes: CursoID 2, 4, 5
--
-- La columna Estado se actualiza al valor 3 (NoNotificarYaActualizado).
-- El OUTPUT muestra los datos relevantes para seguimiento y registro.
-----------------------------------------------------------------------------------
*/

UPDATE nv
SET nv.Estado = 3 -- NoNotificarYaActualizado
OUTPUT
    deleted.Estado AS [Estado Anterior],
    inserted.Estado AS [Estado Posterior],
    deleted.NotificacionVencimientoID AS NotificacionVencimientoID,
    deleted.RegistroCapacitacionID AS RegistroCapacitacionID
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
WHERE nv.Estado = 0
  AND rc.FechaVencimiento > '2025-01-01'
  AND j.CursoId NOT IN (2, 4, 5)
  AND EXISTS (
    SELECT 1
    FROM dbo.RegistrosCapacitaciones rc2
    INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
    WHERE rc2.CapacitadoID = rc.CapacitadoID
      AND j2.CursoId = j.CursoId
      AND rc2.RegistroCapacitacionID <> rc.RegistroCapacitacionID
      AND j2.Fecha > j.Fecha
)