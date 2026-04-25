# Visualización de Estado de Notificación en Detalles de Capacitado

**Fecha:** 14 de octubre de 2025  
**Objetivo:** Mostrar el estado de la notificación de vencimiento asociada a cada registro de capacitación en la vista de detalles del capacitado

---

## 🎯 Cambios Implementados

### 1. **Modelo: RegistroCapacitacion.cs**

#### ✅ Navegación Inversa Agregada
```csharp
// Relación inversa con NotificacionVencimiento (1:N)
// La FK está en NotificacionVencimiento.RegistroCapacitacionID
public virtual ICollection<NotificacionVencimiento> NotificacionesVencimiento { get; set; }
```

**Ubicación:** Después de la propiedad `Capacitado`  
**Propósito:** Permite acceder a las notificaciones asociadas desde el registro  
**Nota:** Aunque en la práctica cada registro tiene solo una notificación, el modelo usa `ICollection<>` para seguir las convenciones de Entity Framework (relación 1:N)

---

#### ✅ Propiedades NotMapped Agregadas

**`EstadoNotificacion`**
```csharp
[NotMapped]
public EstadoNotificacionVencimiento? EstadoNotificacion
{
    get
    {
        return this.NotificacionesVencimiento?.FirstOrDefault()?.Estado;
    }
}
```
- **Tipo:** `EstadoNotificacionVencimiento?` (nullable)
- **Propósito:** Acceso rápido al estado de la notificación
- **Retorna:** El estado de la primera notificación si existe, `null` si no
- **Nota:** Usa `.FirstOrDefault()` porque es una colección

---

**`EstadoNotificacionTexto`**
```csharp
[NotMapped]
public string EstadoNotificacionTexto
{
    get
    {
        var notificacion = this.NotificacionesVencimiento?.FirstOrDefault();
        
        if (notificacion == null)
            return "Sin notificación";

        switch (notificacion.Estado)
        {
            case EstadoNotificacionVencimiento.NotificacionPendiente:
                return "Pendiente";
            case EstadoNotificacionVencimiento.Notificado:
                return "Notificado";
            case EstadoNotificacionVencimiento.NoNotificar:
                return "No Notificar";
            case EstadoNotificacionVencimiento.NoNotificarYaActualizado:
                return "Ya Actualizado";
            default:
                return "Desconocido";
        }
    }
}
```
- **Tipo:** `string`
- **Propósito:** Obtener texto legible del estado
- **Mapeo de valores:**
  - `NotificacionPendiente` → "Pendiente"
  - `Notificado` → "Notificado"
  - `NoNotificar` → "No Notificar"
  - `NoNotificarYaActualizado` → "Ya Actualizado"
  - `null` → "Sin notificación"

---

**`TieneNotificacion`**
```csharp
[NotMapped]
public bool TieneNotificacion
{
    get
    {
        return this.NotificacionesVencimiento?.Any() ?? false;
    }
}
```
- **Tipo:** `bool`
- **Propósito:** Verificación rápida de existencia de notificación
- **Uso:** Evita verificaciones null repetidas en las vistas
- **Nota:** Usa `.Any()` para verificar si hay elementos en la colección

---

### 2. **Controlador: CapacitadosController.cs**

#### ✅ Método `Details` Modificado

**Antes:**
```csharp
var capacitados = db.Capacitados.Include(c => c.RegistrosCapacitacion);
```

**Después:**
```csharp
// Incluir RegistrosCapacitacion y sus NotificacionesVencimientos asociadas
var capacitados = db.Capacitados
    .Include(c => c.RegistrosCapacitacion.Select(rc => rc.NotificacionesVencimiento));
```

**Cambios:**
- Agregado `.Select(rc => rc.NotificacionesVencimiento)` para cargar las notificaciones
- Comentario explicativo agregado
- **Resultado:** Entity Framework carga automáticamente las notificaciones en una sola consulta (eager loading)
- **Nota:** `NotificacionesVencimiento` es plural porque es una colección `ICollection<>`

---

### 3. **Vista: Details.cshtml**

#### ✅ Columnas Agregadas a la Tabla

**Nuevas columnas de encabezado:**
```html
<th>
    @Html.DisplayName("Vencimiento")
</th>
<th>
    @Html.DisplayName("Estado Notificación")
</th>
```

---

#### ✅ Lógica de Estilos por Estado

```csharp
// Determinar el estilo para el estado de notificación
string estadoNotificacionClass = "";

if (item.TieneNotificacion)
{
    switch (item.EstadoNotificacion)
    {
        case Cursos.Models.Enums.EstadoNotificacionVencimiento.NotificacionPendiente:
            estadoNotificacionClass = "label label-warning";
            break;
        case Cursos.Models.Enums.EstadoNotificacionVencimiento.Notificado:
            estadoNotificacionClass = "label label-info";
            break;
        case Cursos.Models.Enums.EstadoNotificacionVencimiento.NoNotificar:
            estadoNotificacionClass = "label label-default";
            break;
        case Cursos.Models.Enums.EstadoNotificacionVencimiento.NoNotificarYaActualizado:
            estadoNotificacionClass = "label label-success";
            break;
    }
}
```

**Mapeo de estilos Bootstrap:**
- 🟡 **Pendiente** → `label label-warning` (amarillo/naranja)
- 🔵 **Notificado** → `label label-info` (azul)
- ⚫ **No Notificar** → `label label-default` (gris)
- 🟢 **Ya Actualizado** → `label label-success` (verde)

---

#### ✅ Renderizado de Columnas

**Columna Vencimiento:**
```html
<td>
    @if (item.FechaVencimiento.HasValue)
    {
        @item.FechaVencimiento.Value.ToShortDateString()
    }
    else
    {
        <span style="color: #999;">Sin vencimiento</span>
    }
</td>
```

**Columna Estado Notificación:**
```html
<td>
    @if (item.TieneNotificacion)
    {
        <span class="@estadoNotificacionClass">@item.EstadoNotificacionTexto</span>
    }
    else
    {
        <span style="color: #999; font-style: italic;">Sin notificación</span>
    }
</td>
```

---

## 🎨 Visualización en Pantalla

### Ejemplo de Tabla Resultante:

| | Jornada | Nota Previa | Nota | Aprobado | Vencimiento | Estado Notificación | |
|---|---------|-------------|------|----------|-------------|---------------------|---|
| ✓ | Curso A - 01/2025 | 0 | 85 | Aprobado | 01/01/2026 | 🟢 **Ya Actualizado** | 🗑️ |
| ✓ | Curso A - 06/2024 | 0 | 80 | Aprobado | 06/01/2025 | 🟡 **Pendiente** | |
| ✓ | Curso B - 03/2024 | 0 | 90 | Aprobado | *Sin vencimiento* | *Sin notificación* | |

---

## 📊 Estados de Notificación Visibles

| Estado Enum | Texto Mostrado | Color/Estilo | Significado |
|-------------|----------------|--------------|-------------|
| `NotificacionPendiente` (0) | Pendiente | 🟡 Warning | La notificación está pendiente de envío |
| `Notificado` (1) | Notificado | 🔵 Info | La notificación fue enviada |
| `NoNotificar` (2) | No Notificar | ⚫ Default | Marcada manualmente para no notificar |
| `NoNotificarYaActualizado` (3) | Ya Actualizado | 🟢 Success | Capacitado cursó jornada posterior (sistema automático) |
| `null` | Sin notificación | ⚪ Gris cursiva | El registro no tiene notificación asociada |

---

## 🔄 Flujo de Datos

```
1. Usuario accede a /Capacitados/Details/123
   ↓
2. CapacitadosController.Details()
   ↓
3. EF carga Capacitado + RegistrosCapacitacion + NotificacionesVencimientos
   ↓
4. Vista renderiza tabla con registros
   ↓
5. Para cada registro:
   - item.TieneNotificacion → verifica si existe notificación
   - item.EstadoNotificacion → obtiene el enum del estado
   - item.EstadoNotificacionTexto → obtiene texto legible
   ↓
6. Se aplica clase CSS según el estado
   ↓
7. Se muestra badge coloreado con el estado
```

---

## ✅ Ventajas de esta Implementación

### **Performance**
- ✅ **Eager Loading:** Carga notificaciones en una sola consulta
- ✅ **No N+1:** Evita consultas múltiples por cada registro
- ✅ **Propiedades calculadas:** No requieren consultas adicionales

### **Mantenibilidad**
- ✅ **Código limpio:** Lógica encapsulada en propiedades del modelo
- ✅ **Reutilizable:** Propiedades disponibles en cualquier vista
- ✅ **Separación de responsabilidades:** Lógica en modelo, presentación en vista

### **Usabilidad**
- ✅ **Visual:** Badges de colores facilitan identificación rápida
- ✅ **Informativo:** Estados claros y descriptivos
- ✅ **Consistente:** Usa estilos Bootstrap estándar

---

## 🧪 Casos de Prueba

### Caso 1: Registro con Notificación Pendiente
- **Datos:** Registro aprobado con `FechaVencimiento` futura
- **Esperado:** Badge amarillo "Pendiente"
- **Verificar:** El estado se actualiza a "Ya Actualizado" al aprobar jornada posterior

### Caso 2: Registro con Notificación Ya Actualizada
- **Datos:** Registro aprobado + existe registro posterior del mismo curso
- **Esperado:** Badge verde "Ya Actualizado"
- **Verificar:** Este es el resultado de la funcionalidad implementada anteriormente

### Caso 3: Registro sin Vencimiento
- **Datos:** Registro aprobado con `FechaVencimiento = null`
- **Esperado:** 
  - Columna Vencimiento: "Sin vencimiento" (gris)
  - Columna Estado: "Sin notificación" (gris cursiva)

### Caso 4: Registro sin Notificación Asociada
- **Datos:** Registro que aún no tiene `NotificacionVencimiento` creada
- **Esperado:** "Sin notificación" (gris cursiva)
- **Nota:** La notificación se crea al acceder a la vista de NotificacionesVencimientos

### Caso 5: Múltiples Registros del Mismo Curso
- **Datos:** Capacitado con 3 registros del Curso A en diferentes fechas
- **Esperado:**
  - Registro más reciente: puede tener "Pendiente" si no está vencido
  - Registros anteriores: deben mostrar "Ya Actualizado" (verde)

---

## 🔗 Integración con Funcionalidad Anterior

Esta implementación complementa perfectamente la funcionalidad de actualización automática de notificaciones:

1. **Al aprobar un registro:** Sistema marca notificaciones antiguas como "Ya Actualizado"
2. **En vista de capacitado:** Usuario ve visualmente cuáles notificaciones fueron actualizadas
3. **Feedback inmediato:** El badge verde confirma que el sistema funcionó correctamente

---

## 📝 Notas Adicionales

### Consideraciones de Seguridad
- Las columnas solo se muestran a usuarios autenticados
- Respeta el mismo control de acceso que el resto de la vista

### Consideraciones de Performance
- Si un capacitado tiene muchos registros (>100), considerar paginación
- El eager loading actual es eficiente para casos normales

### Futuras Mejoras Sugeridas
1. **Tooltip:** Agregar tooltip con fecha de la notificación
2. **Ordenamiento:** Permitir ordenar tabla por estado de notificación
3. **Filtro:** Agregar filtro para ver solo registros con notificaciones pendientes
4. **Fecha de notificación:** Mostrar cuándo se envió la notificación
5. **Link directo:** Agregar enlace a la notificación específica

---

## 🚀 Resultado Final

La vista de detalles del capacitado ahora proporciona:
- ✅ Visibilidad completa del estado de notificaciones
- ✅ Identificación visual rápida mediante colores
- ✅ Información sobre vencimientos de certificaciones
- ✅ Confirmación visual del funcionamiento del sistema automático
- ✅ Mejor experiencia de usuario para gestión de notificaciones

---

## 📸 Ejemplo Visual

```
╔════════════════════════════════════════════════════════════════════════╗
║ REGISTROS DE CAPACITACIÓN                                             ║
╠═══╦══════════════╦══════╦══════╦═════════╦════════════╦═══════════════╣
║   ║ Jornada      ║ N.P. ║ Nota ║ Aprobado║ Vencimiento║ Estado Notif. ║
╠═══╬══════════════╬══════╬══════╬═════════╬════════════╬═══════════════╣
║ ✓ ║ Curso A 2025 ║  0   ║  85  ║ Aprobado║ 01/01/2026 ║ [YA ACTUALI-  ║
║   ║              ║      ║      ║         ║            ║  ZADO] 🟢     ║
╠═══╬══════════════╬══════╬══════╬═════════╬════════════╬═══════════════╣
║ ✓ ║ Curso A 2024 ║  0   ║  80  ║ Aprobado║ 01/01/2025 ║ [PENDIENTE]🟡 ║
╠═══╬══════════════╬══════╬══════╬═════════╬════════════╬═══════════════╣
║ ✓ ║ Curso B 2024 ║  0   ║  90  ║ Aprobado║ Sin venc.  ║ Sin notific.  ║
╚═══╩══════════════╩══════╩══════╩═════════╩════════════╩═══════════════╝
```

---

## ✨ Conclusión

La implementación exitosa de esta funcionalidad permite a los usuarios visualizar de forma clara y rápida el estado de las notificaciones de vencimiento asociadas a cada registro de capacitación, complementando perfectamente el sistema automático de actualización de notificaciones implementado previamente.
