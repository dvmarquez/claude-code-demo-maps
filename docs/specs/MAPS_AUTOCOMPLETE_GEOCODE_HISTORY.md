# Spec: Autocompletado, Geocodificación e Historial de Consultas

## Descripción

Implementar el flujo completo de búsqueda de direcciones: el usuario escribe en un campo de texto con autocompletado (Places API v1), selecciona una sugerencia, el backend obtiene las coordenadas vía Geocoding API y persiste la consulta en PostgreSQL. El frontend muestra el resultado en un mapa interactivo y un historial paginado con filtro por rango de fechas.

---

## Cambios en Base de Datos

### Nueva tabla: `geocode_queries`

| Campo              | Tipo           | Restricciones         |
|--------------------|----------------|-----------------------|
| `id`               | UUID           | PK, default gen_random_uuid() |
| `address`          | VARCHAR(500)   | NOT NULL              |
| `request_payload`  | JSONB          | NOT NULL              |
| `response_payload` | JSONB          | NOT NULL              |
| `latitude`         | DECIMAL(10,8)  | NOT NULL              |
| `longitude`        | DECIMAL(11,8)  | NOT NULL              |
| `queried_at`       | TIMESTAMPTZ    | NOT NULL, default now() |

### Migración requerida
- Crear migración EF Core: `InitialCreate`
- Aplicar con `dotnet ef database update` antes de levantar el backend

---

## Cambios en Backend

### Modelos y DTOs

**Entidad EF Core**
- `GeocodeQuery` — mapea la tabla `geocode_queries`

**DTOs de request**
- `AutocompleteRequest` — `{ Input: string, SessionToken: string }`
- `GeocodeRequest` — `{ PlaceId: string, Address: string, SessionToken: string }`

**DTOs de response**
- `AutocompleteSuggestion` — `{ PlaceId: string, Description: string }`
- `GeocodeResult` — `{ Id, Address, Latitude, Longitude, QueriedAt }`
- `PagedResult<T>` — `{ Data, Page, PageSize, TotalCount, TotalPages }`

### Nuevos endpoints (Minimal API)

#### `POST /api/maps/autocomplete`
- Recibe `AutocompleteRequest`
- Llama a `https://places.googleapis.com/v1/places:autocomplete` con la API key
- Incluye `sessionToken` en el payload a Google
- Retorna `List<AutocompleteSuggestion>`
- No persiste en base de datos

#### `POST /api/maps/geocode`
- Recibe `GeocodeRequest`
- Llama a `https://maps.googleapis.com/maps/api/geocode/json?place_id={placeId}&key={apiKey}`
- Persiste la consulta completa en `geocode_queries` (request + response payloads + lat/lng extraídos)
- Retorna `GeocodeResult`

#### `GET /api/maps/history`
- Query params: `page` (default 1), `pageSize` (default 10), `dateFrom` (opcional), `dateTo` (opcional)
- Filtra por `queried_at` cuando se proveen fechas
- Ordena por `queried_at DESC`
- Retorna `PagedResult<GeocodeResult>`

### Configuración
- Registrar `HttpClient` para llamadas a Google Maps
- Leer `GOOGLE_MAPS_API_KEY` y `POSTGRES_CONNECTION_STRING` desde variables de entorno
- Configurar CORS para `http://localhost:5173`
- Agregar DbContext con Npgsql

### Manejo de errores
- Validar que `Input` / `PlaceId` no estén vacíos (400)
- Si Google Maps retorna error, loguear y retornar 502 con ProblemDetails
- Envolver handlers en try/catch con ILogger

---

## Cambios en Frontend

### Estructura de archivos a crear

```
src/
  hooks/
    useAutocomplete.js
    useGeocode.js
    useHistory.js
  components/
    SearchBox.jsx
    MapPortlet.jsx
    HistoryTable.jsx
    DateFilter.jsx
  pages/
    HomePage.jsx
```

### Hooks

**`useAutocomplete`**
- Recibe el texto ingresado por el usuario
- Aplica debounce de 300 ms antes de llamar a `POST /api/maps/autocomplete`
- Gestiona el `sessionToken` (UUID v4) — se genera al inicio de cada búsqueda nueva y se limpia al seleccionar
- Retorna: `{ suggestions, isLoading, error }`

**`useGeocode`**
- Recibe `{ placeId, address, sessionToken }`
- Llama a `POST /api/maps/geocode`
- Invalida la query de historial al completarse (TanStack Query)
- Retorna: `{ geocode, mutate, isLoading, error }`

**`useHistory`**
- Recibe parámetros de paginación y fechas
- Llama a `GET /api/maps/history`
- Retorna: `{ data, isLoading, error }`

### Componentes

**`SearchBox`**
- Input controlado con debounce (300 ms)
- Genera `sessionToken` UUID v4 al primer keystroke de cada sesión
- Muestra dropdown con sugerencias de `useAutocomplete`
- Al seleccionar: llama a `useGeocode`, limpia el input y el sessionToken
- Estado de carga durante geocodificación

**`MapPortlet`**
- Muestra Google Maps embed (iframe estático o JS API) centrado en la última coordenada
- Pin en la posición de `latitude/longitude`
- Estado inicial: mapa centrado en Quito, Ecuador
- Se actualiza reactivamente con el último resultado de geocode

**`DateFilter`**
- Dos inputs de tipo `date` (desde / hasta) con dayjs
- Emite los valores al padre para pasarlos a `useHistory`

**`HistoryTable`**
- Tabla con columnas: Dirección, Latitud, Longitud, Fecha/Hora
- Paginación: botones Anterior / Siguiente + indicador de página
- Recibe `data` de `useHistory`
- Muestra skeleton o spinner durante carga

**`HomePage`**
- Orquesta todos los componentes
- Gestiona estado compartido: resultado actual de geocode, parámetros de filtro/paginación
- Layout: SearchBox arriba, MapPortlet y HistoryTable abajo

### Dependencias a instalar
- `uuid` — para generar sessionToken en el frontend

---

## Plan de Implementación

### Fase 1 — Backend

1. Configurar `Program.cs`: DbContext, HttpClient, CORS, variables de entorno
2. Crear entidad `GeocodeQuery` y `AppDbContext`
3. Crear migración EF Core `InitialCreate` y aplicarla
4. Implementar endpoint `POST /api/maps/autocomplete`
5. Implementar endpoint `POST /api/maps/geocode`
6. Implementar endpoint `GET /api/maps/history`
7. Probar los tres endpoints con `MapsDemo.Api.http` o curl

### Fase 2 — Frontend

8. Configurar Axios con `VITE_API_BASE_URL` y TanStack Query provider en `main.jsx`
9. Instalar `uuid`
10. Implementar `useAutocomplete` con debounce
11. Implementar `useGeocode`
12. Implementar `useHistory`
13. Implementar `SearchBox`
14. Implementar `MapPortlet`
15. Implementar `DateFilter`
16. Implementar `HistoryTable`
17. Componer `HomePage` con todos los componentes
18. Actualizar `App.jsx` para renderizar `HomePage`

### Fase 3 — Integración y pruebas

19. Levantar Docker Compose (PostgreSQL)
20. Levantar backend y frontend
21. Probar flujo completo: buscar → seleccionar → ver en mapa → ver en historial
22. Probar filtro por fecha y paginación
23. Verificar que la API key no aparece en el bundle del frontend

---

## Criterios de Aceptación

- [ ] El campo de búsqueda muestra sugerencias tras escribir ≥ 3 caracteres (debounce 300 ms)
- [ ] Al seleccionar una sugerencia, el mapa muestra un pin en las coordenadas correctas
- [ ] Cada consulta queda registrada en la tabla `geocode_queries` de PostgreSQL
- [ ] El historial muestra las consultas ordenadas de más reciente a más antigua
- [ ] La paginación funciona con `pageSize` de 10 por defecto
- [ ] El filtro por fecha reduce correctamente los resultados mostrados
- [ ] La API key de Google Maps no aparece en ningún archivo del frontend ni en el bundle
- [ ] El `sessionToken` UUID v4 se genera en el frontend y se envía al backend en cada request
- [ ] Los errores de Google Maps se muestran al usuario de forma amigable
- [ ] El backend retorna ProblemDetails en caso de error (400, 502)
