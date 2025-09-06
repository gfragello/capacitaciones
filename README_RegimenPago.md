# Implementación de Régimen de Pago para Empresas

Se ha implementado un sistema de regímenes de pago para las empresas. Este sistema permite asociar a cada empresa un régimen de pago específico, lo que facilita la gestión de los pagos según diferentes modalidades.

## Cambios realizados

1. Creación de la clase `RegimenPago` con los siguientes atributos:
   - `RegimenPagoID`: Identificador único
   - `Nombre`: Nombre del régimen de pago
   - `Descripcion`: Descripción detallada del régimen

2. Actualización de la clase `Empresa` para incluir una referencia al régimen de pago:
   - `RegimenPagoID`: Clave foránea para relacionar con la tabla `RegimenPago`
   - `RegimenPago`: Propiedad de navegación para acceder al régimen asociado

3. Actualización del controlador `EmpresasController` para soportar la selección y filtrado por régimen de pago.

4. Modificación de las vistas:
   - `Create.cshtml`: Adición de selector para régimen de pago
   - `Edit.cshtml`: Adición de selector para régimen de pago
   - `Details.cshtml`: Visualización del régimen de pago asociado
   - `Index.cshtml`: Inclusión del régimen en la lista de empresas y filtros

5. Creación de migración para actualizar la base de datos.

## Valores iniciales de regímenes de pago

Se han configurado dos regímenes de pago iniciales:
- **Régimen Estándar**: Pago dentro de los 30 días de la factura
- **Pago anticipado**: Requiere pago por adelantado antes de la capacitación

## Instrucciones para aplicar los cambios

### Opción 1: Ejecutar la migración (recomendado)

1. Abrir la Consola del Administrador de Paquetes en Visual Studio
2. Ejecutar el siguiente comando para aplicar la migración:

```
Update-Database
```

### Opción 2: Ejecutar el script SQL manualmente

Si prefiere aplicar los cambios directamente en la base de datos, se ha proporcionado un script SQL (`sql_regimen_pago.sql`) que puede ejecutar en SQL Server Management Studio o herramienta similar.

El script realiza las siguientes acciones:
1. Crea la tabla `RegimenPagoes` si no existe
2. Inserta los valores iniciales para los regímenes
3. Añade la columna `RegimenPagoID` a la tabla `Empresas`
4. Crea la relación entre las tablas
5. Asigna el "Régimen Estándar" a todas las empresas existentes

## Notas adicionales

- Las empresas nuevas deberán tener un régimen de pago seleccionado.
- En un futuro, se podría implementar un mantenimiento completo de regímenes de pago para permitir la creación y edición de nuevos regímenes.
