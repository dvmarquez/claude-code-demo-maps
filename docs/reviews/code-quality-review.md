# Code Quality Review — MapsDemo.Api

Fecha: 2026-05-22
Branch: feature/maps-autocomplete-geocode-history

---

## Resumen

- Total issues: 17
- Críticos: 4
- Advertencias: 7
- Sugerencias: 6

---

## Issues por archivo

### Program.cs

**[ADVERTENCIA] Carga manual del .env duplica lógica de AppDbContextFactory**
- Línea: 6–24
- Problema: `Program.cs` implementa su propio parser de `.env` con bucle manual, mientras que `AppDbContextFactory.cs` usa `dotenv.net`. La lógica existe dos veces con implementaciones distintas.
- Recomendación: Centralizar usando `DotEnv.Load` de `dotenv.net` y eliminar el parser manual.

**[ADVERTENCIA] Connection string construida sin validación de nulls**
- Línea: 36–40
- Problema: Si las variables de entorno no están configuradas, la cadena tendrá valores `null` incrustados sin advertencia. La aplicación arranca pero falla al primer acceso a BD.
- Recomendación: Validar que la configuración sea completa antes de registrar el `DbContext`, lanzando excepción descriptiva en startup.

**[SUGERENCIA] CORS fijo para `http://localhost:5173` sin configuración por entorno**
- Línea: 30–34
- Problema: El origen está hardcodeado y requiere recompilación para cambiar.
- Recomendación: Leer el origen de `appsettings.json` o variable de entorno `ALLOWED_ORIGIN`.

**[SUGERENCIA] Migraciones no se aplican automáticamente en startup**
- Línea: 47
- Problema: No hay `db.Database.MigrateAsync()` en startup. El paso es manual y puede omitirse.
- Recomendación: Considerar aplicación automática de migraciones en desarrollo, controlada por configuración.

---

### Endpoints/MapsEndpoints.cs

**[CRÍTICO] API key de Google Maps expuesta en URL del Geocoding API**
- Línea: 92–93
- Problema: La llamada a Geocoding API construye la URL con `?key={apiKey}`. La key queda expuesta en logs del servidor, historial del HttpClient y logs de red/proxy. La Places API v1 la envía correctamente como header `X-Goog-Api-Key`.
- Recomendación: Usar el header `X-Goog-Api-Key` también para Geocoding API, igual que se hace en autocomplete.

**[CRÍTICO] Validación de input en autocomplete sin longitud mínima**
- Línea: 25–26
- Problema: Solo se valida que `Input` no sea nulo. No se aplica el mínimo de 3 caracteres definido en el proyecto. Una sola letra genera requests a Google con costo innecesario.
- Recomendación: Agregar `if (request.Input.Trim().Length < 3) return Results.Problem(...)`.

**[CRÍTICO] SessionToken no validado como UUID v4**
- Línea: 20–74, 76–146
- Problema: El `sessionToken` se acepta y reenvía a Google sin validación de formato. El proyecto especifica "sessionToken validado como UUID v4 antes de procesar".
- Recomendación: Agregar `if (!Guid.TryParse(request.SessionToken, out _)) return Results.BadRequest(...)`.

**[CRÍTICO] Desreferencia sin guardia en `results[0]` puede lanzar excepción**
- Línea: 113–116
- Problema: Cuando Geocoding API retorna `status == "OK"` pero `results` es array vacío (documentado por Google como posible), `results[0]` lanza excepción. El `try/catch` captura el error como "Geocode failed" sin distinción.
- Recomendación: Verificar que el array `results` tenga al menos un elemento antes de acceder al índice 0. Retornar 404 o 422 con mensaje descriptivo si está vacío.

**[ADVERTENCIA] BadRequest sin ProblemDetails RFC 9457**
- Líneas: 26, 83
- Problema: Las respuestas de validación usan `Results.BadRequest(new { title = "..." })` (objeto anónimo) en vez de `ProblemDetails`. Rompe consistencia con el resto del código.
- Recomendación: Reemplazar con `Results.Problem(title: "...", statusCode: 400)`.

**[ADVERTENCIA] API key leída en cada request, no en startup**
- Líneas: 28–30, 85–87
- Problema: `Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY")` se llama en cada invocación. El error de configuración se descubre en runtime durante el primer request.
- Recomendación: Leer la API key una vez en startup, validar que no sea nula, e inyectarla via DI.

**[ADVERTENCIA] Ausencia de logs en paths exitosos**
- Líneas: 20–74, 76–146, 148–173
- Problema: Solo se loguea en errores. No hay `LogInformation` al inicio de cada handler ni al retornar resultado exitoso, dificultando el trazado en producción.
- Recomendación: Agregar log de inicio (sin loguear datos del usuario) y log de resultado exitoso en cada handler.

**[ADVERTENCIA] HandleHistory sin ILogger ni manejo de excepciones**
- Línea: 148–173
- Problema: Es el único handler sin `ILogger` ni `try/catch`. Una falla de BD propaga un 500 sin formato `ProblemDetails` ni log estructurado.
- Recomendación: Agregar `ILogger<Program> logger`, envolver en `try/catch`, retornar `Results.Problem(...)` en el catch.

**[ADVERTENCIA] `pageSize` sin límite superior**
- Línea: 151
- Problema: Un cliente puede pasar `pageSize=100000`, cargando toda la tabla en memoria.
- Recomendación: Validar `if (pageSize < 1 || pageSize > 100) return Results.BadRequest(...)`.

**[ADVERTENCIA] `page` no valida valores menores o iguales a cero**
- Línea: 151
- Problema: `page=0` produce `.Skip(-pageSize)`, generando excepción de BD o comportamiento indefinido.
- Recomendación: Agregar `if (page < 1) return Results.BadRequest(...)`.

**[SUGERENCIA] CancellationToken no propagado en handlers**
- Líneas: 20, 76, 148
- Problema: Si el cliente cancela la request, las llamadas a HttpClient y `SaveChangesAsync` continúan sin posibilidad de cancelación.
- Recomendación: Agregar `CancellationToken cancellationToken` como parámetro final en cada handler y propagarlo.

**[SUGERENCIA] ResponsePayload almacena el JSON completo de Google Maps**
- Línea: 130
- Problema: Se persiste el body completo de Geocoding API cuando solo se necesitan lat/lng. Genera almacenamiento innecesario.
- Recomendación: Si no hay caso de uso documentado para el payload completo, almacenar solo los campos necesarios.

---

### Data/AppDbContext.cs

**[ADVERTENCIA] Ausencia de índice en `queried_at`**
- Línea: 38–41
- Problema: La columna `queried_at` se usa en filtro (`WHERE`) y ordenamiento (`ORDER BY DESC`) sin índice. Cada query de historial realiza un sequential scan sobre toda la tabla.
- Recomendación: Agregar en `OnModelCreating`: `entity.HasIndex(e => e.QueriedAt).HasDatabaseName("IX_geocode_queries_queried_at");` y generar la migración correspondiente.

---

### Data/AppDbContextFactory.cs

**[SUGERENCIA] Inconsistencia en carga del .env vs Program.cs**
- Línea: 1, 21
- Problema: La factory usa `DotEnv.Load` de `dotenv.net` mientras `Program.cs` tiene su propio parser manual. Dos formas distintas de hacer lo mismo en el mismo proyecto.
- Recomendación: Unificar usando `dotenv.net` en ambos lugares.

---

### Data/GeocodeQuery.cs

**[SUGERENCIA] Propiedades de entidad sin `required` keyword**
- Línea: 6–8
- Problema: Las propiedades string se inicializan a `string.Empty` para evitar nullable warnings, pero no expresan la intención de no-nulabilidad con la keyword `required` de C# 11.
- Recomendación: Usar `public required string Address { get; set; }` para las propiedades obligatorias.

---

### DTOs

**[SUGERENCIA] DTOs de request sin DataAnnotations para validación declarativa**
- Archivos: `AutocompleteRequest.cs`, `GeocodeRequest.cs`
- Problema: No hay atributos `[Required]`, `[MinLength(3)]`, etc. .NET 10 soporta validación declarativa via `WithParameterValidation()`.
- Recomendación: Agregar atributos de validación y habilitar `WithParameterValidation()` en los endpoints.

---

### Migrations/20260507115624_InitialCreate.cs

Sin issues críticos. La migración refleja correctamente el modelo. Se confirma la ausencia del índice en `queried_at` (reportado en AppDbContext.cs).

---

### Properties/launchSettings.json

Sin issues. El perfil `http` expone el puerto 5000 conforme al CLAUDE.md.

---

## Conclusión

El código base es funcional y sigue la mayoría de las convenciones del proyecto. Los cuatro issues críticos deben resolverse antes de cualquier despliegue:

1. **API key en URL de Geocoding** — compromete la confidencialidad en entornos con logging habilitado
2. **Sin validación de longitud mínima en autocomplete** — genera costos innecesarios en Google Maps
3. **SessionToken sin validación UUID v4** — incumple un requisito explícito del proyecto
4. **Acceso sin guardia a `results[0]`** — puede provocar excepciones no controladas

Entre las advertencias, la ausencia de índice en `queried_at` es la de mayor impacto en performance a medida que la tabla crezca, y la falta de límite en `pageSize` representa un vector de abuso de recursos.
