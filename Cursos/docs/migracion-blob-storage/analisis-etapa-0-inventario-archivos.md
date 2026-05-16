# Analisis etapa 0 - Inventario de uso de archivos

## Objetivo

Documentar los puntos actuales donde la aplicacion lee, escribe, rota, elimina o referencia archivos en file system, tomando como referencia la estrategia de migracion incremental a Azure Blob Storage.

Este analisis no modifica codigo funcional. El alcance incluye fotos de capacitados y otros archivos locales relevantes. Se excluyen duplicados generados bajo `obj/Debug/Package/PackageTmp` y `obj/Release/Package/PackageTmp`, porque son artefactos de build y repiten vistas/codigo fuente.

## Resumen ejecutivo

El mayor acoplamiento al file system esta en el flujo de fotos de capacitados. Hay escritura directa en controladores y helpers, procesamiento de imagen desde ruta fisica dentro del modelo `Capacitado`, rotacion con `Image.FromFile`, eliminacion con `File.Delete`, envio a OVAL leyendo la foto desde disco y varias vistas que construyen URLs con `~/images/FotosCapacitados/@...PathCompleto`.

Tambien hay otros archivos locales a considerar: actas escaneadas de jornadas en `~/Images/Actas/`, documentos de interes en `~/documents/`, logs en `~/logs/`, assets estaticos usados al generar certificados PDF, fuentes locales como fallback y una herramienta masiva que importa fotos desde `~/Images/FotosCapacitadosImportar`.

Ya existen campos de transicion para fotos de capacitados (`TipoAlmacenamientoFoto` y `BlobStorageUri`), pero el soporte Blob Storage esta incompleto y no debe activarse sin abstraccion previa.

## Criterios de riesgo

| Riesgo | Criterio |
|---|---|
| Alto | Operaciones sobre fotos de personas, rutas fisicas persistidas, escritura/eliminacion en disco, vistas que dependen de rutas locales o integraciones externas que necesitan la imagen. |
| Medio | Archivos persistentes no-foto o utilidades administrativas que requeriran decision de migracion, pero con menor superficie de usuario final. |
| Bajo | Assets estaticos, archivos generados en memoria, logs o referencias visuales que pueden mantenerse fuera del alcance inicial de Blob Storage. |

## Hallazgos - fotos de capacitados

| Operacion | Archivo | Metodo / vista | Ruta o dato | Riesgo | Observaciones |
|---|---|---|---|---|---|
| Escritura de foto nueva | `Controllers/CapacitadosController.cs` | `Create` POST | `~/Images/FotosCapacitados/` | Alto | Si `TipoAlmacenamientoFotoDefecto` es `FileSystem`, genera nombre/carpeta, llama a `PathArchivoHelper.ObtenerPathArchivo` y persiste `PathArchivo`. |
| Rama Blob no implementada | `Controllers/CapacitadosController.cs` | `Create` POST | `TipoAlmacenamiento.BlobStorage` | Alto | Existe codigo comentado de prueba y luego `NotImplementedException`. No hay implementacion productiva ni abstraccion. Revisar ademas manejo de credenciales/configuracion antes de avanzar. |
| Escritura/reemplazo de foto | `Controllers/CapacitadosController.cs` | `Edit` POST | `~/Images/FotosCapacitados/` | Alto | Si recibe `upload`, crea un nuevo `PathArchivo` y guarda el archivo en disco. El flujo no evalua `TipoAlmacenamientoFoto`. |
| Carga AJAX de foto | `Controllers/CapacitadosController.cs` | `CargarFotoCapacitado` | Delegado a `Capacitado.CargarFoto` | Alto | Endpoint usado desde la pantalla de carga de foto; guarda y procesa la imagen desde el modelo. |
| Rotacion AJAX de foto | `Controllers/CapacitadosController.cs` | `RotarFotoCapacitado` | Delegado a `Capacitado.RotarFoto` | Alto | La rotacion depende de que la foto exista como archivo fisico local. |
| Eliminacion de foto | `Controllers/CapacitadosController.cs` | `EliminarFoto`, `EliminarFotoDesdeCargarFoto`, privado `EliminarFoto` | `~/Images/FotosCapacitados/{SubDirectorio}/{NombreArchivo}` | Alto | Borra archivo con `File.Exists`/`File.Delete`, remueve `PathArchivo` y guarda cambios. Debe ser polimorfico por backend. |
| Escritura centralizada en helper | `Helpers/PathArchivoHelper.cs` | `ObtenerPathArchivo` | `Path.Combine(pathDirectorio, NombreArchivo)` | Alto | Punto central de `Directory.Exists`, `Directory.CreateDirectory` y `HttpPostedFileBase.SaveAs`. Es candidato natural para quedar detras de `IFileStorageService`. |
| Generacion de nombre/carpeta | `Helpers/PathArchivoHelper.cs` | `ObtenerNombreFotoCapacitado`, `ObtenerCarpetaFotoCapacitado` | `Foto_{id}_{random}{ext}` y subdirectorio numerico | Medio | Logica reutilizable, pero no deberia asumir necesariamente file system en etapas futuras. |
| Procesamiento al cargar | `Models/Capacitado.cs` | `CargarFoto` | `~/Images/FotosCapacitados/` | Alto | Guarda con `PathArchivoHelper`, luego reabre con `Image.FromFile`, quita EXIF via streams y sobrescribe con `Image.Save`. Mezcla entidad, HTTP, disco y procesamiento de imagen. |
| Rotacion | `Models/Capacitado.cs` | `RotarFoto` | `~/Images/FotosCapacitados/` | Alto | Usa `Image.FromFile`, `RotateFlip` e `Image.Save` sobre la misma ruta fisica. |
| Cambio de extension | `Models/Capacitado.cs` | `CambiarExtensionFotoAJPG` | `.jpeg` a `.jpg` | Medio | Usa `File.Move` y no actualiza por si mismo el registro `PathArchivo`. Hay otra variante en helper que si actualiza DB. |
| Cambio de extension con DB | `Helpers/CapacitadoHelper.cs` | `CambiarExtensionFotoAJPG` | `.jpeg` a `.jpg` | Alto | Usado desde envio OVAL; mueve archivo fisico y actualiza `PathArchivo.NombreArchivo`. Debe quedar detras de almacenamiento/servicio de foto. |
| Lectura para OVAL SOAP | `Helpers/EnvioOVAL/EnvioOVALHelper.cs` | `EnviarDatosRegistroSOAP` | `~/Images/FotosCapacitados/` | Alto | Si hay foto, fuerza opcionalmente `.jpeg` a `.jpg`, arma ruta local y llama `GetImageAsByteArray`. Integracion externa sensible. |
| Lectura para OVAL REST | `Helpers/EnvioOVAL/EnvioOVALHelper.cs` | `EnviarDatosRegistroRest` | `~/Images/FotosCapacitados/` | Alto | Arma ruta local, convierte imagen a byte array y luego Base64. Debe soportar fotos en Blob Storage. |
| Conversion imagen a bytes | `Helpers/EnvioOVAL/EnvioOVALHelper.cs` | `GetImageAsByteArray` | Ruta fisica recibida | Alto | Usa `Image.FromFile` y deja nota TODO sobre bloqueo del archivo. Conviene migrar a streams. |
| Importacion masiva de fotos | `Controllers/CustomToolsController.cs` | `AsignarCapacitadosFotos` POST | `~/Images/FotosCapacitadosImportar` a `~/Images/FotosCapacitados/` | Medio/Alto | Recorre carpeta local con `Directory.GetFiles`, crea directorio destino y copia con `File.Copy`. Es una utilidad administrativa, pero toca fotos de capacitados. |
| Limpieza de huerfanos | `Helpers/ArchivosFSHelper.cs` | `RecorrerArchivosHuerfanos` | `PathArchivos` tipo `FotoCapacitado` | Medio | Escribe reporte TXT y mueve/elimina archivos huerfanos. Deberia revisarse antes de migrar o eliminar fotos locales. |
| Modelo de referencia | `Models/Capacitado.cs` | Propiedades | `PathArchivoID`, `PathArchivo`, `TipoAlmacenamientoFoto`, `BlobStorageUri` | Alto | Ya hay base para coexistencia. Hoy muchas lecturas siguen mirando solo `PathArchivo`. |
| Modelo de path | `Models/PathArchivo.cs` | `PathCompleto` | `Path.Combine(SubDirectorio, NombreArchivo)` | Alto | Expone una ruta relativa dependiente de file system; se consume en vistas. |
| Configuracion de backend | `Helpers/ConfiguracionHelper.cs` | `GetCapacitado_TipoAlmacenamientoFotoDefecto` | `TipoAlmacenamientoFotoDefecto` / `Capacitados` | Alto | Feature flag existente para elegir `FileSystem` o `BlobStorage`, pero solo el camino FileSystem funciona. |

## Hallazgos - vistas y referencias URL de fotos

| Operacion | Archivo | Vista / metodo | Ruta o dato | Riesgo | Observaciones |
|---|---|---|---|---|---|
| Visualizacion detalle/edicion | `Views/Capacitados/Edit.cshtml` | Vista | `~/images/FotosCapacitados/@Model.PathArchivo.PathCompleto` | Alto | Genera link a la imagen original y thumbnail con query de resize/crop. |
| Visualizacion detalle | `Views/Capacitados/Details.cshtml` | Vista | `~/images/FotosCapacitados/@Model.PathArchivo.PathCompleto` | Alto | Igual que Edit, acoplado a `PathArchivo`. |
| Carga/rotacion de foto | `Views/Shared/_CapacitadoCargarFotoPartial.cshtml` | Partial | `srcCapacitadoFotoOriginal`, link e `img` | Alto | Usada en flujo AJAX; el JS depende de URL local para restaurar/mostrar imagen. |
| Lista registros | `Views/Shared/_ListRegistrosCapacitacionPartial.cshtml` | Partial | `@item.Capacitado.PathArchivo.PathCompleto` | Alto | Thumbnail de 75x75 desde ruta local. |
| Lista registros con fotos | `Views/Shared/_ListRegistrosCapacitacionFotosPartial.cshtml` | Partial | `@item.Capacitado.PathArchivo.PathCompleto` | Alto | Thumbnail de 150x150 desde ruta local. |
| Carga fotos por jornada | `Views/Jornadas/CargarFotos.cshtml` | Vista | `@item.Capacitado.PathArchivo.PathCompleto` | Alto | Pantalla operativa para revisar/cargar fotos de una jornada. |
| Fallback sin foto | Varias vistas | Vista | `~/images/sinfoto_75x75.png`, `~/images/sinfoto_150x150.png` | Bajo | Asset estatico; puede permanecer local o CDN. |
| Iconos de acciones | Varias vistas | Vista | `~/images/*.png` | Bajo | Iconos estaticos no relacionados con almacenamiento de fotos de personas. |

## Hallazgos - actas de jornadas

| Operacion | Archivo | Metodo / vista | Ruta o dato | Riesgo | Observaciones |
|---|---|---|---|---|---|
| Escritura de acta escaneada | `Controllers/JornadasController.cs` | `AgregarActa` POST | `~/Images/Actas/Acta_{JornadaID}{ext}` | Alto | Usa `HttpPostedFileBase.SaveAs` y asocia `Jornada.PathArchivo`. Similar al patron de fotos, aunque es otro tipo de archivo. |
| Eliminacion de acta | `Controllers/JornadasController.cs` | `EliminarActa` | `~/Images/Actas/{NombreArchivo}` | Alto | Usa `File.Exists`/`File.Delete`, remueve `PathArchivo` y guarda. |
| Visualizacion de acta | `Views/Jornadas/Details.cshtml` | Vista | `~/images/Actas/@Model.PathArchivo.NombreArchivo` | Medio/Alto | URL hardcodeada al file system web. Si actas migran, necesitara URL resuelta por servicio. |
| Modelo de referencia | `Models/Jornada.cs` | Propiedades | `PathArchivoID`, `PathArchivo` | Medio/Alto | Comparte la entidad `PathArchivo`; no tiene campos Blob equivalentes especificos. |

## Hallazgos - documentos de interes

| Operacion | Archivo | Metodo / vista | Ruta o dato | Riesgo | Observaciones |
|---|---|---|---|---|---|
| Descarga de documento | `Controllers/DocumentosInteresController.cs` | `Download` | `~/documents/{NombreArchivo}` | Medio | Valida `Activo`, arma ruta con `Server.MapPath`, verifica `File.Exists` y retorna `File(filePath, contentType, NombreArchivo)`. |
| Metadata de documento | `Models/DocumentoInteres.cs` | Propiedad | `NombreArchivo` | Medio | El CRUD gestiona nombre/descripcion/activo; el archivo fisico parece administrarse fuera del flujo MVC actual. |
| Tipos de contenido | `Controllers/DocumentosInteresController.cs` | `GetContentType` | Extension de archivo | Bajo/Medio | Logica reutilizable si se descarga desde Blob Storage. |
| Vista de listado | `Views/DocumentosInteres/Index.cshtml` | Vista | Accion `Download` | Bajo | No construye ruta fisica directa; depende del endpoint. |

## Hallazgos - archivos generados, importaciones y logs

| Operacion | Archivo | Metodo / vista | Ruta o dato | Riesgo | Observaciones |
|---|---|---|---|---|---|
| Logs de OVAL y otros modulos | `Helpers/LogHelper.cs` | `WriteMessage` | `~/logs/{module}/{yyyyMMdd}_{module}.txt` | Medio | Escribe con `StreamWriter` en append. Si se migra infraestructura, evaluar Application Insights, tabla o Blob append. |
| Listado de logs | `Helpers/LogHelper.cs` | `GetModuleLogFiles` | `~/logs/{module}/` | Medio | Usa `Directory.Exists` y `Directory.GetFiles`, devuelve rutas web locales. |
| Reporte de huerfanos | `Helpers/ArchivosFSHelper.cs` | `RecorrerArchivosHuerfanos` | `~/ArchivosHuerfanos_*.txt` | Bajo/Medio | Escribe un TXT de auditoria con IDs. Puede mantenerse local temporalmente, pero conviene redisenarlo para migracion. |
| Exportaciones Excel | `AccountController`, `CapacitadosController`, `CustomToolsController`, `EmpresasController`, `JornadasController`, `RegistrosCapacitacionController`, `NotificacionesVencimientosController`, `ExportarExcelHelper` | Varios | `MemoryStream` + `File(stream, ...)` | Bajo | Generan archivos en memoria para descarga; no persisten en file system. No bloquean Blob Storage. |
| Importaciones Excel | `Controllers/CustomToolsController.cs` | Varios metodos con `Request.Files["UploadedFile"]` y `ExcelPackage(file.InputStream)` | Stream HTTP | Bajo/Medio | Procesan uploads en memoria. Solo son relevantes para Blob si en el futuro se decide persistir archivos originales. |
| Generacion PDF de certificados/actas | `CapacitadosController`, `JornadasController`, helpers PDF | Varios | `MemoryStream` + `File(stream, ...)` | Bajo | Los PDFs generados se devuelven en memoria; no hay persistencia local en los hallazgos revisados. |

## Hallazgos - assets estaticos y fuentes

| Operacion | Archivo | Metodo / vista | Ruta o dato | Riesgo | Observaciones |
|---|---|---|---|---|---|
| Firma en certificado PDF | `Helpers/CertificadoHelper.cs` | `DibujarFirma` | `~/images/certificados/firma-alejandro-lacruz.png` | Bajo/Medio | Lee asset estatico con `XImage.FromFile`. No es foto de capacitado, pero depende de ruta fisica local. |
| Logo en certificado PDF | `Helpers/CertificadoHelper.cs` | `DibujarLogo` | `~/images/logos/CSL_logo_main_h.png` | Bajo/Medio | Igual al anterior. Podria mantenerse como recurso local/embebido. |
| Fuentes PDF | `Helpers/FontHelper.cs` | `LoadFontData`, `GetFontFilePath` | Recurso embebido o archivo local | Bajo | Primero intenta recurso embebido; fallback usa `File.Exists` y `File.ReadAllBytes`. No requiere Blob para fotos. |
| Logos/iconos en vistas | Varias vistas | HTML `img` / `Url.Content` | `~/images/...` | Bajo | Assets de UI. No forman parte del almacenamiento de archivos de usuario. |

## Puntos de acoplamiento principales

1. `PathArchivoHelper.ObtenerPathArchivo` concentra parte de la escritura en disco para fotos, pero no cubre procesamiento, rotacion, eliminacion ni lectura.
2. `Capacitado` contiene logica de infraestructura: `HttpPostedFileBase`, `Server.MapPath`, `Image.FromFile`, `File.Move` y `Image.Save`.
3. Las vistas de fotos dependen de `PathArchivo.PathCompleto`, que asume subdirectorio + nombre de archivo local.
4. OVAL requiere la foto como byte array/base64 y hoy solo sabe leerla desde disco.
5. Actas y documentos usan patrones similares de `PathArchivo`/`NombreArchivo`, pero no tienen los campos de coexistencia Blob que ya tiene `Capacitado`.
6. La configuracion `TipoAlmacenamientoFotoDefecto` existe, pero el backend `BlobStorage` no esta implementado y no debe ser el valor por defecto todavia.

## Riesgos especificos para migracion a Blob Storage

| Riesgo | Impacto | Mitigacion sugerida |
|---|---|---|
| Fotos existentes dejan de mostrarse | Alto | Mantener coexistencia: `PathArchivo` para FileSystem y `BlobStorageUri` para Blob. Resolver URL desde servicio/ViewModel. |
| Rotacion/procesamiento depende de rutas fisicas | Alto | Extraer procesamiento a streams y moverlo a servicio de imagen/foto. |
| OVAL no puede enviar fotos Blob | Alto | Exponer metodo de servicio que devuelva stream/bytes de foto sin importar backend. |
| Eliminacion parcial deja DB y storage inconsistentes | Alto | Centralizar delete en servicio transaccional/compensable; registrar errores. |
| Vistas hardcodeadas ignoran `TipoAlmacenamientoFoto` | Alto | Introducir `FotoUrl` o helper de resolucion de URL. |
| Actas/documentos quedan fuera de la abstraccion inicial | Medio | Definir si el PR inicial cubre solo fotos o una abstraccion generica para `PathArchivo`. |
| Credenciales y acceso publico en prototipos | Alto | No guardar secretos en repositorio; usar App Settings/Key Vault y preferir contenedor privado o SAS temporal. |
| Herramientas administrativas importan desde carpetas locales | Medio | Mantener temporalmente file system para importacion o crear flujo de carga administrado. |

## Recomendaciones para las siguientes etapas

1. En la etapa 1, crear una abstraccion de almacenamiento sin cambiar comportamiento visible y hacer que el backend inicial sea FileSystem.
2. Priorizar fotos de capacitados como primer dominio, porque concentran datos personales, vistas, procesamiento, eliminacion y OVAL.
3. Extraer la logica de `Capacitado.CargarFoto`, `RotarFoto` y `CambiarExtensionFotoAJPG` hacia un servicio de fotos antes de activar Blob Storage.
4. Introducir una forma unica de resolver la URL de foto (`FotoUrl`, helper o servicio) antes de modificar vistas.
5. Dejar actas, documentos, logs y assets estaticos documentados como alcance posterior o decisiones separadas, salvo que se quiera una abstraccion general para todos los `PathArchivo` desde el inicio.
6. No activar `TipoAlmacenamientoFotoDefecto = BlobStorage` hasta eliminar el `NotImplementedException`, externalizar credenciales y definir seguridad del contenedor.

## Lista resumida por prioridad

| Prioridad | Elementos |
|---|---|
| P1 | `CapacitadosController`, `Models/Capacitado`, `PathArchivoHelper`, vistas de fotos, `EnvioOVALHelper`. |
| P2 | `CustomToolsController.AsignarCapacitadosFotos`, `ArchivosFSHelper`, `CapacitadoHelper.CambiarExtensionFotoAJPG`. |
| P3 | Actas en `JornadasController` y `Views/Jornadas/Details.cshtml`. |
| P4 | `DocumentosInteresController.Download` y modelo `DocumentoInteres`. |
| P5 | Logs, assets estaticos, fuentes y exportaciones generadas en memoria. |
