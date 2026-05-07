# Code Quality Review — MapsDemo.Api

**Fecha:** 2026-05-07
**Branch:** main
**Archivos revisados:**
- `Program.cs`
- `Data/GeocodeQuery.cs`
- `Data/AppDbContext.cs`
- `Data/AppDbContextFactory.cs`
- `DTOs/AutocompleteRequest.cs`
- `DTOs/AutocompleteSuggestion.cs`
- `DTOs/GeocodeRequest.cs`
- `DTOs/GeocodeResult.cs`
- `DTOs/PagedResult.cs`
- `Endpoints/MapsEndpoints.cs`

---

## Resumen
- Criticos: 4
- Advertencias: 6
- Sugerencias: 5

---

## Issues encontrados

### Program.cs

#### [CRITICO] — API key expuesta en URL de geocoding

**Linea:** `MapsEndpoints.cs:93`
**Problema:** La API key se inyecta directamente en la URL del request GET a la Geocoding API: `?place_id=...&key={apiKey}`. Las URLs completas (incluyendo query strings) son frecuentemente registradas por servidores proxy, balanceadores de carga, middleware de ASP.NET, y herramientas de observabilidad como Application Insights o Datadog. Esto expone la API key en logs del sistema, lo cual viola el requerimiento explícito del proyecto: "La API key de Google Maps NUNCA debe aparecer en logs ni responses".
**Recomendacion:** Mover la API key a un header HTTP igual que se hace en el endpoint de autocomplete (`X-Goog-Api-Key`). La Geocoding API admite autenticacion via header.
**Ejemplo de fix:**
```csharp
var url = $"https://maps.googleapis.com/maps/api/geocode/json?place_id={Uri.EscapeDataString(request.PlaceId)}";
var geoRequest = new HttpRequestMessage(HttpMethod.Get, url);
geoRequest.Headers.Add("X-Goog-Api-Key", apiKey);
var response = await client.SendAsync(geoRequest);
```

#### [CRITICO] — Sin validacion de longitud minima en autocomplete

**Linea:** `MapsEndpoints.cs:25-26`
**Problema:** La validacion solo verifica que `Input` no sea nulo o vacio (`IsNullOrWhiteSpace`), pero el criterio de revision establece un minimo de 3 caracteres. Enviar 1 o 2 caracteres a la Places API genera llamadas innecesarias y puede resultar en costos elevados de API. Ademas, las sugerencias para inputs muy cortos tienen baja calidad.
**Recomendacion:** Agregar validacion de longitud minima antes de llamar a la API externa.
**Ejemplo de fix:**
```csharp
if (string.IsNullOrWhiteSpace(request.Input) || request.Input.Trim().Length < 3)
    return Results.BadRequest(new ProblemDetails
    {
        Title = "Input must be at least 3 characters",
        Status = 400
    });
```

#### [CRITICO] — Sin validacion del sessionToken como UUID v4

**Linea:** `MapsEndpoints.cs:25, 82` (ambos handlers)
**Problema:** El `sessionToken` se recibe del cliente y se reenvía directamente a Google Places API sin ninguna validacion de formato. Un token malformado puede provocar errores de facturacion en Google (las sesiones no se agrupan correctamente), y un actor malicioso podría enviar valores arbitrarios como `"'; DROP TABLE geocode_queries;--"` o strings extremadamente largos en el campo sessionToken que se incluyen en el payload JSON enviado a Google.
**Recomendacion:** Validar que `sessionToken` sea un UUID v4 valido antes de procesarlo.
**Ejemplo de fix:**
```csharp
if (!Guid.TryParse(request.SessionToken, out _))
    return Results.BadRequest(new ProblemDetails
    {
        Title = "sessionToken must be a valid UUID",
        Status = 400
    });
```

#### [CRITICO] — Sin validacion del campo Address en el endpoint de geocode

**Linea:** `MapsEndpoints.cs:82-83`
**Problema:** Solo se valida que `PlaceId` no sea nulo/vacio, pero `Address` no tiene ninguna validacion. El `Address` se persiste directamente en la base de datos como `VARCHAR(500)`. Si llega un valor nulo, `GeocodeQuery.Address` sera `string.Empty` (por el inicializador de la entidad), lo cual produce registros sin informacion util. Si llegara un string de mas de 500 caracteres, EF Core lanzaria una excepcion en `SaveChangesAsync` que solo se captura como error generico 500, sin mensaje util al cliente.
**Recomendacion:** Validar que `Address` no sea nulo/vacio y que no supere 500 caracteres.
**Ejemplo de fix:**
```csharp
if (string.IsNullOrWhiteSpace(request.Address) || request.Address.Length > 500)
    return Results.BadRequest(new ProblemDetails
    {
        Title = "Address is required and must not exceed 500 characters",
        Status = 400
    });
```

---

### Endpoints/MapsEndpoints.cs

#### [ADVERTENCIA] — Acceso a indices de array sin verificacion en HandleGeocode

**Linea:** `MapsEndpoints.cs:113-116`
**Problema:** Se accede a `results[0]` directamente despues de verificar que `status == "OK"`, pero no se comprueba que el array `results` tenga al menos un elemento. Si Google retorna `"status": "OK"` pero `"results": []` (situacion poco frecuente pero posible), se lanzara una `IndexOutOfRangeException` que sera capturada como error 500 generico. Ademas, la cadena de `.GetProperty()` sin `TryGetProperty()` lanzara `KeyNotFoundException` si la estructura del response de Google cambia.
**Recomendacion:** Verificar que `results` tenga elementos antes de acceder al indice 0 y usar `TryGetProperty` en la cadena de acceso a `geometry/location`.
**Ejemplo de fix:**
```csharp
var results = doc.RootElement.GetProperty("results");
if (results.GetArrayLength() == 0)
{
    logger.LogWarning("Geocoding API returned OK but empty results for PlaceId: {PlaceId}", request.PlaceId);
    return Results.Problem("No results found for the given place", statusCode: 404);
}

if (!results[0].TryGetProperty("geometry", out var geometry) ||
    !geometry.TryGetProperty("location", out var location))
{
    logger.LogError("Unexpected Geocoding API response structure");
    return Results.Problem("Unexpected response from Geocoding API", statusCode: 502);
}
```

#### [ADVERTENCIA] — Respuesta de error no usa ProblemDetails consistentemente

**Linea:** `MapsEndpoints.cs:26, 83`
**Problema:** Las validaciones de entrada retornan `Results.BadRequest(new { title = "..." })` con un objeto anonimo, mientras que los errores de API externa retornan `Results.Problem(...)` que produce un `ProblemDetails` estandar (RFC 7807). Esto genera inconsistencia en el formato de las respuestas de error, obligando al frontend a manejar dos estructuras distintas para los errores.
**Recomendacion:** Usar `Results.Problem()` o `Results.ValidationProblem()` en todos los casos de error para garantizar formato consistente.
**Ejemplo de fix:**
```csharp
return Results.Problem(
    detail: "PlaceId is required",
    statusCode: 400,
    title: "Validation Error");
```

#### [ADVERTENCIA] — Log de error de Places API incluye el body completo del response

**Linea:** `MapsEndpoints.cs:49`
**Problema:** `logger.LogError("Places API error {Status}: {Body}", response.StatusCode, body)` registra el body completo del response de error de Google. Los responses de error de Google Maps pueden incluir informacion de diagnostico que menciona la API key o detalles de configuracion de la cuenta. Ademas, en entornos de alta concurrencia, los bodies de error pueden ser grandes y saturar el sistema de logs.
**Recomendacion:** Limitar el log a los primeros N caracteres del body o extraer solo el campo `error.message` del response JSON.
**Ejemplo de fix:**
```csharp
var truncatedBody = body.Length > 200 ? body[..200] + "..." : body;
logger.LogError("Places API error {Status}: {Body}", response.StatusCode, truncatedBody);
```

#### [ADVERTENCIA] — Sin CancellationToken en los handlers de endpoints

**Linea:** `MapsEndpoints.cs:20, 76, 148`
**Problema:** Ninguno de los tres handlers acepta `CancellationToken`. Si el cliente cancela la solicitud HTTP (cierra el browser, timeout del frontend), los requests en vuelo a Google Maps API y las operaciones de base de datos continuan ejecutandose, consumiendo recursos innecesariamente. En endpoints con alto trafico esto puede generar presion en el connection pool de PostgreSQL y en el HttpClient.
**Recomendacion:** Agregar `CancellationToken cancellationToken` como parametro y propagarlo a `client.SendAsync()`, `db.SaveChangesAsync()`, `query.CountAsync()` y `query.ToListAsync()`.
**Ejemplo de fix:**
```csharp
private static async Task<IResult> HandleAutocomplete(
    AutocompleteRequest request,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    // ...
    var response = await client.SendAsync(httpRequest, cancellationToken);
    // ...
}
```

#### [ADVERTENCIA] — HandleHistory no tiene manejo de errores

**Linea:** `MapsEndpoints.cs:148-173`
**Problema:** El handler `HandleHistory` no tiene bloque `try/catch`. Si la base de datos no esta disponible, si hay un timeout de conexion, o si los parametros de paginacion generan un overflow (por ejemplo `page = int.MaxValue` hace que `(page - 1) * pageSize` desborde), se lanzara una excepcion no manejada que resultara en una respuesta 500 sin formato ProblemDetails, exponiendo el stack trace en desarrollo o fallando de forma poco descriptiva.
**Recomendacion:** Envolver el cuerpo del handler en `try/catch` y agregar validacion de los parametros `page` y `pageSize`.
**Ejemplo de fix:**
```csharp
if (page < 1) page = 1;
if (pageSize < 1 || pageSize > 100) pageSize = 10;

try
{
    // ... logica existente
}
catch (Exception ex)
{
    logger.LogError(ex, "History query failed");
    return Results.Problem("Internal error retrieving history", statusCode: 500);
}
```

#### [ADVERTENCIA] — Sin indice en la columna queried_at

**Archivo:** `Data/AppDbContext.cs`
**Linea:** `AppDbContext.cs:38-41`
**Problema:** La consulta de historial ordena por `QueriedAt` descendente y filtra por rangos de fecha usando esta columna (`dateFrom`, `dateTo`), pero no hay ningun indice definido sobre `queried_at` en la configuracion de EF Core. A medida que la tabla `geocode_queries` crezca, las consultas sin indice realizaran full table scans en PostgreSQL, degradando el rendimiento significativamente.
**Recomendacion:** Agregar un indice sobre `queried_at` en `OnModelCreating`.
**Ejemplo de fix:**
```csharp
entity.HasIndex(e => e.QueriedAt)
      .HasDatabaseName("ix_geocode_queries_queried_at");
```

---

### Program.cs

#### [SUGERENCIA] — Parseo manual del archivo .env duplicado entre Program.cs y AppDbContextFactory.cs

**Linea:** `Program.cs:6-24` / `AppDbContextFactory.cs:11-21`
**Problema:** La logica de carga del archivo `.env` esta duplicada: `Program.cs` implementa el parseo manualmente (split por `=`, `SetEnvironmentVariable`), mientras que `AppDbContextFactory.cs` usa la libreria `dotenv.net` correctamente. Esto viola DRY y puede generar discrepancias si el formato del `.env` tiene casos borde (valores con `=` dentro, comillas, etc.) que `dotenv.net` maneja pero el parseo manual no.
**Recomendacion:** Usar `dotenv.net` en `Program.cs` de la misma forma que en `AppDbContextFactory.cs`.
**Ejemplo de fix:**
```csharp
// En Program.cs, reemplazar el bloque manual por:
using dotenv.net;
DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 5));
```

#### [SUGERENCIA] — CORS hardcodeado en lugar de leerse desde configuracion

**Linea:** `Program.cs:32`
**Problema:** El origen permitido `http://localhost:5173` esta hardcodeado en el codigo. En un entorno de staging o produccion, el frontend tendria una URL diferente y seria necesario modificar y recompilar el codigo para cambiarlo.
**Recomendacion:** Leer el origen permitido desde una variable de entorno o `appsettings.json`.
**Ejemplo de fix:**
```csharp
var allowedOrigin = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN") ?? "http://localhost:5173";
options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigin)
          .AllowAnyHeader()
          .AllowAnyMethod());
```

#### [SUGERENCIA] — AsNoTracking ausente en la consulta de historial

**Linea:** `MapsEndpoints.cs:155`
**Problema:** La consulta `db.GeocodeQueries.AsQueryable()` en `HandleHistory` carga entidades con tracking de EF Core activado por defecto. Dado que el historial es de solo lectura (solo se usa para proyectar a `GeocodeResult` con `.Select()`), el tracking es innecesario y agrega overhead de memoria y CPU al change tracker de EF Core.
**Recomendacion:** Agregar `.AsNoTracking()` para consultas de solo lectura.
**Ejemplo de fix:**
```csharp
var query = db.GeocodeQueries.AsNoTracking().AsQueryable();
```

#### [SUGERENCIA] — Logs sin informacion de request en HandleAutocomplete y HandleGeocode

**Linea:** `MapsEndpoints.cs:20, 76`
**Problema:** No hay log al inicio del request ni al exito de la operacion. Siguiendo las convenciones del proyecto ("Logs con ILogger en puntos clave: inicio de request, error, respuesta de Google"), deberia haber al menos un log de nivel `Debug` o `Information` al recibir el request y al retornar la respuesta exitosa. Esto dificulta el diagnostico en produccion.
**Recomendacion:** Agregar logs en los puntos clave de cada handler.
**Ejemplo de fix:**
```csharp
logger.LogInformation("Autocomplete request for input length {Length}", request.Input.Length);
// ... al final exitoso:
logger.LogInformation("Autocomplete returned {Count} suggestions", suggestions.Count);
```

#### [SUGERENCIA] — GeocodeQuery.ResponsePayload almacena el response completo de Google incluyendo datos innecesarios

**Archivo:** `Data/GeocodeQuery.cs` / `Endpoints/MapsEndpoints.cs:129`
**Problema:** Se almacena el `body` completo del response de la Geocoding API como `response_payload` en la columna JSONB. El response de Google Maps Geocoding API puede ser muy extenso (incluye address_components, formatted_address, tipos de lugar, plus codes, viewport, bounds, etc.). Esto aumenta el tamano de la base de datos y no aporta valor si solo se necesitan las coordenadas y la direccion formateada.
**Recomendacion:** Considerar almacenar solo los campos relevantes o documentar explicitamente por que se necesita el response completo (por ejemplo, para auditoria o debugging futuro).

---

## Archivos sin problemas

- `DTOs/AutocompleteRequest.cs` — Definicion correcta como record, campos apropiados.
- `DTOs/AutocompleteSuggestion.cs` — Correcto.
- `DTOs/GeocodeRequest.cs` — Correcto.
- `DTOs/GeocodeResult.cs` — Correcto.
- `DTOs/PagedResult.cs` — Correcto, uso de generics apropiado.
- `Data/GeocodeQuery.cs` — Modelo bien definido con valores por defecto defensivos.
- `Data/AppDbContext.cs` — Configuracion de EF Core correcta, tipos de columna apropiados, primary key configurada. El unico issue es la falta del indice en `queried_at` (reportado arriba).
- `Data/AppDbContextFactory.cs` — Implementacion correcta de `IDesignTimeDbContextFactory` para migraciones.
