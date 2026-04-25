# Integracion LENEL - Contrato tecnico esperado por Capacitaciones CSL

## Objetivo

Este documento describe el contrato HTTP que hoy espera la aplicacion `Capacitaciones CSL` para enviar registros al sistema externo actualmente llamado OVAL. Dado que OVAL sera reemplazado por LENEL, el objetivo es que el equipo de LENEL implemente un servicio compatible con este contrato para evitar cambios en la aplicacion consumidora.

Este documento describe lo que esta desarrollado actualmente en `Capacitaciones CSL`. No pretende fijar de forma definitiva el contrato de integracion futuro: si ambos equipos acuerdan cambios en el diseño, el contrato puede modificarse, teniendo en cuenta los ajustes que eso requiera en la aplicacion consumidora.

## Resumen ejecutivo

Con el codigo actual, la aplicacion no consume un unico endpoint, sino un flujo REST de dos pasos:

1. Obtener credenciales de sesion desde un endpoint de autenticacion.
2. Enviar el registro de capacitacion a un endpoint principal usando los headers devueltos por el paso anterior.

Si se decide mantener la aplicacion `Capacitaciones CSL` sin cambios, LENEL deberia exponer ambos pasos con esta interfaz. Si se acuerda un contrato distinto entre equipos, sera necesario ajustar la aplicacion `Capacitaciones CSL` en consecuencia.

## Donde se configura en la aplicacion

La aplicacion toma los datos de conexion desde la entidad `PuntoServicio`:

| Campo en Capacitaciones CSL | Uso |
| --- | --- |
| `Direccion` | Base URL del servicio REST |
| `DireccionRequest` | Path del endpoint principal de alta/envio |
| `DireccionToken` | URL absoluta del endpoint de autenticacion |
| `Usuario` | Usuario para autenticacion |
| `Password` | Password para autenticacion |

Esto permite cambiar las URLs sin recompilar la aplicacion, siempre que el contrato siga siendo compatible.

## Condiciones en las que la aplicacion invoca LENEL

La aplicacion envia un registro cuando el registro de capacitacion:

- Tiene un tipo de documento habilitado para envio externo.
- Ya fue calificado.
- Tiene estado de envio `PendienteEnvio` o `Rechazado`.

Los registros enviados pueden representar dos resultados de negocio:

- `APR`: capacitacion aprobada.
- `REC`: capacitacion no aprobada/rechazada.

## Flujo esperado

### Paso 1 - Autenticacion

La aplicacion hace un `POST` a la URL configurada en `DireccionToken`.

**Headers enviados por Capacitaciones CSL**

| Header | Valor |
| --- | --- |
| `accept` | `application/json` |
| `content-type` | `application/x-www-form-urlencoded` |

**Body enviado por Capacitaciones CSL**

Se envian parametros form-urlencoded con estos nombres exactos:

| Campo | Tipo | Descripcion |
| --- | --- | --- |
| `user` | string | Usuario configurado en `PuntoServicio.Usuario` |
| `passwd` | string | Password configurado en `PuntoServicio.Password` |

**Respuesta esperada por Capacitaciones CSL**

Respuesta HTTP exitosa con JSON similar a:

```json
{
  "status": true,
  "cliente": "cliente-lenel",
  "api_key": "token-o-api-key"
}
```

**Interpretacion que hace Capacitaciones CSL**

- Si `status` es `true`, toma `cliente` y `api_key` y continua con el paso 2.
- Si `status` es `false`, o faltan `cliente` / `api_key`, la autenticacion se considera fallida.
- Si la respuesta HTTP no es exitosa, la autenticacion tambien se considera fallida.

## Paso 2 - Envio del registro

La aplicacion hace un `POST` a:

`{Direccion}/{DireccionRequest}`

Ejemplo conceptual:

```text
Base URL      = https://lenel.example.com
Request path  = api/inducciones/ingresar
URL final     = https://lenel.example.com/api/inducciones/ingresar
```

**Headers enviados por Capacitaciones CSL**

| Header | Valor |
| --- | --- |
| `cliente` | Valor devuelto por el endpoint de autenticacion |
| `api_key` | Valor devuelto por el endpoint de autenticacion |
| `Content-type` | `application/json` |

No se envia header `Authorization: Bearer ...`.

**Body JSON enviado por Capacitaciones CSL**

```json
{
  "tipo_doc": "CI",
  "rut_trabajador": "12345678",
  "nombres_trabajador": "Juan",
  "ape_paterno_trabajador": "Perez",
  "ape_materno_trabajador": "",
  "rut_contratista": "219999990012",
  "nombre_contratista": "Empresa SA",
  "tipo_induccion": "CAP_SEG",
  "estado_induccion": "APR",
  "fecha_induccion": "23-04-2026",
  "imagen": "/9j/4AAQSkZJRgABAQ..."
}
```

## Definicion de campos del body

| Campo | Tipo | Obligatorio | Origen en Capacitaciones CSL | Observaciones |
| --- | --- | --- | --- | --- |
| `tipo_doc` | string | Si | Mapeo `Capacitado.TipoDocumento.TipoDocumentoOVAL` | El valor exacto depende de la configuracion del tipo de documento en Capacitaciones CSL |
| `rut_trabajador` | string | Si | `Capacitado.Documento` | Se envia como string, sin transformaciones adicionales visibles en este flujo |
| `nombres_trabajador` | string | Si | `Capacitado.Nombre` | |
| `ape_paterno_trabajador` | string | Si | `Capacitado.Apellido` | |
| `ape_materno_trabajador` | string | Si, pero puede venir vacio | Constante vacia | Hoy la aplicacion envia siempre `""` |
| `rut_contratista` | string | Si | `Capacitado.Empresa.RUT` | |
| `nombre_contratista` | string | Si | `Capacitado.Empresa.NombreFantasia` | |
| `tipo_induccion` | string | Si | Constante | Hoy la aplicacion envia siempre `CAP_SEG` |
| `estado_induccion` | string | Si | Derivado del estado del registro | `APR` si el registro esta aprobado, `REC` en caso contrario |
| `fecha_induccion` | string | Si | `Jornada.Fecha` | Formato exacto `dd-MM-yyyy` |
| `imagen` | string | No | Foto del capacitado en Base64 | Si existe foto, se envia codificada en Base64 a partir de un JPEG; si no existe, se envia cadena vacia |

## Respuesta esperada del endpoint principal

La aplicacion considera correcto el envio solo si recibe `HTTP 200 OK` y un JSON cuyo resultado booleano sea positivo.

### Respuesta exitosa aceptada por Capacitaciones CSL

Capacitaciones CSL interpreta como exito cualquiera de estas variantes:

```json
{
  "data": true
}
```

o

```json
{
  "status": true
}
```

### Respuesta de rechazo funcional

Si el servicio responde `HTTP 200 OK`, pero el booleano viene en `false`, la aplicacion espera un campo `error` con el detalle:

```json
{
  "data": false,
  "error": "El documento no se encuentra ingresado en LENEL"
}
```

o

```json
{
  "status": false,
  "error": "Registro duplicado"
}
```

### Interpretacion que hace Capacitaciones CSL

- `HTTP 200` + `data=true` o `status=true`: el registro queda como `Aceptado`.
- `HTTP 200` + `data=false` o `status=false` + `error`: el registro queda como `Rechazado` y se guarda el texto de `error`.
- `HTTP` no exitoso: la aplicacion lo trata como error tecnico. No lo considera ni aceptado ni rechazado funcionalmente.
- JSON con estructura distinta: puede provocar error de parseo o ser interpretado como fallo tecnico.

## Requisitos de compatibilidad para LENEL

Para que no haya cambios en `Capacitaciones CSL`, LENEL deberia cumplir con todo lo siguiente:

1. Exponer un endpoint de autenticacion compatible con `POST application/x-www-form-urlencoded` usando `user` y `passwd`.
2. Devolver en la autenticacion un JSON con `status`, `cliente` y `api_key`.
3. Exponer un endpoint principal `POST application/json`.
4. Aceptar los headers `cliente` y `api_key` como mecanismo de autenticacion del endpoint principal.
5. Aceptar exactamente los nombres de campos JSON definidos arriba.
6. Responder con `HTTP 200` y un JSON con `data` o `status` booleano.
7. Incluir `error` cuando el booleano sea `false`.

## Consideraciones tecnicas adicionales

- La autenticacion se ejecuta antes de cada envio.
- En el paso de token, la aplicacion fuerza `TLS 1.2`.
- La aplicacion no envia un campo `Notas` en el flujo REST.
- La aplicacion no implementa versionado de API ni negotiation especial aparte de los headers mencionados.
- La aplicacion procesa la respuesta de forma sincrona.
- La imagen se serializa como Base64 sin metadata adicional; no se envia MIME type ni nombre de archivo.

## Recomendacion de implementacion para LENEL

Si el objetivo es minimizar riesgo y evitar cambios en `Capacitaciones CSL`, la implementacion mas segura es:

1. Mantener el flujo de dos pasos tal cual.
2. Mantener los nombres de headers y campos exactamente iguales.
3. Responder siempre `HTTP 200` para rechazos funcionales y reservar `4xx/5xx` para errores tecnicos reales.
4. Devolver mensajes claros en `error`, porque ese texto se persiste en la aplicacion y sirve para diagnostico operativo.

## Fuera de alcance o referencias no normativas

- La aplicacion tambien tiene soporte historico para integracion SOAP, pero no es la referencia recomendada para LENEL si se quiere reemplazar el flujo REST.
- Existe un `RegistroOVALController` usado como mock local de pruebas, pero no representa el contrato real consumido actualmente por el flujo REST productivo.

## Fuente tecnica utilizada

Este documento se redacto a partir del analisis del codigo de integracion existente en `Capacitaciones CSL`, principalmente del helper de envio REST/SOAP y de las entidades de configuracion asociadas.