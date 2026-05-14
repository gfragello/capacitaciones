# Propuesta de mejora para certificados PDF

## Objetivo

Mejorar el aspecto profesional, la mantenibilidad y la robustez del certificado generado actualmente por `Helpers\CertificadoHelper.cs`, con especial atención al cambio de fuente y a la forma en que se despliega la información de cursos realizados.

## Estado actual

El certificado se genera en `CertificadoHelper.GenerarCertificado(Capacitado c)` usando PDFsharp 1.50.5147 sobre una página A4 vertical. El flujo actual es imperativo: se crea una página, se obtiene un `XGraphics`, se dibujan bordes, textos, firma y logo en coordenadas absolutas.

Los recursos principales usados por el certificado son:

- Fuente embebida `NotoSansMono`, registrada mediante `FontResolver` y `FontHelper`.
- Firma en `images\certificados\firma-alejandro-lacruz.png`.
- Logo en `images\logos\CSL_logo_main_h.png`.
- PDFsharp como motor de dibujo y serialización PDF.

La elección de `NotoSansMono` resuelve un problema práctico: al ser monoespaciada permite alinear columnas mediante `string.Format`. Esto permite simular una tabla para cursos, fechas de realización y vencimiento.

## Hallazgos

### 1. La fuente está resolviendo un problema de layout

La tabla de cursos no es una tabla real. Actualmente se arma texto con anchos fijos:

```csharp
string.Format("   {0, -25} {1, -12} {2, -12}", "Curso", "Realizado", "Vencimiento")
```

Ese enfoque obliga a mantener una fuente monoespaciada. Si se cambia a una fuente proporcional más elegante, la alineación visual se rompe. Por eso el cambio de fuente debería ir acompañado de un cambio en el mecanismo de layout de la tabla.

### 2. Uso intensivo de coordenadas absolutas

La composición depende de valores fijos para `X`, `Y`, alto y ancho. Esto hace que el certificado sea frágil ante:

- Nombres largos de capacitados.
- Cursos con descripciones extensas.
- Cantidad variable de cursos vigentes.
- Cambios de fuente, tamaño o idioma.
- Logos o firmas con dimensiones distintas.

El código calcula `largoMaximo`, pero ese valor no se usa luego para ajustar columnas, truncar texto o reubicar contenido.

### 3. No hay separación clara entre datos, layout y estilos

El helper mezcla en el mismo método:

- Obtención de datos desde el modelo (`ObtenerRegistrosCapacitacionVigentes`).
- Decisiones de contenido.
- Estilos visuales.
- Posicionamiento.
- Renderizado PDF.

Esto dificulta probar el certificado, cambiar su diseño o generar variantes futuras.

### 4. Riesgo en el ciclo de vida del documento PDF

`GenerarCertificado` crea el documento dentro de un `using` y lo retorna:

```csharp
using (PdfDocument pdfDocument = new PdfDocument())
{
    ...
    return pdfDocument;
}
```

El controller luego intenta guardarlo dentro de otro `using`. Conceptualmente el helper está devolviendo un objeto que ya fue dispuesto al salir del bloque. Si hoy funciona, depende de detalles internos de PDFsharp y no de un contrato seguro. `ActaHelper`, en cambio, devuelve el `PdfDocument` sin envolverlo en `using`, que es el patrón correcto para este diseño.

### 5. Recursos gráficos y estilos poco parametrizados

Los colores, tamaños, coordenadas y rutas de imagen están codificados dentro del helper. Esto vuelve costoso ajustar el diseño o soportar más de una identidad visual.

### 6. La librería actual no es el principal problema

PDFsharp permite dibujar con precisión y ya está integrado en el proyecto. El problema principal no parece ser PDFsharp, sino el uso de dibujo manual para construir elementos que deberían ser componentes de layout: secciones, bloques de texto, tabla, firma, logo y pie.

## Propuesta visual

Para lograr un aspecto más profesional, propongo pasar de un certificado centrado en texto monoespaciado a una composición más editorial y documental:

- Mantener A4 vertical.
- Usar una fuente proporcional profesional para títulos y texto.
- Usar una tabla real para los cursos, con columnas medidas en puntos y no por cantidad de caracteres, priorizando el ancho disponible para el nombre del curso.
- Reducir el uso de bordes decorativos múltiples y reemplazarlos por un marco más sobrio.
- Dar más aire vertical entre secciones.
- Jerarquizar mejor el contenido: título, persona certificada, documento, cursos, texto de acreditación, fecha, firma y logo.
- Usar el logo como elemento institucional, no como relleno visual inferior.
- Aplicar una paleta institucional breve: verde, azul, gris neutro y negro.

Una estructura sugerida:

1. Encabezado institucional con logo pequeño o centrado.
2. Título principal: `CERTIFICADO`.
3. Nombre del capacitado como foco visual principal.
4. Documento en tamaño menor.
5. Texto introductorio.
6. Tabla de cursos con encabezado, filas alternadas suaves y columnas reales para `Curso` y `Vencimiento`.
7. Texto de cumplimiento académico.
8. Fecha.
9. Firma, nombre y cargo.

## Propuesta de fuente

El cambio de fuente debería hacerse en dos niveles:

### Fuente principal recomendada

Usar una fuente proporcional, legible y sobria para el certificado completo. Opciones recomendadas:

- `Noto Sans`: continuidad con la familia actual, buena cobertura de caracteres y licencia SIL Open Font License.
- `Source Sans 3`: aspecto profesional, muy legible, licencia SIL Open Font License.
- `IBM Plex Sans`: personalidad institucional, buena legibilidad, licencia SIL Open Font License.

Mi recomendación es `Noto Sans` si se quiere un cambio conservador y de bajo riesgo. Permite mantener coherencia con `NotoSansMono`, soporta bien acentos y caracteres españoles, y evita una ruptura visual fuerte.

### Fuente para datos tabulares

Hay dos caminos válidos:

- Recomendado: usar la misma fuente proporcional y dibujar columnas reales. Esto da el aspecto más profesional.
- Alternativo: conservar una monoespaciada solo para la tabla, pero con diseño más cuidado. Esta opción mantiene alineación simple, pero perpetúa una solución menos elegante.

Si se implementan columnas reales, ya no es necesario que el certificado dependa de una fuente monoespaciada.

## Propuesta técnica con PDFsharp

No considero necesario cambiar de librería en una primera etapa. PDFsharp ya está integrado, es gratuito para uso comercial y de código abierto bajo licencia MIT, y cubre las necesidades actuales si se mejora la capa de layout.

El rediseño podría implementarse con estas piezas:

### 1. Definir constantes de layout

Crear una estructura clara de márgenes y anchos:

```csharp
const double PageMargin = 42;
const double ContentWidth = 595 - (PageMargin * 2);
const double TableHeaderHeight = 24;
const double TableRowHeight = 28;
```

Esto evitaría coordenadas dispersas y facilitaría ajustes.

### 2. Crear métodos de renderizado por sección

Dividir el renderizado en métodos más expresivos:

- `DibujarMarcoInstitucional`.
- `DibujarEncabezado`.
- `DibujarDatosCapacitado`.
- `DibujarTablaCursos`.
- `DibujarTextoAcreditacion`.
- `DibujarFirma`.
- `DibujarLogo`.

Cada método debería recibir una posición inicial y devolver la nueva posición `Y`. Ese patrón ya aparece parcialmente en `ActaHelper`, por lo que encaja con el estilo existente del proyecto.

### 3. Dibujar una tabla real

La tabla debería usar rectángulos y columnas con ancho fijo o relativo:

```csharp
var columnas = new[]
{
    new ColumnaCertificado("Curso", 380),
    new ColumnaCertificado("Vencimiento", 120)
};
```

Cada celda se dibuja dentro de un `XRect`, con padding interno y alineación independiente. Así se puede usar una fuente proporcional sin perder alineación.

### 4. Resolver textos largos

Para cursos largos se debería definir una política explícita:

- Ajustar a dos líneas dentro de la celda cuando sea posible.
- Reducir el tamaño de fuente dentro de un rango mínimo aceptable.
- Truncar con puntos suspensivos solo si el texto sigue excediendo.
- Si hay demasiados cursos, generar una segunda página o anexar detalle.

La política recomendada para certificados es permitir hasta dos líneas por curso y pasar a una segunda página si la cantidad de cursos supera el espacio disponible.

### 5. Corregir el ciclo de vida del PDF

`GenerarCertificado` debería crear y devolver el `PdfDocument` sin `using`, dejando que el controller lo disponga como ya hace hoy:

```csharp
var pdfDocument = new PdfDocument();
...
return pdfDocument;
```

### 6. Centralizar estilos

Crear una clase o estructura interna para estilos:

```csharp
private static class CertificadoStyles
{
    public static readonly XColor VerdeInstitucional = XColor.FromArgb(0, 163, 108);
    public static readonly XColor AzulInstitucional = XColor.FromArgb(0, 112, 192);
    public static readonly XColor GrisTexto = XColor.FromArgb(55, 65, 81);
}
```

Esto facilita iterar sobre el diseño sin recorrer todo el helper.

## Alternativa con MigraDoc

Si se quiere ir un paso más allá en estructura documental, propongo evaluar MigraDoc, del mismo ecosistema de PDFsharp. MigraDoc permite trabajar con secciones, párrafos, estilos y tablas de forma más declarativa.

Ventajas:

- Tablas reales nativas.
- Mejor manejo de saltos de página.
- Separación más natural entre contenido y presentación.
- Licencia MIT, apta para uso comercial y proyectos de código abierto.
- Misma familia tecnológica que PDFsharp, por lo que el cambio conceptual es menor que migrar a una herramienta completamente distinta.

Desventajas:

- Requiere agregar dependencia y aprender su modelo de documento.
- Para diseños muy gráficos o certificados con composición precisa, puede ser necesario combinar MigraDoc con PDFsharp.
- El rediseño completo llevaría más tiempo que una mejora incremental con PDFsharp puro.

Mi recomendación: no migrar de entrada. Primero corregir el layout con PDFsharp. Evaluar MigraDoc solo si se confirma que habrá más documentos con tablas, secciones variables o paginación compleja.

## Librerías que no recomendaría para este caso

- `iText`: técnicamente potente, pero su modelo AGPL/comercial no cumple bien con el objetivo de una opción gratuita simple para uso comercial cerrado.
- `QuestPDF`: es excelente para layout moderno, pero su licencia actual no es una opción universalmente gratuita para cualquier uso comercial; depende del tamaño/condición de la organización.
- Motores HTML a PDF basados en navegadores o `wkhtmltopdf`: pueden ser útiles para documentos desde HTML, pero agregan dependencia operativa y no parecen necesarios para este certificado.

## Plan de implementación sugerido

### Etapa 1: Mejora segura sobre PDFsharp

1. Corregir el `using` de `PdfDocument` en `GenerarCertificado`.
2. Agregar nueva fuente proporcional embebida, por ejemplo `Noto Sans`.
3. Extender `FontResolver` y `FontHelper` para resolver regular y bold de la nueva fuente.
4. Reemplazar `string.Format` para cursos por una tabla dibujada con columnas reales.
5. Centralizar márgenes, colores, tamaños y rutas de recursos.
6. Ajustar el diseño visual con una composición más sobria.
7. Probar certificados con nombres largos, cursos largos, sin vencimiento y varios cursos vigentes.

### Etapa 2: Robustez de contenido variable

1. Implementar medición de texto y ajuste por cantidad de líneas.
2. Definir máximo de filas por página.
3. Agregar segunda página cuando los cursos no entren.
4. Agregar pruebas/manual checklist con casos borde.

### Etapa 3: Evaluación de MigraDoc

Evaluar MigraDoc solo si luego de la etapa 1 aparecen necesidades como:

- Certificados multipágina frecuentes.
- Variantes de diseño por cliente o curso.
- Tablas más complejas.
- Necesidad de generar más documentos institucionales con estructura similar.

## Resultado esperado

Con la mejora incremental sobre PDFsharp se puede lograr un certificado más profesional sin asumir una migración grande. El cambio clave no es solo reemplazar `NotoSansMono`, sino dejar de depender de una fuente monoespaciada para simular tablas.

La decisión recomendada es:

- Mantener PDFsharp por ahora.
- Cambiar a una fuente proporcional como `Noto Sans`.
- Dibujar la tabla de cursos como tabla real.
- Separar estilos y secciones para facilitar futuros ajustes.
- Considerar MigraDoc más adelante si el volumen de documentos o la complejidad de layout crecen.

## Decisiones a cerrar antes de ejecutar

Para pasar de propuesta a implementación conviene dejar explícitas las siguientes definiciones. Si estas decisiones quedan aprobadas, el plan de desarrollo puede ejecutarse sin ambigüedad funcional ni visual.

### 1. Alcance de la primera entrega

La primera implementación incluye:

- Mejora visual con composición sobria e institucional.
- Tabla real de cursos con columnas medidas en puntos y fuente proporcional.
- Políticas de truncado o multilínea para nombres de capacitado y nombres de curso.

Quedan fuera de esta entrega:

- Segunda página automática.
- Checklist formal de casos borde.

### 2. Fuente oficial del certificado

- `Noto Sans` como fuente principal del certificado.
- Uso de la misma fuente en toda la pieza, incluida la tabla de cursos.

### 3. Diseño objetivo aprobado

La composición del certificado es:

1. Encabezado institucional con logo.
2. Título principal `CERTIFICADO`.
3. Nombre del capacitado como foco visual.
4. Documento en un bloque secundario.
5. Tabla de cursos.
6. Texto de acreditación.
7. Fecha.
8. Firma.

### 4. Identidad visual

**Paleta de colores**

Se mantiene la identidad cromática institucional ya presente en el código y se agregan neutros de soporte para mejorar legibilidad y jerarquía visual:

- Verde institucional: `RGB(0, 163, 108)` — acento principal, marco y encabezado de tabla.
- Azul institucional: `RGB(0, 112, 192)` — uso exclusivo en separadores finos o detalles secundarios. No se usa en el marco principal.
- Negro texto: `RGB(30, 30, 30)` — cuerpo de texto general.
- Gris suave: `RGB(245, 247, 249)` — fondo alternado de filas de tabla.
- Blanco: fondo del certificado.

**Marco o borde**

Se reemplaza el triple borde decorativo actual por un único borde redondeado en verde institucional, con un grosor de 3 puntos y radio de esquina de 12 puntos. Esto simplifica el fondo visual y da mayor jerarquía al contenido.

**Logo**

- Archivo: `images/logos/CSL_logo_main_h.png` (ya existente).
- Posición: integrado al encabezado superior, centrado horizontalmente.
- Dimensiones: 140 × 84 puntos.
- Margen superior desde el borde de contenido: 12 puntos.

**Firma**

- Archivo: `images/certificados/firma-alejandro-lacruz.png` (ya existente).
- Posición: centrada horizontalmente en el bloque de cierre.
- Dimensiones: 157 × 79 puntos (sin cambio respecto al valor actual).

**Jerarquía tipográfica**

Todos los niveles usan `Noto Sans`:

| Nivel | Tamaño | Estilo | Uso |
|---|---|---|---|
| T1 | 30 pt | Bold | Título `CERTIFICADO` |
| T2 | 26 pt | Bold | Nombre del capacitado |
| T3 | 14 pt | Regular | Documento y bloque introductorio |
| T4 | 10 pt | Bold | Encabezado de tabla |
| T5 | 10 pt | Regular | Filas de tabla |
| T6 | 11 pt | Regular | Texto de acreditación |
| T7 | 12 pt | Regular | Fecha |
| T8 | 11 pt | Regular | Nombre y cargo del firmante |

### 5. Texto institucional definitivo

Se aprueban los siguientes textos fijos para el certificado:

- Título oficial: `CERTIFICADO`.
- Texto introductorio previo a la tabla: `Se certifica que ha realizado a satisfacción los siguientes cursos:`.
- Texto de acreditación posterior a la tabla: `Cumpliendo con los requisitos académicos y de asistencia exigidos para su aprobación.`
- Formato de fecha: `dd/MM/yyyy`.
- Nombre del firmante: `Alejandro Lacruz`.
- Cargo del firmante: `Coordinador General`.
- No se agrega leyenda institucional adicional en esta etapa.

### 6. Reglas de negocio para los datos mostrados

La tabla del certificado muestra solo `Curso` y `Vencimiento`, priorizando el espacio disponible para el nombre del curso.

Se mantienen los mismos criterios usados actualmente en `CertificadoHelper` y en `Capacitado.ObtenerRegistrosCapacitacionVigentes()`:

- Se muestran todos los registros de capacitación vigentes del capacitado.
- Un registro se considera vigente cuando `FechaVencimiento` no tiene valor o cuando `FechaVencimiento > DateTime.Now`.
- No se aplica deduplicación, consolidación ni reordenamiento adicional en el helper; se respeta el orden actual en que el modelo devuelve los registros.
- No se muestra la fecha de realización en esta versión del certificado.
- La columna `Vencimiento` usa `FechaVencimiento` cuando existe.
- Cuando `FechaVencimiento` no tiene valor, se muestra el texto `Sin vencimiento`.

### 7. Política para contenido largo o variable

Para esta primera implementación se definen las siguientes reglas de layout para casos no ideales:

- Nombre del capacitado: hasta 2 líneas.
- Nombre de curso: hasta 2 líneas por fila.
- Tamaño mínimo de fuente permitido: 9 pt.
- Se permite truncado con puntos suspensivos solo como último recurso, después de agotar el ajuste en dos líneas y la reducción hasta el tamaño mínimo.
- Si aun así el contenido no entra correctamente en una sola página, el caso queda fuera de alcance de esta iteración y se resuelve en la etapa de soporte multipágina.

### 8. Límite operativo por página

En esta iteración el certificado debe resolverse en una sola página.

- No se implementa segunda página automática.
- No se implementa anexo ni documento complementario.
- Si la combinación de cursos vigentes y textos largos supera el espacio disponible aun aplicando las reglas del punto 7, el caso se considera fuera de alcance de esta entrega y deberá abordarse en la siguiente etapa.

### 9. Decisión técnica de esta etapa

Dejar formalmente aprobada la decisión tecnológica para evitar reabrir discusión durante la ejecución:

- Mantener `PDFsharp` en esta etapa.
- No migrar a `MigraDoc` en la primera implementación.

### 10. Criterios de aceptación

La mejora se considera terminada cuando se cumplen todas las condiciones siguientes:

- El certificado se genera correctamente en PDF sin devolver un `PdfDocument` ya dispuesto.
- El certificado usa `Noto Sans` como fuente principal en lugar de `NotoSansMono`.
- El diseño implementado respeta la composición aprobada: encabezado con logo, título, nombre del capacitado, documento, tabla de cursos, texto de acreditación, fecha y firma.
- El marco visual se resuelve con un único borde redondeado verde y no con el triple borde decorativo actual.
- La tabla de cursos se renderiza como tabla real con columnas `Curso` y `Vencimiento`, sin usar `string.Format` para simular alineación.
- La columna `Curso` dispone de mayor ancho que en la versión actual y prioriza la legibilidad del nombre del curso.
- La columna `Vencimiento` muestra la fecha correspondiente o el texto `Sin vencimiento`, según el criterio definido.
- El certificado renderiza correctamente acentos y caracteres españoles.
- Un nombre largo de capacitado y un nombre largo de curso se resuelven dentro de las reglas aprobadas: hasta 2 líneas, tamaño mínimo de 9 pt y truncado con puntos suspensivos solo como último recurso.
- La implementación mantiene el alcance de una sola página; si un caso no entra en una sola página, no se considera resuelto por esta entrega.

### 11. Casos de prueba a cubrir

El set mínimo de validación para esta entrega es el siguiente:

- Capacitado con nombre largo: el nombre debe resolverse en hasta 2 líneas, respetando el tamaño mínimo aprobado.
- Curso con nombre largo: el nombre del curso debe resolverse en hasta 2 líneas dentro de la celda `Curso`.
- Un solo curso vigente: la tabla debe renderizar correctamente con una sola fila.
- Varios cursos vigentes: la tabla debe mantener alineación correcta entre `Curso` y `Vencimiento`.
- Curso sin vencimiento: la columna `Vencimiento` debe mostrar `Sin vencimiento`.
- Datos con acentos y caracteres españoles: el PDF debe renderizarlos correctamente.
- Caso con contenido suficiente para tensionar el alto disponible: si no entra en una sola página aun aplicando las reglas del punto 7, debe quedar identificado como fuera de alcance de esta iteración.

### 12. Activos y aprobadores

Se toman como definitivos los siguientes activos y responsables para esta etapa:

- Logo institucional: `images/logos/CSL_logo_main_h.png`.
- Firma institucional: `images/certificados/firma-alejandro-lacruz.png`.
- Aprobación visual: responsable solicitante del rediseño del certificado.
- Validación funcional: responsable funcional del módulo de capacitaciones.
- Aprobación institucional final: Coordinación General.

### 13. Exclusiones de la etapa

Queda explícitamente fuera de esta entrega:

- Segunda página automática.
- Anexo o documento complementario.
- Inclusión de la fecha de realización en la tabla.
- Variantes de diseño por cliente, curso o identidad visual.
- Internacionalización adicional.
- Nuevos tipos de documento institucional.
- Migración de `PDFsharp` a `MigraDoc` u otra librería.
- Refactorización general de helpers no necesaria para implementar esta mejora.

## Cierre del documento

Con las decisiones registradas en este documento, la propuesta queda cerrada como especificación de trabajo para la primera etapa de implementación del nuevo certificado PDF.

No deberían requerirse nuevas definiciones funcionales o visuales antes de comenzar el desarrollo, salvo que cambie el alcance aprobado.