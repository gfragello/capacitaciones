# Análisis de migración: reemplazo del envío OVAL por la API Alutel (`PUT /Cardholder/SafetyCards`)

> Documento de análisis técnico interno. Describe cómo sustituir la integración operativa OVAL por Alutel a partir del contrato enviado por el proveedor (`PUT Cardholder Safety Card.md`) y del código actual de `Capacitaciones CSL`.
>
> Análisis inicial: 2026-07-06.  
> Última actualización: 2026-07-17.

---

## 1. Objetivo del documento

Definir un diseño accionable para la nueva integración con Alutel: alcance funcional, impacto en el código, tratamiento de datos históricos, reglas de estado, seguridad, procesamiento por lotes, migración y preguntas pendientes, clasificadas según la fase en que deban resolverse.

Este documento no es todavía una especificación funcional definitiva. Ya están confirmados el alcance total del reemplazo, el significado de las fechas, el mapeo curso–tarjeta y el comportamiento de las propiedades omitidas. Continúan abiertas las reglas de selección del registro, elegibilidad y operación del proceso.

La ejecución se detalla en [`PLAN_DESARROLLO_INTEGRACION_ALUTEL.md`](./PLAN_DESARROLLO_INTEGRACION_ALUTEL.md). Las decisiones pendientes no impiden construir la base técnica ni ejecutar un spike controlado en Staging; actúan como gates para completar la orquestación, los lotes, los reintentos y la habilitación en Producción.

---

## 2. Resumen ejecutivo (TL;DR)

- El proveedor confirmó que `PUT {baseUrl}/Cardholder/SafetyCards` reemplaza completamente el envío OVAL, aunque el nuevo endpoint **no es funcionalmente equivalente**. OVAL registra una capacitación con resultado, empresa y foto; Alutel actualiza fechas de tarjetas de seguridad.
- **Decisión confirmada:** los datos de envíos OVAL ya realizados deben conservarse intactos y seguir siendo interpretables como historial. Las columnas, estados y tipos legacy no se renombran ni se eliminan durante esta migración.
- **Decisión confirmada:** la opción OVAL debe desaparecer completamente de la interfaz de usuario. No deben quedar menú, panel, botones, modales, exportaciones, enlaces a logs ni acciones invocables directamente por el usuario.
- La integración OVAL quedará deshabilitada y sus datos serán históricos de solo lectura. Las columnas `EnvioOVAL*` **no se reutilizarán** para representar envíos a Alutel.
- Alutel se implementará como una integración nueva, con elegibilidad, estados, intentos, configuración y trazabilidad propios.
- La autenticación cambia a OAuth 2.0 Client Credentials contra Microsoft Entra ID, con Bearer token y credenciales específicas por entorno.
- Alutel acepta lotes y devuelve un resumen agregado. La respuesta debe validarse de forma estricta antes de marcar elementos como procesados.
- **Mapeo confirmado:** Tarjeta Verde corresponde al curso `CursoID = 1`, Tarjeta Azul a `CursoID = 3` y Actualización de Seguridad/Refresh a `CursoID = 2`. Los tres campos reciben el `RegistroCapacitacion.FechaVencimiento` calculado desde la vigencia del curso y la fecha de la jornada.
- **Habilitación por curso:** se agregará `Curso.TipoVigenciaAlutel?`. `null` significa que el curso no genera envíos; un valor Verde, Azul o Refresh habilita la integración y determina el campo del request. `PermiteEnviosOVAL` no se reutilizará.
- **Visibilidad LENEL:** las views mostrarán la opción de envío únicamente para cursos configurados para Alutel, cuando la integración global esté habilitada y el usuario tenga el rol requerido. La misma regla se validará nuevamente en el servidor.
- **Actualización parcial confirmada:** cada envío incluirá únicamente la vigencia que se desea actualizar. Las propiedades de las otras tarjetas se omitirán y Alutel conservará sus valores existentes.
- Los bloqueadores funcionales restantes son determinar qué registros son elegibles y cómo elegir entre varias capacitaciones del mismo curso.

---

## 3. Estado actual: cómo funciona el envío OVAL

### 3.1 Componentes involucrados

| Componente | Ubicación | Rol actual |
| --- | --- | --- |
| `EnvioOVALHelper` | `Helpers/EnvioOVAL/EnvioOVALHelper.cs` | Singleton que ejecuta los envíos SOAP o REST y persiste su resultado. |
| `DatosTokenSeguridadOVAL` | `Helpers/EnvioOVAL/DatosTokenSeguridadOVAL.cs` | DTO con `cliente` y `api_key` devueltos por el login OVAL. |
| `RespuestaOVAL` | Modelo existente | Resultado interno: 0 = aceptado, 1 = rechazo funcional, -1 = error técnico. |
| `EstadosEnvioOVAL` | `Models/Enums/EstadosEnvioOVAL.cs` | `NoEnviar`, `PendienteEnvio`, `Aceptado`, `Rechazado`. |
| `RegistroCapacitacion` | `Models/RegistroCapacitacion.cs` | Conserva `EnvioOVALEstado`, fecha, usuario, mensaje y `ListoParaEnviarOVAL`. |
| `PuntoServicio` | `Models/PuntoServicio.cs` | URLs, usuario, contraseña y tipo de integración por punto de servicio. |
| `RegistrosCapacitacionController` / `JornadasController` | `Controllers/*` | Acciones de envío individual, por jornada y de rechazados. |
| `IndexOVAL` / `enviarOVAL.js` | `Views/RegistrosCapacitacion` y `Scripts` | Interfaz, botones y llamadas AJAX para operar OVAL. |
| `ConfiguracionHelper` | `Helpers/ConfiguracionHelper.cs` | Lee valores de la tabla `Configuracion` de la base de datos; **no lee `Web.config`**. |

### 3.2 Flujo REST actual

1. Se obtiene el `PuntoServicio` asociado al curso de la jornada.
2. Se solicita un token propietario con `POST` a `DireccionToken`, enviando `user` y `passwd` como `application/x-www-form-urlencoded`.
3. Se envía un registro individual mediante `POST {Direccion}/{DireccionRequest}`, con headers `cliente` y `api_key`.
4. El body incluye documento, nombre, contratista, `APR`/`REC`, fecha de inducción y foto Base64.
5. Un `HTTP 200` con `data=true` o `status=true` se guarda como `Aceptado`; un falso con `error` se guarda como `Rechazado`.
6. Ante un error técnico, el método retorna sin persistir un nuevo estado, fecha o mensaje de intento.

### 3.3 Limitaciones del flujo actual que no deben copiarse

- Las operaciones que modifican estado se exponen como `GET` y no tienen antiforgery.
- Los controladores permiten roles amplios; el permiso de enviar no está separado del permiso de consultar.
- `EnvioOVALHelper` es singleton y mantiene un `ApplicationDbContext` mutable, que no es seguro para concurrencia.
- Los envíos masivos son un bucle sincrónico de envíos individuales y se detienen ante el primer error técnico.
- No existe un estado persistido para `EnProceso`, `ErrorTransitorio` o `ResultadoIndeterminado`.

Estas limitaciones forman parte del legado y no son requisitos para Alutel.

---

## 4. Contrato recibido para Alutel

`PUT {baseUrl}/Cardholder/SafetyCards` recibe un array de objetos:

```json
[
  {
    "documentNumber": "4895623",
    "vtoTarjetaVerde": "20271231",
    "vtoTarjetaAzul": "20280131",
    "vtoActualizacionSeguridad": "20270115"
  }
]
```

- `documentNumber` es obligatorio.
- Las tres fechas son opcionales y deben tener formato estricto `yyyyMMdd`.
- La respuesta contiene `successfulProcessed`, `failedProcessed` y `failedDocuments[]`.
- La autenticación usa OAuth 2.0 Client Credentials contra Microsoft Entra ID.
- El contrato proporciona un único `tenant_id`, scopes diferentes para Staging y Producción, y aclara que las credenciales son específicas de cada entorno.
- Los códigos documentados son `200`, `400`, `401` y `503`.
- Luego de actualizar, Alutel ejecuta validaciones asociadas a las credenciales de seguridad. Debe confirmarse si esas validaciones son sincrónicas y qué garantiza exactamente `successfulProcessed`.

---

## 5. Decisiones de arquitectura confirmadas

### 5.1 Compatibilidad histórica OVAL

Se preservarán sin cambios de nombre ni significado:

- `EnvioOVALEstado`.
- `EnvioOVALFechaHora`.
- `EnvioOVALUsuario`.
- `EnvioOVALMensaje`.
- `EstadosEnvioOVAL`.
- Datos de `PuntoServicio` y tipos legacy necesarios para interpretar registros existentes.

Los valores existentes continuarán significando exclusivamente lo ocurrido con OVAL. No se sobrescribirán con respuestas de Alutel y no se resetearán durante el despliegue.

### 5.2 Retiro completo de OVAL de la interfaz

Debe desaparecer todo elemento visible u operable relacionado con OVAL:

- Entrada de menú y panel `IndexOVAL`.
- Botón de envío individual.
- Botón de envío por jornada.
- Acción “Enviar registros rechazados”.
- Modal y mensajes de envío OVAL.
- Exportación OVAL y enlaces a sus logs.
- Scripts y recursos de interfaz asociados.
- Campos configurables “Permite envíos OVAL” y “Tipo de Documento OVAL” cuando se presenten al usuario.

No alcanza con ocultar enlaces. Las rutas que realizan envíos deben quedar deshabilitadas para impedir su invocación directa:

- `RegistrosCapacitacion/EnviarDatosRegistroOVAL`.
- `RegistrosCapacitacion/EnviarDatosRegistosOVALRechazados`.
- `Jornadas/EnviarDatosOVAL`.
- Cualquier otra acción que invoque `EnviarDatosListaRegistros` o `EnviarDatosRegistroOVAL`.

La pantalla histórica `IndexOVAL` también dejará de estar disponible para usuarios. Si en el futuro se necesita auditoría del legado, deberá exponerse mediante un reporte administrativo separado, explícitamente de solo lectura y no accesible desde la navegación normal.

### 5.3 Integración Alutel separada

Alutel tendrá componentes y semántica propios:

- `EnvioAlutelHelper` o, preferiblemente, un servicio inyectable.
- `AlutelTokenProvider` para OAuth y caché de token.
- DTOs específicos de request y response.
- Regla `EsElegibleParaAlutel` independiente de `ListoParaEnviarOVAL`.
- Estados e intentos de integración propios.

No se agregará una rama Alutel dentro de `EnvioOVALHelper`, porque eso mantendría acopladas dos operaciones funcionalmente diferentes.

### 5.4 Política de corte

Debe definirse una fecha/hora de entrada en vigor:

```text
Antes del corte  -> historial OVAL, sin nuevos envíos.
Desde el corte   -> integración operativa Alutel.
```

Los registros OVAL históricos no se enviarán automáticamente a Alutel. Si el negocio necesita una carga inicial, se tratará como una migración controlada y separada, con reconciliación y protección contra regresión de fechas.

### 5.5 Reversibilidad

Puede conservarse temporalmente el código legacy detrás de una bandera de funcionalidad desactivada para facilitar una contingencia técnica. Esa bandera no debe volver a mostrar OVAL en la interfaz por accidente.

La reversión solo puede restaurar el enrutamiento. No deshace fechas ya actualizadas en Alutel y depende de que el servicio OVAL continúe disponible.

---

## 6. Brecha funcional crítica: no es un reemplazo 1:1

| Aspecto | OVAL | Alutel `SafetyCards` | Implicancia |
| --- | --- | --- | --- |
| Propósito | Registrar una capacitación | Actualizar vigencias de tarjetas | Cambia la semántica de la operación. |
| Foto | Base64 JPEG | No existe | Se deja de enviar; no está previsto otro endpoint por el momento. |
| Resultado | `APR` / `REC` | No existe | Definir si solo los aprobados generan actualizaciones. |
| Contratista | Se envía | No existe | Se deja de enviar; la trazabilidad permanecerá únicamente en CSL. |
| Tipo de inducción | `CAP_SEG` | No existe | El destino se determina por `CursoID`: 1 = Verde, 3 = Azul, 2 = Refresh. |
| Fechas | Fecha de la jornada | Vencimiento de tres tipos de capacitación | Cada campo toma `RegistroCapacitacion.FechaVencimiento` del curso correspondiente. |
| Respuesta | Una por registro, con mensaje | Resumen de lote | Exige correlación y validación estrictas. |
| Identificador | Documento más tipo | Solo documento | Puede haber ambigüedad entre tipos documentales. |
| Estado local | Estados OVAL históricos | Requiere estados Alutel | No deben compartir columnas. |

El mayor riesgo no es técnico sino funcional: actualizar una vigencia incorrecta puede afectar las validaciones de credenciales y, por extensión, el control de acceso.

---

## 7. Reglas de datos

### 7.1 Mapeo confirmado

| Campo Alutel | Origen confirmado | Curso |
| --- | --- | --- |
| `documentNumber` | `Capacitado.Documento` | No aplica; falta confirmar normalización y tipos admitidos. |
| `vtoTarjetaVerde` | `RegistroCapacitacion.FechaVencimiento` | `TV - Tarjeta Verde`, `CursoID = 1`. |
| `vtoTarjetaAzul` | `RegistroCapacitacion.FechaVencimiento` | `TA - Tarjeta Azul`, `CursoID = 3`. |
| `vtoActualizacionSeguridad` | `RegistroCapacitacion.FechaVencimiento` | `RF - Refresh`, `CursoID = 2`. |

Los tres parámetros representan vencimientos de registros de capacitación. En particular, `vtoActualizacionSeguridad` representa el vencimiento del registro del curso Refresh, no la fecha de realización de la jornada.

La cadena de cálculo es:

```text
Curso define la regla de vigencia
-> Jornada aporta la fecha de realización
-> RegistroCapacitacion almacena FechaVencimiento
-> Alutel recibe FechaVencimiento en formato yyyyMMdd
```

La implementación existente en `Jornada.ObtenerFechaVencimiento()` aplica estas reglas:

- Curso sin vigencia -> `FechaVencimiento = null`.
- Vigencia hasta fin de año -> 31 de diciembre del año de la jornada.
- Vigencia en años -> mismo día y mes de la jornada, sumando los años configurados.
- Jornada del 29 de febrero cuyo año de vencimiento no es bisiesto -> ajuste al 28 de febrero.

Participantes de jornadas diferentes del mismo curso pueden tener fechas de vencimiento distintas.

La habilitación y el tipo de vigencia no deben depender permanentemente de comparar `CursoID`. Se incorporará al curso una propiedad nullable:

```text
TipoVigenciaAlutel = null            -> no genera envíos LENEL
TipoVigenciaAlutel = TarjetaVerde    -> vtoTarjetaVerde
TipoVigenciaAlutel = TarjetaAzul     -> vtoTarjetaAzul
TipoVigenciaAlutel = Refresh         -> vtoActualizacionSeguridad
```

La migración inicial asignará Verde al curso 1, Refresh al curso 2 y Azul al curso 3. Los demás cursos quedarán en `null`. `PermiteEnviosOVAL` se conservará como dato legacy, pero no participará en la integración nueva.

La habilitación se define en `Curso` y las jornadas la heredan consultando su curso actual. No se copiará otro booleano a `Jornada`, salvo que el negocio solicite en el futuro una excepción por instancia.

### 7.2 Alcance del payload por evento — confirmado

Cuando una capacitación dispara una actualización, el request debe incluir únicamente el campo correspondiente a la vigencia que se desea modificar. Las otras dos propiedades se omiten del JSON.

El proveedor confirmó, a partir de sus pruebas previas, que el servicio actualiza los datos incluidos uno a uno y que la ausencia de las demás propiedades no elimina ni modifica sus vencimientos anteriores.

Ejemplo conceptual para una actualización de Tarjeta Azul:

```json
[
  {
    "documentNumber": "4895623",
    "vtoTarjetaAzul": "20280131"
  }
]
```

`vtoTarjetaVerde` y `vtoActualizacionSeguridad` no deben serializarse en ese item.

### 7.3 Campos opcionales

Las propiedades de fecha que no correspondan deben omitirse del JSON; no deben enviarse como `null` ni como cadena vacía salvo confirmación explícita del proveedor.

La implementación exigirá al menos una fecha por elemento, aunque el contrato marque las tres como opcionales. De esta forma nunca se enviará un item que contenga solamente `documentNumber`.

La omisión de una propiedad conserva su valor en Alutel. Continúa siendo conveniente consultar qué hacen `null` y cadena vacía como parte de las pruebas de contrato, aunque la aplicación no enviará esos valores. También falta confirmar si se permite retroceder una vigencia ya almacenada.

### 7.4 Elegibilidad

`ListoParaEnviarOVAL` no puede reutilizarse: considera calificados tanto los registros `Aprobado` como `NoAprobado` y depende de estados OVAL.

La regla Alutel debe definir, como mínimo:

- Estado de capacitación permitido.
- `Curso.TipoVigenciaAlutel` con valor; `null` excluye el registro.
- Existencia y validez de la fecha a enviar.
- Tipo de documento admitido.
- Fecha de corte de la integración.
- Tratamiento de correcciones, revocaciones y registros históricos.

### 7.5 Visibilidad de la opción LENEL

Las views no deben comparar `CursoID` ni reutilizar `PermiteEnviosOVAL`. Deben consumir una propiedad del view model o política de presentación calculada en el servidor.

La opción general “Envío LENEL” se mostrará únicamente cuando:

- `AlutelHabilitado == true`.
- `Curso.TipoVigenciaAlutel.HasValue`.
- El usuario tenga el rol autorizado.

Los botones por registro exigirán además que el registro cumpla `EsElegibleParaAlutel` y no tenga una operación incompatible en curso. Si una condición no se cumple, la opción debe ocultarse o mostrarse deshabilitada con un motivo definido por UX.

Estas condiciones son solo de presentación. El controlador o servicio debe repetir autorización, habilitación, tipo de vigencia y elegibilidad antes de crear una operación; ocultar un botón no constituye un control de seguridad.

### 7.6 Documentos duplicados

Durante la fase inicial no se incluirán dos items con el mismo `documentNumber` en un lote. Si la selección produce duplicados, el lote se rechazará durante la validación previa hasta definir la regla de selección o consolidación.

Un mapa simple `documentNumber -> RegistroCapacitacionID` es insuficiente porque una persona puede tener múltiples capacitaciones y fechas. La eventual combinación de varias actualizaciones en un único item se resolverá después de validar el contrato de duplicados y aprobar la regla interna correspondiente.

---

## 8. Autenticación y seguridad

### 8.1 OAuth 2.0 Client Credentials

- `POST https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token`.
- Body `application/x-www-form-urlencoded`: `grant_type`, `client_id`, `client_secret`, `scope`.
- Uso posterior: `Authorization: Bearer <access_token>`.

### 8.2 Caché de token

El token se almacenará en memoria hasta poco antes de su expiración:

- Usar un margen de seguridad respecto de `expires_in`.
- Sincronizar la renovación para evitar solicitudes concurrentes de token.
- Ante un `401`, invalidar el token, renovarlo y reintentar una sola vez.
- Un segundo `401` o un `403` debe considerarse problema de credenciales/permisos, no un error transitorio ilimitadamente reintentable.

El proveedor de token no debe compartir un `ApplicationDbContext` ni depender del singleton OVAL.

### 8.3 Secretos

`client_secret` no debe almacenarse:

- En el repositorio.
- En comentarios de archivos de configuración.
- En la tabla `Configuracion` en texto claro.
- En `PuntoServicio.Password` sin protección adicional.
- En logs o mensajes visibles al usuario.

La estrategia preferida es un almacén seguro del entorno de despliegue. Si la infraestructura no lo permite, usar configuración protegida de ASP.NET y documentar cómo se distribuyen y rotan las claves.

### 8.4 TLS y red

La aplicación usa .NET Framework 4.8.1. La política TLS debe configurarse de forma consistente con el sistema operativo y la infraestructura, evitando cambios globales por request. También deben verificarse proxy, DNS, firewall y posibles allowlists para Microsoft Entra ID y ambos endpoints de Alutel.

---

## 9. Procesamiento por lotes y correlación

### 9.1 Construcción del lote

1. Seleccionar registros elegibles para Alutel.
2. Agrupar por entorno, destino y credenciales.
3. Mapear cada capacitación a la tarjeta correspondiente.
4. Consolidar duplicados por documento.
5. Validar documento y fechas antes de serializar.
6. Asignar un `BatchId` interno y persistir los intentos como `EnProceso`.
7. Enviar el lote configurado.

El tamaño de lote será configurable. El spike en Staging comenzará con un único item por request y solo se incrementará después de validar límites, duplicados, consistencia de la respuesta e idempotencia.

### 9.2 Validación de la respuesta

No se debe asumir que todo documento ausente en `failedDocuments` fue exitoso sin validar la coherencia de la respuesta.

Antes de actualizar estados deben cumplirse estas invariantes:

```text
successfulProcessed + failedProcessed == cantidad de items enviados
failedProcessed == cantidad de failedDocuments
failedDocuments es subconjunto de los documentos enviados
no existen documentos ambiguos o duplicados en el lote
```

Si todas se cumplen:

- Documento presente en `failedDocuments` -> `RechazadoFuncional`.
- Documento enviado y ausente de `failedDocuments` -> `Aceptado`.

Si alguna no se cumple, si el JSON es inválido o si la conexión se corta después de enviar la solicitud, el resultado será `Indeterminado`; ningún item debe marcarse como aceptado.

### 9.3 Clasificación de errores HTTP

| Resultado | Clasificación | Acción |
| --- | --- | --- |
| `200` coherente | Definitivo | Persistir aceptados y rechazados. |
| `200` inconsistente/JSON inválido | Indeterminado | No aceptar; alertar y reconciliar. |
| `400` | Error permanente de payload | No reintentar igual; corregir o aislar el item. |
| `401` | Token inválido/expirado | Renovar y reintentar una vez. |
| Segundo `401` / `403` | Configuración o permisos | Detener y alertar. |
| `429` | Rate limit, si aplica | Respetar `Retry-After` y aplicar backoff. |
| `503`, timeout, determinados `5xx` | Transitorio o indeterminado | Inicialmente no reintentar automáticamente si la solicitud pudo haber sido procesada; habilitar backoff solo tras confirmar idempotencia y reconciliación. |
| Código no documentado | Indeterminado hasta clasificar | Registrar sin marcar éxito. |

La idempotencia del endpoint debe confirmarse antes de automatizar reintentos ante resultados indeterminados. El único reintento automático previsto para el spike es renovar el token y repetir una vez ante `401`.

---

## 10. Modelo de estado y auditoría Alutel

Se recomienda un modelo aditivo con dos niveles, sin modificar ni migrar valores OVAL históricos:

- `OperacionIntegracionAlutel`: representa el trabajo lógico, su origen, documento, tarjeta, fecha y estado actual; incorpora control de concurrencia.
- `IntentoIntegracionAlutel`: registra de forma inmutable cada invocación HTTP, su `BatchId`, entorno, número de intento, fechas, código HTTP y mensaje sanitizado.

Separar operación e intentos evita perder la historia al reintentar y permite reclamar una operación una sola vez. Antes de crear la migración debe definirse qué ocurre con la auditoría si el `RegistroCapacitacion` de origen se elimina físicamente.

Estados sugeridos:

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

Esto permite diferenciar un rechazo de datos, una indisponibilidad temporal y un resultado que requiere reconciliación.

---

## 11. Impacto en el código

### 11.1 Nuevo

- `Helpers/EnvioAlutel/EnvioAlutelService.cs`.
- `AlutelTokenProvider` con caché sincronizada.
- DTO `SafetyCardUpdateItem`.
- DTO `SafetyCardsBatchResponse`.
- Modelos de operación e intentos Alutel.
- Enum de estados Alutel.
- Regla o servicio de elegibilidad y mapeo.
- Enum `TipoVigenciaAlutel` y propiedad nullable `Curso.TipoVigenciaAlutel`.
- Configuración tipada y validada al iniciar o usar la integración.
- Mecanismo de exclusión para evitar dos envíos concurrentes del mismo item.

### 11.2 A modificar

- Punto de disparo de la integración nueva, una vez definido si será manual restringido, automático o en segundo plano.
- Navegación, vistas y recursos para retirar OVAL.
- Formularios de curso, jornada y tipo de documento para no mostrar propiedades OVAL legacy.
- Vistas y view models de cursos, jornadas y registros para configurar `TipoVigenciaAlutel` y decidir la visibilidad de “Envío LENEL” sin comparar IDs en Razor.
- Acciones de edición de registro, curso, jornada y tipo de documento: cargar la entidad existente y actualizar solo campos permitidos. Si simplemente se quitan propiedades OVAL del formulario y se mantiene `EntityState.Modified`, sus valores pueden sobrescribirse con defaults.
- Flujo de calificaciones: decidir si el estado histórico OVAL `Aceptado` continúa bloqueando edición/borrado, porque hoy esa regla depende de un indicador que desaparecerá.
- Configuración de despliegue para URLs, tenant, scope y secretos Alutel.
- Proyecto `.csproj` para incluir los nuevos componentes.
- Base de datos mediante una migración **aditiva** para la auditoría Alutel.

### 11.3 A conservar como legado de solo lectura

- Columnas `EnvioOVAL*`.
- Enum `EstadosEnvioOVAL`.
- Clases y metadatos necesarios para leer datos históricos.
- Integración SOAP/REST legacy mientras dure la ventana de contingencia, deshabilitada por configuración.

### 11.4 A implementar y probar antes del corte; retirar o deshabilitar al activar Alutel

- Enlaces a `IndexOVAL`.
- Botones `btnEnvioRegistroOVAL`, `btnEnvioRegistrosOVALJornada` y `btnEnvioRegistrosOVALRechazados`.
- Carga y uso de `Scripts/enviarOVAL.js`.
- Acciones públicas de envío OVAL en los controladores.
- Edición visible de `PermiteEnviosOVAL` y `TipoDocumentoOVAL`.
- Cualquier texto operativo que invite al usuario a enviar o reprocesar OVAL.

Tras finalizar la ventana de contingencia podrán eliminarse el helper OVAL, las referencias SOAP y el mock local. Esa limpieza no incluirá las columnas ni los valores históricos.

---

## 12. Estrategia de configuración

`ConfiguracionHelper.GetValue` consulta la tabla `Configuracion`; no es un wrapper de `Web.config`. La implementación debe elegir conscientemente el origen de cada valor.

### Configuración no sensible

- `BaseUrl`.
- `TenantId`.
- `Scope`.
- `EndpointPath`.
- `TamanioLote`.
- `TimeoutSegundos`.
- Cantidad máxima de reintentos.
- Bandera de habilitación Alutel.
- Bandera legacy OVAL, desactivada.

Puede residir en configuración por entorno o, si se necesita edición dinámica, en la tabla `Configuracion` con validaciones y auditoría.

### Configuración sensible

- `ClientId` si la política de infraestructura lo considera sensible.
- `ClientSecret`.

Debe provenir de un almacén seguro o configuración protegida y no ser editable desde las pantallas genéricas de configuración.

No se recomienda extender `PuntoServicio` con secretos en texto claro. Si cada punto necesita credenciales distintas, se debe guardar una referencia a una credencial segura, no el secreto directamente.

---

## 13. Estrategia incremental de desarrollo y migración

No es necesario resolver todas las preguntas abiertas antes de iniciar. El trabajo debe avanzar por fases con gates explícitos:

1. **Construir la base técnica desacoplada:** configuración tipada, DTOs, token provider, cliente HTTP, mapeo confirmado, auditoría y pruebas con mocks.
2. **Proteger el legado OVAL:** conservar sus datos, retirar la interfaz y deshabilitar rutas sin sobrescribir valores históricos desde formularios existentes.
3. **Ejecutar un spike en Staging:** autenticación, actualización de una sola vigencia, parseo de respuesta y registro de evidencia con documentos de prueba.
4. **Cerrar reglas funcionales internas:** elegibilidad, selección entre múltiples capacitaciones, cursos sin vigencia, disparador, roles y tratamiento de errores.
5. **Completar la orquestación:** selección de registros, persistencia de intentos, concurrencia y experiencia del operador.
6. **Validar lotes y reintentos:** comenzar con lote unitario y sin reintentos automáticos —excepto renovar una vez ante `401`— hasta confirmar idempotencia, límites y reconciliación.
7. **Definir fecha de corte y alcance histórico:** no reprocesar registros OVAL por defecto.
8. **Realizar pruebas de aceptación y canario en Staging.**
9. **Desplegar deshabilitado en Producción**, aplicar la migración aditiva y verificar configuración.
10. **Habilitar un primer envío controlado** y reconciliar antes de ampliar el volumen.
11. **Retirar el código legacy** al finalizar la ventana de contingencia, manteniendo columnas y valores OVAL.

### Reversión

- Desactivar Alutel mediante configuración.
- No reactivar automáticamente la interfaz OVAL.
- Si se autoriza excepcionalmente volver a OVAL, hacerlo mediante una decisión operativa explícita y temporal.
- Reconciliar primero cualquier lote Alutel con resultado aceptado o indeterminado.

---

## 14. Casos de prueba mínimos

### Datos y reglas

- Aprobado elegible para cada tipo de tarjeta.
- No aprobado.
- Curso que no corresponde a ninguna tarjeta.
- Curso sin vencimiento.
- Fecha opcional omitida, nula y vacía.
- Documento con espacios, guiones, ceros iniciales y tipo extranjero.
- Fecha anterior a la ya existente.
- Varias capacitaciones para el mismo documento.
- Dos cursos que actualizan tarjetas diferentes del mismo documento.

### Respuesta y transporte

- Lote completamente exitoso.
- Errores parciales con contadores coherentes.
- Contadores inconsistentes.
- Documento fallido no incluido en el request.
- JSON inválido con `HTTP 200`.
- `400`, `401`, segundo `401`, `403`, `429`, `503`, timeout y `5xx`.
- Timeout después de que el proveedor pudo haber procesado la solicitud.
- Reintento idempotente.

### Persistencia y concurrencia

- Dos usuarios/procesos intentan enviar el mismo registro.
- Alutel acepta pero falla el guardado local.
- Reinicio de la aplicación con items `EnProceso`.
- El token expira o se renueva con solicitudes concurrentes.

### Compatibilidad y UI

- Los valores OVAL históricos no cambian tras el despliegue.
- No existe navegación, botón, modal ni texto operativo OVAL.
- Las rutas antiguas no ejecutan envíos aunque se invoquen directamente.
- Los campos legacy OVAL no aparecen en formularios de usuario.

---

## 15. Observabilidad y operación

Registrar por lote:

- `BatchId`.
- Entorno y destino.
- Cantidades preparadas, aceptadas, rechazadas, transitorias e indeterminadas.
- Duración y código HTTP.
- Identificador de correlación devuelto por el proveedor, si existe.

No registrar:

- Bearer tokens.
- `client_secret`.
- Fotos.
- Payload completo con datos personales.

Los documentos deben enmascararse en logs generales. El acceso a trazas con identificación completa debe estar restringido y auditado.

Definir alertas para:

- Errores de autenticación.
- Respuestas inconsistentes.
- Crecimiento de pendientes o indeterminados.
- Tasa elevada de rechazos.
- Indisponibilidad sostenida.

---

## 16. Riesgos

| Riesgo | Impacto | Mitigación |
| --- | --- | --- |
| Seleccionar un registro o vencimiento incorrecto | Alto | Aplicar el mapeo confirmado y definir reglas para múltiples capacitaciones. |
| Mezclar estados OVAL y Alutel | Alto | Estados e intentos separados; OVAL de solo lectura. |
| Marcar éxito desde una respuesta inconsistente | Alto | Validar invariantes; usar estado `Indeterminado`. |
| Duplicados con fechas diferentes | Alto | Rechazarlos en la validación inicial; consolidar solo después de aprobar una regla determinista. |
| Reprocesar histórico y reducir vigencias | Alto | Migración separada, dry run y reconciliación. |
| Secreto expuesto | Alto | Almacén seguro, rotación y ausencia de secretos en repo/BD/logs. |
| Acciones OVAL todavía invocables | Alto | Deshabilitar rutas, no limitarse a ocultar botones. |
| Opción LENEL visible para un curso no habilitado | Alto | Derivar visibilidad de `TipoVigenciaAlutel`, feature flag y rol; repetir todas las validaciones en servidor. |
| Ocultar campos OVAL y sobrescribirlos al editar | Alto | Cargar la entidad persistida y actualizar solo campos permitidos; pruebas de regresión del histórico. |
| Bloqueo invisible de calificaciones OVAL aceptadas | Medio | Resolver explícitamente la regla antes de retirar el indicador OVAL. |
| Reintentos no idempotentes | Alto | Confirmar contrato antes de automatizar reintentos. |
| Errores parciales sin motivo | Medio | Pedir detalle por documento y mantener auditoría local. |
| Rate limit o lote máximo desconocido | Medio | Confirmar límites y hacer tamaño configurable. |
| Procesamiento sincrónico de lotes grandes | Medio | Evaluar trabajo en segundo plano según volumen. |

---

## 17. Decisiones resueltas y preguntas para el proveedor

### 17.1 Respuestas confirmadas

1. `PUT /Cardholder/SafetyCards` reemplaza completamente OVAL.
2. Por el momento no está previsto otro endpoint para capacitación, resultado, empresa o foto; esos datos dejan de transmitirse al sistema externo.
3. Los tres campos Alutel representan vencimientos de registros de capacitación:
   - `vtoTarjetaVerde` -> curso `TV - Tarjeta Verde`, `CursoID = 1`.
   - `vtoTarjetaAzul` -> curso `TA - Tarjeta Azul`, `CursoID = 3`.
   - `vtoActualizacionSeguridad` -> curso `RF - Refresh`, `CursoID = 2`.
4. La fecha enviada es `RegistroCapacitacion.FechaVencimiento`, calculada por `Jornada.ObtenerFechaVencimiento()` a partir de la configuración de vigencia del curso y la fecha de la jornada.
5. El ejemplo del contrato confirma que un item puede incluir una sola vigencia y omitir las otras dos.
6. El proveedor confirmó que el servicio actualiza únicamente los campos incluidos y conserva sin cambios los vencimientos correspondientes a las propiedades omitidas.

### 17.2 Preguntas del proveedor que condicionan fases posteriores

Estas respuestas no impiden desarrollar el cliente ni realizar pruebas unitarias. Deben resolverse antes de habilitar las conductas relacionadas —regresión de fechas, aceptación definitiva, lotes y reintentos— y, en todos los casos, antes de Producción.

1. ¿Se permite reemplazar una fecha existente por otra anterior y cómo se corrige o revoca una fecha enviada por error?
2. ¿Qué garantiza `successfulProcessed`: actualización persistida o también validaciones posteriores completadas?
3. ¿Se garantiza que `successfulProcessed + failedProcessed` coincide con la cantidad enviada y que `failedProcessed` coincide con `failedDocuments.Count`?
4. ¿Qué ocurre con documentos duplicados dentro del mismo lote?
5. ¿La operación es idempotente y el lote permite resultados parciales?
6. ¿Cuál es el máximo de registros y bytes por request?
7. ¿Cuál es la normalización exacta de `documentNumber`, incluyendo tipos de documento, países y ceros iniciales?
8. Ante un timeout o resultado indeterminado, ¿existe un endpoint o mecanismo para consultar si la actualización fue aplicada?
9. ¿Las credenciales son globales para toda la aplicación o dependen del punto de servicio?

### 17.3 Consultas necesarias antes de Producción, pero no bloqueantes para elaborar el plan

1. ¿Qué hacen los campos enviados explícitamente como `null` o cadena vacía? La implementación prevista los omitirá.
2. ¿Es válido enviar solamente `documentNumber` y qué efecto tiene? La aplicación no prevé construir requests sin ninguna fecha.
3. ¿Puede devolverse un motivo por documento fallido?
4. ¿Qué ocurre con personas inexistentes, inactivas o sin tarjeta?
5. ¿Existen `429`, `Retry-After` u otros códigos no documentados?
6. ¿Cuáles son los timeouts y reintentos recomendados?
7. ¿El tenant publicado es común a Staging y Producción?
8. ¿Los `client_id` son diferentes por entorno?
9. ¿Cuándo vencen y cómo se rotan los secretos? ¿Hay superposición durante la rotación?
10. ¿Existen allowlists de IP, proxy o firewall?
11. ¿Pueden suministrar credenciales y documentos de prueba existentes en Staging, junto con el resultado esperado?
12. ¿Los documentos utilizados en el ejemplo del contrato existen en Staging o son solamente ilustrativos?
13. ¿Existe un mecanismo para restaurar los datos de Staging después de una prueba?
14. ¿Cuál es el SLA, canal de soporte, versionado y política de aviso de cambios?

---

## 18. Decisiones internas de negocio pendientes

Estas definiciones no deben delegarse exclusivamente al proveedor:

1. Si solo se envían registros aprobados.
2. Qué hacer ante registros no aprobados, revocaciones o correcciones.
3. Qué capacitación gana cuando hay varias del mismo curso para la misma persona.
4. Qué hacer cuando el curso está configurado sin vigencia y `FechaVencimiento` es `null`.
5. Si existe carga inicial, qué registros incluye y cuál es su fecha mínima.
6. Qué tratamiento tendrán los registros OVAL `PendienteEnvio` y `Rechazado` existentes.
7. Desde qué pantalla y sobre qué conjunto de registros actuará el operador, dado que la interfaz OVAL desaparecerá.
8. Quién puede iniciar, reintentar, cancelar o reconciliar envíos Alutel.
9. Si el envío permanecerá completamente manual o si los reintentos/transitorios serán ejecutados por un job.
10. Qué volumen normal y máximo debe soportar el proceso.
11. Qué evidencia necesita soporte para auditar el legado OVAL sin volver a exponer su operación.
12. Si los registros históricos OVAL `Aceptado` deben continuar impidiendo editar o borrar calificaciones; actualmente esa restricción existe en la interfaz y quedaría invisible al retirar sus indicadores.

---

## 19. Interrogantes pendientes y gates del desarrollo

Ninguna de estas preguntas impide elaborar el plan ni comenzar la base técnica. Se agrupan por el momento en que su respuesta pasa a ser obligatoria.

### Gate A — antes de conectar el cliente al flujo real del operador

1. ¿Solo se envían capacitaciones aprobadas?
2. ¿Qué debe ocurrir ante no aprobaciones, revocaciones o correcciones?
3. ¿Qué registro se utiliza cuando existen varias capacitaciones del mismo curso para una persona?
4. ¿Qué se hace con cursos sin vigencia?
5. ¿Desde qué pantalla y sobre qué registros realizará el operador el envío Alutel?
6. ¿Qué roles podrán enviar y consultar resultados?
7. ¿Cómo debe normalizarse `documentNumber`?
8. ¿Los registros históricos OVAL `Aceptado` continuarán bloqueando la edición o eliminación de calificaciones, o esa regla debe reemplazarse por otra condición de cierre?

### Gate B — antes de habilitar lotes y reintentos automáticos

9. ¿Qué garantiza exactamente `successfulProcessed`?
10. ¿Qué garantías de consistencia existen entre los contadores y `failedDocuments`?
11. ¿Cómo se tratan documentos duplicados, resultados parciales e idempotencia?
12. ¿Cuál es el tamaño máximo de lote y de request?
13. ¿Cómo se reconcilia un timeout o resultado indeterminado?
14. ¿Los reintentos serán manuales o ejecutados por un proceso automático?
15. ¿Cuál es el volumen normal y máximo esperado?

### Gate C — antes de habilitar Producción

16. ¿Se permite disminuir una vigencia y cómo se corrige o revoca una fecha enviada por error?
17. ¿Las credenciales son globales o dependen del punto de servicio?
18. ¿Qué mecanismo seguro se utilizará para almacenar y rotar `client_secret`?
19. ¿Habrá carga inicial y cuál será su alcance y fecha mínima?
20. ¿Qué ocurrirá con los registros OVAL pendientes y rechazados?
21. ¿Cuál es la fecha y hora de corte?

---

## 20. Conclusión y recomendación

- OVAL queda como **histórico compatible y de solo lectura**. Sus columnas y valores se preservan, pero no se reutilizan para Alutel.
- La opción OVAL debe **desaparecer completamente de la interfaz** y sus rutas de escritura deben quedar deshabilitadas.
- Alutel se implementará como una integración nueva, con estados, auditoría, elegibilidad, configuración y seguridad propios.
- El cliente Alutel debe validar estrictamente la coherencia de cada respuesta y distinguir rechazos funcionales, errores permanentes, transitorios e indeterminados.
- El mapeo curso–tarjeta, el cálculo de vencimiento y la actualización parcial del payload ya están confirmados. La base técnica y el spike de Staging pueden comenzar de inmediato con lotes unitarios, sin reproceso histórico y sin reintentos automáticos ante resultados indeterminados.
- La habilitación LENEL se configurará por curso mediante `TipoVigenciaAlutel?`; esa misma configuración gobernará el mapeo y la visibilidad de las opciones de envío, sin reutilizar `PermiteEnviosOVAL`.
- Las decisiones pendientes operan como gates: elegibilidad y selección antes de conectar el flujo real; contrato de lotes e idempotencia antes de automatizar; credenciales, corte e histórico antes de Producción.
- La migración debe tener una fecha de corte explícita. Los registros históricos no se reprocesarán automáticamente.

La arquitectura objetivo queda resumida así:

```text
Historial OVAL existente -> se conserva intacto, sin interfaz ni nuevos envíos
Nuevos eventos elegibles -> integración Alutel independiente
Carga histórica opcional -> migración controlada y reconciliada
```
