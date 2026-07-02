# Guía de Desarrollo con IA — .NET (Clean Architecture + CQRS) · v3 (compacta)

Reglas **obligatorias** para IA que genere/modifique código. Ante conflicto, **estas reglas prevalecen** sobre el modelo, pero **el código/convención existente del proyecto prevalece sobre este documento**. Si el código existente viola una regla, **reportarlo**, no propagarlo.
**DEBE/NO DEBE** = inviolable · **DEBERÍA** = recomendación fuerte · **PUEDE** = opcional.
**Stack:** .NET 8 · ASP.NET Core · EF Core · MediatR · FluentValidation · Mapster · Asp.Versioning · xUnit.

## Reglas núcleo (nunca se rompen)
1. **Controllers delgados:** solo `ISender`/`IMediator`. Nada de lógica, SQL, `DbContext` ni repositorios.
2. **Nunca exponer entidades de dominio** por la API: siempre DTOs (`record`).
3. **Validación técnica** en FluentValidation; **reglas de negocio y validación contra BD** en Handler/Domain.
4. **Dependencias:** `Domain ← Application ← Infrastructure/API`. Nunca invertir.
5. **Commands/Queries/DTOs** = `record` inmutable; **un Handler no llama a otro**.
6. **Async:** propagar `CancellationToken`; prohibido `async void`, `.Result`, `.Wait()`.
7. **Errores** vía middleware global (ProblemDetails); nunca exponer `Exception.Message` técnico.
8. **Leer solo lo necesario:** proyectar al DTO, `AsNoTracking`, paginar/ordenar/filtrar en BD; sin N+1; sin `SELECT *`.
9. **Seguro por defecto:** authz obligatoria, DTOs explícitos (anti over-posting), nunca secretos en el código.
10. **Proceso:** trabajo multiarchivo → resumen + aprobación antes de codificar; tocar solo lo necesario; dejar build/format/tests en verde.

## Principios (aplican a TODO el código)
- **Clean Architecture:** negocio desacoplado de frameworks; dependencias según la tabla de capas.
- **SOLID:** **S** una clase/Handler = una responsabilidad · **O** extender sin modificar (abstracciones/polimorfismo, no `if`/`switch` por tipo) · **L** toda implementación cumple el contrato de su puerto · **I** interfaces pequeñas y específicas (sin métodos que el cliente no usa) · **D** depender de puertos, nunca de implementaciones concretas.
- **Alta cohesión / bajo acoplamiento:** agrupar lo que cambia junto (por feature); comunicación entre capas solo por interfaces.
- **DRY + reutilizar antes de crear:** buscar y extender lo existente antes de duplicar o crear nuevo (sin romper SRP).
- **Composición sobre herencia.** **Claridad sobre complejidad:** la solución más simple que cumpla el requerimiento.

## Flujo
Comprender requerimiento → identificar reglas de negocio → buscar código reutilizable → diseñar/analizar impacto → (si multiarchivo) plan + aprobación → implementar **completo y operativo** → revisar. **No asumir** reglas inexistentes. **Preguntar solo** si la respuesta cambia el diseño.

## Capas y responsabilidades
| Capa | Contenido | Depende de |
|---|---|---|
| **Domain** | Entidades, VOs, Enums, excepciones de dominio, reglas. Sin frameworks. | — |
| **Application** | Commands/Queries/Handlers, DTOs, puertos (interfaces), Validators, behaviors. | Domain |
| **Infrastructure** | EF Core, repos, JWT, servicios externos. Implementa los puertos. | Application, Domain |
| **API** | Controllers, middleware, DI, config, Swagger. Orquesta vía MediatR. | Application (+Infra solo en DI) |

- Domain sin dependencias a otras capas/frameworks.
- Application **PUEDE** usar abstracciones de `Microsoft.EntityFrameworkCore` para lectura (vía puerto `IAppDbContext`), **no** el proveedor (Npgsql/SqlServer) ni el `DbContext` concreto. *(Decisión opinada estilo Jason Taylor; cambiarla por repos+Specification si se exige Application libre de EF.)*

## Estructura y nombres
- Organización **por feature** en Application (`Features/<X>/Commands|Queries|Dtos`).
- Un archivo = un tipo. Sufijos: `Command`, `Query`, `Handler`, `Validator`, `Dto`, `Configuration`, `Repository`. Métodos async terminan en `Async`.

## CQRS
- Commands modifican; Queries consultan. Command no devuelve entidad (devuelve `void`/`id`/DTO).
- Un Handler = un caso de uso. La lógica compartida va a un servicio de dominio/aplicación.
- **Pipeline behaviors** (no repetir en cada Handler): `ValidationBehavior` (FluentValidation antes del Handler → `ValidationException`), `LoggingBehavior`, `UnitOfWorkBehavior` (envuelve **solo Commands** en transacción).

## Validación
- FluentValidation = solo técnica (requerido, longitud, formato, rango, regex). **Prohibido** `MustAsync`/repos/servicios en validators.
- Validación contra persistencia y reglas de negocio → en el Handler/Domain.

## Mapeo
- **Mapster** centralizado en Application. Prohibido mapeo manual repetido en Handlers y mezclar con AutoMapper. Para lectura, proyectar al DTO en la consulta (`Select`/`ProjectToType`).

## Controllers
- Reciben HTTP → delegan en `ISender` → devuelven HTTP con el contrato de abajo. Solo inyectan `ISender`/`IMediator`.

## Contrato de respuesta
- **Éxito:** envelope `{ "success": true, "data": <DTO>, "traceId": "..." }`. Códigos: 200 (lectura/update), 201 (create, con `Location`), 204 (sin cuerpo).
- **Error:** **ProblemDetails (RFC 7807)** desde el middleware global, con `traceId` y `errors` (si validación).
- **Mapeo excepción → HTTP** (única fuente: middleware global):

| Excepción | HTTP |
|---|---|
| `ValidationException` | 400 |
| `NotFoundException` | 404 |
| `BusinessRuleException` / invariante | 409 |
| `UnauthorizedAccessException` | 401 |
| Falta de permiso | 403 |
| `DbUpdateConcurrencyException` | 409 |
| No controlada | 500 |

## Errores y logging
- Middleware global único; **sin** `try/catch` repetidos. Nunca devolver `Exception.Message` técnico al cliente (mensaje genérico + detalle solo en `ILogger`).
- `ILogger<T>` con **message templates** (`logger.LogInformation("Creado {Id}", id)`, no interpolación). Niveles correctos. **Nunca** loguear datos sensibles.

## Paginación
- `PagedRequest(Page, PageSize, Sort?, Search?)` → `PagedResult<T>(Items, Total, Page, PageSize)`. `PageSize` con tope (p.ej. 100) en el Handler.
- **Orden seguro:** `Sort` se resuelve contra **lista blanca** de campos; valor inválido → orden por defecto. Nunca SQL/`OrderBy` dinámico desde texto del usuario.

## Persistencia (EF Core)
- `DbContext`/repos solo en Infrastructure; repos solo persisten (sin reglas de negocio). **Un repo por agregado** (no `IRepository<T>` genérico).
- Lectura: `AsNoTracking` + proyección al DTO. `Include` solo si necesario. Sin N+1.
- Config por entidad con `IEntityTypeConfiguration<T>` (no data annotations dispersas). Enums como `int`; VOs como *owned types*. Fechas en **UTC**.
- Concurrencia optimista (`rowversion`) → conflicto = 409. Escrituras vía `IUnitOfWork.SaveChangesAsync(ct)`.

## Migraciones y transacciones
- Migraciones generadas desde el modelo (`dotnet ef migrations add`); no editar migraciones aplicadas salvo corrección justificada.
- Una transacción = una operación de negocio (la cubre `UnitOfWorkBehavior`).

## Seguridad
- JWT + Roles/Claims/Policies. **Seguro por defecto** (fallback policy autenticada; lo público con `[AllowAnonymous]` explícito).
- Permisos de negocio en el **Handler**, usando el puerto `ICurrentUserService` (no `HttpContext` en Application).
- DTOs/Commands explícitos (anti over-posting). Nunca secretos/contraseñas en código ni en texto plano; nunca exponer SQL/config/tokens/detalles internos.

## Configuración, fechas, Swagger, rendimiento, testing
- Config vía `IOptions<T>` (sin hardcode). Fechas en UTC con `DateTimeOffset`; usar `TimeProvider` (no `DateTime.UtcNow`).
- Swagger: todo endpoint documentado **en español y con ejemplos** (resumen, descripción, params, request, response, errores, auth) vía `[ProducesResponseType]`.
- Rendimiento: solo lo necesario, sin `Include`/procesamiento innecesario; optimizar solo con evidencia; liberar `IDisposable`.
- Testing (xUnit + FluentAssertions + NSubstitute, AAA, `Metodo_Estado_Resultado`): priorizar Handlers, reglas de negocio, permisos, validaciones y errores. Mockear puertos, no el dominio. Integración con `WebApplicationFactory` + Testcontainers. No probar DTOs/config trivial.

## Anti-patrones (NUNCA)
Devolver entidades por la API · `DbContext`/SQL/repos/lógica en el Controller · inyectar algo ≠ `ISender` en el Controller · reglas de negocio o BD en un Validator · un Handler llamando a otro · `async void`/`.Result`/`.Wait()` · omitir `CancellationToken` · `SELECT *`/traer la entidad completa · tragar excepciones · devolver `Exception.Message` técnico · estado estático mutable · invertir dependencias · mezclar excepciones con `Result<T>` o Mapster con AutoMapper.

## Comandos
```bash
dotnet build            # warnings = errores
dotnet format           # según .editorconfig
dotnet test             # pruebas
dotnet ef migrations add <N> -p Infrastructure -s Api
dotnet ef database update     -p Infrastructure -s Api
dotnet run --project src/Api
```

## Terminado cuando
Cumple el requerimiento y es operativo · arquitectura y dependencias respetadas · SOLID, sin duplicación · validado · seguro (authz, sin secretos, DTOs explícitos) · errores por middleware con el contrato · logging estructurado · Swagger en español con ejemplos · pruebas relevantes en verde · `build`/`format` limpios · solo se tocaron los archivos necesarios.

**Regla final:** calidad de arquitectura sobre velocidad. Siempre la solución más simple, mantenible, reutilizable y consistente con el proyecto.

