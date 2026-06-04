# Code Quality Review — MapsDemo.Api

Fecha: 2026-06-04
Branch: feature/maps-autocomplete-geocode-history

---

## Resumen

- Total issues: 13
- Críticos: 1
- Advertencias: 7
- Sugerencias: 5

---

## Issues por Archivo

### Program.cs

**[ADVERTENCIA] Carga manual del .env duplica lógica de AppDbContextFactory**
- Líneas: 6–24
- Problema: `Program.cs` implementa su propio parser de `.env` con bucle manual, mientras que `AppDbContextFactory.cs` usa `dotenv.net`. La misma responsabilidad está cubierta dos veces con implementaciones distintas. El parser manual no maneja valores con `=` dentro del valor (por ejemplo, cadenas de conexión con `Password=abc=123`), pues parte en el primer `=` encontrado.
- Recomendación: Eliminar el parser manual y llamar a `DotEnv.Load` de `dotenv.net` en ambos lugares.

**[ADVERTENCIA] Connection string construida sin validación de nulls**
- Líneas: 36–40
- Problema: Si `POSTGRES_CONNECTION_STRING` no está definida y alguna de `POSTGRES_DB`, `POSTGRES_USER` o `POSTGRES_PASSWORD` tampoco lo está, la cadena de conexión contendrá el literal `null` incrustado. La aplicación arranca sin error, pero falla al primer acceso a la base de datos con un mensaje de error poco descriptivo.
- Recomendación: Validar que la configuración esté completa antes de registrar el `DbContext`; lanzar una excepción descriptiva en startup si falta alguna variable obligatoria.

**[ADVERTENCIA] CORS hardcodeado para un único origen**
- Líneas: 30–34
- Problema: El origen `http://localhost:5173` está fijo en código. Requiere recompilación para ajustarlo en otro entorno (staging, producción). Además, solo cubre HTTP; si el frontend corre en HTTPS la política bloquea los requests.
- Recomendación: Leer el origen desde `appsettings.json` o una variable de entorno (`ALLOWED_ORIGINS`), permitiendo una lista separada por comas para mayor flexibilidad.

---

### Endpoints/MapsEndpoints.cs

**[CRÍTICO] HandleHistory sin manejo de excepciones ni ILogger**
- Líneas: 153–178
- Problema: Es el único handler que carece de bloque `try/catch` e `ILogger`. Cualquier error de base de datos (timeout, pérdida de conexión, excepción de EF Core) propaga una excepción no controlada hacia el middleware. ASP.NET devuelve un 500 genérico sin formato `ProblemDetails` y sin log estructurado, lo que hace imposible diagnosticar el problema en producción.
- Recomendación: Inyectar `ILogger<Program> logger` en el handler, envolver el cuerpo en `try/catch` y retornar `Results.Problem("Internal error fetching history", statusCode: 500)` en el bloque catch con `logger.LogError(ex, "History query failed")`.

**[ADVERTENCIA] pageSize sin límite superior y page sin validación de mínimo**
- Líneas: 155–156
- Problema: Un cliente puede enviar `pageSize=1000000`, lo que fuerza a EF Core a cargar millones de filas en memoria. Asimismo, `page=0` produce `.Skip(-pageSize)`, que puede arrojar excepción en el proveedor de base de datos o devolver datos inesperados.
- Recomendación: Agregar validación explícita antes de ejecutar la consulta:
  ```csharp
  if (page < 1) return Results.Problem(title: "page must be >= 1", statusCode: 400);
  if (pageSize < 1 || pageSize > 100) return Results.Problem(title: "pageSize must be between 1 and 100", statusCode: 400);
  ```

**[ADVERTENCIA] API key leída en cada request, no en startup**
- Líneas: 34–36, 94–96
- Problema: `Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY")` se invoca en cada request. El error de configuración se descubre en runtime durante la primera llamada, no durante el arranque. Adicionalmente, la variable se lee dos veces (una por endpoint) cuando podría centralizarse.
- Recomendación: Leer y validar la API key en `Program.cs` durante el arranque (fallando con excepción descriptiva si no está configurada) e inyectarla vía `IOptions<T>` o como string nombrado a través de DI.

**[ADVERTENCIA] Ausencia de logs en paths exitosos**
- Líneas: 20, 82, 153
- Problema: Solo se registran eventos de error. No existe `LogInformation` al inicio de cada handler ni al retornar un resultado exitoso. Esto dificulta trazar el flujo completo en producción y correlacionar requests con respuestas de Google.
- Recomendación: Agregar al inicio de cada handler un log de nivel `Information` con el nombre de la operación (sin incluir datos sensibles del usuario) y otro al completar exitosamente, indicando el número de resultados o el ID persistido.

**[ADVERTENCIA] PlaceId interpolado en URL sin validación de formato**
- Línea: 101
- Problema: `Uri.EscapeDataString` escapa caracteres especiales, pero no valida que `PlaceId` tenga el formato esperado de Google (prefijo `places/` o similar). Un valor arbitrariamente largo o con caracteres inesperados se envía directamente a la API externa. Aunque el riesgo de inyección HTTP es bajo dado el escape, un `PlaceId` malformado genera una llamada innecesaria a Google que retorna 404.
- Recomendación: Agregar una validación básica de longitud máxima (por ejemplo, 500 caracteres) y opcionalmente verificar que no esté vacío después del trim.

**[SUGERENCIA] CancellationToken no propagado en ningún handler**
- Líneas: 20, 82, 153
- Problema: Si el cliente cancela la request HTTP, las llamadas a `client.SendAsync`, `db.SaveChangesAsync` y `query.CountAsync`/`ToListAsync` continúan ejecutándose hasta completar. Esto desperdicia recursos del hilo de trabajo y conexiones a la base de datos.
- Recomendación: Agregar `CancellationToken cancellationToken` como parámetro final en cada handler y propagarlo a `SendAsync(httpRequest, cancellationToken)`, `SaveChangesAsync(cancellationToken)`, `CountAsync(cancellationToken)` y `ToListAsync(cancellationToken)`.

---

### Data/AppDbContext.cs

**[ADVERTENCIA] Ausencia de índice en queried_at**
- Líneas: 38–41
- Problema: La columna `queried_at` se usa en cláusulas `WHERE` (filtros `dateFrom`/`dateTo`) y en `ORDER BY DESC` dentro de `HandleHistory`. Sin índice, cada consulta de historial realiza un sequential scan completo de la tabla. El impacto es insignificante con pocos registros pero se vuelve crítico en producción a medida que la tabla crece.
- Recomendación: Agregar en `OnModelCreating`:
  ```csharp
  entity.HasIndex(e => e.QueriedAt)
        .HasDatabaseName("IX_geocode_queries_queried_at");
  ```
  y generar la migración correspondiente con `dotnet ef migrations add AddIndexQueriedAt`.

---

### Data/AppDbContextFactory.cs

**[SUGERENCIA] Inconsistencia en mecanismo de carga del .env**
- Líneas: 1, 21
- Problema: La factory usa `DotEnv.Load` de `dotenv.net` mientras `Program.cs` tiene su propio parser manual basado en `File.ReadAllLines`. Dos implementaciones distintas para el mismo propósito aumentan el riesgo de comportamiento diferente (por ejemplo, ante valores con `=` o espacios alrededor del signo igual).
- Recomendación: Unificar usando `dotenv.net` en ambos archivos para consistencia y corrección.

---

### Data/GeocodeQuery.cs

**[SUGERENCIA] Propiedades string sin keyword required**
- Líneas: 6–8
- Problema: Las propiedades `Address`, `RequestPayload` y `ResponsePayload` se inicializan a `string.Empty` para satisfacer el análisis de nullability, pero esto oculta errores: si una propiedad queda sin asignar el código compila sin advertencia y persiste una cadena vacía.
- Recomendación: Marcar las propiedades obligatorias con `required`:
  ```csharp
  public required string Address { get; set; }
  ```
  Esto hace que el compilador exija la asignación en todos los puntos de construcción.

---

### DTOs (AutocompleteRequest.cs, GeocodeRequest.cs)

**[SUGERENCIA] Sin atributos de validación declarativa**
- Archivos: `AutocompleteRequest.cs`, `GeocodeRequest.cs`
- Problema: Las validaciones de formato y longitud se implementan manualmente con `if/return` en cada handler. .NET 10 soporta validación declarativa mediante atributos `[Required]`, `[MinLength(3)]`, `[MaxLength(500)]` combinados con `WithParameterValidation()` en el registro del endpoint.
- Recomendación: Agregar data annotations en los records y habilitar `app.MapPost(...).WithParameterValidation()`. Esto centraliza la validación, la hace visible en OpenAPI y elimina código repetitivo en los handlers.

---

### Migrations/20260507115624_InitialCreate.cs

Sin issues propios. La migración refleja correctamente el modelo definido en `AppDbContext`. Se confirma la ausencia del índice sobre `queried_at` (reportado en la sección de `AppDbContext.cs`).

---

## Archivos Revisados

- `Program.cs` — con advertencias
- `Endpoints/MapsEndpoints.cs` — con advertencias y un crítico
- `Data/AppDbContext.cs` — con advertencia
- `Data/AppDbContextFactory.cs` — con sugerencia
- `Data/GeocodeQuery.cs` — con sugerencia
- `DTOs/AutocompleteRequest.cs` — con sugerencia
- `DTOs/AutocompleteSuggestion.cs` — sin issues
- `DTOs/GeocodeRequest.cs` — con sugerencia
- `DTOs/GeocodeResult.cs` — sin issues
- `DTOs/PagedResult.cs` — sin issues
- `Migrations/20260507115624_InitialCreate.cs` — sin issues propios

---

## Conclusión

Respecto a la revisión anterior (2026-05-22), los cuatro issues críticos han sido resueltos correctamente:

- La API key de Google Maps ahora se envía como header `X-Goog-Api-Key` en ambos endpoints.
- La validación de longitud mínima de 3 caracteres en autocomplete está implementada.
- El sessionToken se valida como UUID con `Guid.TryParse` antes de procesar.
- El acceso a `results[0]` fue reemplazado por la integración con Places Details API v1 que retorna un objeto único.

El único issue crítico restante es `HandleHistory` sin manejo de excepciones, que puede producir respuestas malformadas ante fallos de base de datos. Las advertencias de mayor impacto operativo son la ausencia del índice en `queried_at` y la falta de validación de `pageSize`, que representan vectores de degradación de performance en producción.
