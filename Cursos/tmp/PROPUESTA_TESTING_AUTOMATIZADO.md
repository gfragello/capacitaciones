# Propuesta de Testing Automatizado

## Alcance

Este documento propone una estrategia de testing automatizado para validar las funcionalidades afectadas por el refactor de notificaciones de vencimiento de cursos.

---

## 1. Infraestructura recomendada

### Framework de testing

| Componente | Herramienta | Justificación |
|---|---|---|
| Unit Tests | **MSTest** o **xUnit** | Integración nativa con Visual Studio y .NET 4.8.1 |
| Mocking | **Moq** | Simular dependencias (DbContext, controllers) |
| Base de datos en tests | **Effort** o **SQLite In-Memory** | Simular EF6 sin SQL Server real |
| Assertions | **FluentAssertions** | Assertions legibles y expresivas |

### Proyecto de test sugerido

Crear un nuevo proyecto `Cursos.Tests` en la solución:

```
Cursos.Tests/
├── Models/
│   ├── RegistroCapacitacionTests.cs
│   ├── CursoTests.cs
│   └── NotificacionVencimientoTests.cs
├── Controllers/
│   ├── NotificacionesVencimientosControllerTests.cs
│   ├── RegistrosCapacitacionControllerTests.cs
│   ├── JornadasControllerTests.cs
│   └── CapacitadosControllerTests.cs
├── Helpers/
│   ├── NotificacionesVencimientosHelperTests.cs
│   └── NotificacionesEMailHelperTests.cs
├── Integration/
│   └── FlujoNotificacionesIntegrationTests.cs
└── TestHelpers/
    ├── TestDbContextFactory.cs
    └── TestDataBuilder.cs
```

---

## 2. Tests unitarios por componente

### 2.1 RegistroCapacitacion – CambiarEstado()

Esta es la pieza central del refactor. Los tests deben cubrir:

```
Categoría: CambiarEstado - Cambios de estado básicos
─────────────────────────────────────────────────────
TEST: CambiarEstado_DeInscriptoAAprobado_CambiaEstadoCorrectamente
  Given: Un RegistroCapacitacion con Estado = Inscripto
  When:  Se llama CambiarEstado(Aprobado, ejecutarAcciones: false)
  Then:  Estado == Aprobado

TEST: CambiarEstado_DeInscriptoANoAprobado_CambiaEstadoCorrectamente
  Given: Un RegistroCapacitacion con Estado = Inscripto
  When:  Se llama CambiarEstado(NoAprobado, ejecutarAcciones: false)
  Then:  Estado == NoAprobado

TEST: CambiarEstado_MismoEstado_NoEjecutaAcciones
  Given: Un RegistroCapacitacion con Estado = Aprobado
  When:  Se llama CambiarEstado(Aprobado, ejecutarAcciones: true)
  Then:  No se ejecutan acciones (no se llama a EjecutarAccionesAlAprobar)

TEST: CambiarEstado_ConEjecutarAccionesFalse_NoCreaNotificaciones
  Given: Un RegistroCapacitacion con Estado = Inscripto
  When:  Se llama CambiarEstado(Aprobado, ejecutarAcciones: false)
  Then:  No se crean notificaciones

TEST: CambiarEstado_RegistroNuevoSinID_NoEjecutaAcciones
  Given: Un RegistroCapacitacion con RegistroCapacitacionID = 0
  When:  Se llama CambiarEstado(Aprobado, ejecutarAcciones: true)
  Then:  No se ejecutan acciones (registro aún no guardado)
```

```
Categoría: CambiarEstado - Integración con notificaciones
─────────────────────────────────────────────────────
TEST: CambiarEstado_AAprobado_ConAcciones_CreaNotificacionVencimiento
  Given: Un RegistroCapacitacion guardado (ID > 0)
         con Jornada de un Curso con NotificarVencimiento = true
  When:  Se llama CambiarEstado(Aprobado, ejecutarAcciones: true)
  Then:  Se crea una NotificacionVencimiento con Estado = NotificacionPendiente

TEST: CambiarEstado_AAprobado_CursoSinNotificar_NoCreaNotificacion
  Given: Un RegistroCapacitacion guardado (ID > 0)
         con Jornada de un Curso con NotificarVencimiento = false
  When:  Se llama CambiarEstado(Aprobado, ejecutarAcciones: true)
  Then:  No se crea NotificacionVencimiento

TEST: CambiarEstado_AAprobado_MarcaNotificacionesAnterioresComoObsoletas
  Given: Un Capacitado con una NotificacionVencimiento pendiente para Curso X (Jornada antigua)
  When:  Se aprueba un nuevo RegistroCapacitacion del mismo Curso X (Jornada más reciente)
  Then:  La notificación anterior cambia a Estado = NoNotificarYaActualizado
```

### 2.2 RegistroCapacitacion – Calificar()

```
TEST: Calificar_ConNotaMayorOIgualAMinima_EstadoAprobado
  Given: Un RegistroCapacitacion con Jornada que tiene NotaMinimaAprobacion = 70
  When:  Se llama Calificar(80)
  Then:  Estado == Aprobado, Nota == 80

TEST: Calificar_ConNotaMenorAMinima_EstadoNoAprobado
  Given: Un RegistroCapacitacion con Jornada que tiene NotaMinimaAprobacion = 70
  When:  Se llama Calificar(60)
  Then:  Estado == NoAprobado, Nota == 60

TEST: Calificar_UsaCambiarEstadoConEjecutarAccionesFalse
  Given: Un RegistroCapacitacion válido
  When:  Se llama Calificar(nota)
  Then:  Se invoca CambiarEstado con ejecutarAcciones: false
```

### 2.3 RegistroCapacitacion – Propiedades computadas

```
TEST: EstadoNotificacion_ConNotificacion_DevuelveEstadoCorrecto
  Given: RegistroCapacitacion con una NotificacionVencimiento (Estado = Notificado)
  Then:  EstadoNotificacion == Notificado

TEST: EstadoNotificacion_SinNotificaciones_DevuelveNull
  Given: RegistroCapacitacion sin NotificacionesVencimiento
  Then:  EstadoNotificacion == null

TEST: EstadoNotificacionTexto_NotificacionPendiente_DevuelvePendiente
  Given: RegistroCapacitacion con NotificacionVencimiento en estado NotificacionPendiente
  Then:  EstadoNotificacionTexto == "Pendiente"

TEST: TieneNotificacion_ConNotificaciones_DevuelveTrue
  Given: RegistroCapacitacion con al menos una NotificacionVencimiento
  Then:  TieneNotificacion == true

TEST: TieneNotificacion_SinNotificaciones_DevuelveFalse
  Given: RegistroCapacitacion con colección vacía
  Then:  TieneNotificacion == false
```

### 2.4 Curso – NotificarVencimiento

```
TEST: Curso_NotificarVencimiento_DefaultFalse
  Given: Un nuevo Curso
  Then:  NotificarVencimiento == false

TEST: Curso_NotificarVencimiento_SePuedeCambiar
  Given: Un Curso con NotificarVencimiento = false
  When:  Se asigna NotificarVencimiento = true
  Then:  NotificarVencimiento == true
```

### 2.5 NotificacionesVencimientosController

```
Categoría: CrearNotificacionVencimiento
─────────────────────────────────────────────────────
TEST: CrearNotificacion_RegistroAprobadoConCursoNotificable_CreaRegistro
  Given: RegistroCapacitacion aprobado, Curso con NotificarVencimiento = true
  When:  Se llama CrearNotificacionVencimiento(registroId)
  Then:  Se inserta un nuevo registro en NotificacionVencimientos
         con Estado = NotificacionPendiente

TEST: CrearNotificacion_CursoNoNotificable_NoCreaNada
  Given: RegistroCapacitacion aprobado, Curso con NotificarVencimiento = false
  When:  Se llama CrearNotificacionVencimiento(registroId)
  Then:  No se insertan registros

TEST: CrearNotificacion_RegistroNoAprobado_NoCreaNada
  Given: RegistroCapacitacion con Estado != Aprobado
  When:  Se llama CrearNotificacionVencimiento(registroId)
  Then:  No se insertan registros

TEST: CrearNotificacion_YaExisteNotificacion_NoDuplica
  Given: RegistroCapacitacion que ya tiene una NotificacionVencimiento
  When:  Se llama CrearNotificacionVencimiento(registroId)
  Then:  No se crean duplicados
```

```
Categoría: ActualizarNotificacionesObsoletasPorCapacitado
─────────────────────────────────────────────────────
TEST: ActualizarObsoletas_ConNotificacionAnterior_MarcaComoYaActualizado
  Given: Capacitado con NotificacionPendiente para Jornada de Curso X (fecha antigua)
  When:  Se aprueba nueva Jornada del mismo Curso X (fecha más reciente)
  Then:  Notificación anterior cambia a NoNotificarYaActualizado

TEST: ActualizarObsoletas_SinNotificacionesAnteriores_NoModificaNada
  Given: Capacitado sin notificaciones previas para el curso
  When:  Se aprueba un RegistroCapacitacion
  Then:  No se modifican registros

TEST: ActualizarObsoletas_NotificacionDeOtroCurso_NoSeModifica
  Given: Capacitado con NotificacionPendiente para Curso Y
  When:  Se aprueba Jornada de Curso X
  Then:  Notificación de Curso Y permanece sin cambios

TEST: ActualizarObsoletas_NotificacionYaNotificada_NoSeModifica
  Given: Capacitado con notificación en estado Notificado
  When:  Se aprueba nueva Jornada del mismo curso
  Then:  La notificación Notificada no se modifica (solo se actualizan las Pendientes)
```

```
Categoría: LimpiarNotificacionesObsoletas
─────────────────────────────────────────────────────
TEST: LimpiarObsoletas_NotificacionesPendientesConRegistroMasReciente_SeMarcaObsoleta
  Given: NotificacionVencimiento pendiente para RegistroCapacitacion de una jornada antigua
         Y existe otro RegistroCapacitacion aprobado para el mismo capacitado y curso
         con jornada más reciente
  When:  Se ejecuta LimpiarNotificacionesObsoletas()
  Then:  La notificación antigua se marca como NoNotificarYaActualizado

TEST: LimpiarObsoletas_NotificacionPendienteSinRegistroMasReciente_PermaneceIgual
  Given: NotificacionVencimiento pendiente sin registros más recientes
  When:  Se ejecuta LimpiarNotificacionesObsoletas()
  Then:  La notificación permanece en estado NotificacionPendiente
```

```
Categoría: ObtenerNotificacionesVencimiento
─────────────────────────────────────────────────────
TEST: ObtenerNotificaciones_SoloDevuelveCursosConNotificarVencimiento
  Given: Notificaciones para cursos con y sin NotificarVencimiento
  When:  Se llama ObtenerNotificacionesVencimiento()
  Then:  Solo se devuelven las de cursos con NotificarVencimiento = true

TEST: ObtenerNotificaciones_FiltraEstadosObsoletos
  Given: Notificaciones en estados Pendiente, Notificado, NoNotificar, NoNotificarYaActualizado
  When:  Se llama ObtenerNotificacionesVencimiento() con filtro de pendientes
  Then:  Solo se devuelven las pendientes
```

### 2.6 JornadasController – Aprobación masiva

```
TEST: CalificarJornada_ApruebaTodos_CreaNotificacionesPorRegistro
  Given: Jornada con 3 RegistrosCapacitacion inscriptos
         Curso con NotificarVencimiento = true
  When:  Se califica la jornada con notas que aprueban todos
  Then:  Se crean 3 NotificacionVencimiento (una por registro)

TEST: CalificarJornada_ApruebaParcial_SoloCreaNotificacionesParaAprobados
  Given: Jornada con 3 RegistrosCapacitacion inscriptos
  When:  Se califica: 2 aprueban, 1 no aprueba
  Then:  Se crean 2 NotificacionVencimiento

TEST: CalificarJornada_UsaCambiarEstadoConEjecutarAcciones
  Given: Jornada con registros
  When:  Se califica la jornada
  Then:  Primero: CambiarEstado(estado, ejecutarAcciones: false) + SaveChanges
         Luego: CambiarEstado(Aprobado, ejecutarAcciones: true) para cada aprobado
```

---

## 3. Tests de integración

### 3.1 Flujo completo de aprobación y notificación

```
TEST: FlujoCompleto_AprobarRegistro_CreaYLimpiaNotificaciones
  Setup:
    - Crear Curso con NotificarVencimiento = true, Vigencia = 365 días
    - Crear Capacitado
    - Crear Jornada 1 (fecha: hace 400 días)
    - Crear RegistroCapacitacion 1 (inscripto)

  Step 1: Aprobar Registro 1
    Then: Se crea NotificacionVencimiento para Registro 1 (Pendiente)

  Step 2: Crear Jornada 2 (fecha: hoy) y Registro 2 (inscripto)

  Step 3: Aprobar Registro 2
    Then:
      - Se crea NotificacionVencimiento para Registro 2 (Pendiente)
      - NotificacionVencimiento de Registro 1 → NoNotificarYaActualizado
```

### 3.2 Flujo de envío de emails

```
TEST: FlujoEmail_NotificacionPendiente_SeEnviaYMarcaComoNotificado
  Setup:
    - NotificacionVencimiento con Estado = NotificacionPendiente
    - Curso con vigencia vencida
    - Empresa con email configurado

  Step 1: Ejecutar EnviarEmailsEmpresa()
    Then:
      - Se envía email a la empresa
      - Estado cambia a Notificado
```

---

## 4. Tests de regresión

Estos tests verifican que las funcionalidades existentes no se rompieron:

```
TEST: Regresion_ImportarExcel_EstadoSeAsignaCorrectamente
  Given: Archivo Excel con columna "Aprobado" = "Si"
  When:  Se ejecuta la importación (CustomToolsController)
  Then:  Estado == Aprobado (no se usa propiedad Aprobado)

TEST: Regresion_VistaDetalle_MuestraEstadoSinAprobado
  Given: Capacitado con RegistrosCapacitacion
  When:  Se accede a la vista de detalle
  Then:  Se muestra Estado como texto, no "Aprobado: Sí/No"

TEST: Regresion_CertificadoHelper_FuncionaConNuevoModelo
  Given: RegistroCapacitacion con Estado = Aprobado
  When:  Se genera certificado
  Then:  Se genera correctamente sin depender de propiedad Aprobado
```

---

## 5. Prioridad de implementación

### Fase 1 – Alta prioridad (cubren la lógica central)

| # | Test | Riesgo que mitiga |
|---|------|-------------------|
| 1 | CambiarEstado – cambios básicos (5 tests) | Regresión en lógica de estados |
| 2 | CrearNotificacionVencimiento (4 tests) | Notificaciones no se crean |
| 3 | ActualizarObsoletas (4 tests) | Notificaciones duplicadas llegan a empresas |
| 4 | Calificar (3 tests) | Notas no cambian estado correctamente |

### Fase 2 – Media prioridad (cubren flujos de usuario)

| # | Test | Riesgo que mitiga |
|---|------|-------------------|
| 5 | Jornadas – aprobación masiva (3 tests) | Aprobación masiva no genera notificaciones |
| 6 | ObtenerNotificaciones (2 tests) | Se muestran notificaciones incorrectas |
| 7 | LimpiarObsoletas (2 tests) | Notificaciones heredadas no se limpian |

### Fase 3 – Baja prioridad (integración y regresión)

| # | Test | Riesgo que mitiga |
|---|------|-------------------|
| 8 | Flujo completo (1 test) | Interacción entre componentes falla |
| 9 | Regresión (3 tests) | Funcionalidades existentes rotas |
| 10 | Propiedades computadas (5 tests) | Vista muestra datos incorrectos |

---

## 6. Cobertura esperada

| Componente | Tests | Cobertura estimada |
|---|---|---|
| `RegistroCapacitacion.CambiarEstado()` | 8 | ~95% |
| `RegistroCapacitacion.Calificar()` | 3 | ~90% |
| `RegistroCapacitacion` propiedades | 5 | ~100% |
| `NotificacionesVencimientosController` | 12 | ~80% |
| `JornadasController` (calificación) | 3 | ~70% |
| **Total** | **~36 tests** | **~85% de la lógica refactorizada** |

---

## 7. Notas de implementación

### Configuración del proyecto de tests

```xml
<!-- Cursos.Tests.csproj - Paquetes NuGet necesarios -->
<PackageReference Include="MSTest.TestFramework" Version="3.1.1" />
<PackageReference Include="MSTest.TestAdapter" Version="3.1.1" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Effort.EF6" Version="2.2.16" />
```

### Patrón de test recomendado

```csharp
[TestClass]
public class RegistroCapacitacionTests
{
    [TestMethod]
    public void CambiarEstado_DeInscriptoAAprobado_CambiaEstadoCorrectamente()
    {
        // Arrange
        var registro = new RegistroCapacitacion
        {
            Estado = EstadosRegistroCapacitacion.Inscripto
        };

        // Act
        registro.CambiarEstado(EstadosRegistroCapacitacion.Aprobado, ejecutarAcciones: false);

        // Assert
        Assert.AreEqual(EstadosRegistroCapacitacion.Aprobado, registro.Estado);
    }
}
```

### Mock del DbContext para tests de controller

```csharp
public class TestDbContextFactory
{
    public static ApplicationDbContext CreateInMemory()
    {
        // Usar Effort para crear un DbContext in-memory
        var connection = Effort.DbConnectionFactory.CreateTransient();
        return new ApplicationDbContext(connection);
    }
}
```
