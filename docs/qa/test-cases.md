# Casos de Prueba — Maps Demo

Fecha: 2026-05-22
Branch: feature/maps-autocomplete-geocode-history
Spec de referencia: `docs/specs/MAPS_AUTOCOMPLETE_GEOCODE_HISTORY.md`

---

## Índice

1. [POST /api/maps/autocomplete](#1-post-apimapsautocomplete)
2. [POST /api/maps/geocode](#2-post-apimapsgeocode)
3. [GET /api/maps/history](#3-get-apimapshistory)
4. [Flujo completo de integración](#4-flujo-completo-de-integración)
5. [Frontend — SearchBox](#5-frontend--searchbox)
6. [Frontend — MapPortlet](#6-frontend--mapportlet)
7. [Frontend — HistoryTable y DateFilter](#7-frontend--historytable-y-datefilter)

---

## 1. POST /api/maps/autocomplete

**URL:** `POST http://localhost:5000/api/maps/autocomplete`
**Content-Type:** `application/json`

### TC-AC-01 — Búsqueda con input válido retorna sugerencias

**Precondición:** Backend corriendo, `GOOGLE_MAPS_API_KEY` configurada.

**Request:**
```json
{
  "input": "Av. Amazonas, Quito",
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resultado esperado:**
- HTTP 200
- Body: array JSON con al menos un elemento
- Cada elemento tiene `placeId` (string no vacío) y `description` (string no vacío)
- `description` contiene texto relacionado a la búsqueda

---

### TC-AC-02 — Búsqueda con input corto retorna sugerencias (comportamiento actual)

**Precondición:** Backend corriendo.

**Request:**
```json
{
  "input": "Av",
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resultado esperado:**
- HTTP 200 con sugerencias (comportamiento actual del backend)

> **Nota:** El spec y la revisión de calidad identifican que debería retornar 400 para inputs menores a 3 caracteres. Este caso documenta el comportamiento actual y servirá para validar la corrección futura.

---

### TC-AC-03 — Input vacío retorna 400

**Request:**
```json
{
  "input": "",
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resultado esperado:**
- HTTP 400
- Body contiene `title` con descripción del error

---

### TC-AC-04 — Input solo espacios retorna 400

**Request:**
```json
{
  "input": "   ",
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resultado esperado:**
- HTTP 400

---

### TC-AC-05 — Input nulo retorna 400

**Request:**
```json
{
  "input": null,
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resultado esperado:**
- HTTP 400

---

### TC-AC-06 — SessionToken ausente — comportamiento actual

**Request:**
```json
{
  "input": "Quito"
}
```

**Resultado esperado:**
- HTTP 200 con sugerencias (sessionToken es enviado como null a Google)

> **Nota:** La revisión de calidad indica que el sessionToken debería validarse como UUID v4. Este caso documenta el comportamiento actual.

---

### TC-AC-07 — Request body malformado retorna error

**Request:** body inválido (no JSON)
```
this is not json
```

**Resultado esperado:**
- HTTP 400 o HTTP 415

---

### TC-AC-08 — Búsqueda no encuentra resultados retorna array vacío

**Request:**
```json
{
  "input": "xkqzwmvnpqrst12345nonexistent",
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resultado esperado:**
- HTTP 200
- Body: `[]` (array vacío)

---

### TC-AC-09 — El endpoint no persiste en la base de datos

**Precondición:** Registrar el total de filas en `geocode_queries` antes de la llamada.

**Acción:** Ejecutar TC-AC-01.

**Resultado esperado:**
- El total de filas en `geocode_queries` no aumenta.

---

### TC-AC-10 — Llamadas con el mismo sessionToken agrupan sesión de facturación

**Acción:** Ejecutar tres llamadas a `/autocomplete` usando el mismo `sessionToken`.

**Resultado esperado:**
- HTTP 200 en las tres llamadas
- Las tres comparten el mismo `sessionToken` en las requests a Google (verificable en logs del servidor)

---

## 2. POST /api/maps/geocode

**URL:** `POST http://localhost:5000/api/maps/geocode`
**Content-Type:** `application/json`

### TC-GC-01 — Geocodificación exitosa retorna coordenadas y persiste en BD

**Precondición:** Obtener un `placeId` válido desde TC-AC-01.

**Request:**
```json
{
  "placeId": "EhxBdi4gQW1hem9uYXMsIFF1aXRvLCBFY3VhZG9yIi4q...",
  "address": "Av. Amazonas, Quito, Ecuador",
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resultado esperado:**
- HTTP 200
- Body:
  ```json
  {
    "id": "<uuid>",
    "address": "Av. Amazonas, Quito, Ecuador",
    "latitude": <decimal>,
    "longitude": <decimal>,
    "queriedAt": "<ISO 8601 con timezone>"
  }
  ```
- `id` es un UUID v4 válido
- `latitude` está en rango [-90, 90]
- `longitude` está en rango [-180, 180]
- `queriedAt` es una fecha en UTC reciente (dentro de los últimos 5 segundos)

**Verificación en BD:**
```sql
SELECT * FROM geocode_queries WHERE id = '<id retornado>';
```
- Fila existe con todos los campos completos
- `request_payload` y `response_payload` son JSON válidos no nulos

---

### TC-GC-02 — PlaceId vacío retorna 400

**Request:**
```json
{
  "placeId": "",
  "address": "Av. Amazonas, Quito, Ecuador",
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resultado esperado:**
- HTTP 400
- Body contiene `title` con descripción del error

---

### TC-GC-03 — PlaceId nulo retorna 400

**Request:**
```json
{
  "placeId": null,
  "address": "Av. Amazonas, Quito, Ecuador",
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resultado esperado:**
- HTTP 400

---

### TC-GC-04 — PlaceId inválido (no reconocido por Google) retorna 502

**Request:**
```json
{
  "placeId": "INVALID_PLACE_ID_THAT_DOES_NOT_EXIST",
  "address": "Dirección inválida",
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resultado esperado:**
- HTTP 502
- Body contiene detalle del error de Google Maps

**Verificación en BD:** No se persiste ninguna fila nueva.

---

### TC-GC-05 — Request sin `address` persiste con address vacío o nulo

**Request:**
```json
{
  "placeId": "<placeId válido>",
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resultado esperado:**
- HTTP 200 (el campo `address` no es validado actualmente)
- `address` en la respuesta puede ser null o cadena vacía

---

### TC-GC-06 — Geocodificación persiste el placeId en request_payload

**Precondición:** Ejecutar TC-GC-01.

**Verificación en BD:**
```sql
SELECT request_payload FROM geocode_queries ORDER BY queried_at DESC LIMIT 1;
```

**Resultado esperado:**
- `request_payload` contiene `placeId` y `address` como JSON

---

### TC-GC-07 — Geocodificación persiste el response completo de Google en response_payload

**Verificación en BD:**
```sql
SELECT response_payload FROM geocode_queries ORDER BY queried_at DESC LIMIT 1;
```

**Resultado esperado:**
- `response_payload` es un JSON válido con estructura de respuesta de Google Geocoding API
- Contiene los campos `status`, `results`

---

### TC-GC-08 — Múltiples geocodificaciones de la misma dirección crean filas separadas

**Acción:** Ejecutar TC-GC-01 dos veces con el mismo `placeId`.

**Verificación en BD:**
```sql
SELECT COUNT(*) FROM geocode_queries WHERE address = 'Av. Amazonas, Quito, Ecuador';
```

**Resultado esperado:**
- COUNT aumenta en 1 por cada llamada (no hay deduplicación)

---

### TC-GC-09 — El sessionToken cierra la sesión de facturación de Places API

**Precondición:** Realizar llamadas a `/autocomplete` con un `sessionToken`. Usar ese mismo token en `/geocode`.

**Acción:** Verificar en logs o Google Cloud Console que el `sessionToken` aparece en la llamada de geocode.

**Resultado esperado:**
- El mismo UUID v4 enviado en autocomplete aparece en la request de geocode enviada al backend

---

## 3. GET /api/maps/history

**URL:** `GET http://localhost:5000/api/maps/history`

### TC-HI-01 — Historial sin filtros retorna página 1 con 10 registros por defecto

**Precondición:** Existen al menos 10 filas en `geocode_queries`.

**Request:** `GET /api/maps/history`

**Resultado esperado:**
- HTTP 200
- Body:
  ```json
  {
    "data": [...],
    "page": 1,
    "pageSize": 10,
    "totalCount": <N>,
    "totalPages": <ceil(N/10)>
  }
  ```
- `data` tiene exactamente 10 elementos (o menos si hay menos de 10 registros)
- Los registros están ordenados por `queriedAt` de más reciente a más antiguo
- Cada elemento tiene: `id`, `address`, `latitude`, `longitude`, `queriedAt`

---

### TC-HI-02 — Paginación con `page=2` retorna los siguientes registros

**Precondición:** Existen al menos 11 filas en `geocode_queries`.

**Request:** `GET /api/maps/history?page=2&pageSize=10`

**Resultado esperado:**
- HTTP 200
- `page`: 2
- `data` contiene registros distintos a los de la página 1
- `data` no se superpone con la respuesta de TC-HI-01

---

### TC-HI-03 — `pageSize` personalizado limita los resultados

**Request:** `GET /api/maps/history?page=1&pageSize=3`

**Resultado esperado:**
- HTTP 200
- `pageSize`: 3
- `data` tiene máximo 3 elementos
- `totalPages` = ceil(totalCount / 3)

---

### TC-HI-04 — Página fuera de rango retorna data vacía (no error)

**Precondición:** Conocer el total de páginas de TC-HI-01.

**Request:** `GET /api/maps/history?page=9999`

**Resultado esperado:**
- HTTP 200
- `data`: `[]`
- `totalCount` refleja el total real de registros

---

### TC-HI-05 — Filtro `dateFrom` excluye registros anteriores

**Precondición:** Existen registros con fechas distintas en BD.

**Request:** `GET /api/maps/history?dateFrom=2026-05-22`

**Resultado esperado:**
- HTTP 200
- Todos los elementos en `data` tienen `queriedAt >= 2026-05-22T00:00:00Z`
- `totalCount` refleja solo los registros dentro del rango

---

### TC-HI-06 — Filtro `dateTo` excluye registros posteriores

**Request:** `GET /api/maps/history?dateTo=2026-05-21`

**Resultado esperado:**
- HTTP 200
- Todos los elementos en `data` tienen `queriedAt < 2026-05-22T00:00:00Z`

---

### TC-HI-07 — Filtro combinado `dateFrom` y `dateTo` acota correctamente

**Request:** `GET /api/maps/history?dateFrom=2026-05-20&dateTo=2026-05-22`

**Resultado esperado:**
- HTTP 200
- Todos los elementos en `data` tienen `queriedAt` dentro del rango [2026-05-20, 2026-05-22]
- Registros fuera del rango no aparecen

---

### TC-HI-08 — Rango de fechas sin resultados retorna array vacío

**Request:** `GET /api/maps/history?dateFrom=2000-01-01&dateTo=2000-01-02`

**Resultado esperado:**
- HTTP 200
- `data`: `[]`
- `totalCount`: 0
- `totalPages`: 0

---

### TC-HI-09 — BD vacía retorna estructura válida con datos vacíos

**Precondición:** Tabla `geocode_queries` sin registros (limpiar con `TRUNCATE geocode_queries`).

**Request:** `GET /api/maps/history`

**Resultado esperado:**
- HTTP 200
- `data`: `[]`
- `totalCount`: 0
- `totalPages`: 0

---

### TC-HI-10 — Ordenamiento es siempre descendente por fecha

**Precondición:** Existen al menos 2 registros con distintos `queriedAt`.

**Request:** `GET /api/maps/history?pageSize=50`

**Resultado esperado:**
- HTTP 200
- Para cada par de elementos consecutivos `data[i]` y `data[i+1]`: `data[i].queriedAt >= data[i+1].queriedAt`

---

### TC-HI-11 — pageSize=0 — comportamiento ante valores límite

**Request:** `GET /api/maps/history?pageSize=0`

**Resultado esperado (comportamiento actual):**
- HTTP 500 o división por cero en cálculo de `totalPages`

> **Nota:** La revisión de calidad identifica que `pageSize` debería validarse (mínimo 1). Este caso documenta el comportamiento actual para verificar la corrección futura.

---

### TC-HI-12 — page=0 — comportamiento ante valores límite

**Request:** `GET /api/maps/history?page=0`

**Resultado esperado (comportamiento actual):**
- HTTP 500 o error de BD por `.Skip(-pageSize)`

> **Nota:** La revisión de calidad identifica que `page` debería validarse (mínimo 1). Este caso documenta el comportamiento actual.

---

## 4. Flujo completo de integración

### TC-INT-01 — Flujo end-to-end: autocomplete → geocode → historial

**Pasos:**

1. `POST /api/maps/autocomplete` con `input: "Plaza de la Independencia, Quito"` y un UUID v4 como sessionToken
2. Tomar el primer `placeId` de la respuesta
3. `POST /api/maps/geocode` con ese `placeId`, la `description` como `address`, y el mismo `sessionToken`
4. `GET /api/maps/history?page=1&pageSize=1`

**Resultado esperado:**
- Paso 1: HTTP 200 con al menos una sugerencia
- Paso 3: HTTP 200 con coordenadas válidas. El `id` retornado es un UUID.
- Paso 4: El primer elemento de `data` es el registro del paso 3 (mismo `id`, `address`, `latitude`, `longitude`)

---

### TC-INT-02 — La API key de Google no aparece en respuestas del backend

**Acción:** Capturar con Wireshark o proxy HTTP (ej. Fiddler) las respuestas de los tres endpoints.

**Resultado esperado:**
- Ninguna respuesta del backend incluye el valor de `GOOGLE_MAPS_API_KEY` en el body, headers o logs accesibles al cliente

---

### TC-INT-03 — CORS permite requests desde el frontend

**Acción:** Realizar un request fetch desde `http://localhost:5173` a `http://localhost:5000/api/maps/history`.

**Resultado esperado:**
- No hay error de CORS en la consola del browser
- La respuesta llega correctamente al frontend

---

### TC-INT-04 — CORS bloquea requests desde orígenes no autorizados

**Acción:** Realizar un request fetch desde un origen distinto (ej. `http://localhost:4000`).

**Resultado esperado:**
- Error de CORS en el browser
- El backend retorna el header `Access-Control-Allow-Origin` solo para `http://localhost:5173`

---

### TC-INT-05 — El historial se actualiza tras cada geocodificación exitosa

**Precondición:** Contar las filas actuales en `geocode_queries`.

**Acción:** Ejecutar TC-INT-01 completo.

**Resultado esperado:**
- La tabla `geocode_queries` tiene una fila más
- La nueva fila tiene todos los campos correctamente populados: `id`, `address`, `latitude`, `longitude`, `queried_at`, `request_payload`, `response_payload`

---

## 5. Frontend — SearchBox

### TC-FE-AC-01 — Autocompletado se dispara después de 300ms de inactividad

**Precondición:** Frontend corriendo en `http://localhost:5173`.

**Pasos:**
1. Hacer clic en el campo de búsqueda
2. Escribir "Av. Amazonas" rápidamente
3. Esperar 300ms sin escribir

**Resultado esperado:**
- Durante la escritura, no se realizan llamadas a `/api/maps/autocomplete`
- Después de 300ms, se realiza una sola llamada con el texto completo
- Aparece el dropdown de sugerencias

---

### TC-FE-AC-02 — Dropdown aparece con sugerencias al escribir

**Pasos:**
1. Escribir "Quito" en el campo de búsqueda
2. Esperar el debounce

**Resultado esperado:**
- Aparece un dropdown con al menos una sugerencia
- Cada sugerencia muestra la dirección completa
- El spinner de carga aparece mientras se espera la respuesta

---

### TC-FE-AC-03 — Seleccionar sugerencia dispara geocodificación y limpia el campo

**Pasos:**
1. Ejecutar TC-FE-AC-02
2. Hacer clic en la primera sugerencia

**Resultado esperado:**
- El campo de búsqueda se limpia
- El dropdown desaparece
- Se realiza una llamada a `POST /api/maps/geocode`
- Las coordenadas aparecen debajo del campo de búsqueda

---

### TC-FE-AC-04 — El sessionToken es consistente entre autocomplete y geocode

**Pasos:**
1. Abrir DevTools → Network
2. Escribir en el campo de búsqueda y esperar sugerencias
3. Seleccionar una sugerencia

**Resultado esperado:**
- El `sessionToken` en las llamadas a `/autocomplete` es un UUID v4 válido
- El mismo `sessionToken` aparece en la llamada a `/geocode`
- Después de geocodificar, la siguiente búsqueda usa un UUID diferente

---

### TC-FE-AC-05 — El campo queda deshabilitado durante geocodificación

**Pasos:**
1. Seleccionar una sugerencia
2. Observar el campo inmediatamente

**Resultado esperado:**
- El campo está deshabilitado (`disabled`) mientras se procesa el geocode
- Aparece el spinner de carga
- Al completarse, el campo vuelve a estar habilitado

---

### TC-FE-AC-06 — Error de geocodificación muestra mensaje al usuario

**Precondición:** Simular error en el backend (detener el backend o usar un placeId inválido mediante herramienta de mocking).

**Resultado esperado:**
- Aparece el mensaje "Error al obtener coordenadas. Intente de nuevo."
- El campo vuelve a estar habilitado
- El dropdown desaparece

---

### TC-FE-AC-07 — La API key de Google no aparece en el bundle ni en requests del browser

**Pasos:**
1. Abrir DevTools → Network
2. Ejecutar una búsqueda y geocodificación completa
3. Revisar todas las requests: payload, headers, response

**Resultado esperado:**
- El valor de `GOOGLE_MAPS_API_KEY` no aparece en ningún request ni response visible en el browser
- El frontend solo llama a `localhost:5000`, nunca directamente a `googleapis.com` (excepto el iframe del mapa)

---

## 6. Frontend — MapPortlet

### TC-FE-MAP-01 — Mapa muestra ubicación por defecto al cargar

**Precondición:** Abrir la aplicación sin haber geocodificado nada.

**Resultado esperado:**
- El iframe del mapa está visible
- El mapa está centrado en Quito, Ecuador (lat: -0.1806532, lng: -78.4678382)

---

### TC-FE-MAP-02 — Mapa se actualiza con el pin del resultado geocodificado

**Pasos:**
1. Ejecutar TC-FE-AC-03
2. Observar el mapa

**Resultado esperado:**
- El iframe se recarga con las nuevas coordenadas
- El mapa muestra un pin en la ubicación geocodificada

---

### TC-FE-MAP-03 — Mapa refleja siempre el resultado más reciente

**Pasos:**
1. Geocodificar "Quito, Ecuador"
2. Geocodificar "Buenos Aires, Argentina"

**Resultado esperado:**
- Tras la primera geocodificación, el mapa muestra Quito
- Tras la segunda, el mapa muestra Buenos Aires

---

## 7. Frontend — HistoryTable y DateFilter

### TC-FE-HI-01 — Tabla muestra el historial ordenado de más reciente a más antiguo

**Precondición:** Existen registros en la BD.

**Resultado esperado:**
- La primera fila corresponde al registro más reciente
- Las fechas en la columna "Fecha y hora" están en orden descendente

---

### TC-FE-HI-02 — Historial se actualiza automáticamente tras geocodificar

**Pasos:**
1. Observar el conteo actual en el historial
2. Geocodificar una dirección nueva

**Resultado esperado:**
- El historial se recarga automáticamente (sin recargar la página)
- El nuevo registro aparece como primera fila
- El conteo total aumenta en 1

---

### TC-FE-HI-03 — Paginación navega entre páginas correctamente

**Precondición:** Existen más de 10 registros en la BD.

**Pasos:**
1. Verificar que hay más de una página (indicador "Página 1 de N")
2. Hacer clic en "Siguiente"

**Resultado esperado:**
- El indicador cambia a "Página 2 de N"
- Los registros de la tabla son distintos a los de la página 1
- El botón "Anterior" se habilita
- El botón "Siguiente" se deshabilita en la última página

---

### TC-FE-HI-04 — Botón "Anterior" deshabilitado en la primera página

**Resultado esperado:**
- En la página 1, el botón "Anterior" está deshabilitado visualmente (opacidad reducida) y no es clickeable

---

### TC-FE-HI-05 — Filtro por fecha limita los resultados mostrados

**Pasos:**
1. Ingresar fecha "Desde": la fecha de hoy
2. Ingresar fecha "Hasta": la fecha de hoy

**Resultado esperado:**
- La tabla muestra solo los registros con `queriedAt` de hoy
- El total de registros cambia
- La paginación se resetea a la página 1

---

### TC-FE-HI-06 — Botón "Limpiar" restaura el historial completo

**Precondición:** Filtro de fecha aplicado (TC-FE-HI-05).

**Pasos:**
1. Hacer clic en "Limpiar"

**Resultado esperado:**
- Los inputs de fecha se limpian
- El historial muestra todos los registros sin filtro
- El total de registros vuelve al valor original

---

### TC-FE-HI-07 — Fecha "Hasta" no puede ser anterior a fecha "Desde"

**Pasos:**
1. Ingresar "Desde": 2026-05-22
2. Intentar ingresar "Hasta": 2026-05-20

**Resultado esperado:**
- El selector de fecha "Hasta" tiene `min` igual a la fecha "Desde"
- No permite seleccionar una fecha anterior

---

### TC-FE-HI-08 — Estado de carga muestra skeleton mientras se obtienen datos

**Pasos:**
1. Throttlear la red a "Slow 3G" en DevTools
2. Recargar la página o limpiar filtros

**Resultado esperado:**
- Durante la carga, aparecen 5 barras skeleton animadas en lugar de la tabla
- Una vez cargados los datos, la tabla aparece correctamente

---

### TC-FE-HI-09 — Tabla vacía muestra mensaje informativo

**Precondición:** No existen registros (o filtro de fecha que no coincide con ningún registro).

**Resultado esperado:**
- Se muestra el texto "No hay consultas registradas."
- No aparece una tabla vacía con encabezados sin filas

---

### TC-FE-HI-10 — Filtro por fecha resetea la paginación a página 1

**Precondición:** El usuario está en la página 2 del historial.

**Pasos:**
1. Aplicar cualquier filtro de fecha

**Resultado esperado:**
- La paginación vuelve a la página 1 automáticamente

---

## Resumen de cobertura

| Módulo                   | Total casos | Happy path | Validaciones | Edge cases |
|--------------------------|-------------|------------|--------------|------------|
| POST /autocomplete       | 10          | 3          | 4            | 3          |
| POST /geocode            | 9           | 5          | 2            | 2          |
| GET /history             | 12          | 5          | 3            | 4          |
| Integración              | 5           | 3          | 2            | 0          |
| Frontend — SearchBox     | 7           | 4          | 2            | 1          |
| Frontend — MapPortlet    | 3           | 3          | 0            | 0          |
| Frontend — History/Date  | 10          | 5          | 3            | 2          |
| **Total**                | **56**      | **28**     | **16**       | **12**     |

---

## Datos de prueba sugeridos

### Direcciones con PlaceId conocido
| Dirección | PlaceId (referencia) |
|-----------|---------------------|
| Av. Amazonas, Quito, Ecuador | `EhxBdi4gQW1hem9uYXMsIFF1aXRvLCBFY3VhZG9yIi4q...` |
| Plaza de la Independencia, Quito | Obtener via `/autocomplete` |
| Buenos Aires, Argentina | Obtener via `/autocomplete` |

> Los `placeId` de Google Maps pueden cambiar. Obtenerlos siempre con una llamada fresca a `/autocomplete` antes de usar en `/geocode`.

### SessionTokens de prueba (UUID v4)
```
550e8400-e29b-41d4-a716-446655440000
6ba7b810-9dad-41d4-80b4-00c04fd430c8
6ba7b812-9dad-41d4-80b4-00c04fd430c8
```

### Comando SQL para limpiar datos de prueba
```sql
-- Ver registros de hoy
SELECT * FROM geocode_queries WHERE queried_at::date = CURRENT_DATE ORDER BY queried_at DESC;

-- Eliminar registros de prueba por dirección
DELETE FROM geocode_queries WHERE address ILIKE '%test%';

-- Ver total de registros
SELECT COUNT(*) FROM geocode_queries;
```
