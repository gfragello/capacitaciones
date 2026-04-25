-- ============================================================
-- Scripts de consultas para testing de notificaciones de vencimiento
-- ============================================================
-- Uso: reemplazar los parámetros entre << >> con valores reales.
-- Referencia de estados:
--   RegistrosCapacitaciones.Estado:  0=Inscripto, 1=Aprobado, 2=NoAprobado
--   NotificacionesVencimientos.Estado: 0=NotificacionPendiente, 1=Notificado, 2=NoNotificar, 3=NoNotificarYaActualizado
-- ============================================================


-- ===========================================
-- 1. DATOS BASE: Cursos, Jornadas, Capacitados
-- ===========================================

-- Verificar configuración de un curso (NotificarVencimiento, Vigencia)
SELECT CursoID, Descripcion, NotificarVencimiento, Vigencia, SinVigencia, EvaluacionConNota, PuntajeMinimo, PuntajeMaximo
FROM dbo.Cursos
WHERE CursoID = <<CursoID>>;

-- Verificar jornada y su curso asociado
SELECT j.JornadaID, j.Fecha, j.CursoID, c.Descripcion, c.NotificarVencimiento, c.Vigencia
FROM dbo.Jornadas j
INNER JOIN dbo.Cursos c ON c.CursoID = j.CursoID
WHERE j.JornadaID = <<JornadaID>>;

-- Buscar capacitado por documento
SELECT c.CapacitadoID, c.Documento, c.Nombre, c.Apellido, td.Abreviacion AS TipoDocumento, e.RazonSocial AS Empresa
FROM dbo.Capacitados c
LEFT JOIN dbo.TiposDocumento td ON td.TipoDocumentoID = c.TipoDocumentoID
LEFT JOIN dbo.Empresas e ON e.EmpresaID = c.EmpresaID
WHERE c.Documento = '<<Documento>>';


-- ===========================================
-- 2. REGISTROS DE CAPACITACIÓN
-- ===========================================

-- Ver registro específico por ID
SELECT rc.RegistroCapacitacionID, rc.JornadaID, rc.CapacitadoID, rc.Estado, rc.Nota, rc.FechaVencimiento, rc.EnvioOVALEstado
FROM dbo.RegistrosCapacitaciones rc
WHERE rc.RegistroCapacitacionID = <<RegistroCapacitacionID>>;

-- Ver todos los registros de un capacitado en una jornada
SELECT rc.RegistroCapacitacionID, rc.JornadaID, rc.CapacitadoID, rc.Estado, rc.Nota, rc.FechaVencimiento
FROM dbo.RegistrosCapacitaciones rc
WHERE rc.JornadaID = <<JornadaID>> AND rc.CapacitadoID = <<CapacitadoID>>;

-- Ver todos los registros de una jornada (para tests masivos)
SELECT rc.RegistroCapacitacionID, rc.CapacitadoID, cap.Nombre, cap.Apellido, cap.Documento,
       rc.Estado, rc.Nota, rc.FechaVencimiento
FROM dbo.RegistrosCapacitaciones rc
INNER JOIN dbo.Capacitados cap ON cap.CapacitadoID = rc.CapacitadoID
WHERE rc.JornadaID = <<JornadaID>>
ORDER BY cap.Apellido, cap.Nombre;

-- Historial completo de un capacitado en un curso específico
SELECT rc.RegistroCapacitacionID, rc.JornadaID, j.Fecha AS FechaJornada,
       rc.Estado, rc.Nota, rc.FechaVencimiento
FROM dbo.RegistrosCapacitaciones rc
INNER JOIN dbo.Jornadas j ON j.JornadaID = rc.JornadaID
WHERE rc.CapacitadoID = <<CapacitadoID>> AND j.CursoID = <<CursoID>>
ORDER BY j.Fecha DESC;


-- ===========================================
-- 3. NOTIFICACIONES DE VENCIMIENTO
-- ===========================================

-- Ver notificación asociada a un registro específico (TEST-PRE-004, 005)
SELECT nv.NotificacionVencimientoID, nv.RegistroCapacitacionID, nv.Fecha, nv.Estado, nv.MailNotificacionVencimiento
FROM dbo.NotificacionesVencimientos nv
WHERE nv.RegistroCapacitacionID = <<RegistroCapacitacionID>>;

-- Ver todas las notificaciones de un capacitado en un curso (TEST-PRE-009)
SELECT nv.NotificacionVencimientoID, nv.RegistroCapacitacionID, nv.Fecha, nv.Estado, nv.MailNotificacionVencimiento,
       rc.JornadaID, j.Fecha AS FechaJornada, rc.Estado AS EstadoRegistro
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON rc.RegistroCapacitacionID = nv.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON j.JornadaID = rc.JornadaID
WHERE rc.CapacitadoID = <<CapacitadoID>> AND j.CursoID = <<CursoID>>
ORDER BY j.Fecha DESC;

-- Contar notificaciones por estado para una jornada completa (TEST-PRE-007, 008)
SELECT nv.Estado,
       CASE nv.Estado
           WHEN 0 THEN 'NotificacionPendiente'
           WHEN 1 THEN 'Notificado'
           WHEN 2 THEN 'NoNotificar'
           WHEN 3 THEN 'NoNotificarYaActualizado'
       END AS EstadoDescripcion,
       COUNT(*) AS Cantidad
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON rc.RegistroCapacitacionID = nv.RegistroCapacitacionID
WHERE rc.JornadaID = <<JornadaID>>
GROUP BY nv.Estado;

-- Verificar que NO existen notificaciones para registros de un curso sin NotificarVencimiento (TEST-PRE-003)
SELECT nv.*
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON rc.RegistroCapacitacionID = nv.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON j.JornadaID = rc.JornadaID
INNER JOIN dbo.Cursos c ON c.CursoID = j.CursoID
WHERE c.NotificarVencimiento = 0 AND rc.JornadaID = <<JornadaID>>;

-- Verificar que no hay duplicados de notificación para un registro (TEST-PRE-005)
SELECT nv.RegistroCapacitacionID, COUNT(*) AS CantidadNotificaciones
FROM dbo.NotificacionesVencimientos nv
WHERE nv.RegistroCapacitacionID = <<RegistroCapacitacionID>>
GROUP BY nv.RegistroCapacitacionID
HAVING COUNT(*) > 1;


-- ===========================================
-- 4. LIMPIEZA DE NOTIFICACIONES OBSOLETAS (TEST-PRE-009, 010)
-- ===========================================

-- Ver notificaciones pendientes que deberían haber sido marcadas como obsoletas
-- (el capacitado tiene un registro aprobado más reciente del mismo curso)
SELECT nv.NotificacionVencimientoID, nv.RegistroCapacitacionID, nv.Estado,
       rc.CapacitadoID, cap.Documento, cap.Nombre, cap.Apellido,
       j.CursoID, c.Descripcion AS Curso, j.Fecha AS FechaJornada
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON rc.RegistroCapacitacionID = nv.RegistroCapacitacionID
INNER JOIN dbo.Jornadas j ON j.JornadaID = rc.JornadaID
INNER JOIN dbo.Cursos c ON c.CursoID = j.CursoID
INNER JOIN dbo.Capacitados cap ON cap.CapacitadoID = rc.CapacitadoID
WHERE nv.Estado = 0 -- NotificacionPendiente
  AND EXISTS (
      SELECT 1
      FROM dbo.RegistrosCapacitaciones rc2
      INNER JOIN dbo.Jornadas j2 ON j2.JornadaID = rc2.JornadaID
      WHERE rc2.CapacitadoID = rc.CapacitadoID
        AND j2.CursoID = j.CursoID
        AND rc2.Estado = 1 -- Aprobado
        AND j2.Fecha > j.Fecha
  );

-- TEST-PRE-010: control puntual antes y después de abrir la vista de notificaciones
-- Caso actual preparado manualmente: JONNY ABERASATEGUY (Documento 44416158)
-- Antes de abrir la vista: la notificación 64263 debe seguir en Estado = 0
-- Después de abrir la vista: la notificación 64263 debe pasar a Estado = 3
SELECT nv.NotificacionVencimientoID, nv.RegistroCapacitacionID, nv.Estado,
       CASE nv.Estado
           WHEN 0 THEN 'NotificacionPendiente'
           WHEN 1 THEN 'Notificado'
           WHEN 2 THEN 'NoNotificar'
           WHEN 3 THEN 'NoNotificarYaActualizado'
       END AS EstadoDescripcion,
       rc.CapacitadoID, cap.Documento, cap.Nombre, cap.Apellido,
       rc.JornadaID, j.Fecha AS FechaJornada, j.CursoID, c.Descripcion AS Curso,
       rc.Estado AS EstadoRegistro, rc.FechaVencimiento
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON rc.RegistroCapacitacionID = nv.RegistroCapacitacionID
INNER JOIN dbo.Capacitados cap ON cap.CapacitadoID = rc.CapacitadoID
INNER JOIN dbo.Jornadas j ON j.JornadaID = rc.JornadaID
INNER JOIN dbo.Cursos c ON c.CursoID = j.CursoID
WHERE nv.NotificacionVencimientoID IN (64263, 75282)
ORDER BY j.Fecha, nv.NotificacionVencimientoID;

-- TEST-PRE-010: prueba de obsolescencia del registro viejo respecto del más nuevo
-- Si esta consulta devuelve fila antes de abrir la vista, el fixture está bien preparado.
-- Después de abrir la vista debería seguir devolviendo fila, pero con nv.Estado = 3 en la consulta anterior.
SELECT nv.NotificacionVencimientoID, nv.RegistroCapacitacionID, nv.Estado,
       rc.CapacitadoID, cap.Documento,
       rc.JornadaID, j.Fecha AS FechaJornadaVieja, j.CursoID,
       rc2.RegistroCapacitacionID AS RegistroMasReciente,
       j2.Fecha AS FechaJornadaMasReciente,
       rc2.Estado AS EstadoRegistroMasReciente
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON rc.RegistroCapacitacionID = nv.RegistroCapacitacionID
INNER JOIN dbo.Capacitados cap ON cap.CapacitadoID = rc.CapacitadoID
INNER JOIN dbo.Jornadas j ON j.JornadaID = rc.JornadaID
INNER JOIN dbo.RegistrosCapacitaciones rc2 ON rc2.CapacitadoID = rc.CapacitadoID
INNER JOIN dbo.Jornadas j2 ON j2.JornadaID = rc2.JornadaID
WHERE nv.NotificacionVencimientoID = 64263
  AND j2.CursoID = j.CursoID
  AND j2.Fecha > j.Fecha
  AND rc2.Estado = 1
ORDER BY j2.Fecha DESC;


-- ===========================================
-- 5. VERIFICACIÓN DE EMAILS / WEBJOB (TEST-PRE-016, 017)
-- ===========================================

-- Notificaciones pendientes con datos de empresa (para verificar envío de emails)
SELECT nv.NotificacionVencimientoID, nv.Estado, nv.MailNotificacionVencimiento,
       rc.RegistroCapacitacionID, rc.FechaVencimiento,
       cap.Nombre, cap.Apellido, cap.Documento,
       e.RazonSocial AS Empresa, e.Email AS EmailEmpresa,
       c.Descripcion AS Curso
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON rc.RegistroCapacitacionID = nv.RegistroCapacitacionID
INNER JOIN dbo.Capacitados cap ON cap.CapacitadoID = rc.CapacitadoID
LEFT JOIN dbo.Empresas e ON e.EmpresaID = cap.EmpresaID
INNER JOIN dbo.Jornadas j ON j.JornadaID = rc.JornadaID
INNER JOIN dbo.Cursos c ON c.CursoID = j.CursoID
WHERE nv.Estado = 0 -- NotificacionPendiente
ORDER BY rc.FechaVencimiento;


-- ===========================================
-- 6. CONSULTAS RÁPIDAS CON DATOS DE PRUEBA ACTUALES
-- ===========================================

-- Jornada A1 (TV - Tarjeta Verde)
SELECT rc.RegistroCapacitacionID, rc.CapacitadoID, cap.Documento, cap.Nombre, cap.Apellido,
       rc.Estado, rc.Nota, rc.FechaVencimiento
FROM dbo.RegistrosCapacitaciones rc
INNER JOIN dbo.Capacitados cap ON cap.CapacitadoID = rc.CapacitadoID
WHERE rc.JornadaID = 7074
ORDER BY cap.Apellido, cap.Nombre;

-- Jornada B1 (Refresh)
SELECT rc.RegistroCapacitacionID, rc.CapacitadoID, cap.Documento, cap.Nombre, cap.Apellido,
       rc.Estado, rc.Nota, rc.FechaVencimiento
FROM dbo.RegistrosCapacitaciones rc
INNER JOIN dbo.Capacitados cap ON cap.CapacitadoID = rc.CapacitadoID
WHERE rc.JornadaID = 7075
ORDER BY cap.Apellido, cap.Nombre;

-- Notificaciones de la Jornada A1
SELECT nv.NotificacionVencimientoID, nv.RegistroCapacitacionID, nv.Fecha, nv.Estado,
       cap.Documento, cap.Nombre, cap.Apellido
FROM dbo.NotificacionesVencimientos nv
INNER JOIN dbo.RegistrosCapacitaciones rc ON rc.RegistroCapacitacionID = nv.RegistroCapacitacionID
INNER JOIN dbo.Capacitados cap ON cap.CapacitadoID = rc.CapacitadoID
WHERE rc.JornadaID = 7074
ORDER BY cap.Apellido, cap.Nombre;
