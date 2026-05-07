# Skill — Google Maps Integration

## Propósito
Conocimiento experto para integrar Google Maps Places API v1 y Geocoding API 
en una arquitectura donde el backend actúa como proxy y protege la API key.

## Places API v1 — Autocomplete

### Endpoint de Google
```
POST https://places.googleapis.com/v1/places:autocomplete
```

### Headers requeridos
```
Content-Type: application/json
X-Goog-Api-Key: {GOOGLE_MAPS_API_KEY}
```

### Request
```json
{
  "input": "Av. Amazonas",
  "sessionToken": "uuid-v4-generado-en-frontend"
}
```

### Response relevante
```json
{
  "suggestions": [
    {
      "placePrediction": {
        "placeId": "ChIJ...",
        "text": {
          "text": "Av. Amazonas, Quito, Ecuador"
        }
      }
    }
  ]
}
```

### Lo que el backend retorna al frontend
```json
[
  {
    "placeId": "ChIJ...",
    "description": "Av. Amazonas, Quito, Ecuador"
  }
]
```

## Geocoding API — Obtener coordenadas

### Endpoint de Google
```
GET https://maps.googleapis.com/maps/api/geocode/json
```

### Query params requeridos
```
place_id={placeId}
key={GOOGLE_MAPS_API_KEY}
sessiontoken={sessionToken}
```

### Response relevante
```json
{
  "results": [
    {
      "geometry": {
        "location": {
          "lat": -0.1806532,
          "lng": -78.4678382
        }
      },
      "formatted_address": "Av. Amazonas, Quito, Ecuador"
    }
  ],
  "status": "OK"
}
```

### Extracción de coordenadas en .NET
```csharp
var location = response.Results[0].Geometry.Location;
var latitude = location.Lat;
var longitude = location.Lng;
```

## Session Tokens — Facturación correcta

### Flujo correcto
1. Frontend genera un UUID v4 al inicio de cada búsqueda nueva
2. Ese token se envía en CADA llamada a autocomplete
3. Cuando el usuario selecciona una sugerencia, el mismo token se envía a geocode
4. Geocode cierra la sesión — ese token no debe reutilizarse
5. Frontend genera un nuevo UUID v4 para la siguiente búsqueda

### Generación del token en React
```javascript
import { v4 as uuidv4 } from 'uuid';
const sessionToken = uuidv4();
```

### Por qué es importante
Sin session token, cada llamada a autocomplete se factura individualmente.
Con session token, todas las llamadas de una sesión de búsqueda se agrupan 
en una sola unidad de facturación junto con el geocode final.

## Manejo de Errores de Google Maps

### Status codes importantes
| Status | Significado | Acción |
|--------|-------------|--------|
| OK | Éxito | Procesar response |
| ZERO_RESULTS | Sin resultados | Retornar lista vacía |
| INVALID_REQUEST | Request mal formado | Log + ProblemDetails 400 |
| REQUEST_DENIED | API key inválida | Log + ProblemDetails 500 |
| OVER_QUERY_LIMIT | Cuota excedida | Log + ProblemDetails 429 |

### Patrón de manejo en .NET
```csharp
if (response.Status != "OK" && response.Status != "ZERO_RESULTS")
{
    logger.LogError("Google Maps error: {Status}", response.Status);
    return Results.Problem(
        detail: $"Error al consultar Google Maps: {response.Status}",
        statusCode: StatusCodes.Status502BadGateway
    );
}
```

## Persistencia en PostgreSQL

### Lo que se guarda en request_payload
```json
{
  "placeId": "ChIJ...",
  "address": "Av. Amazonas, Quito, Ecuador",
  "sessionToken": "uuid"
}
```

### Lo que se guarda en response_payload
El response completo de Google Maps sin modificar.

### Los campos latitude y longitude
Se extraen del response y se guardan como campos propios 
en DECIMAL(10,8) y DECIMAL(11,8) respectivamente.

## CORS en el Backend
Configurar CORS para permitir requests desde el frontend en desarrollo:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

## Reglas de Seguridad
- La API key NUNCA debe enviarse al frontend
- La API key se lee desde variables de entorno, nunca hardcodeada
- El sessionToken lo genera el frontend pero lo usa el backend para llamar a Google
- Validar que el input de autocomplete tenga al menos 3 caracteres antes de llamar a Google
