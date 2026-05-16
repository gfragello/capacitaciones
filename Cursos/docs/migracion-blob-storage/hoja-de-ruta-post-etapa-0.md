# Hoja de ruta post etapa 0 (migración a Azure Blob Storage)

## Estado actual

- La **PR 1 (inventario y documentación)** ya está completada.
- La siguiente PR recomendada es la **PR 2: abstracción de almacenamiento para manejo de archivos**.

## Orden recomendado de PRs (PR 2 a PR 8)

1. **PR 2 - Abstracción de almacenamiento**
   - Crear `IFileStorageService`.
   - Crear `FileSystemStorageService`.
   - Conectar el flujo actual de fotos para usar la abstracción sin cambiar comportamiento visible.
2. **PR 3 - Servicio dedicado de fotos de capacitados**
   - Extraer lógica de carga/rotación/eliminación a un servicio específico.
3. **PR 4 - Implementación Azure Blob Storage**
   - Agregar `AzureBlobStorageService` detrás de la misma interfaz.
   - No activarlo por defecto.
4. **PR 5 - Nuevas fotos en Blob Storage**
   - Guardar nuevas fotos en Blob por configuración/feature flag.
   - Mantener soporte de lectura para file system.
5. **PR 6 - Resolución de URLs vía servicio**
   - Eliminar rutas hardcodeadas en vistas y usar resolución centralizada.
6. **PR 7 - Migración de fotos existentes**
   - Proceso por lotes, trazabilidad de errores y sin borrar originales.
7. **PR 8 - Limpieza de legado**
   - Retirar dependencias directas restantes al file system para fotos.

## Alcance de la PR 2

### Incluye

- Primera versión de `IFileStorageService` y `FileSystemStorageService`.
- Encapsular operaciones de:
  - guardar,
  - abrir/leer,
  - resolver path físico,
  - resolver URL,
  - verificar existencia,
  - eliminar,
  - mover archivos
  para `~/Images/FotosCapacitados/`.
- Adaptar el flujo actual de fotos de capacitados para usar esta abstracción (helpers/controlador/modelo involucrados).
- Mantener **FileSystem** como único backend funcional.

### No incluye

- Implementación real de Azure Blob Storage.
- Activar Blob Storage como default.
- Reescritura de vistas para usar `FotoUrl`.
- Migración de archivos existentes.
- Refactor de dominios de almacenamiento no relacionados (actas, documentos, logs, assets estáticos).

## Criterios de aceptación de la PR 2

- El alta/edición/eliminación de foto de capacitado mantiene el comportamiento actual.
- Rotación y cambio de extensión (`.jpeg` a `.jpg`) siguen funcionando igual.
- Lecturas críticas de fotos (incluyendo envío OVAL) siguen funcionando.
- No se agregan secretos ni configuración real de Azure.
- No se altera el backend por defecto (sigue siendo file system).
