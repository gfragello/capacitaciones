# Implementación Alutel

## Estado actual

La base técnica y la orquestación desacoplada de Alutel están implementadas, pero la integración permanece deshabilitada y todavía no está conectada a controladores ni vistas. No se debe interpretar la presencia del flujo interno como una habilitación operativa.

La implementación disponible incluye:

- configuración tipada y validada;
- secreto obtenido desde la variable de entorno `CURSOS_ALUTEL_CLIENT_SECRET`;
- token OAuth Client Credentials con caché y renovación sincronizada;
- cliente `PUT Cardholder/SafetyCards`, request unitario y un único reintento ante `401`;
- serialización parcial de Verde, Azul o Refresh;
- política de elegibilidad y selección según las reglas confirmadas en el Gate 1;
- validación estricta de respuestas;
- entidades y estados de auditoría separados de OVAL;
- servicio de orquestación con reclamo transaccional, auditoría previa y recuperación de operaciones interrumpidas;
- pruebas MSTest sin IIS, credenciales ni acceso de red.

Continúan pendientes la interfaz operativa, el retiro de OVAL, la prueba transaccional del almacén EF6 contra SQL Server y la validación del contrato en Staging. El ciclo de la migración ya fue validado en la BD de desarrollo.

## Organización del módulo

El código específico del proveedor se concentra bajo una única raíz:

```text
Integraciones/Alutel/
    Dominio/
        EstadoIntegracionAlutel.cs
        IntentoIntegracionAlutel.cs
        OperacionIntegracionAlutel.cs
        ResultadoTecnicoAlutel.cs
        TipoVigenciaAlutel.cs
    Aplicacion/
        AlutelEligibilityPolicy.cs
        AlutelIntegrationService.cs
        IClock.cs
    Infraestructura/
        AlutelConfiguration.cs
        AlutelContracts.cs
        AlutelInfrastructure.cs
        AlutelMappingService.cs
        AlutelSafetyCardsClient.cs
        AlutelTokenProvider.cs
        AlutelVigenciaGateway.cs
        EfAlutelOperationStore.cs
```

Las pruebas correspondientes se agrupan en `Cursos.Tests/Integraciones/Alutel/`. Las migraciones EF6 permanecen en `Migrations/` para conservar la secuencia global de la aplicación, y el contexto central continúa en `Models/IdentityModels.cs`.

Los namespaces siguen la misma división:

```text
Cursos.Integraciones.Alutel.Dominio
Cursos.Integraciones.Alutel.Aplicacion
Cursos.Integraciones.Alutel.Infraestructura
```

Esta separación es organizativa dentro del proyecto web existente; no introduce ensamblados adicionales.

## Dependencias

La dirección permitida es:

```text
Infraestructura -> Aplicacion -> Dominio
             |               |
             +---------------+

Aplicación MVC existente -> Dominio / Aplicacion / Infraestructura
```

- `Dominio` no depende de `Aplicacion` ni de `Infraestructura`.
- `Aplicacion` puede depender de `Dominio` y de modelos centrales como `RegistroCapacitacion`.
- `Infraestructura` puede depender de `Aplicacion` y `Dominio`.
- El contexto EF6 central puede depender de las entidades de `Dominio`.

`IClock` pertenece a `Aplicacion` porque la política de elegibilidad y el token provider consumen la abstracción. `SystemClock` permanece en `Infraestructura` como su implementación de producción.

`IAlutelIntegrationService` depende de dos puertos de aplicación: `IAlutelVigenciaGateway` e `IAlutelOperationStore`. El gateway combina el mapeo con el cliente SafetyCards; el almacén EF6 concentra las transacciones y no expone `ApplicationDbContext` al servicio.

## Configuración y secretos

`Alutel:Habilitado` permanece en `false` por defecto. `Alutel:ClientId` queda deliberadamente vacío en `Web.config` hasta disponer de la configuración del entorno. El `client_secret` no se almacena en archivos versionados, tablas de configuración ni pantallas administrativas; se obtiene mediante `IAlutelSecretProvider` desde `CURSOS_ALUTEL_CLIENT_SECRET`.

La clave `Oval:LegacyHabilitado` está declarada para la contingencia futura, pero esta base todavía no modifica el comportamiento de las rutas OVAL existentes.

## Entity Framework 6

La migración inicial permanece en:

```text
Migrations/202607171115000_IntegracionAlutelInicial.cs
Migrations/202607171115000_IntegracionAlutelInicial.Designer.cs
Migrations/202607171115000_IntegracionAlutelInicial.resx
```

La migración debe conservar exclusivamente:

- la columna `Cursos.TipoVigenciaAlutel`;
- las tablas `OperacionesIntegracionAlutel` e `IntentosIntegracionAlutel`;
- sus claves, relaciones e índices;
- la asignación inicial de Verde, Refresh y Azul a los cursos 1, 2 y 3.

Mover clases o cambiar namespaces no debe generar cambios SQL: los nombres físicos están fijados por los atributos de tabla y el modelo persistente no cambia.

### Advertencia de regeneración

No ejecutar:

```powershell
ef6 migrations add IntegracionAlutelInicial --force
```

En este repositorio, la CLI de EF6 interpreta esta migración aditiva como una migración inicial y puede reemplazar `Up` por la creación completa de la base. No se debe regenerar automáticamente la migración ni su snapshot. Si EF6 detecta diferencias únicamente por nombres CLR, hay que detenerse y revisar antes de crear otra migración.

### Validación realizada en desarrollo

El 2026-07-25 se aplicó, revirtió y reaplicó correctamente la migración en la BD de desarrollo. La base permanece actualmente actualizada con `IntegracionAlutelInicial`.

Se confirmó:

- la creación de `Cursos.TipoVigenciaAlutel`, `OperacionesIntegracionAlutel` e `IntentosIntegracionAlutel`, junto con sus claves e índices;
- el registro correcto de la migración en `__MigrationHistory`;
- la asignación inicial de curso 1 = Verde, curso 2 = Refresh y curso 3 = Azul;
- que ningún otro curso quedó habilitado accidentalmente;
- que las columnas y los valores `EnvioOVAL*` permanecieron intactos después del ciclo aplicar–revertir–reaplicar.

Permanece pendiente la prueba de `EfAlutelOperationStore` contra SQL Server real: reclamo, finalización, concurrencia mediante contextos independientes y recuperación de operaciones `EnProceso`. Hasta completar esa prueba, `F3-06` continúa abierta.

## Orquestación y recuperación

`AlutelIntegrationService.ProcesarAsync` recibe explícitamente el registro y el usuario de origen. El flujo no consulta `HttpContext`:

1. valida elegibilidad y límites de los datos de auditoría;
2. reclama la operación con aislamiento `Serializable`;
3. persiste la operación `EnProceso` y un intento inicialmente `Indeterminado`;
4. invoca al proveedor mediante `IAlutelVigenciaGateway`;
5. completa el intento y actualiza la operación a `Aceptado`, `Fallido` o `Indeterminado`.

El reclamo se serializa por documento y tipo de vigencia. Una fecha igual o menor que otra ya aceptada no se reenvía. Una operación `EnProceso` o `Indeterminado` bloquea nuevos envíos del mismo documento y tipo hasta que se resuelva, evitando reintentos automáticos de resultados inciertos.

Si la aplicación se interrumpe después de persistir el reclamo, `RecuperarEnProcesoAsync` cambia a `Indeterminado` las operaciones anteriores al umbral indicado y cierra sus intentos inconclusos con un mensaje sanitizado. La recuperación no reenvía datos; deja la operación disponible para la futura reconciliación administrativa. El umbral deberá ser mayor que el timeout máximo del cliente cuando se conecte a un proceso periódico o a una acción administrativa.

Las pruebas automatizadas cubren el orden reclamar–enviar–completar, estados técnicos, concurrencia, cancelación, recuperación y el caso en que Alutel responde pero falla el guardado local. La semántica transaccional aún debe probarse contra SQL Server junto con la migración.

## Ejecución local

Compilar primero el proyecto web, porque `Cursos.Tests` referencia `bin/Cursos.dll`:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' .\Cursos.csproj /t:Build /p:Configuration=Debug /m:1 /v:minimal
```

Luego ejecutar las pruebas:

```powershell
dotnet test .\Cursos.Tests\Cursos.Tests.csproj --no-restore
```

Las pruebas usan un transporte HTTP simulado y no necesitan credenciales. La primera ejecución puede requerir omitir `--no-restore` para restaurar MSTest.

## Activación futura

El Gate 1 y la orquestación interna están resueltos, pero todavía no existe un flujo de usuario que pueda activarse. Antes de habilitar acciones se debe implementar la Fase 11 (controladores, autorización e interfaz), además de repetir en el servidor todas las validaciones de elegibilidad.

Antes de habilitar un entorno también se debe:

- aplicar y revertir la migración sobre una copia representativa;
- inventariar y retirar la superficie operativa OVAL según la Fase 2;
- proteger los POST legacy que todavía adjuntan entidades completas;
- validar credenciales, conectividad y contrato contra Staging;
- mantener `Alutel:Habilitado=false` hasta completar esas verificaciones.
