# Migración de Base de Datos de Producción a Desarrollo (Azure SQL)

## Objetivo

Restaurar un respaldo `.bacpac` de la base de datos de producción en el ambiente de desarrollo (Azure SQL), ambos publicados en Azure pero en cuentas distintas, para realizar pruebas completas de migración y nuevas features.

---

## Prerrequisitos

- Acceso a ambas cuentas de Azure (producción y desarrollo)
- Archivo `.bacpac` de la base de datos de producción ya generado
- [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) v18+ o [Azure Data Studio](https://learn.microsoft.com/en-us/azure-data-studio/download-azure-data-studio)
- [SqlPackage CLI](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage) (opcional, para línea de comandos)
- Permisos de administrador en el servidor SQL de desarrollo en Azure

---

## Opción A: Usando el Portal de Azure

### Paso 1 – Subir el .bacpac a una cuenta de almacenamiento de Azure

Si el `.bacpac` de producción está en tu máquina local, necesitás subirlo a un Azure Blob Storage accesible desde la cuenta de desarrollo.

1. Ir al [Portal de Azure](https://portal.azure.com) de la cuenta de **desarrollo**
2. Buscar **Cuentas de almacenamiento** (Storage accounts)
3. Si no tenés una, crear una nueva:
   - Hacer clic en **+ Crear**
   - Seleccionar el grupo de recursos de desarrollo
   - Elegir un nombre (ej: `stcapacitacionesdev`)
   - Región: la misma que el servidor SQL de desarrollo
   - Rendimiento: Estándar
   - Redundancia: LRS (suficiente para este propósito)
   - Hacer clic en **Revisar + crear** → **Crear**
4. Dentro de la cuenta de almacenamiento, ir a **Contenedores** (Containers)
5. Crear un nuevo contenedor:
   - Nombre: `bacpac-files`
   - Nivel de acceso: **Privado**
6. Abrir el contenedor y hacer clic en **Cargar** (Upload)
7. Seleccionar el archivo `.bacpac` de producción y subirlo
8. Una vez subido, hacer clic en el archivo y copiar la **URL** del blob

### Paso 2 – Importar el .bacpac en el servidor SQL de desarrollo

1. En el portal de Azure, ir al **Servidor SQL** de desarrollo (no la base de datos, sino el servidor)
2. En el menú lateral, hacer clic en **Importar base de datos** (Import database)
3. Configurar:
   - **Almacenamiento**: Seleccionar la cuenta de almacenamiento, contenedor y archivo `.bacpac` que subiste
   - **Nombre de la base de datos**: Elegir un nombre temporal diferente al actual, por ejemplo: `Capacitaciones_PROD_COPY` (para no pisar la base de desarrollo hasta estar seguro)
   - **Plan de tarifa**: Seleccionar el nivel de servicio (puede ser el más básico para pruebas: Basic o S0)
   - **Autenticación**: Ingresar el usuario y contraseña del administrador del servidor SQL de desarrollo
4. Hacer clic en **Aceptar** (OK)
5. El proceso de importación puede tardar varios minutos dependiendo del tamaño. Se puede monitorear en **Notificaciones** (la campana) o en **Historial de importación/exportación** del servidor SQL

### Paso 3 – Verificar la importación

1. Ir al servidor SQL de desarrollo
2. Verificar que aparece la nueva base de datos `Capacitaciones_PROD_COPY`
3. Conectarse desde SSMS o Azure Data Studio:
   ```
   Servidor: <nombre-servidor-dev>.database.windows.net
   Autenticación: SQL Server
   Usuario: <admin-usuario>
   Contraseña: <admin-contraseña>
   Base de datos: Capacitaciones_PROD_COPY
   ```
4. Ejecutar una consulta rápida de verificación:
   ```sql
   SELECT COUNT(*) AS TotalCapacitados FROM dbo.Capacitados;
   SELECT COUNT(*) AS TotalNotificaciones FROM dbo.NotificacionesVencimientos;
   SELECT COUNT(*) AS TotalRegistros FROM dbo.RegistrosCapacitaciones;
   ```

---

## Opción B: Usando SSMS (SQL Server Management Studio)

### Paso 1 – Conectarse al servidor SQL de desarrollo

1. Abrir SSMS
2. Conectarse al servidor de desarrollo:
   - Servidor: `<nombre-servidor-dev>.database.windows.net`
   - Autenticación: **SQL Server Authentication**
   - Usuario y contraseña del administrador
3. Si da error de firewall, ir al portal de Azure → Servidor SQL → **Redes** (Networking) → Agregar tu IP actual

### Paso 2 – Importar el .bacpac

1. En el Object Explorer, hacer clic derecho en **Bases de datos** (Databases)
2. Seleccionar **Importar aplicación de capa de datos...** (Import Data-tier Application...)
3. En el asistente:
   - **Configuración de importación**: Seleccionar **Archivo local** y buscar el `.bacpac` en tu disco
   - **Configuración de base de datos**: 
     - Nombre: `Capacitaciones_PROD_COPY`
     - Edición: Standard o Basic (para pruebas)
     - Tamaño máximo: según necesidad
4. Hacer clic en **Siguiente** → **Finalizar**
5. Esperar a que complete la importación (puede tardar varios minutos)

### Paso 3 – Verificar

Misma verificación que en la Opción A, Paso 3.

---

## Opción C: Usando SqlPackage (línea de comandos)

### Paso 1 – Instalar SqlPackage

```bash
# Windows (winget)
winget install Microsoft.SqlPackage

# macOS
brew install sqlpackage

# Linux (.NET tool)
dotnet tool install -g microsoft.sqlpackage
```

### Paso 2 – Importar el .bacpac

```bash
SqlPackage /Action:Import \
  /SourceFile:"C:\Ruta\Al\Archivo\produccion.bacpac" \
  /TargetServerName:"<nombre-servidor-dev>.database.windows.net" \
  /TargetDatabaseName:"Capacitaciones_PROD_COPY" \
  /TargetUser:"<admin-usuario>" \
  /TargetPassword:"<admin-contraseña>" \
  /TargetEncryptConnection:True
```

> **Nota:** Si el `.bacpac` está en Azure Blob Storage en lugar de local, podés usar `/SourceUri:` con la URL del blob y `/SourceAccessToken:` con un SAS token.

### Paso 3 – Verificar

Conectarse desde SSMS, Azure Data Studio o `sqlcmd` y ejecutar las consultas de verificación.

---

## Paso 4 – Reemplazar la base de datos de desarrollo (si corresponde)

Una vez que verificaste que la importación fue exitosa, podés reemplazar la base de datos actual de desarrollo.

### Opción 1: Renombrar (recomendado)

```sql
-- Conectarse a la base de datos master del servidor de desarrollo
-- 1. Renombrar la base de desarrollo actual como backup
ALTER DATABASE [Capacitaciones_DEV] MODIFY NAME = [Capacitaciones_DEV_OLD];

-- 2. Renombrar la copia de producción como la base de desarrollo
ALTER DATABASE [Capacitaciones_PROD_COPY] MODIFY NAME = [Capacitaciones_DEV];
```

> **Importante:** Estas operaciones requieren que no haya conexiones activas a las bases de datos. Puede ser necesario detener la aplicación web temporalmente.

### Opción 2: Cambiar el connection string

En lugar de renombrar, podés apuntar la aplicación web de desarrollo al nuevo nombre de base de datos:

1. En el portal de Azure, ir a la **App Service** de desarrollo
2. Ir a **Configuración** → **Cadenas de conexión** (Connection strings) o **Variables de entorno**
3. Modificar la cadena `DefaultConnection` para que apunte a `Capacitaciones_PROD_COPY`:
   ```
   Server=tcp:<nombre-servidor-dev>.database.windows.net,1433;Initial Catalog=Capacitaciones_PROD_COPY;Persist Security Info=False;User ID=<usuario>;Password=<contraseña>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```
4. Guardar y reiniciar la App Service

---

## Paso 5 – Ejecutar migraciones de Entity Framework

Después de restaurar la base de producción en desarrollo, es probable que necesites aplicar las migraciones pendientes de esta rama de refactor.

### Desde Visual Studio

1. Abrir la solución `Cursos.sln`
2. Verificar que el `Web.config` apunta a la base de datos de desarrollo con los datos de producción
3. Abrir la **Consola del Administrador de Paquetes** (Package Manager Console)
4. Ejecutar:
   ```powershell
   Update-Database -Verbose
   ```
5. Esto aplicará las migraciones pendientes:
   - `202510170342127_Curso_NotificarVencimiento` (agrega columna `NotificarVencimiento` a `Cursos`)
   - `202601201708142_RegistroCapacitacion_EliminarAprobado` (elimina columna `Aprobado` de `RegistrosCapacitaciones`)

### Verificar las migraciones

```sql
-- Verificar qué migraciones están aplicadas
SELECT MigrationId, ProductVersion 
FROM dbo.__MigrationHistory 
ORDER BY MigrationId DESC;

-- Verificar que la columna Aprobado ya no existe
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'RegistrosCapacitaciones' 
  AND COLUMN_NAME = 'Aprobado';
-- Resultado esperado: 0 filas

-- Verificar que NotificarVencimiento existe
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Cursos' 
  AND COLUMN_NAME = 'NotificarVencimiento';
-- Resultado esperado: 1 fila (bit)
```

---

## Paso 6 – Configurar datos para pruebas

Después de restaurar y migrar, configurar el flag `NotificarVencimiento` en los cursos:

```sql
-- Ver cursos actuales
SELECT CursoID, Descripcion, NotificarVencimiento, Vigencia, Activo
FROM dbo.Cursos
ORDER BY CursoID;

-- Activar notificaciones para los cursos que corresponda
-- (ajustar IDs según tu criterio)
UPDATE dbo.Cursos
SET NotificarVencimiento = 1
WHERE CursoID NOT IN (2, 4, 5)  -- Excluir Refresh, Induction, Nursery
  AND Vigencia > 0
  AND Activo = 1;
```

Luego, ejecutar el plan de depuración de datos descrito en `PROPUESTA_DEPURACION_NOTIFICACIONES.md`.

---

## Paso 7 – Ejecutar el plan de testing

Con la base de datos de producción restaurada, migrada y configurada en el ambiente de desarrollo, proceder con el plan de testing descrito en `PLAN_TESTING_PREPRODUCCION.md`.

---

## Solución de problemas comunes

### Error de firewall al conectarse

```
Cannot open server '<servidor>' requested by the login. 
Client with IP address 'x.x.x.x' is not allowed to access the server.
```

**Solución:** Portal de Azure → Servidor SQL → Redes → Agregar la IP del cliente actual.

### Error de tamaño de base de datos

```
The database cannot be created because it would cause the server to exceed its allowed quota of databases.
```

**Solución:** Eliminar bases de datos que ya no se usen en el servidor de desarrollo, o escalar el plan del servidor.

### Error de versión incompatible al importar

```
Could not import package. Warning SQL72012: The object already exists.
```

**Solución:** Asegurarse de que el nombre de la base de datos destino no existe. Si existe, eliminarla primero o usar otro nombre.

### Error al aplicar migraciones

```
There is already an object named 'X' in the database.
```

**Solución:** La base de producción puede no tener todas las migraciones en `__MigrationHistory`. Insertar manualmente los registros de migraciones ya aplicadas:

```sql
-- Solo si la estructura ya existe pero falta el registro en __MigrationHistory
INSERT INTO dbo.__MigrationHistory (MigrationId, ContextKey, Model, ProductVersion)
VALUES ('202510170342127_Curso_NotificarVencimiento', 
        '<ContextKey de tu proyecto>', 
        <Model binario>, 
        '6.4.0');
```

> **Nota:** Es más seguro usar `Update-Database -Script` en Visual Studio para generar el SQL y revisarlo antes de ejecutar.

### La aplicación no conecta después de cambiar la base

**Solución:** Reiniciar la App Service desde el portal de Azure → App Service → **Reiniciar**. Verificar que la cadena de conexión está actualizada.

---

## Checklist resumido

- [ ] Subir `.bacpac` de producción a Blob Storage o tenerlo localmente
- [ ] Importar `.bacpac` en el servidor SQL de desarrollo (Portal / SSMS / SqlPackage)
- [ ] Verificar que la importación fue exitosa (consultas de conteo)
- [ ] Apuntar la aplicación de desarrollo a la nueva base de datos
- [ ] Ejecutar migraciones pendientes de Entity Framework (`Update-Database`)
- [ ] Verificar que las migraciones se aplicaron correctamente
- [ ] Configurar flag `NotificarVencimiento` en los cursos
- [ ] Ejecutar depuración de datos (`PROPUESTA_DEPURACION_NOTIFICACIONES.md`)
- [ ] Ejecutar plan de testing (`PLAN_TESTING_PREPRODUCCION.md`)
