# Alutel API - Safety Cards Update

## Objetivo

Actualizar en forma masiva las fechas de vigencia de tarjetas de seguridad de uno o más trabajadores.

---

## Entornos

### Staging

Base URL:

```text
https://alutelapi-stg.upmuruguay.net/alutelapi
```

### Producción

Base URL:

```text
https://alutelapi-prod.upmuruguay.net/alutelapi
```

> **Importante:** Las credenciales y los tokens son específicos de cada entorno. No utilizar credenciales de Staging en Producción.

---

## Endpoint


```http
PUT {baseUrl}/Cardholder/SafetyCards
```

Este endpoint requiere autenticación mediante Bearer Token de Microsoft Entra ID (Azure AD). 

---

## Autenticación

Obtener un token OAuth 2.0 mediante Client Credentials Flow.

Token Endpoint:

```http
POST https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token
```
### Tenant ID:

```text
9eab37f0-91c6-47e3-9c00-fe8544bd272e
```

### Scope Staging

```text
ee514d61-041c-45d8-91f3-b61ebc5717db/.default
```

### Scope Producción

```text
e89b4ff0-f0ca-431e-a7b8-a57f727e66c8/.default
```

### Parámetros:

```text
grant_type=client_credentials
client_id=<client-id>
client_secret=<client-secret>
scope=<scope>
```

### Headers requeridos para consumir la API:

```http
Authorization: Bearer <access_token>
Content-Type: application/json
Accept: application/json
```

Las credenciales (`client_id` y `client_secret`) son suministradas por UPM. 

---

## Request

### Body

Enviar un array JSON con uno o más registros.

```json
[
  {
    "documentNumber": "4895623",
    "vtoTarjetaVerde": "20271231",
    "vtoTarjetaAzul": "20280131",
    "vtoActualizacionSeguridad": "20270115"
  },
  {
    "documentNumber": "1985623",
    "vtoTarjetaVerde": "20270630"
  }
]
```

### Campos

| Campo | Tipo | Obligatorio | Descripción |
|---------|---------|---------|---------|
| documentNumber | string | Sí | Documento de identidad de la persona. |
| vtoTarjetaVerde | string | No | Fecha de vencimiento de Tarjeta Verde. |
| vtoTarjetaAzul | string | No | Fecha de vencimiento de Tarjeta Azul. |
| vtoActualizacionSeguridad | string | No | Fecha de actualización de seguridad. |

Los campos de fecha deben enviarse en formato `yyyyMMdd`. 

---

## Formato de Fechas

Todas las fechas deben enviarse en formato:

```text
yyyyMMdd
```

Ejemplo válido:

```text
20271231
```

Ejemplos inválidos:

```text
2027-12-31
31/12/2027
```

La API valida estrictamente este formato. 

---

## Respuesta

### Ejemplo

```json
{
  "successfulProcessed": 2,
  "failedProcessed": 0,
  "failedDocuments": []
}
```

### Campos de Respuesta

| Campo | Tipo | Descripción |
|---------|---------|---------|
| successfulProcessed | number | Cantidad de registros procesados correctamente. |
| failedProcessed | number | Cantidad de registros con error. |
| failedDocuments | string[] | Lista de documentos que no pudieron procesarse. |

---

## Recomendaciones

- Se pueden enviar múltiples registros en una misma solicitud. 
- Para cargas masivas se recomienda agrupar varios registros por llamada.
- Si existen errores parciales, reprocesar únicamente los documentos incluidos en `failedDocuments`. 
- Luego de la actualización, el sistema ejecuta automáticamente las validaciones asociadas a las credenciales de seguridad.

---

## Códigos de Respuesta

| Código | Significado |
|----------|-------------|
| 200 | Solicitud procesada correctamente. |
| 400 | Datos inválidos o formato incorrecto. |
| 401 | Token inválido o ausente. |
| 503 | Servicio temporalmente no disponible. |