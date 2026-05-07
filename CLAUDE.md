# Claude Code Demo — Maps Integration

## Descripción del Proyecto
Aplicación de demostración que integra Google Maps (Places API v1 y Geocoding API) con un backend proxy en .NET 10 y un frontend en React. El usuario busca direcciones con autocompletado, selecciona una sugerencia, y el backend obtiene las coordenadas y persiste la consulta en PostgreSQL. El frontend muestra el resultado en un mapa y un historial paginado con filtro por fecha.

## Stack Tecnológico

### Backend
- .NET 10 Web API
- Entity Framework Core con Npgsql (PostgreSQL)
- Patrón: Minimal API
- Puerto: 5000

### Frontend
- React 19 con Vite
- TanStack Query (React Query) para llamadas HTTP
- Axios como cliente HTTP
- Tailwind CSS v4 para estilos
- dayjs para manejo de fechas
- Puerto: 5173

### Base de Datos
- PostgreSQL 16 en Docker
- Base de datos: maps_demo
- Puerto: 5432

## Estructura del Proyecto

```
claude-code-demo-maps/
├── backend/
│   └── MapsDemo.Api/
├── frontend/
│   └── maps-demo-ui/
├── .claude/
│   ├── commands/
│   └── skills/
├── docker-compose.yml
├── .env
└── CLAUDE.md
```

## Variables de Entorno
Las variables sensibles viven en `.env` en la raíz. Nunca hardcodear API keys en el código.

### Backend requiere:
- `GOOGLE_MAPS_API_KEY` — API key de Google Maps con Places API v1 y Geocoding API habilitadas
- `POSTGRES_CONNECTION_STRING` — cadena de conexión a PostgreSQL

### Frontend requiere:
- `VITE_API_BASE_URL` — URL base del backend (default: http://localhost:5000)

## Convenciones de Código

### Backend
- Nombres de clases y métodos en PascalCase
- Nombres de variables y parámetros en camelCase
- Endpoints en kebab-case
- Siempre usar async/await
- Manejo de errores con try/catch retornando ProblemDetails
- Logs con ILogger

### Frontend
- Componentes en PascalCase
- Hooks personalizados con prefijo `use`
- Estilos únicamente con clases de Tailwind, sin CSS custom salvo en `index.css`
- Separar lógica de UI: los llamados HTTP van en hooks, no en componentes
- El frontend nunca llama directamente a Google Maps — siempre a través del backend

## Base de Datos

### Tabla principal: geocode_queries

| Campo            | Tipo           | Descripción                              |
|------------------|----------------|------------------------------------------|
| id               | UUID           | Primary key                              |
| address          | VARCHAR(500)   | Dirección ingresada por el usuario       |
| request_payload  | JSONB          | Request enviado a Google Maps            |
| response_payload | JSONB          | Response completo de Google Maps         |
| latitude         | DECIMAL(10,8)  | Latitud extraída del response            |
| longitude        | DECIMAL(11,8)  | Longitud extraída del response           |
| queried_at       | TIMESTAMPTZ    | Fecha y hora de la consulta              |

## Arquitectura del Backend

### Flujo completo

```
Frontend escribe → POST /api/maps/autocomplete (Places API v1) → sugerencias
Usuario selecciona → POST /api/maps/geocode (Geocoding API) → lat/lng + persiste
Frontend → GET /api/maps/history → historial paginado
```

### Endpoints

#### POST /api/maps/autocomplete
- Recibe: `{ "input": "...", "sessionToken": "uuid" }`
- Actúa como proxy hacia Places API v1 de Google
- Retorna lista de sugerencias:
```json
[
  { "placeId": "...", "description": "Av. Amazonas, Quito, Ecuador" }
]
```
- No persiste en base de datos
- El sessionToken se envía a Google para agrupar la sesión de facturación

#### POST /api/maps/geocode
- Recibe: `{ "placeId": "...", "address": "...", "sessionToken": "uuid" }`
- Llama a Geocoding API de Google usando el placeId
- El sessionToken cierra la sesión de facturación de Places API
- Persiste en PostgreSQL
- Retorna:
```json
{
  "id": "uuid",
  "address": "Av. Amazonas, Quito, Ecuador",
  "latitude": -0.1806532,
  "longitude": -78.4678382,
  "queriedAt": "2026-05-07T10:00:00Z"
}
```

#### GET /api/maps/history
- Query params: `page` (default 1), `pageSize` (default 10), `dateFrom`, `dateTo`
- Retorna:
```json
{
  "data": [...],
  "page": 1,
  "pageSize": 10,
  "totalCount": 50,
  "totalPages": 5
}
```

## Arquitectura del Frontend

### Páginas
- Una sola página principal (`HomePage`)

### Componentes
- `SearchBox` — input con debounce de 300ms, genera sessionToken (UUID v4) al inicio de cada búsqueda, muestra dropdown de sugerencias de Places API, al seleccionar invoca geocode y limpia el sessionToken
- `MapPortlet` — mapa de Google Maps con el pin del resultado más reciente
- `HistoryTable` — tabla paginada de consultas
- `DateFilter` — filtro de fecha para el historial

### Hooks
- `useAutocomplete` — llama a `/api/maps/autocomplete` con debounce de 300ms
- `useGeocode` — llama a `/api/maps/geocode` al seleccionar una sugerencia
- `useHistory` — llama a `/api/maps/history` con parámetros de paginación y filtro

## Reglas Importantes
- No modificar `docker-compose.yml` salvo que se solicite explícitamente
- No crear archivos de configuración adicionales sin avisar
- Siempre correr las migraciones de EF Core antes de levantar el backend
- El frontend nunca llama directamente a Google Maps — siempre a través del backend
- La API key de Google Maps nunca debe aparecer en el código del frontend
- El sessionToken debe ser un UUID v4 generado en el frontend y enviado al backend en cada request de autocomplete y geocode
