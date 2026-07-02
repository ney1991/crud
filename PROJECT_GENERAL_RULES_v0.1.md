# Reglas Generales del Proyecto

**Versión:** 1.0 (Base)

## Objetivo

Construir una API .NET mantenible, desacoplada, escalable y preparada
para evolucionar sin modificar la lógica del negocio.

## Filosofía del proyecto

-   El negocio tiene prioridad sobre la tecnología.
-   La mantenibilidad tiene prioridad sobre la rapidez de
    implementación.
-   La simplicidad prevalece sobre soluciones innecesariamente
    complejas.
-   Reutilizar componentes existentes antes de crear nuevos.
-   Toda decisión debe favorecer la escalabilidad y el desacoplamiento.

## Convenciones de idioma

-   Código fuente, clases, propiedades, tablas y columnas en inglés.
-   Swagger completamente documentado en español.
-   Mensajes al usuario en español.

## Arquitectura

-   Clean Architecture, SOLID y CQRS.
-   Domain nunca depende de Infrastructure.
-   Todas las dependencias apuntan hacia el centro.
-   Una responsabilidad por clase.

## Tecnología

-   ASP.NET Core.
-   Entity Framework Core.
-   PostgreSQL como motor inicial.
-   La arquitectura permitirá reemplazar PostgreSQL sin modificar Domain
    ni Application.
-   MediatR.
-   FluentValidation.

## Persistencia

-   Repository y UnitOfWork abstraen la persistencia.
-   Nunca acceder al DbContext desde Controllers ni Application.
-   Todas las entidades utilizarán Guid como clave primaria.
-   Los Guid serán generados por la aplicación.

## Auditoría

Todas las entidades persistentes heredarán de `AuditableEntity`.

Campos: - Id (Guid) - CreatedAt (DateTimeOffset) - CreatedBy (Guid?) -
UpdatedAt (DateTimeOffset) - UpdatedBy (Guid?)

Reglas: - Todas las fechas se almacenarán en UTC. - Se utilizará
DateTimeOffset. - CreatedAt se asignará automáticamente al crear. -
UpdatedAt se actualizará automáticamente en cada modificación. -
CreatedBy y UpdatedBy serán automáticos. - Mientras no exista
autenticación podrán ser null. - Ninguna entidad redefinirá estos
campos.

## Internacionalización

-   La aplicación será global.
-   Cada usuario tendrá un TimeZoneId (IANA).
-   Nunca almacenar offsets UTC.
-   Las fechas se almacenarán en UTC.
-   La conversión a hora local se realizará únicamente utilizando
    TimeZoneId.
-   Nunca realizar conversiones manuales de zonas horarias.

## API

-   Controllers delgados.
-   Comunicación exclusivamente mediante ISender/MediatR.
-   Versionado desde la primera versión.
-   Swagger obligatorio, en español y con ejemplos.
-   Los errores utilizarán ProblemDetails.

## Validaciones

-   FluentValidation: validaciones técnicas.
-   Domain/Application: reglas de negocio.

## Logging

-   Logging estructurado mediante ILogger.
-   Nunca registrar datos sensibles.
-   Las excepciones serán registradas por el middleware global.

## Seguridad

-   Nunca exponer información interna.
-   Nunca devolver entidades de dominio.
-   Toda comunicación externa utilizará DTOs.

## Configuración

-   Toda configuración utilizará IOptions.
-   No habrá valores hardcodeados.
-   Secretos y cadenas de conexión fuera del código fuente.

## Base de datos

-   Restricciones críticas en negocio y base de datos.
-   Índices únicos para datos únicos.
-   Solo las migraciones modificarán el esquema.

## Testing

-   Casos de uso con pruebas unitarias.
-   Endpoints críticos con pruebas de integración.

## Calidad

No se aceptarán implementaciones que: - Rompan Clean Architecture. -
Introduzcan acoplamiento innecesario. - Dupliquen lógica. - Dificulten
las pruebas. - Reduzcan la mantenibilidad.
