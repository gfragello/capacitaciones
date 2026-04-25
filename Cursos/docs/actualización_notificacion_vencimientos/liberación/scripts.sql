-- =====================================================================
-- Scripts de actualización de datos - Notificación de Vencimientos
-- Ejecutar luego de impactar las modificaciones en la BD (migraciones)
--
-- Estructura:
--   PASO 1 - Configurar Cursos.NotificarVencimiento
--   PASO 2 - Backup de la tabla NotificacionesVencimientos
--   PASO 3 - Diagnóstico previo
--   PASO 4 - DELETE duplicados (conservar el de mayor ID)
--   PASO 5 - DELETE notificaciones de cursos sin NotificarVencimiento
--   PASO 6 - DELETE notificaciones de registros no aprobados
--   PASO 7 - UPDATE pendientes obsoletas -> NoNotificarYaActualizado
--   PASO 8 - Verificación final (chequeo de invariantes + COMMIT/ROLLBACK automático)
--
-- Puede ejecutarse completo desde SSMS o sqlcmd.
-- Si las 4 invariantes del PASO 8 dan 0, el script hace COMMIT automáticamente.
-- Si alguna falla, hace ROLLBACK y termina con error.
-- Ver PROPUESTA_DEPURACION_NOTIFICACIONES.md.
-- =====================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- =====================================================================
-- PASO 1 - Configurar Cursos.NotificarVencimiento
-- =====================================================================
-- La migración Curso_NotificarVencimiento agrega la columna NotificarVencimiento
-- con valor por defecto 0 (false) para todos los cursos.
--
-- En la versión anterior, la lógica de notificaciones excluía los cursos:
--   CursoID 2 (Refresh), CursoID 4 (Inducción), CursoID 5 (Viveros)
-- y notificaba todos los demás que tuvieran vencimiento.
--
-- Las siguientes sentencias replican ese comportamiento con el nuevo flag:
--   - NotificarVencimiento = 0 para cursos excluidos o sin vigencia
--   - NotificarVencimiento = 1 para cursos con vigencia y no excluidos

UPDATE Cursos
SET NotificarVencimiento = 0
WHERE CursoID IN (2, 4, 5)
   OR SinVigencia = 1;

PRINT 'PASO 1a - Cursos configurados para NO notificar (excluidos o sin vigencia): ' + CAST(@@ROWCOUNT AS VARCHAR(10));

UPDATE Cursos
SET NotificarVencimiento = 1
WHERE CursoID NOT IN (2, 4, 5)
  AND SinVigencia = 0;

PRINT 'PASO 1b - Cursos configurados para notificar (con vigencia y no excluidos): ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO

-- =====================================================================
-- PASO 2 - Backup de la tabla NotificacionesVencimientos
-- =====================================================================
-- Se crea una tabla de respaldo con sufijo de fecha. Si el script se vuelve
-- a ejecutar el mismo día, la creación fallará y abortará (deseable).

DECLARE @backupName sysname = N'NotificacionesVencimientos_BACKUP_' + CONVERT(varchar(8), GETDATE(), 112);

IF OBJECT_ID(N'dbo.' + @backupName, 'U') IS NOT NULL
BEGIN
    RAISERROR('PASO 2 - El backup %s ya existe. Abortando para no sobreescribir.', 16, 1, @backupName);
    RETURN;
END

DECLARE @sql nvarchar(max) = N'SELECT * INTO dbo.' + QUOTENAME(@backupName) + N' FROM dbo.NotificacionesVencimientos;';
EXEC sp_executesql @sql;

PRINT 'PASO 2 - Backup creado: dbo.' + @backupName;

DECLARE @backupCount int;
SET @sql = N'SELECT @c = COUNT(*) FROM dbo.' + QUOTENAME(@backupName);
EXEC sp_executesql @sql, N'@c int OUTPUT', @c = @backupCount OUTPUT;

DECLARE @originalCount int = (SELECT COUNT(*) FROM dbo.NotificacionesVencimientos);

IF @backupCount <> @originalCount
BEGIN
    RAISERROR('PASO 2 - El backup tiene %d filas pero el original %d. Abortando.', 16, 1, @backupCount, @originalCount);
    RETURN;
END

PRINT 'PASO 2 - Backup verificado: ' + CAST(@backupCount AS varchar(10)) + ' filas.';
GO

-- =====================================================================
-- PASO 3 - Diagnóstico previo (informativo)
-- =====================================================================
PRINT '--- PASO 3 - Diagnóstico previo ---';

SELECT 'Total'                              AS Categoria, COUNT(*) AS Cantidad FROM dbo.NotificacionesVencimientos
UNION ALL SELECT 'Estado 0 - Pendiente',             COUNT(*) FROM dbo.NotificacionesVencimientos WHERE Estado = 0
UNION ALL SELECT 'Estado 1 - Notificado',            COUNT(*) FROM dbo.NotificacionesVencimientos WHERE Estado = 1
UNION ALL SELECT 'Estado 2 - NoNotificar',           COUNT(*) FROM dbo.NotificacionesVencimientos WHERE Estado = 2
UNION ALL SELECT 'Estado 3 - YaActualizado',         COUNT(*) FROM dbo.NotificacionesVencimientos WHERE Estado = 3
UNION ALL SELECT 'Duplicadas (filas sobrantes)',     COUNT(*) - COUNT(DISTINCT RegistroCapacitacionID) FROM dbo.NotificacionesVencimientos
UNION ALL SELECT 'De cursos con NotificarVencimiento = 0',
    COUNT(*)
    FROM dbo.NotificacionesVencimientos nv
    INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
    INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
    INNER JOIN dbo.Cursos c ON j.CursoId = c.CursoID
    WHERE c.NotificarVencimiento = 0
UNION ALL SELECT 'De registros NO aprobados',
    COUNT(*)
    FROM dbo.NotificacionesVencimientos nv
    INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
    WHERE rc.Estado <> 1
UNION ALL SELECT 'Pendientes con capacitado ya renovado',
    COUNT(*)
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
GO

-- =====================================================================
-- Depuración transaccional (PASOS 4-7)
-- =====================================================================
BEGIN TRAN DepuracionNotificaciones;

    -- --------------------------------------------------------------
    -- PASO 4 - Eliminar notificaciones duplicadas (conservar mayor ID)
    -- --------------------------------------------------------------
    ;WITH dup AS (
        SELECT NotificacionVencimientoID,
               ROW_NUMBER() OVER (
                   PARTITION BY RegistroCapacitacionID
                   ORDER BY NotificacionVencimientoID DESC
               ) AS rn
        FROM dbo.NotificacionesVencimientos
    )
    DELETE FROM dbo.NotificacionesVencimientos
    WHERE NotificacionVencimientoID IN (SELECT NotificacionVencimientoID FROM dup WHERE rn > 1);

    PRINT 'PASO 4 - Duplicados eliminados: ' + CAST(@@ROWCOUNT AS varchar(10));

    -- --------------------------------------------------------------
    -- PASO 5 - Eliminar notificaciones de cursos sin NotificarVencimiento
    -- --------------------------------------------------------------
    DELETE nv
    FROM dbo.NotificacionesVencimientos nv
    INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
    INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
    INNER JOIN dbo.Cursos c ON j.CursoId = c.CursoID
    WHERE c.NotificarVencimiento = 0;

    PRINT 'PASO 5 - Notificaciones de cursos NO notificables eliminadas: ' + CAST(@@ROWCOUNT AS varchar(10));

    -- --------------------------------------------------------------
    -- PASO 6 - Eliminar notificaciones de registros no aprobados
    -- (RegistrosCapacitaciones.Estado: 0 = Inscripto, 1 = Aprobado, 2 = NoAprobado)
    -- --------------------------------------------------------------
    DELETE nv
    FROM dbo.NotificacionesVencimientos nv
    INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
    WHERE rc.Estado <> 1;

    PRINT 'PASO 6 - Notificaciones de registros NO aprobados eliminadas: ' + CAST(@@ROWCOUNT AS varchar(10));

    -- --------------------------------------------------------------
    -- PASO 7 - Marcar pendientes obsoletas como NoNotificarYaActualizado (Estado = 3)
    --         Equivalente al método LimpiarNotificacionesObsoletas() del controller.
    -- --------------------------------------------------------------
    UPDATE nv
    SET nv.Estado = 3
    FROM dbo.NotificacionesVencimientos nv
    INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
    INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
    INNER JOIN dbo.Cursos c ON j.CursoId = c.CursoID
    WHERE nv.Estado = 0
      AND c.NotificarVencimiento = 1
      AND rc.FechaVencimiento IS NOT NULL
      AND EXISTS (
        SELECT 1
        FROM dbo.RegistrosCapacitaciones rc2
        INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
        WHERE rc2.CapacitadoID = rc.CapacitadoID
          AND j2.CursoId = j.CursoId
          AND rc2.RegistroCapacitacionID <> rc.RegistroCapacitacionID
          AND j2.Fecha > j.Fecha
          AND rc2.Estado = 1
      );

    PRINT 'PASO 7 - Pendientes obsoletas marcadas como YaActualizado: ' + CAST(@@ROWCOUNT AS varchar(10));

-- =====================================================================
-- PASO 8 - Verificación final
-- =====================================================================
PRINT '--- PASO 8 - Verificación final ---';

-- 8a. Comparativa antes/después
DECLARE @backupTable sysname = N'NotificacionesVencimientos_BACKUP_' + CONVERT(varchar(8), GETDATE(), 112);
DECLARE @q nvarchar(max) =
    N'SELECT ''Antes (backup)'' AS Momento, COUNT(*) AS Total,
             SUM(CASE WHEN Estado = 0 THEN 1 ELSE 0 END) AS Pendientes,
             SUM(CASE WHEN Estado = 1 THEN 1 ELSE 0 END) AS Notificadas,
             SUM(CASE WHEN Estado = 2 THEN 1 ELSE 0 END) AS NoNotificar,
             SUM(CASE WHEN Estado = 3 THEN 1 ELSE 0 END) AS YaActualizado
      FROM dbo.' + QUOTENAME(@backupTable) + N'
      UNION ALL
      SELECT ''Después'', COUNT(*),
             SUM(CASE WHEN Estado = 0 THEN 1 ELSE 0 END),
             SUM(CASE WHEN Estado = 1 THEN 1 ELSE 0 END),
             SUM(CASE WHEN Estado = 2 THEN 1 ELSE 0 END),
             SUM(CASE WHEN Estado = 3 THEN 1 ELSE 0 END)
      FROM dbo.NotificacionesVencimientos;';
EXEC sp_executesql @q;

-- 8b. Chequeo de invariantes (los cuatro valores deben ser 0)
DECLARE @I2_CursosNoNotificables int = (
  SELECT COUNT(*)
  FROM dbo.NotificacionesVencimientos nv
  INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
  INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
  INNER JOIN dbo.Cursos c ON j.CursoId = c.CursoID
  WHERE c.NotificarVencimiento = 0
);

DECLARE @I1_RegistrosNoAprobados int = (
  SELECT COUNT(*)
  FROM dbo.NotificacionesVencimientos nv
  INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
  WHERE rc.Estado <> 1
);

DECLARE @I3_DuplicadosSobrantes int = (
  SELECT COUNT(*) - COUNT(DISTINCT RegistroCapacitacionID)
  FROM dbo.NotificacionesVencimientos
);

DECLARE @I4_PendientesObsoletas int = (
  SELECT COUNT(*)
  FROM dbo.NotificacionesVencimientos nv
  INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
  INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
  INNER JOIN dbo.Cursos c ON j.CursoId = c.CursoID
  WHERE nv.Estado = 0
    AND c.NotificarVencimiento = 1
    AND EXISTS (
    SELECT 1
    FROM dbo.RegistrosCapacitaciones rc2
    INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
    WHERE rc2.CapacitadoID = rc.CapacitadoID
      AND j2.CursoId = j.CursoId
      AND rc2.RegistroCapacitacionID <> rc.RegistroCapacitacionID
      AND j2.Fecha > j.Fecha
      AND rc2.Estado = 1
    )
);

SELECT
  @I2_CursosNoNotificables AS I2_CursosNoNotificables,
  @I1_RegistrosNoAprobados AS I1_RegistrosNoAprobados,
  @I3_DuplicadosSobrantes AS I3_DuplicadosSobrantes,
  @I4_PendientesObsoletas AS I4_PendientesObsoletas;

IF @I2_CursosNoNotificables = 0
   AND @I1_RegistrosNoAprobados = 0
   AND @I3_DuplicadosSobrantes = 0
   AND @I4_PendientesObsoletas = 0
BEGIN
  COMMIT TRAN DepuracionNotificaciones;
  PRINT 'PASO 8c - COMMIT ejecutado automaticamente. Los cambios quedaron persistidos.';
END
ELSE
BEGIN
  ROLLBACK TRAN DepuracionNotificaciones;
  RAISERROR(
    'PASO 8c - Validacion final fallida. Se ejecuto ROLLBACK automatico. Invariantes: I2=%d, I1=%d, I3=%d, I4=%d.',
    16,
    1,
    @I2_CursosNoNotificables,
    @I1_RegistrosNoAprobados,
    @I3_DuplicadosSobrantes,
    @I4_PendientesObsoletas
  );
END

-- =====================================================================
-- Limpieza del backup
-- =====================================================================
-- Recomendación operativa:
--   - Conservar la tabla backup al menos 30 días corridos después de la liberación.
--   - Si el costo de almacenamiento no es un problema, puede conservarse hasta 60 días.
--   - No eliminarla antes de completar el monitoreo post-release y el cierre del período de soporte inicial.
-- Ejecutar el DROP sólo cuando el cambio se considere estable.
-- DROP TABLE dbo.NotificacionesVencimientos_BACKUP_AAAAMMDD;

