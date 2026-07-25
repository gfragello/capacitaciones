# Implementación Alutel

## Estado actual

La base técnica de Alutel está implementada, pero la integración permanece deshabilitada y todavía no está conectada a controladores, vistas ni a un servicio de orquestación. No se debe interpretar la presencia del cliente HTTP y de las entidades como una habilitación operativa.

La implementación disponible incluye:

- configuración tipada y validada;
- secreto obtenido desde la variable de entorno `CURSOS_ALUTEL_CLIENT_SECRET`;
- token OAuth Client Credentials con caché y renovación sincronizada;
- cliente `PUT Cardholder/SafetyCards`, request unitario y un único reintento ante `401`;
- serialización parcial de Verde, Azul o Refresh;
- política de elegibilidad y selección según las reglas confirmadas en el Gate 1;
- validación estricta de respuestas;
- entidades y estados de auditoría separados de OVAL;
- pruebas MSTest sin IIS, credenciales ni acceso de red.

Continúan pendientes la orquestación que crea y persiste operaciones e intentos, la interfaz operativa, el retiro de OVAL, la prueba de la migración sobre una copia de la BD y la validación del contrato en Staging.

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
        IClock.cs
    Infraestructura/
        AlutelConfiguration.cs
        AlutelContracts.cs
        AlutelInfrastructure.cs
        AlutelMappingService.cs
        AlutelSafetyCardsClient.cs
        AlutelTokenProvider.cs
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

La migración todavía no fue aplicada ni revertida sobre una copia representativa. Esa validación debe preservar todos los datos y valores `EnvioOVAL*`.

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

El Gate 1 está resuelto, pero todavía no existe un flujo completo que pueda activarse. Antes de conectar acciones de usuario se deben implementar la Fase 8 (orquestación y persistencia) y la Fase 11 (controladores, autorización e interfaz), además de repetir en el servidor todas las validaciones de elegibilidad.

Antes de habilitar un entorno también se debe:

- aplicar y revertir la migración sobre una copia representativa;
- inventariar y retirar la superficie operativa OVAL según la Fase 2;
- proteger los POST legacy que todavía adjuntan entidades completas;
- validar credenciales, conectividad y contrato contra Staging;
- mantener `Alutel:Habilitado=false` hasta completar esas verificaciones.
