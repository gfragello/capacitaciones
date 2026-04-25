# Migración: Eliminar columna "Aprobado" de RegistrosCapacitaciones

## 📋 Resumen
Se ha completado la **Fase 1** de la refactorización para eliminar la propiedad redundante `Aprobado` de la clase `RegistroCapacitacion`. La propiedad `Estado` (enum `EstadosRegistroCapacitacion`) es ahora la única fuente de verdad para el estado de un registro.

---

## ✅ Cambios realizados en el código

### 1. **Modelo** (`RegistroCapacitacion.cs`)
- ❌ Eliminada propiedad `public bool Aprobado`
- ❌ Eliminada propiedad calculada `AprobadoTexto`
- ✅ Se usa únicamente `Estado` (EstadosRegistroCapacitacion)

### 2. **Controllers**

#### `CustomToolsController.cs` (línea ~398)
**Antes:**
```csharp
r.Aprobado = (aprobadoTetxo == "Si" || aprobadoTetxo == "S") ? true : false;
```

**Después:**
```csharp
// Determinar estado según el texto "Aprobado"
bool esAprobado = (aprobadoTetxo == "Si" || aprobadoTetxo == "S");
r.Estado = esAprobado ? EstadosRegistroCapacitacion.Aprobado : EstadosRegistroCapacitacion.NoAprobado;
```

#### `CapacitadosController.cs` (línea ~342)
**Antes:**
```csharp
nuevoRC.Aprobado = true;
```

**Después:**
```csharp
nuevoRC.Estado = EstadosRegistroCapacitacion.Aprobado;
```

### 3. **Views**

#### `Create.cshtml`
- Reemplazado checkbox de `Aprobado` por dropdown de `Estado`
- Ahora usa `@Html.EnumDropDownListFor(model => model.Estado)`

#### `Edit.cshtml`
- `@Html.HiddenFor(model => model.Aprobado)` → `@Html.HiddenFor(model => model.Estado)`
- `@Html.DisplayFor(model => model.Aprobado)` → `@Html.DisplayFor(model => model.Estado)`

#### `Details.cshtml` (RegistrosCapacitacion)
- `@Html.DisplayFor(model => model.Aprobado)` → `@Html.DisplayFor(model => model.Estado)`

#### `Details.cshtml` (Capacitados)
- Columna "Aprobado" → "Estado"
- `@Html.DisplayFor(modelItem => item.Aprobado)` → `@Html.DisplayFor(modelItem => item.Estado)`

---

## 🗄️ Migración de Base de Datos (PENDIENTE)

### Script SQL sugerido:

```sql
-- PASO 1: Sincronizar datos de Aprobado hacia Estado
-- (Si Estado ya existe en la BD)
UPDATE dbo.RegistrosCapacitaciones
SET Estado = CASE 
    WHEN Aprobado = 1 THEN 1  -- EstadosRegistroCapacitacion.Aprobado
    ELSE 2                     -- EstadosRegistroCapacitacion.NoAprobado
END
WHERE Estado = 0;  -- Solo actualizar los que están en Inscripto (por si no se sincronizaron)

-- PASO 2: Verificar que no haya datos inconsistentes
SELECT 
    RegistroCapacitacionID,
    Aprobado,
    Estado,
    CASE Estado 
        WHEN 0 THEN 'Inscripto'
        WHEN 1 THEN 'Aprobado'
        WHEN 2 THEN 'NoAprobado'
    END AS EstadoTexto
FROM dbo.RegistrosCapacitaciones
ORDER BY Estado, Aprobado;

-- PASO 3: Eliminar columna Aprobado
ALTER TABLE dbo.RegistrosCapacitaciones
DROP COLUMN Aprobado;
```

### O usando Entity Framework Migration:

```powershell
# En Package Manager Console
Add-Migration EliminarColumnaAprobado
Update-Database
```

**Contenido del método `Up()` de la migración:**

```csharp
public override void Up()
{
    // Sincronizar datos antes de eliminar
    Sql(@"
        UPDATE dbo.RegistrosCapacitaciones
        SET Estado = CASE 
            WHEN Aprobado = 1 THEN 1
            ELSE 2
        END
        WHERE Estado = 0
    ");
    
    // Eliminar la columna
    DropColumn("dbo.RegistrosCapacitaciones", "Aprobado");
}

public override void Down()
{
    // Restaurar columna en caso de rollback
    AddColumn("dbo.RegistrosCapacitaciones", "Aprobado", c => c.Boolean(nullable: false, defaultValue: false));
    
    // Sincronizar datos al revés
    Sql(@"
        UPDATE dbo.RegistrosCapacitaciones
        SET Aprobado = CASE 
            WHEN Estado = 1 THEN 1
            ELSE 0
        END
    ");
}
```

---

## 🧪 Pruebas recomendadas

Después de aplicar la migración, verificar:

1. ✅ **Crear un nuevo registro** de capacitación
2. ✅ **Editar un registro** existente
3. ✅ **Calificar registros** (con nota y aprobado/no aprobado)
4. ✅ **Importar desde Excel** (CustomToolsController)
5. ✅ **Inscribir capacitado** desde CapacitadosController
6. ✅ **Ver detalles** de registros en todas las vistas
7. ✅ **Verificar el cambio de estado** con el método `CambiarEstado()`

---

## 📊 Ventajas de este cambio

1. **Eliminación de redundancia**: Ya no hay dos propiedades representando el mismo concepto
2. **Consistencia de datos**: Una sola fuente de verdad (`Estado`)
3. **Más expresividad**: El enum tiene 3 estados (Inscripto, Aprobado, NoAprobado) vs un simple boolean
4. **Menos posibilidad de errores**: No puede haber inconsistencias entre `Aprobado` y `Estado`
5. **Código más limpio**: Menos lógica condicional para verificar el estado

---

## ⚠️ Notas importantes

- La columna `Aprobado` **debe eliminarse de la base de datos** para completar la refactorización
- Todos los usos de `Aprobado` en el código han sido reemplazados por `Estado`
- No se encontraron errores de compilación después de los cambios
- La propiedad `AprobadoTexto` ya no existe; usar directamente `Estado` (ASP.NET MVC mostrará automáticamente el valor del enum)

---

## 📝 Checklist de implementación

- [x] Eliminar propiedades `Aprobado` y `AprobadoTexto` del modelo
- [x] Actualizar todos los controllers que usan `Aprobado`
- [x] Actualizar todas las views que referencian `Aprobado`
- [x] Verificar que no hay errores de compilación
- [ ] **Crear migración de Entity Framework**
- [ ] **Aplicar migración en base de datos de desarrollo**
- [ ] **Probar todas las funcionalidades afectadas**
- [ ] **Aplicar migración en base de datos de producción**

---

**Fecha de refactorización**: 17 de octubre de 2025  
**Archivos modificados**: 7  
**Estado**: ✅ Código completado - Pendiente migración de BD
