# Migración incremental de archivos a Azure Blob Storage

## Objetivo

Migrar gradualmente el almacenamiento de imágenes relacionadas a los capacitados y otros archivos desde el file system local de la aplicación hacia una cuenta de Azure Blob Storage.

La migración debe hacerse de forma incremental, segura y reversible, minimizando el tiempo de dedicación manual y permitiendo que agentes de IA como GitHub Copilot y Codex asistan en la implementación por etapas.

## Contexto actual

Actualmente la aplicación almacena las fotos de capacitados y otros archivos en el file system, con rutas asociadas a estructuras como:

- `~/Images/FotosCapacitados/`
- `PathArchivo`
- `PathArchivoID`
- `NombreArchivo`
- `PathCompleto`

También existen señales de una migración ya iniciada o parcialmente preparada:

- El modelo `Capacitado` contiene propiedades como `TipoAlmacenamientoFoto` y `BlobStorageUri`.
- Existen migraciones relacionadas con estos campos.
- En `CapacitadosController` existe una rama para `TipoAlmacenamiento.BlobStorage`, aunque actualmente no está implementada completamente.

Esto permite plantear una migración incremental sin necesidad de cambiar todo el sistema en una sola intervención.

## Principio general de la estrategia

No conviene reemplazar directamente cada acceso al file system por llamadas a Azure Blob Storage.

La estrategia recomendada es:

1. Identificar todos los usos actuales del file system.
2. Introducir una abstracción de almacenamiento.
3. Mantener inicialmente el comportamiento actual detrás de esa abstracción.
4. Implementar Azure Blob Storage como una segunda implementación.
5. Activar Blob Storage solo para nuevos archivos o nuevas fotos.
6. Mantener coexistencia entre archivos viejos en file system y archivos nuevos en Blob Storage.
7. Migrar los archivos existentes en una etapa posterior.
8. Eliminar código legado solo después de validar la migración.

## Riesgos principales

| Riesgo | Mitigación recomendada |
|---|---|
| Romper la visualización de fotos existentes | Permitir coexistencia entre file system y Blob Storage |
| Exponer fotos públicamente sin control | Preferir contenedor privado, SAS temporal o endpoint autenticado |
| Guardar secretos en el repositorio | Usar variables de entorno, App Settings o Key Vault |
| Cambiar demasiadas cosas en un único PR | Dividir la implementación en PRs pequeños |
| Dificultar rollback | Mantener `PathArchivo` y `BlobStorageUri` durante la transición |
| Procesamiento de imágenes acoplado al disco | Procesar imágenes en memoria usando streams |
| Vistas con rutas hardcodeadas | Resolver URLs desde un servicio o ViewModel |

## Etapa 0 - Inventario y documentación

### Objetivo

Entender todos los puntos donde la aplicación lee, escribe, rota, elimina o muestra archivos.

### Tareas sugeridas

Buscar referencias a:

- `Server.MapPath`
- `System.IO.File`
- `File.Move`
- `File.Delete`
- `Path.Combine`
- `SaveAs`
- `FotosCapacitados`
- `PathArchivo`
- `BlobStorageUri`
- `TipoAlmacenamientoFoto`

Clasificar cada hallazgo por tipo de operación:

- Carga de foto
- Visualización de foto
- Eliminación de foto
- Rotación de foto
- Cambio de extensión `.jpeg` a `.jpg`
- Envío de foto a sistemas externos
- Otros archivos no relacionados con fotos

### Resultado esperado

Una lista clara de los puntos de acoplamiento con el file system y una estimación de qué tan riesgoso es modificar cada uno.

### Prompt sugerido para Copilot o Codex

```text
Analizá el repositorio y encontrá todos los lugares donde se lean, escriban, roten, eliminen o referencien archivos de fotos de capacitados o archivos relacionados. Clasificá cada hallazgo por operación, archivo, método y riesgo para migrar de file system a Azure Blob Storage. No modifiques código todavía.
```

## Etapa 1 - Crear una abstracción de almacenamiento

### Objetivo

Evitar que controladores, modelos y vistas dependan directamente del file system.

### Propuesta

Crear una interfaz de almacenamiento, por ejemplo:

```csharp
public interface IFileStorageService
{
    string Save(Stream fileStream, string fileName, string contentType, string folder);
    Stream Get(string pathOrUri);
    string GetUrl(string pathOrUri);
    void Delete(string pathOrUri);
    bool Exists(string pathOrUri);
}
```

Crear una primera implementación basada en file system:

```csharp
public class FileSystemStorageService : IFileStorageService
{
    // Implementación usando la lógica actual de ~/Images/FotosCapacitados
}
```

### Resultado esperado

El sistema debe seguir funcionando igual que antes, pero la lógica de almacenamiento debe empezar a concentrarse detrás de una interfaz.

### Criterio de aceptación

- La aplicación sigue guardando fotos en el file system.
- Las fotos existentes siguen mostrándose.
- No se introduce todavía dependencia funcional con Azure Blob Storage.
- El comportamiento visible para el usuario no cambia.

## Etapa 2 - Extraer la lógica de fotos de `Capacitado`

### Objetivo

Reducir el acoplamiento entre la entidad `Capacitado` y la infraestructura de archivos.

Actualmente el modelo contiene lógica relacionada con:

- Carga de foto
- Rotación de foto
- Cambio de extensión de foto
- Acceso a rutas físicas
- Manipulación de imágenes con `System.Drawing`

### Propuesta

Crear un servicio dedicado, por ejemplo:

```csharp
public class CapacitadoFotoService
{
    public bool CargarFoto(int capacitadoId, HttpPostedFileBase foto);
    public bool RotarFoto(int capacitadoId, string direccion);
    public bool EliminarFoto(int capacitadoId);
    public string ObtenerUrlFoto(Capacitado capacitado);
}
```

Este servicio debería usar `IFileStorageService` internamente.

### Resultado esperado

La entidad `Capacitado` debería quedar más enfocada en datos y reglas de negocio, mientras que la manipulación de archivos se mueve a servicios específicos.

## Etapa 3 - Separar procesamiento de imágenes

### Objetivo

Evitar que el procesamiento de imágenes dependa de rutas físicas en disco.

### Propuesta

Crear un servicio específico para procesamiento de imágenes:

```csharp
public class ImageProcessingService
{
    public Stream QuitarExif(Stream imagenOriginal);
    public Stream Rotar(Stream imagenOriginal, string direccion);
    public Stream NormalizarAJpeg(Stream imagenOriginal);
}
```

### Flujo recomendado

```text
HttpPostedFileBase.InputStream
        ↓
ImageProcessingService
        ↓
MemoryStream procesado
        ↓
IFileStorageService.Save
```

### Resultado esperado

La aplicación debería poder procesar una imagen sin tener que guardarla previamente como archivo temporal en el file system.

## Etapa 4 - Implementar Azure Blob Storage detrás de la interfaz

### Objetivo

Agregar una implementación de almacenamiento basada en Azure Blob Storage, sin activarla todavía como comportamiento por defecto.

### Propuesta

Crear una implementación:

```csharp
public class AzureBlobStorageService : IFileStorageService
{
    public string Save(Stream fileStream, string fileName, string contentType, string folder)
    {
        // Subir archivo a Azure Blob Storage
        // Retornar URI o identificador del blob
    }

    public Stream Get(string pathOrUri)
    {
        // Descargar blob como stream
    }

    public string GetUrl(string pathOrUri)
    {
        // Retornar URL pública, SAS temporal o URL de endpoint interno
    }

    public void Delete(string pathOrUri)
    {
        // Eliminar blob
    }

    public bool Exists(string pathOrUri)
    {
        // Verificar existencia del blob
    }
}
```

### Configuración sugerida

Agregar configuración sin incluir secretos reales en el repositorio:

```xml
<add key="StorageProvider" value="FileSystem" />
<add key="AzureBlobStorageContainerFotosCapacitados" value="fotoscapacitados" />
```

La cadena de conexión o credenciales deberían gestionarse fuera del código fuente mediante:

- Variables de entorno
- App Settings del entorno de despliegue
- Azure Key Vault, si corresponde

### Decisión de seguridad

Para fotos de personas, se recomienda evitar contenedores públicos salvo que exista una decisión explícita aceptando ese riesgo.

Opciones preferibles:

- Contenedor privado con SAS temporal.
- Endpoint autenticado en la aplicación que entregue la imagen tras validar permisos.
- URL firmada con expiración corta.

## Etapa 5 - Activar Blob Storage solo para fotos nuevas

### Objetivo

Permitir que las nuevas fotos se guarden en Blob Storage, manteniendo las fotos existentes en el file system.

### Estrategia

Durante un período de transición, permitir coexistencia:

```text
Fotos antiguas  → FileSystem
Fotos nuevas    → BlobStorage
```

Usar `TipoAlmacenamientoFoto` para determinar dónde está almacenada cada foto.

### Reglas sugeridas

- Si `TipoAlmacenamientoFoto` es `FileSystem` o `null`, leer desde `PathArchivo`.
- Si `TipoAlmacenamientoFoto` es `BlobStorage`, leer desde `BlobStorageUri` o identificador equivalente.
- Al cargar una nueva foto, guardar en Blob Storage y actualizar `TipoAlmacenamientoFoto`.

### Resultado esperado

La aplicación debe soportar simultáneamente fotos antiguas y nuevas.

## Etapa 6 - Adaptar vistas para resolver URLs mediante servicio o ViewModel

### Objetivo

Eliminar rutas hardcodeadas como:

```html
~/images/FotosCapacitados/@Model.PathArchivo.PathCompleto
```

### Propuesta

Agregar una propiedad en el ViewModel o resolver la URL desde un servicio:

```csharp
public string FotoUrl { get; set; }
```

La vista debería usar:

```html
<img src="@Model.FotoUrl" alt="" />
```

### Resultado esperado

Las vistas no deberían necesitar saber si la foto está en file system o en Blob Storage.

## Etapa 7 - Migrar fotos existentes

### Objetivo

Mover las fotos ya existentes desde el file system hacia Azure Blob Storage.

### Estrategia recomendada

Crear un proceso de migración separado, que pueda ejecutarse por lotes y reintentarse.

Puede ser:

- Un comando o herramienta interna.
- Un endpoint administrativo protegido.
- Un job temporal.
- Un script controlado manualmente.

### Proceso sugerido

1. Buscar capacitados con foto en file system.
2. Verificar que el archivo físico exista.
3. Subir el archivo a Blob Storage.
4. Actualizar `BlobStorageUri`.
5. Actualizar `TipoAlmacenamientoFoto` a `BlobStorage`.
6. Registrar el resultado.
7. No borrar el archivo físico en esta primera etapa.

### Requisitos del migrador

- Debe ser reentrante.
- Debe poder procesar por lotes.
- Debe registrar errores.
- Debe tolerar archivos faltantes.
- Debe permitir reintentos.
- No debe borrar originales hasta que la migración esté validada.

## Etapa 8 - Limpieza final

### Objetivo

Eliminar código legado una vez que la migración esté validada.

### Tareas posibles

- Eliminar ramas no usadas de almacenamiento en file system, si ya no aplican.
- Eliminar `NotImplementedException` relacionado con Blob Storage.
- Consolidar helpers duplicados.
- Eliminar rutas hardcodeadas a `~/Images/FotosCapacitados/`.
- Documentar el nuevo flujo definitivo.
- Definir política de limpieza de archivos locales antiguos.

Esta etapa debe hacerse solo después de validar que las fotos migradas se muestran, rotan, eliminan y consumen correctamente desde todos los flujos relevantes.

## Orden recomendado de PRs

### PR 1 - Inventario y documentación

Título sugerido:

```text
Document current file storage usage for Blob Storage migration
```

Contenido:

- Documentar estado actual.
- Listar puntos de acceso al file system.
- Describir estrategia de migración.
- Sin cambios funcionales.

### PR 2 - Abstracción de almacenamiento

Título sugerido:

```text
Add storage service abstraction for file handling
```

Contenido:

- Crear `IFileStorageService`.
- Crear `FileSystemStorageService`.
- Mantener comportamiento actual.
- No activar Azure todavía.

### PR 3 - Servicio de fotos de capacitados

Título sugerido:

```text
Move capacitado photo operations into dedicated service
```

Contenido:

- Crear `CapacitadoFotoService`.
- Mover carga, rotación, eliminación y resolución de URL.
- Mantener file system como backend.

### PR 4 - Implementación de Azure Blob Storage

Título sugerido:

```text
Add Azure Blob Storage implementation for file storage
```

Contenido:

- Crear `AzureBlobStorageService`.
- Agregar configuración necesaria.
- No activar como default.
- Evitar secretos en el repositorio.

### PR 5 - Guardar nuevas fotos en Blob Storage

Título sugerido:

```text
Store new capacitado photos in Azure Blob Storage behind feature flag
```

Contenido:

- Activar Blob Storage por configuración.
- Guardar nuevas fotos en Blob Storage.
- Actualizar `TipoAlmacenamientoFoto` y `BlobStorageUri`.
- Mantener soporte para fotos antiguas en file system.

### PR 6 - Resolver URLs mediante servicio o ViewModel

Título sugerido:

```text
Resolve capacitado photo URLs through storage service
```

Contenido:

- Eliminar rutas hardcodeadas en vistas.
- Usar `FotoUrl` o mecanismo equivalente.
- Soportar file system y Blob Storage.

### PR 7 - Migrador de fotos existentes

Título sugerido:

```text
Add migration job for existing capacitado photos to Blob Storage
```

Contenido:

- Crear proceso de migración.
- Procesar por lotes.
- Registrar errores.
- No borrar originales.

### PR 8 - Limpieza de código legado

Título sugerido:

```text
Remove legacy direct file system photo handling
```

Contenido:

- Eliminar código muerto.
- Eliminar dependencias directas innecesarias al file system.
- Consolidar helpers.
- Actualizar documentación.

## Checklist de pruebas

### Carga de foto

- [ ] Se puede subir una foto nueva.
- [ ] Se valida extensión y tamaño.
- [ ] Se conserva o normaliza correctamente el content type.
- [ ] Se quita EXIF si corresponde.
- [ ] Se guarda la referencia correcta en base de datos.
- [ ] No se rompen fotos existentes.

### Visualización

- [ ] Se muestra una foto almacenada en file system.
- [ ] Se muestra una foto almacenada en Blob Storage.
- [ ] Funciona la vista de edición del capacitado.
- [ ] Funciona la vista de carga de foto.
- [ ] Funciona el cache busting si aplica.
- [ ] No se expone información sensible en la URL.

### Rotación

- [ ] Se puede rotar una foto en file system.
- [ ] Se puede rotar una foto en Blob Storage.
- [ ] El resultado se guarda correctamente.
- [ ] La imagen actualizada se muestra luego de la rotación.

### Eliminación

- [ ] Se elimina la referencia en base de datos.
- [ ] Se elimina el archivo físico si corresponde.
- [ ] Se elimina el blob si corresponde.
- [ ] Se manejan errores sin dejar datos inconsistentes.

### Migración

- [ ] El migrador puede ejecutarse por lotes.
- [ ] El migrador es reentrante.
- [ ] Los errores quedan registrados.
- [ ] Los archivos faltantes no detienen todo el proceso.
- [ ] Los originales locales no se borran en la primera migración.
- [ ] Se puede reintentar una migración parcial.

## Plan de rollback

Durante la transición, mantener coexistencia entre file system y Blob Storage.

Si ocurre un problema con Blob Storage:

1. Cambiar configuración para volver a `StorageProvider = FileSystem`.
2. Mantener lectura de fotos antiguas mediante `PathArchivo`.
3. No borrar archivos locales hasta completar validaciones.
4. Mantener `BlobStorageUri` como dato adicional, no como único dato crítico, durante la transición.

En etapas avanzadas, antes de eliminar soporte de file system, realizar backup de:

- Base de datos.
- Carpeta local de imágenes.
- Contenedor de Blob Storage.

## Uso recomendado de agentes de IA

### GitHub Copilot Pro+

Usar para:

- Refactors pequeños.
- Generación de interfaces y servicios.
- Revisión de cambios por archivo.
- Explicación de código existente.
- Sugerencias dentro de Visual Studio o VS Code.

Prompt sugerido:

```text
Quiero refactorizar el manejo de fotos de capacitados para que no dependa directamente del file system. Creá una interfaz IFileStorageService y una implementación FileSystemStorageService que mantenga el comportamiento actual. No agregues Azure todavía. Evitá cambios funcionales.
```

### Codex a través de ChatGPT Plus

Usar para:

- Planificación de etapas.
- Revisión de arquitectura.
- Generación de checklists.
- Diseño del migrador.
- Análisis de riesgos.
- Preparación de prompts para Copilot.

Prompt sugerido:

```text
Estoy migrando una app ASP.NET MVC C# que guarda fotos en ~/Images/FotosCapacitados hacia Azure Blob Storage. Ya existen campos TipoAlmacenamientoFoto y BlobStorageUri. Quiero una estrategia incremental con coexistencia entre file system y Blob Storage. Dame una propuesta de clases, responsabilidades, feature flags y orden de PRs.
```

## Decisiones pendientes

Antes de implementar Blob Storage en producción, definir:

- Nombre definitivo del contenedor.
- Si el contenedor será público o privado.
- Si se usarán SAS temporales.
- Dónde se guardarán las credenciales.
- Cómo se manejarán thumbnails o transformaciones de imagen.
- Si se mantiene compatibilidad permanente con file system.
- Qué otros archivos, además de fotos de capacitados, serán migrados.
- Cómo se auditarán errores de migración.

## Recomendación final

La prioridad no debería ser subir archivos a Azure cuanto antes, sino reducir primero el acoplamiento actual al file system.

El camino más seguro es:

1. Documentar usos actuales.
2. Crear una abstracción de almacenamiento.
3. Mantener file system detrás de esa abstracción.
4. Extraer lógica de fotos a un servicio.
5. Implementar Azure Blob Storage como backend alternativo.
6. Activarlo solo para fotos nuevas.
7. Migrar fotos existentes posteriormente.
8. Limpiar código legado al final.

Este enfoque permite avanzar en cambios pequeños, revisables y aptos para ser delegados a agentes de IA sin asumir el riesgo de una migración completa en un único paso.
