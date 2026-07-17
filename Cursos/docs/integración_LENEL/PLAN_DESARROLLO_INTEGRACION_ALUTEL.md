# Plan de desarrollo: integración Alutel y retiro operativo de OVAL

> Plan operativo basado en `ANALISIS_REEMPLAZO_OVAL_ALUTEL.md` y en el contrato `PUT Cardholder Safety Card.md`.
>
> Versión inicial: 2026-07-17.  
> Estado: listo para iniciar las fases no condicionadas.

---

## 1. Objetivo

Implementar la integración de `Capacitaciones CSL` con `PUT /Cardholder/SafetyCards`, retirar completamente la operación OVAL de la interfaz y conservar intactos sus datos históricos.

El plan permite comenzar antes de resolver todas las preguntas pendientes. Cada decisión abierta se asocia a un gate que impide avanzar únicamente sobre la parte del sistema afectada.

---

## 2. Documentos de referencia

- [`ANALISIS_REEMPLAZO_OVAL_ALUTEL.md`](./ANALISIS_REEMPLAZO_OVAL_ALUTEL.md): alcance, decisiones, riesgos y preguntas abiertas.
- [`PUT Cardholder Safety Card.md`](./PUT%20Cardholder%20Safety%20Card.md): contrato entregado por el proveedor.
- [`INTEGRACION_LENEL_REEMPLAZO_OVAL.md`](./INTEGRACION_LENEL_REEMPLAZO_OVAL.md): contrato OVAL actual usado como referencia histórica.

Si una decisión del análisis cambia, este plan debe actualizarse antes de implementar la fase afectada.

---

## 3. Decisiones confirmadas

| Tema | Decisión |
| --- | --- |
| Reemplazo | `PUT /Cardholder/SafetyCards` reemplaza completamente OVAL. |
| Datos no contemplados | No se enviarán foto, resultado `APR/REC`, empresa ni otros datos OVAL; no está previsto otro endpoint. |
| Tarjeta Verde | Curso `TV - Tarjeta Verde`, `CursoID = 1`, usando `RegistroCapacitacion.FechaVencimiento`. |
| Tarjeta Azul | Curso `TA - Tarjeta Azul`, `CursoID = 3`, usando `RegistroCapacitacion.FechaVencimiento`. |
| Refresh | Curso `RF - Refresh`, `CursoID = 2`, usando `RegistroCapacitacion.FechaVencimiento`. |
| Habilitación por curso | `Curso.TipoVigenciaAlutel?`: `null` deshabilita; Verde/Azul/Refresh habilitan y determinan el campo. No se reutiliza `PermiteEnviosOVAL`. |
| Alcance de configuración | La configuración vive en `Curso`; las jornadas la derivan de su curso y no almacenan otra copia inicialmente. |
| Visibilidad | Las views muestran “Envío LENEL” solo con Alutel habilitado, curso configurado y usuario autorizado; el servidor repite todas las validaciones. |
| Payload | Se enviará únicamente la propiedad de la vigencia que se desea actualizar. |
| Campos omitidos | Alutel conserva sus valores existentes. No se enviarán como `null` ni como cadena vacía. |
| OVAL histórico | Se conservan columnas, valores, enum y metadatos necesarios para interpretarlos. |
| OVAL operativo | Desaparecerá completamente de la interfaz y sus rutas de escritura quedarán deshabilitadas. |
| Estados | Alutel tendrá estados y auditoría independientes de `EnvioOVALEstado`. |
| Histórico | No se reprocesará automáticamente. Una eventual carga será un trabajo separado. |

---

## 4. Alcance

### Incluido

- Retiro de navegación, pantallas, botones, reportes y configuración editable OVAL.
- Deshabilitación de rutas que actualmente realizan envíos OVAL.
- Protección de todos los valores OVAL históricos durante altas y ediciones.
- Configuración Alutel separada y segura.
- OAuth 2.0 Client Credentials y caché de token.
- Cliente HTTP para `PUT /Cardholder/SafetyCards`.
- Mapeo curso–tarjeta y serialización parcial del payload.
- Configuración LENEL por curso y visibilidad condicional en las views correspondientes.
- Modelo de operación/intentos Alutel y migración EF aditiva.
- Validación estricta de respuestas y estados de error.
- Orquestación inicial con un item por request.
- Infraestructura de pruebas automatizadas.
- Spike y pruebas de contrato en Staging.
- Interfaz operativa Alutel una vez resuelto su flujo de usuario.
- Despliegue controlado y canario en Producción.

### Fuera de alcance inicial

- Carga histórica de capacitaciones.
- Reutilización de columnas o estados OVAL para Alutel.
- Eliminación de columnas, enum o migraciones históricas OVAL.
- Reintentos automáticos ante timeouts o resultados indeterminados.
- Lotes con documentos duplicados.
- Eliminación inmediata de todo el código legacy antes de terminar la contingencia.
- Refactor general de la aplicación MVC o incorporación de un contenedor de dependencias como objetivo independiente.

---

## 5. Restricciones defensivas del desarrollo inicial

Hasta que se superen los gates correspondientes:

- Usar mocks o Staging; nunca Producción.
- Mantener la integración Alutel deshabilitada mediante configuración en Producción.
- Enviar un único item por request.
- Enviar una sola propiedad de vigencia por item.
- Rechazar localmente cursos con `TipoVigenciaAlutel = null`, fechas nulas y documentos vacíos.
- No incluir dos items con el mismo `documentNumber`.
- No ejecutar carga histórica.
- No reintentar automáticamente un timeout o resultado indeterminado.
- Renovar el token y repetir una sola vez únicamente ante `401`.
- No modificar datos `EnvioOVAL*`.
- No registrar secretos, tokens, fotos ni payloads completos con datos personales.

---

## 6. Arquitectura objetivo

```text
Acción del operador o proceso autorizado
                |
                v
Elegibilidad y mapeo curso -> tarjeta
                |
                v
Creación/reclamo de operación Alutel
                |
                v
Token provider OAuth -> cliente SafetyCards
                |
                v
Validación estricta de respuesta
                |
                v
Persistencia de intento y estado operativo

Historial OVAL -----------------> permanece separado y de solo lectura
```

### Componentes previstos

| Componente | Responsabilidad |
| --- | --- |
| `IAlutelConfiguration` | Exponer configuración tipada y validada. |
| `IAlutelTokenProvider` | Obtener, cachear e invalidar el token OAuth. |
| `IAlutelSafetyCardsClient` | Ejecutar el `PUT` y devolver un resultado técnico tipado. |
| `IAlutelMappingService` | Mapear curso y vencimiento a una única propiedad del request. |
| `IAlutelEligibilityPolicy` | Aplicar las reglas de negocio de selección. |
| `IAlutelIntegrationService` | Orquestar operación, intento, envío y persistencia del resultado. |
| `OperacionIntegracionAlutel` | Representar el trabajo lógico y su estado actual. |
| `IntentoIntegracionAlutel` | Auditoría inmutable de cada intento HTTP. |

Las interfaces permiten probar la integración sin IIS, `HttpContext.Current` ni llamadas reales al proveedor.

---

## 7. Gates de avance

### Gate 0 — comienzo

No tiene dependencias funcionales. Habilita:

- Infraestructura de pruebas.
- DTOs, configuración y cliente con mocks.
- Token provider.
- Modelo de auditoría y migración aditiva.
- Mapeo confirmado.
- Implementación del retiro OVAL en una rama de desarrollo.

### Gate 1 — conexión con el flujo real del operador

Debe estar resuelto:

- Si solo se envían capacitaciones aprobadas.
- Tratamiento de no aprobaciones, revocaciones y correcciones.
- Selección cuando existen varios registros del mismo curso y persona.
- Tratamiento de cursos sin vigencia.
- Pantalla y conjunto de registros desde los que actuará el operador.
- Comportamiento visual cuando el curso está habilitado pero no existen registros elegibles: ocultar o mostrar deshabilitado con motivo.
- Roles para enviar y consultar resultados.
- Normalización de `documentNumber`.
- Si un OVAL histórico `Aceptado` continúa bloqueando la edición o eliminación de calificaciones.
- Qué ocurre con la auditoría Alutel si se elimina un `RegistroCapacitacion`.

### Gate 2 — lotes y reintentos automáticos

Debe estar confirmado o validado:

- Significado exacto de `successfulProcessed`.
- Invariantes entre contadores y `failedDocuments`.
- Duplicados, resultados parciales e idempotencia.
- Tamaño máximo de lote y request.
- Rate limit y códigos adicionales.
- Reconciliación de timeouts y resultados indeterminados.
- Volumen esperado y necesidad de job/cola.
- Política de reintentos y backoff.

### Gate 3 — Producción

Debe estar resuelto:

- Credenciales definitivas y si son globales o por punto de servicio.
- Almacenamiento y rotación segura de `client_secret`.
- Conectividad, allowlists y TLS.
- Fecha/hora de corte.
- Tratamiento de pendientes y rechazados OVAL.
- Decisión sobre carga inicial.
- Pruebas de Staging y aceptación del negocio.
- Procedimiento de soporte, monitoreo y reversión.

### Gate 4 — carga histórica opcional

Requiere una autorización independiente con:

- Alcance y fecha mínima.
- Registros elegibles.
- Prevención de regresión de vigencias.
- Dry run revisable.
- Aprobación explícita del lote.
- Reconciliación posterior.

---

## 8. Fases y tareas

### Fase 0 — línea base y seguridad del cambio

**Puede comenzar ahora.**

#### Tareas

- [ ] `F0-01` Inventariar todas las referencias OVAL ejecutables y visibles, incluyendo controladores, vistas, scripts, reportes, configuración, mock y referencias de servicio.
- [ ] `F0-02` Registrar una línea base de cantidades por `EnvioOVALEstado` para comprobar que el despliegue no altera el histórico. No guardar documentos personales en el artefacto.
- [ ] `F0-03` Crear una matriz de rutas OVAL que deberán dejar de ejecutar acciones.
- [ ] `F0-04` Revisar credenciales presentes o históricas en archivos de configuración; rotarlas si alguna vez fueron válidas y excluir secretos nuevos del repositorio.
- [ ] `F0-05` Definir las feature flags `AlutelHabilitado` y `OvalLegacyHabilitado`. La segunda puede proteger código de contingencia, pero nunca volver a mostrar la interfaz OVAL.

#### Criterio de salida

- Existe un inventario revisado y una comparación posible del histórico antes/después.
- Ningún secreto nuevo se almacena en el repositorio.

---

### Fase 1 — infraestructura de pruebas

**Puede comenzar ahora.** La solución actual no contiene un proyecto de pruebas.

#### Tareas

- [ ] `F1-01` Crear un proyecto `Cursos.Tests` compatible con .NET Framework 4.8.1 y agregarlo a `Cursos.sln`.
- [ ] `F1-02` Elegir y documentar framework de pruebas y mocking compatible con el proyecto.
- [ ] `F1-03` Agregar abstracciones para configuración, reloj, token provider y transporte HTTP.
- [ ] `F1-04` Preparar un `HttpMessageHandler` o transporte simulado para probar requests y responses sin red.
- [ ] `F1-05` Configurar una forma repetible de ejecutar las pruebas localmente y en CI, si existe.

#### Criterio de salida

- Las pruebas pueden validar serialización, token, respuestas y estados sin IIS ni acceso a Alutel.
- La solución compila con el nuevo proyecto.

---

### Fase 2 — retiro seguro de la superficie OVAL

**Puede implementarse ahora, pero su despliegue debe coordinarse con el corte operativo.**

#### Tareas de interfaz

- [ ] `F2-01` Retirar el enlace “Panel de envíos OVAL” de `Views/Shared/_Layout.cshtml`.
- [ ] `F2-02` Retirar panel, pestaña, botones, modal, reporte y carga dinámica OVAL de `Views/Jornadas/Details.cshtml`.
- [ ] `F2-03` Retirar `IndexOVAL`, `IndexLogs_enviosOVAL` y sus enlaces de exportación/consulta.
- [ ] `F2-04` Retirar indicadores y acciones OVAL de los parciales de registros.
- [ ] `F2-05` Retirar de formularios y detalles los campos OVAL de curso, jornada, tipo de documento, configuración y registro de capacitación.
- [ ] `F2-06` Retirar la carga y ejecución de `Scripts/enviarOVAL.js`.
- [ ] `F2-07` Eliminar textos operativos, advertencias, recursos y enlaces que presenten OVAL al usuario.

#### Tareas de servidor

- [ ] `F2-08` Eliminar o hacer responder `404/410`, sin efectos secundarios, a las acciones de envío individual, por jornada y de rechazados.
- [ ] `F2-09` Deshabilitar rutas de reporte, panel, logs y parciales OVAL que ya no deban exponerse.
- [ ] `F2-10` Mantener temporalmente `EnvioOVALHelper` y referencias legacy solo para contingencia interna, inaccesibles desde la aplicación.
- [ ] `F2-11` Inicializar explícitamente los registros nuevos posteriores al corte con `EnvioOVALEstado = NoEnviar`.

#### Protección del histórico

- [ ] `F2-12` Reemplazar los POST que adjuntan entidades completas con `EntityState.Modified`: cargar la entidad existente y actualizar únicamente las propiedades editables.
- [ ] `F2-13` Preservar explícitamente `EnvioOVAL*`, `PermiteEnviosOVAL`, `TipoDocumentoOVAL` y vínculos legacy al editar registros, cursos, jornadas y tipos de documento.
- [ ] `F2-14` Resolver el Gate 1 sobre el bloqueo actual de calificaciones cuando `EnvioOVALEstado == Aceptado`; no dejar una restricción invisible.

#### Criterio de salida

- No existe navegación, texto, botón, configuración editable ni reporte OVAL visible.
- Ninguna ruta antigua puede ejecutar un envío.
- Editar entidades no cambia ningún valor OVAL histórico.
- Los registros nuevos no quedan pendientes de OVAL.

---

### Fase 3 — modelo Alutel y migración aditiva

**Puede comenzar ahora.**

#### Diseño propuesto

`OperacionIntegracionAlutel`:

- Origen (`RegistroCapacitacionID` o referencia equivalente).
- Documento normalizado y snapshot mínimo necesario.
- Tipo de vigencia y fecha enviada.
- Estado actual.
- Fecha de creación/actualización.
- Usuario o proceso de origen.
- Control de concurrencia (`RowVersion` o equivalente).

`IntentoIntegracionAlutel`:

- Operación asociada.
- `BatchId`.
- Número de intento.
- Entorno/destino.
- Fecha de inicio/finalización.
- Código HTTP.
- Resultado técnico y mensaje sanitizado.
- Cantidades devueltas por Alutel.

Estados iniciales:

```text
Pendiente
EnProceso
Aceptado
RechazadoFuncional
ErrorTransitorio
ErrorPermanente
Indeterminado
Cancelado
```

#### Tareas

- [ ] `F3-01` Validar el modelo y la política de eliminación de su registro de origen.
- [ ] `F3-02` Crear entidades, enums, relaciones y `DbSet`.
- [ ] `F3-03` Agregar índices para registro, documento, estado y lote.
- [ ] `F3-04` Incorporar control de concurrencia para reclamar una operación una sola vez.
- [ ] `F3-05` Crear una migración EF exclusivamente aditiva.
- [ ] `F3-06` Probar la migración sobre una copia representativa y verificar que no modifica tablas o valores OVAL.
- [ ] `F3-07` Crear el enum `TipoVigenciaAlutel` y la propiedad nullable `Curso.TipoVigenciaAlutel`.
- [ ] `F3-08` Incluir en la migración de datos la asignación inicial: curso 1 = Verde, curso 2 = Refresh, curso 3 = Azul; los demás cursos = `null`.

#### Criterio de salida

- La migración puede aplicarse y revertirse en un entorno de prueba.
- Los datos OVAL antes y después son idénticos.
- Los cursos 1, 2 y 3 quedan configurados con la vigencia esperada y ningún otro curso queda habilitado accidentalmente.
- Dos procesos no pueden reclamar silenciosamente la misma operación.

---

### Fase 4 — configuración tipada y secretos

**Puede comenzar con abstracciones; el proveedor definitivo de secretos debe cerrarse antes de Producción.**

#### Tareas

- [ ] `F4-01` Crear configuración tipada para `BaseUrl`, `TenantId`, `Scope`, endpoint, timeout, tamaño de lote, feature flag y política de reintentos.
- [ ] `F4-02` Validar URLs, enteros y valores requeridos antes de ejecutar un envío.
- [ ] `F4-03` Separar `ClientSecret` de la tabla `Configuracion`, `PuntoServicio.Password` y pantallas administrativas.
- [ ] `F4-04` Implementar `IAlutelSecretProvider` para el mecanismo seguro elegido por infraestructura.
- [ ] `F4-05` Definir valores distintos para Desarrollo, Staging y Producción sin incluir secretos en transformaciones versionadas.
- [ ] `F4-06` Mantener `AlutelHabilitado=false` por defecto fuera de Staging hasta el canario.

#### Criterio de salida

- Una configuración incompleta falla antes de hacer la llamada.
- Tokens y secretos no aparecen en logs, excepciones visibles ni repositorio.

---

### Fase 5 — token provider OAuth

**Puede comenzar ahora con mocks.**

#### Tareas

- [ ] `F5-01` Implementar Client Credentials con body form-urlencoded.
- [ ] `F5-02` Parsear `access_token`, `expires_in` y `token_type` mediante DTO tipado.
- [ ] `F5-03` Cachear el token con margen previo a expiración.
- [ ] `F5-04` Sincronizar la renovación para evitar solicitudes paralelas de token.
- [ ] `F5-05` Invalidar y renovar una sola vez ante `401`.
- [ ] `F5-06` Evitar `ApplicationDbContext` y `HttpContext.Current` dentro del token provider.

#### Pruebas mínimas

- Token nuevo, reutilizado y expirado.
- Dos solicitudes concurrentes con un único refresh.
- Error OAuth y respuesta inválida.
- Segundo `401` sin bucle de reintentos.
- Secretos ausentes de logs.

#### Criterio de salida

- El token provider es determinista y seguro bajo concurrencia.

---

### Fase 6 — cliente y contrato SafetyCards

**Puede comenzar ahora con mocks.**

#### Tareas

- [ ] `F6-01` Crear DTOs de request con propiedades opcionales y omisión de valores nulos.
- [ ] `F6-02` Crear DTO de response para contadores y `failedDocuments`.
- [ ] `F6-03` Implementar cliente asincrónico con un `HttpClient` reutilizable.
- [ ] `F6-04` Ejecutar `PUT /Cardholder/SafetyCards` con Bearer token, `Content-Type` y `Accept` correctos.
- [ ] `F6-05` Formatear fechas exclusivamente como `yyyyMMdd` con cultura invariable.
- [ ] `F6-06` Validar que cada item contenga documento y exactamente una vigencia en la fase inicial.
- [ ] `F6-07` Validar contadores, subconjunto de fallidos y ausencia de duplicados antes de declarar éxito.
- [ ] `F6-08` Clasificar `400`, `401`, `403`, `429`, `5xx`, timeout, JSON inválido y respuesta inconsistente.
- [ ] `F6-09` Representar como `Indeterminado` cualquier caso en que no pueda demostrarse si el proveedor procesó la solicitud.

#### Criterio de salida

- Los tests inspeccionan el JSON y demuestran que solo se serializa la vigencia correspondiente.
- Una respuesta incoherente nunca produce un estado aceptado.
- No existe reintento automático de resultados indeterminados.

---

### Fase 7 — mapeo y validación de entrada

**Puede comenzar ahora.**

#### Tareas

- [ ] `F7-01` Mapear el request desde `Curso.TipoVigenciaAlutel`; los IDs 1/2/3 solo se utilizarán para inicializar la migración, no durante la operación normal.
- [ ] `F7-02` Tomar la fecha desde `RegistroCapacitacion.FechaVencimiento`, sin recalcularla en el cliente.
- [ ] `F7-03` Construir un request que omita las otras dos propiedades.
- [ ] `F7-04` Rechazar curso con `TipoVigenciaAlutel = null`, fecha nula, documento vacío y fecha fuera del formato esperado.
- [ ] `F7-05` Incorporar la normalización de documento cuando se supere el Gate 1.
- [ ] `F7-06` Agregar pruebas que demuestren que cambiar la configuración del curso cambia simultáneamente la elegibilidad y el campo Alutel generado.

#### Criterio de salida

- Existen pruebas para los tres cursos, curso desconocido, fecha nula y año bisiesto.
- Un curso con `TipoVigenciaAlutel = null` nunca genera un request.
- Ningún item contiene más de una vigencia en el flujo inicial.

---

### Fase 8 — orquestación inicial desacoplada

**Puede comenzar sin conectarse todavía a la interfaz real.**

#### Tareas

- [ ] `F8-01` Recibir explícitamente registro y usuario/proceso; no leer identidad dentro del cliente HTTP.
- [ ] `F8-02` Validar entrada y crear o reclamar transaccionalmente una operación.
- [ ] `F8-03` Persistir `EnProceso` antes de invocar al proveedor.
- [ ] `F8-04` Ejecutar un request unitario y persistir el intento inmutable.
- [ ] `F8-05` Actualizar el estado lógico según el resultado validado.
- [ ] `F8-06` Definir recuperación de operaciones que queden `EnProceso` tras reinicio.
- [ ] `F8-07` Simular el caso “Alutel procesó, pero falló el guardado local”.

#### Criterio de salida

- Dos invocaciones concurrentes no generan dos envíos silenciosos.
- Todo intento deja auditoría, incluso si falla.
- El resultado indeterminado exige intervención o reconciliación.

---

### Fase 9 — spike de contrato en Staging

**Requiere credenciales, conectividad y documentos de prueba.**

#### Preparación

- [ ] `F9-01` Obtener credenciales de Staging por un canal seguro.
- [ ] `F9-02` Solicitar al proveedor documentos de prueba existentes para Verde, Azul y Refresh, con valores iniciales y resultado esperado.
- [ ] `F9-03` Confirmar si los documentos del ejemplo contractual son reales en Staging o ilustrativos.
- [ ] `F9-04` Definir cómo verificar o restaurar el estado después de cada prueba.

#### Ejecución

- [ ] `F9-05` Obtener y reutilizar un token.
- [ ] `F9-06` Actualizar una sola vigencia por request para cada tarjeta.
- [ ] `F9-07` Verificar directamente en Alutel que las otras vigencias no cambiaron.
- [ ] `F9-08` Probar reenvío del mismo valor y documentar idempotencia observada.
- [ ] `F9-09` Probar documento inexistente y respuesta funcional.
- [ ] `F9-10` Registrar status, headers relevantes, body sanitizado, duración y resultado observado.
- [ ] `F9-11` Actualizar el análisis y este plan con las respuestas verificadas; una observación de Staging no sustituye una garantía contractual cuando el riesgo lo requiera.

#### Criterio de salida

- Existe evidencia reproducible para las tres tarjetas.
- El proveedor o negocio valida el resultado observado.
- Las diferencias respecto del contrato quedan documentadas.

---

### Fase 10 — reglas funcionales y selección

**Requiere superar el Gate 1.**

#### Tareas

- [ ] `F10-01` Implementar `IAlutelEligibilityPolicy` con las decisiones aprobadas.
- [ ] `F10-02` Seleccionar determinísticamente el registro cuando existan varios del mismo curso y persona.
- [ ] `F10-03` Definir no aprobaciones, revocaciones y correcciones.
- [ ] `F10-04` Definir comportamiento para `FechaVencimiento = null`.
- [ ] `F10-05` Aplicar la normalización de documento confirmada.
- [ ] `F10-06` Resolver la edición/borrado de calificaciones históricamente bloqueadas por OVAL.
- [ ] `F10-07` Resolver eliminación de registros con operaciones Alutel existentes.

#### Criterio de salida

- Cada regla tiene ejemplos aprobados y pruebas automatizadas.
- Un operador puede saber por qué un registro es o no elegible.

---

### Fase 11 — interfaz operativa Alutel

**Requiere superar el Gate 1.**

#### Tareas

- [ ] `F11-01` Confirmar ubicación y alcance: individual, jornada, pendientes u otra selección.
- [ ] `F11-02` Implementar acciones mutantes exclusivamente como `POST`.
- [ ] `F11-03` Agregar antiforgery y rol específico para enviar.
- [ ] `F11-04` Mostrar resultados aceptados, rechazados, transitorios e indeterminados sin exponer información sensible.
- [ ] `F11-05` Permitir consulta y, si se autoriza, reintento/reconciliación según rol.
- [ ] `F11-06` Evitar que la nueva interfaz reutilice nombres o estados OVAL.
- [ ] `F11-07` Construir una propiedad de view model/política de presentación para “Mostrar envío LENEL”; no comparar `CursoID` ni consultar `PermiteEnviosOVAL` directamente en Razor.
- [ ] `F11-08` Mostrar la opción solo si `AlutelHabilitado`, `Curso.TipoVigenciaAlutel.HasValue` y el usuario tiene rol; para acciones por registro exigir además `EsElegibleParaAlutel` y ausencia de una operación incompatible en curso.
- [ ] `F11-09` Repetir en el controlador/servicio las validaciones de feature flag, rol, tipo de vigencia y elegibilidad antes de crear la operación.
- [ ] `F11-10` Incorporar en las pantallas administrativas de curso un selector opcional `TipoVigenciaAlutel` y mostrar su valor en el detalle; no agregar un nuevo checkbox duplicado en jornada.

#### Criterio de salida

- El flujo completo puede ejecutarse con teclado/navegador sin acceder a una URL OVAL.
- Una petición `GET` nunca produce un envío.
- Usuarios sin rol no pueden iniciar ni reintentar operaciones.
- Cursos no configurados para Alutel no muestran opciones LENEL y tampoco pueden forzar un envío invocando directamente la ruta.

---

### Fase 12 — lotes, reintentos y ejecución en segundo plano

**Requiere superar el Gate 2.**

#### Tareas

- [ ] `F12-01` Configurar el tamaño de lote validado con el proveedor.
- [ ] `F12-02` Agrupar por entorno, credencial y destino.
- [ ] `F12-03` Impedir o consolidar duplicados conforme a la regla aprobada.
- [ ] `F12-04` Correlacionar resultados parciales solo si se cumplen las invariantes contractuales.
- [ ] `F12-05` Implementar reintentos limitados con backoff, jitter y `Retry-After` cuando sea seguro.
- [ ] `F12-06` Evaluar job/cola según volumen; no ejecutar lotes grandes dentro de una petición web si compromete IIS.
- [ ] `F12-07` Implementar recuperación y reconciliación de pendientes/indeterminados.

#### Criterio de salida

- Pruebas de concurrencia, duplicados y fallos parciales pasan.
- Ningún reintento puede ejecutarse indefinidamente.
- Los lotes grandes no dependen de mantener abierta la sesión del usuario si el volumen exige background processing.

---

### Fase 13 — preparación, corte y Producción

**Requiere superar el Gate 3.**

#### Tareas

- [ ] `F13-01` Definir fecha/hora de corte y responsables.
- [ ] `F13-02` Aprobar tratamiento de OVAL pendiente/rechazado y confirmar que no habrá reproceso implícito.
- [ ] `F13-03` Aplicar la migración aditiva con Alutel deshabilitado.
- [ ] `F13-04` Desplegar el retiro completo de la superficie OVAL y comprobar rutas antiguas.
- [ ] `F13-05` Validar configuración, secretos, conectividad y autenticación sin enviar datos productivos.
- [ ] `F13-06` Ejecutar smoke tests y comparar la línea base OVAL.
- [ ] `F13-07` Habilitar Alutel para un caso canario autorizado.
- [ ] `F13-08` Reconciliar el canario directamente con Alutel.
- [ ] `F13-09` Ampliar el volumen gradualmente y monitorear métricas.

#### Criterio de salida

- Ningún dato OVAL histórico cambió.
- Las rutas OVAL son inaccesibles y Alutel procesa el canario esperado.
- Soporte dispone de diagnóstico y procedimiento de desactivación.

---

### Fase 14 — carga histórica opcional

**No forma parte del release inicial y requiere el Gate 4.**

#### Tareas

- [ ] `F14-01` Crear una selección separada del flujo normal.
- [ ] `F14-02` Generar dry run con documento enmascarado, curso, fecha propuesta y motivo de inclusión/exclusión.
- [ ] `F14-03` Impedir regresiones de vigencia.
- [ ] `F14-04` Obtener aprobación explícita antes de ejecutar.
- [ ] `F14-05` Procesar por lotes controlados y reconciliar cada uno.

---

### Fase 15 — limpieza legacy

**Solo después de cerrar la ventana de contingencia.**

#### Puede eliminarse

- `Helpers/EnvioOVAL/*`.
- DTOs y mock OVAL que no sean necesarios para interpretar historia.
- Referencias SOAP/WCF y endpoints legacy.
- `Scripts/enviarOVAL.js` y assets sin uso.
- Vistas OVAL ya retiradas.
- Entradas correspondientes de `Cursos.csproj` y `Web.config`.

#### Debe conservarse

- Columnas `EnvioOVAL*`.
- Enum `EstadosEnvioOVAL`.
- Metadatos mínimos para consultar datos históricos.
- Migraciones antiguas.

#### Criterio de salida

- La solución compila y las pruebas pasan sin dependencias de envío OVAL.
- Los datos históricos continúan legibles.

---

## 9. Estrategia de pruebas

### Unitarias

- Mapeo de los tres cursos.
- Habilitación/deshabilitación por `Curso.TipoVigenciaAlutel` y visibilidad calculada de las opciones LENEL.
- Formato `yyyyMMdd`, incluidos años bisiestos.
- Omisión de propiedades no actualizadas.
- Curso desconocido, fecha nula y documento inválido.
- Caché/renovación concurrente de token.
- Clasificación de respuestas y validación de invariantes.
- Políticas de elegibilidad y selección.

### Integración local

- Cliente contra transporte simulado.
- Persistencia de operación e intentos.
- Concurrencia sobre la misma operación.
- Fallo de guardado después de una respuesta aceptada.
- Recuperación después de reinicio.

### Regresión OVAL

- Edición de registro, curso, jornada y tipo de documento sin alterar campos OVAL.
- Altas nuevas con `NoEnviar`.
- Ausencia de UI, texto y rutas operativas OVAL.
- Decisión explícita sobre edición/borrado de calificaciones OVAL aceptadas.

### Contrato Staging

- Autenticación.
- Verde, Azul y Refresh por separado.
- Campos omitidos preservados.
- Documento inexistente.
- Reenvío del mismo valor.
- Contadores y fallidos.
- Límites y timeouts cuando sea seguro reproducirlos.

### Seguridad

- `POST` más antiforgery.
- Roles.
- Secretos/tokens ausentes de logs.
- Mensajes sanitizados y documentos enmascarados.

---

## 10. Inventario inicial de archivos afectados

### OVAL/UI y compatibilidad

- `Views/Shared/_Layout.cshtml`.
- `Views/Jornadas/Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, `IngresarCalificaciones.cshtml`.
- `Views/RegistrosCapacitacion/Edit.cshtml`, `IndexOVAL.cshtml`, `IndexLogs_enviosOVAL.cshtml`.
- `Views/Shared/_ListRegistrosCapacitacionOVALPartial.cshtml` y `_ListRegistrosCapacitacionPartial.cshtml`.
- `Views/Cursos/Create.cshtml`, `Edit.cshtml`, `Details.cshtml`.
- `Views/TiposDocumento/Create.cshtml`, `Edit.cshtml`, `Index.cshtml`.
- `Views/Configuracion/EditarItem.cshtml`.
- `Scripts/enviarOVAL.js`.
- `RegistrosCapacitacionController`, `JornadasController`, `CursosController`, `TiposDocumentoController`, `CapacitadosController`, `ConfiguracionController` y `RegistroOVALController`.
- `Helpers/EnvioOVAL/*` y `RegistroCapacitacionHelper`.

### Alutel/nuevo

- `Helpers/EnvioAlutel/*` o carpeta de servicios equivalente.
- DTOs, enums y entidades nuevas bajo `Models`.
- `Models/Curso.cs` y sus view models para `TipoVigenciaAlutel?`.
- Views de curso para configurar el tipo y views de jornada/registro para visibilidad condicional de “Envío LENEL”.
- `Models/IdentityModels.cs` para nuevos `DbSet`.
- Migración EF aditiva bajo `Migrations`.
- Controlador y vistas Alutel una vez aprobado el flujo.
- Configuración de entorno y proveedor de secretos.
- `Cursos.csproj` y `Cursos.sln`.
- Nuevo proyecto `Cursos.Tests`.

Este inventario debe verificarse nuevamente con `rg` antes de cerrar Fase 2 y Fase 15.

---

## 11. Observabilidad mínima

Por operación/lote:

- Identificador interno y `BatchId`.
- Entorno y destino.
- Tipo de vigencia, sin payload completo.
- Estado y duración.
- Código HTTP.
- Cantidades informadas por Alutel.
- Correlation/request ID del proveedor, si existe.
- Número de intento.

Alertas mínimas:

- Autenticación fallida.
- Respuesta inconsistente.
- Operaciones `EnProceso` vencidas.
- Crecimiento de pendientes o indeterminados.
- Tasa elevada de rechazos.
- Indisponibilidad sostenida.

---

## 12. Despliegue y reversión

### Despliegue

1. Aplicar migración aditiva con Alutel deshabilitado.
2. Verificar aplicación y datos OVAL.
3. Validar configuración y autenticación.
4. Retirar/hacer inaccesible OVAL en la misma activación acordada.
5. Habilitar un único caso canario.
6. Reconciliar.
7. Aumentar gradualmente el alcance.

### Reversión

- Desactivar `AlutelHabilitado`.
- No reactivar automáticamente la interfaz OVAL.
- No revertir destructivamente la migración si ya contiene auditoría; conservar los datos y corregir con un despliegue posterior.
- Reconciliar primero todo resultado `Aceptado` o `Indeterminado`.
- Recordar que desactivar la aplicación no revierte fechas ya actualizadas en Alutel.

---

## 13. Definición de terminado

El reemplazo inicial se considera terminado cuando:

- [ ] OVAL no aparece en navegación, pantallas, botones, reportes ni configuración editable.
- [ ] Ninguna ruta OVAL puede producir efectos secundarios.
- [ ] Todos los valores OVAL históricos permanecen intactos.
- [ ] Registros nuevos no generan pendientes OVAL.
- [ ] Alutel usa autenticación segura y configuración por entorno.
- [ ] Cada curso actualiza exclusivamente su campo correspondiente.
- [ ] `TipoVigenciaAlutel = null` deshabilita el curso y elimina de sus views la opción de envío LENEL.
- [ ] La visibilidad LENEL depende también de feature flag y rol, y las mismas reglas se validan en servidor.
- [ ] Los campos omitidos no se serializan.
- [ ] Estados e intentos Alutel quedan auditados separadamente.
- [ ] Respuestas inconsistentes o ambiguas nunca se marcan como aceptadas.
- [ ] Las reglas funcionales del Gate 1 están aprobadas y probadas.
- [ ] Las pruebas unitarias, integración, regresión, seguridad y Staging pasan.
- [ ] El canario de Producción fue reconciliado.
- [ ] Existe procedimiento de soporte, monitoreo y desactivación.

La carga histórica y la eliminación final del código legacy son entregas posteriores y no condicionan el cierre del reemplazo inicial.

---

## 14. Registro de decisiones

Mantener esta tabla actualizada durante el desarrollo:

| Fecha | Decisión | Responsable | Fases afectadas |
| --- | --- | --- | --- |
| 2026-07-16 | Conservar OVAL como histórico y retirar completamente su interfaz. | Negocio/equipo | F2, F13, F15 |
| 2026-07-16 | Usar estados y auditoría Alutel independientes. | Arquitectura | F3–F13 |
| 2026-07-16 | Mapear cursos 1/3/2 a Verde/Azul/Refresh. | Negocio | F7 |
| 2026-07-17 | Enviar solo la vigencia modificada; las omitidas se conservan. | Proveedor | F6, F7, F9 |
| 2026-07-17 | Habilitar LENEL por curso mediante `TipoVigenciaAlutel?`; usar la misma configuración para mapeo y visibilidad de las views. | Negocio/arquitectura | F3, F7, F11 |
| Pendiente | Elegibilidad y selección de registros. | Negocio | F10, F11 |
| Pendiente | Contrato de idempotencia, lotes y reconciliación. | Proveedor | F12, F13 |
| Pendiente | Secreto y credenciales globales/por punto. | Infraestructura/proveedor | F4, F13 |
| Pendiente | Corte e histórico OVAL pendiente/rechazado. | Negocio/operación | F13, F14 |
