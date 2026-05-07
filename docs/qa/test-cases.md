# Casos de Prueba — Maps Demo

**Versión:** 1.0
**Fecha:** 2026-05-07
**Branch:** feature/maps-autocomplete-geocode-history
**Spec de referencia:** `docs/specs/MAPS_AUTOCOMPLETE_GEOCODE_HISTORY.md`

---

## Convenciones

| Columna | Descripción |
|---------|-------------|
| **ID** | Identificador único del caso |
| **Prioridad** | Alta / Media / Baja |
| **Tipo** | Funcional / Borde / Negativo / Seguridad / UI |
| **Estado** | Pendiente / Pasó / Falló / Bloqueado |

> **Nota de issues conocidos:** Los casos marcados con ⚠️ están relacionados con issues identificados en `docs/reviews/code-quality-review.md` o `docs/reviews/accessibility-review.md` y pueden fallar hasta que sean corregidos.

---

## Prerequisitos

Antes de ejecutar cualquier caso de prueba verificar:

1. Docker Compose corriendo: `docker ps` debe mostrar `claude_code_demo_postgres` en estado `healthy`
2. Backend corriendo en `http://localhost:5000`
3. Frontend corriendo en `http://localhost:5173`
4. Variable `GOOGLE_MAPS_API_KEY` configurada en `.env`
5. Tabla `geocode_queries` existente en la base de datos (migración aplicada)

---

## Suite 1 — POST /api/maps/autocomplete

### TC-001 — Autocomplete retorna sugerencias para input válido
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Precondiciones:** Backend activo, API key configurada.

**Request:**
```http
POST http://localhost:5000/api/maps/autocomplete
Content-Type: application/json

{
  "input": "Av. Amazonas Quito",
  "sessionToken": "a1b2c3d4-0000-0000-0000-000000000001"
}
```

**Resultado esperado:**
- HTTP 200
- Body: array JSON con al menos 1 elemento
- Cada elemento contiene `placeId` (string no vacío) y `description` (string no vacío)
- `description` incluye texto relacionado con "Amazonas" o "Quito"

**Resultado actual:** _______________

---

### TC-002 — Autocomplete retorna array vacío para input sin coincidencias
**Prioridad:** Media | **Tipo:** Funcional | **Estado:** Pendiente

**Request:**
```http
POST http://localhost:5000/api/maps/autocomplete
Content-Type: application/json

{
  "input": "xkqzwvmplaceinexistente9999",
  "sessionToken": "a1b2c3d4-0000-0000-0000-000000000002"
}
```

**Resultado esperado:**
- HTTP 200
- Body: `[]` (array vacío, no error)

**Resultado actual:** _______________

---

### TC-003 — Autocomplete rechaza input vacío
**Prioridad:** Alta | **Tipo:** Negativo | **Estado:** Pendiente

**Request:**
```http
POST http://localhost:5000/api/maps/autocomplete
Content-Type: application/json

{
  "input": "",
  "sessionToken": "a1b2c3d4-0000-0000-0000-000000000003"
}
```

**Resultado esperado:**
- HTTP 400
- Body incluye campo de error (title o detail)
- No se realiza ninguna llamada a Google Places API

**Resultado actual:** _______________

---

### TC-004 — Autocomplete rechaza input de solo espacios
**Prioridad:** Media | **Tipo:** Negativo | **Estado:** Pendiente

**Request:**
```http
POST http://localhost:5000/api/maps/autocomplete
Content-Type: application/json

{
  "input": "   ",
  "sessionToken": "a1b2c3d4-0000-0000-0000-000000000004"
}
```

**Resultado esperado:**
- HTTP 400

**Resultado actual:** _______________

---

### TC-005 ⚠️ — Autocomplete con input de 1 y 2 caracteres
**Prioridad:** Alta | **Tipo:** Borde | **Estado:** Pendiente

> ⚠️ Issue crítico conocido: no existe validación de longitud mínima (ver `code-quality-review.md` issue #2). Este caso **debería** fallar (retornar 400) pero actualmente llama a Places API.

**Request A (1 carácter):**
```http
POST http://localhost:5000/api/maps/autocomplete
Content-Type: application/json

{ "input": "Q", "sessionToken": "a1b2c3d4-0000-0000-0000-000000000005" }
```

**Request B (2 caracteres):**
```http
POST http://localhost:5000/api/maps/autocomplete
Content-Type: application/json

{ "input": "Qu", "sessionToken": "a1b2c3d4-0000-0000-0000-000000000005" }
```

**Resultado esperado (post-fix):**
- HTTP 400 para inputs de menos de 3 caracteres
- Mensaje: "Input must be at least 3 characters"

**Resultado actual:** _______________

---

### TC-006 — Autocomplete con input de exactamente 3 caracteres
**Prioridad:** Media | **Tipo:** Borde | **Estado:** Pendiente

**Request:**
```http
POST http://localhost:5000/api/maps/autocomplete
Content-Type: application/json

{ "input": "Qui", "sessionToken": "a1b2c3d4-0000-0000-0000-000000000006" }
```

**Resultado esperado:**
- HTTP 200
- Array con sugerencias (puede estar vacío si no hay coincidencias)

**Resultado actual:** _______________

---

### TC-007 — Autocomplete sin campo sessionToken
**Prioridad:** Media | **Tipo:** Borde | **Estado:** Pendiente

**Request:**
```http
POST http://localhost:5000/api/maps/autocomplete
Content-Type: application/json

{ "input": "Quito" }
```

**Resultado esperado:**
- HTTP 200 con sugerencias (el sessionToken es opcional para la llamada a Places API)
- O HTTP 400 si se decide requerirlo en el futuro

**Resultado actual:** _______________

---

### TC-008 — Autocomplete no persiste en base de datos
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Anotar el `totalCount` de `GET /api/maps/history`
2. Llamar a `POST /api/maps/autocomplete` con input válido
3. Volver a llamar a `GET /api/maps/history`

**Resultado esperado:**
- El `totalCount` es idéntico antes y después del autocomplete
- La tabla `geocode_queries` no tiene nuevas filas

**Resultado actual:** _______________

---

### TC-009 — Autocomplete incluye sessionToken en llamada a Google
**Prioridad:** Media | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Llamar al endpoint con un `sessionToken` conocido
2. Revisar logs del backend para verificar que el token se envió a Google

**Resultado esperado:**
- El payload enviado a `places.googleapis.com` incluye el `sessionToken` proporcionado

**Resultado actual:** _______________

---

## Suite 2 — POST /api/maps/geocode

### TC-010 — Geocode retorna coordenadas para placeId válido
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Request:**
```http
POST http://localhost:5000/api/maps/geocode
Content-Type: application/json

{
  "placeId": "ChIJn3xCAkCa1ZERclXvWOGRuUQ",
  "address": "Quito, Ecuador",
  "sessionToken": "a1b2c3d4-0000-0000-0000-000000000010"
}
```

**Resultado esperado:**
- HTTP 200
- Body contiene: `id` (UUID), `address` (string), `latitude` (decimal), `longitude` (decimal), `queriedAt` (ISO 8601)
- `latitude` está en rango [-90, 90] y `longitude` en rango [-180, 180]
- `address` coincide con el enviado en el request

**Resultado actual:** _______________

---

### TC-011 — Geocode persiste el registro en PostgreSQL
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Anotar el `totalCount` de `GET /api/maps/history`
2. Llamar a `POST /api/maps/geocode` con placeId válido
3. Capturar el `id` del response
4. Consultar `GET /api/maps/history`

**Resultado esperado:**
- `totalCount` se incrementa en 1
- El primer elemento de `data` tiene el mismo `id` retornado por geocode
- `latitude`, `longitude` y `address` coinciden

**Resultado actual:** _______________

---

### TC-012 — Geocode rechaza placeId vacío
**Prioridad:** Alta | **Tipo:** Negativo | **Estado:** Pendiente

**Request:**
```http
POST http://localhost:5000/api/maps/geocode
Content-Type: application/json

{
  "placeId": "",
  "address": "Quito, Ecuador",
  "sessionToken": "a1b2c3d4-0000-0000-0000-000000000012"
}
```

**Resultado esperado:**
- HTTP 400
- No se persiste nada en la base de datos

**Resultado actual:** _______________

---

### TC-013 — Geocode rechaza placeId de solo espacios
**Prioridad:** Media | **Tipo:** Negativo | **Estado:** Pendiente

**Request:**
```http
POST http://localhost:5000/api/maps/geocode
Content-Type: application/json

{
  "placeId": "   ",
  "address": "Test",
  "sessionToken": "a1b2c3d4-0000-0000-0000-000000000013"
}
```

**Resultado esperado:**
- HTTP 400

**Resultado actual:** _______________

---

### TC-014 — Geocode con placeId inválido retorna 502
**Prioridad:** Alta | **Tipo:** Negativo | **Estado:** Pendiente

**Request:**
```http
POST http://localhost:5000/api/maps/geocode
Content-Type: application/json

{
  "placeId": "INVALID_PLACE_ID_DOES_NOT_EXIST",
  "address": "Dirección inválida",
  "sessionToken": "a1b2c3d4-0000-0000-0000-000000000014"
}
```

**Resultado esperado:**
- HTTP 502
- Body: ProblemDetails con `detail` o `title` descriptivo
- No se persiste nada en la base de datos

**Resultado actual:** _______________

---

### TC-015 ⚠️ — Geocode con placeId que retorna results vacío de Google
**Prioridad:** Alta | **Tipo:** Borde | **Estado:** Pendiente

> ⚠️ Issue crítico conocido: acceso a `results[0]` sin verificar que el array no esté vacío (ver `code-quality-review.md` issue #5). Este caso puede lanzar `IndexOutOfRangeException`.

**Escenario:** Google responde con `{"status": "OK", "results": []}` (situación poco común pero posible).

**Resultado esperado (post-fix):**
- HTTP 502 con ProblemDetails descriptivo
- No se persiste nada

**Resultado actual:** _______________

---

### TC-016 ⚠️ — Geocode con address que supera 500 caracteres
**Prioridad:** Media | **Tipo:** Borde | **Estado:** Pendiente

> ⚠️ Issue conocido: sin validación de longitud de `Address` (ver `code-quality-review.md` issue #4). EF Core puede lanzar excepción al intentar persistir.

**Request:**
```http
POST http://localhost:5000/api/maps/geocode
Content-Type: application/json

{
  "placeId": "ChIJn3xCAkCa1ZERclXvWOGRuUQ",
  "address": "[string de 501 caracteres]",
  "sessionToken": "a1b2c3d4-0000-0000-0000-000000000016"
}
```

**Resultado esperado (post-fix):**
- HTTP 400 con mensaje de validación

**Resultado actual:** _______________

---

### TC-017 — Geocode con address vacío
**Prioridad:** Media | **Tipo:** Borde | **Estado:** Pendiente

**Request:**
```http
POST http://localhost:5000/api/maps/geocode
Content-Type: application/json

{
  "placeId": "ChIJn3xCAkCa1ZERclXvWOGRuUQ",
  "address": "",
  "sessionToken": "a1b2c3d4-0000-0000-0000-000000000017"
}
```

**Resultado esperado:**
- HTTP 400, o bien la coordenada se persiste con `address = ""` (comportamiento actual — a definir si es aceptable)

**Resultado actual:** _______________

---

### TC-018 — Geocode guarda request_payload y response_payload como JSONB válido
**Prioridad:** Media | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Llamar a `POST /api/maps/geocode` con placeId válido
2. Capturar el `id` del response
3. Consultar directamente en PostgreSQL:
   ```sql
   SELECT request_payload, response_payload
   FROM geocode_queries
   WHERE id = '<id-del-response>';
   ```

**Resultado esperado:**
- `request_payload` es JSON válido con campos `placeId` y `address`
- `response_payload` es JSON válido (el response completo de Google Geocoding API)
- Ambos campos son de tipo JSONB (se pueden consultar con operadores `->`, `->>`))

**Resultado actual:** _______________

---

## Suite 3 — GET /api/maps/history

### TC-019 — History retorna lista vacía cuando no hay registros
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Precondición:** Tabla `geocode_queries` vacía (limpiar con `DELETE FROM geocode_queries;` en entorno de prueba limpio).

**Request:**
```http
GET http://localhost:5000/api/maps/history
```

**Resultado esperado:**
```json
{
  "data": [],
  "page": 1,
  "pageSize": 10,
  "totalCount": 0,
  "totalPages": 0
}
```

**Resultado actual:** _______________

---

### TC-020 — History retorna registros ordenados de más reciente a más antiguo
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Crear al menos 3 geocodificaciones con intervalos de tiempo
2. Llamar a `GET /api/maps/history`

**Resultado esperado:**
- `data[0].queriedAt` > `data[1].queriedAt` > `data[2].queriedAt`
- Orden DESC por `queriedAt` estrictamente

**Resultado actual:** _______________

---

### TC-021 — History paginación: defaults page=1, pageSize=10
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Precondición:** Al menos 1 registro en la tabla.

**Request:**
```http
GET http://localhost:5000/api/maps/history
```

**Resultado esperado:**
- `page` = 1
- `pageSize` = 10
- `data` tiene máximo 10 elementos

**Resultado actual:** _______________

---

### TC-022 — History paginación: navegación entre páginas
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Precondición:** Al menos 12 registros en la tabla.

**Request A — Página 1:**
```http
GET http://localhost:5000/api/maps/history?page=1&pageSize=10
```

**Request B — Página 2:**
```http
GET http://localhost:5000/api/maps/history?page=2&pageSize=10
```

**Resultado esperado:**
- Respuesta A: `page`=1, `totalPages`≥2, `data` con 10 elementos
- Respuesta B: `page`=2, `data` con los registros restantes
- No hay `id` duplicado entre las dos páginas
- `totalCount` es igual en ambas respuestas

**Resultado actual:** _______________

---

### TC-023 — History paginación: pageSize personalizado
**Prioridad:** Media | **Tipo:** Funcional | **Estado:** Pendiente

**Precondición:** Al menos 6 registros en la tabla.

**Request:**
```http
GET http://localhost:5000/api/maps/history?page=1&pageSize=3
```

**Resultado esperado:**
- `pageSize` = 3
- `data` tiene exactamente 3 elementos
- `totalPages` = ceil(totalCount / 3)

**Resultado actual:** _______________

---

### TC-024 — History paginación: página más allá del total
**Prioridad:** Media | **Tipo:** Borde | **Estado:** Pendiente

**Precondición:** 5 registros en la tabla (totalPages = 1 con pageSize=10).

**Request:**
```http
GET http://localhost:5000/api/maps/history?page=99&pageSize=10
```

**Resultado esperado:**
- HTTP 200
- `data`: `[]`
- `page`: 99
- `totalCount`: 5 (correcto)
- `totalPages`: 1

**Resultado actual:** _______________

---

### TC-025 — History filtro por dateFrom
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Precondición:** Registros con fechas del día actual y del día anterior.

**Request:**
```http
GET http://localhost:5000/api/maps/history?dateFrom=2026-05-07
```

**Resultado esperado:**
- Todos los elementos de `data` tienen `queriedAt` >= `2026-05-07T00:00:00Z`
- Registros anteriores a esa fecha no aparecen

**Resultado actual:** _______________

---

### TC-026 — History filtro por dateTo (inclusive)
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Precondición:** Registros con fechas del día actual y del día siguiente.

**Request:**
```http
GET http://localhost:5000/api/maps/history?dateTo=2026-05-07
```

**Resultado esperado:**
- Todos los elementos tienen `queriedAt` < `2026-05-08T00:00:00Z` (el día completo queda incluido)
- Registros del día siguiente no aparecen

**Resultado actual:** _______________

---

### TC-027 — History filtro por rango dateFrom + dateTo
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Request:**
```http
GET http://localhost:5000/api/maps/history?dateFrom=2026-05-06&dateTo=2026-05-07
```

**Resultado esperado:**
- Solo aparecen registros con `queriedAt` entre `2026-05-06T00:00:00Z` y `2026-05-07T23:59:59Z`
- `totalCount` refleja solo el subconjunto filtrado

**Resultado actual:** _______________

---

### TC-028 — History filtro con rango que no tiene resultados
**Prioridad:** Media | **Tipo:** Borde | **Estado:** Pendiente

**Request:**
```http
GET http://localhost:5000/api/maps/history?dateFrom=2020-01-01&dateTo=2020-01-02
```

**Resultado esperado:**
- HTTP 200
- `data`: `[]`
- `totalCount`: 0

**Resultado actual:** _______________

---

### TC-029 — History sin filtro retorna todos los registros
**Prioridad:** Media | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Contar registros en la DB: `SELECT COUNT(*) FROM geocode_queries`
2. Llamar a `GET /api/maps/history?pageSize=1000`

**Resultado esperado:**
- `totalCount` coincide con el COUNT de la DB
- Sin filtros aplicados

**Resultado actual:** _______________

---

### TC-030 — History: estructura completa del response
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Precondición:** Al menos 1 registro en la tabla.

**Request:**
```http
GET http://localhost:5000/api/maps/history?page=1&pageSize=5
```

**Resultado esperado:** Body con exactamente esta estructura:
```json
{
  "data": [
    {
      "id": "<UUID>",
      "address": "<string>",
      "latitude": <decimal>,
      "longitude": <decimal>,
      "queriedAt": "<ISO 8601 con timezone>"
    }
  ],
  "page": 1,
  "pageSize": 5,
  "totalCount": <int>,
  "totalPages": <int>
}
```

**Verificar:** ningún campo extra (no se expone `requestPayload`, `responsePayload` ni datos internos).

**Resultado actual:** _______________

---

## Suite 4 — CORS y Seguridad

### TC-031 — CORS permite requests desde el frontend
**Prioridad:** Alta | **Tipo:** Seguridad | **Estado:** Pendiente

**Request:**
```http
OPTIONS http://localhost:5000/api/maps/autocomplete
Origin: http://localhost:5173
Access-Control-Request-Method: POST
Access-Control-Request-Headers: Content-Type
```

**Resultado esperado:**
- HTTP 204
- Header `Access-Control-Allow-Origin: http://localhost:5173`
- Header `Access-Control-Allow-Methods` incluye `POST`
- Header `Access-Control-Allow-Headers` incluye `Content-Type`

**Resultado actual:** ✅ Validado en integración

---

### TC-032 — CORS bloquea requests desde origen no permitido
**Prioridad:** Alta | **Tipo:** Seguridad | **Estado:** Pendiente

**Request:**
```http
OPTIONS http://localhost:5000/api/maps/autocomplete
Origin: http://evil-site.example.com
Access-Control-Request-Method: POST
```

**Resultado esperado:**
- Response NO incluye `Access-Control-Allow-Origin: http://evil-site.example.com`

**Resultado actual:** _______________

---

### TC-033 ⚠️ — API key no aparece en logs del servidor
**Prioridad:** Alta | **Tipo:** Seguridad | **Estado:** Pendiente

> ⚠️ Issue crítico conocido: la API key aparece en la URL de Geocoding API (`?key=...`) que se loguea en cualquier middleware (ver `code-quality-review.md` issue #1).

**Pasos:**
1. Llamar a `POST /api/maps/geocode`
2. Revisar los logs del backend

**Resultado esperado (post-fix):**
- La API key no aparece en ninguna línea de log
- Las URLs logueadas muestran la clave como `***` o están ausentes

**Resultado actual:** _______________

---

### TC-034 — API key no aparece en el bundle del frontend
**Prioridad:** Alta | **Tipo:** Seguridad | **Estado:** Pendiente

**Pasos:**
1. Ejecutar `npm run build` en el frontend
2. Buscar la API key en los archivos generados:
   ```bash
   grep -r "AIza" frontend/maps-demo-ui/dist/
   ```

**Resultado esperado:**
- Sin coincidencias: la API key no está en el bundle

**Resultado actual:** _______________

---

### TC-035 — Response no expone datos internos sensibles
**Prioridad:** Alta | **Tipo:** Seguridad | **Estado:** Pendiente

**Pasos:**
1. Llamar a `GET /api/maps/history`
2. Inspeccionar el body del response

**Resultado esperado:**
- Los campos `requestPayload` y `responsePayload` (que contienen el response completo de Google, incluyendo metadatos internos) **no aparecen** en el response de history
- Solo se exponen: `id`, `address`, `latitude`, `longitude`, `queriedAt`

**Resultado actual:** ✅ Confirmado: `PagedResult<GeocodeResult>` no incluye payloads

---

## Suite 5 — Flujo de usuario completo (Frontend + Backend)

### TC-036 — Flujo completo: buscar → seleccionar → ver en mapa → ver en historial
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Abrir `http://localhost:5173`
2. Escribir "Parque La Carolina" en el campo de búsqueda
3. Esperar al menos 300ms (debounce)
4. Verificar que aparece el dropdown con sugerencias
5. Hacer clic en una sugerencia
6. Verificar que el input se limpia
7. Verificar que el mapa se actualiza mostrando la nueva ubicación
8. Verificar que la tabla de historial muestra la nueva consulta en la primera fila

**Resultado esperado:**
- El dropdown desaparece al seleccionar
- El mapa muestra la coordenada de la dirección seleccionada
- El historial se actualiza automáticamente sin recargar la página
- La fila en historial muestra dirección, latitud, longitud y fecha correctas

**Resultado actual:** _______________

---

### TC-037 — Debounce: no se llama a autocomplete antes de 300ms
**Prioridad:** Media | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Abrir las DevTools del navegador (pestaña Network)
2. Escribir "Q" en el campo de búsqueda
3. Inmediatamente escribir "u", "i", "t", "o" sin pausas
4. Observar las requests en Network

**Resultado esperado:**
- Solo se dispara 1 request a `/api/maps/autocomplete` (no una por cada keystroke)
- El request se dispara aprox. 300ms después de dejar de escribir

**Resultado actual:** _______________

---

### TC-038 — sessionToken se regenera después de cada geocodificación
**Prioridad:** Media | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Abrir DevTools → Network
2. Realizar una búsqueda y seleccionar una sugerencia (sesión 1)
3. Anotar el `sessionToken` enviado en el request de geocode
4. Realizar otra búsqueda y seleccionar otra sugerencia (sesión 2)
5. Anotar el `sessionToken` de la segunda llamada a autocomplete

**Resultado esperado:**
- El `sessionToken` de la sesión 1 ≠ `sessionToken` de la sesión 2
- Los tokens son UUIDs v4 (formato: `xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx`)
- El mismo token se usa en todos los requests de autocomplete de la misma sesión y en el geocode final

**Resultado actual:** _______________

---

### TC-039 — Estado de carga visible durante geocodificación
**Prioridad:** Media | **Tipo:** UI | **Estado:** Pendiente

**Pasos:**
1. Seleccionar una sugerencia del dropdown
2. Observar el estado del input inmediatamente después

**Resultado esperado:**
- El input muestra estado deshabilitado o spinner durante la geocodificación
- No se puede hacer otra búsqueda mientras se procesa la actual

**Resultado actual:** _______________

---

### TC-040 — Error de geocode visible para el usuario
**Prioridad:** Media | **Tipo:** UI | **Estado:** Pendiente

**Pasos:**
1. Simular un error del backend (detener el backend temporalmente)
2. Seleccionar una sugerencia del dropdown

**Resultado esperado:**
- Se muestra un mensaje de error en la UI (no una pantalla en blanco)
- El usuario puede intentar otra búsqueda

**Resultado actual:** _______________

---

### TC-041 — Mapa estado inicial centrado en Quito
**Prioridad:** Baja | **Tipo:** UI | **Estado:** Pendiente

**Pasos:**
1. Abrir `http://localhost:5173` sin haber hecho ninguna búsqueda

**Resultado esperado:**
- El mapa muestra la zona de Quito, Ecuador como ubicación predeterminada (lat≈-0.18, lng≈-78.47)
- No hay error en el iframe

**Resultado actual:** _______________

---

### TC-042 — Filtro de fecha actualiza el historial
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Precondición:** Al menos 2 geocodificaciones en fechas distintas.

**Pasos:**
1. Abrir la sección de filtro por fecha
2. Seleccionar "Desde" = hoy y "Hasta" = hoy
3. Verificar que el historial se actualiza

**Resultado esperado:**
- Solo aparecen registros del día actual
- `totalCount` refleja el subconjunto filtrado
- La paginación se resetea a página 1

**Resultado actual:** _______________

---

### TC-043 — Botón "Limpiar" del filtro restaura el historial completo
**Prioridad:** Media | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Aplicar un filtro de fechas
2. Hacer clic en "Limpiar"

**Resultado esperado:**
- Los inputs de fecha quedan vacíos
- El historial muestra todos los registros nuevamente
- `totalCount` vuelve al total global

**Resultado actual:** _______________

---

### TC-044 — Paginación en el historial
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Precondición:** Más de 10 registros en la base de datos.

**Pasos:**
1. Verificar que el botón "Anterior" está deshabilitado en página 1
2. Hacer clic en "Siguiente"
3. Verificar que el indicador dice "Página 2 de X"
4. Hacer clic en "Anterior"
5. Verificar que vuelve a "Página 1 de X"

**Resultado esperado:**
- "Anterior" deshabilitado en página 1
- "Siguiente" deshabilitado en la última página
- Los datos de la tabla cambian al navegar entre páginas
- No hay duplicados entre páginas

**Resultado actual:** _______________

---

### TC-045 — Nueva geocodificación resetea la paginación a página 1
**Prioridad:** Media | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Navegar a la página 2 del historial
2. Realizar una nueva búsqueda y geocodificación

**Resultado esperado:**
- El historial vuelve automáticamente a la página 1
- El nuevo registro aparece como primera fila

**Resultado actual:** _______________

---

## Suite 6 — Base de Datos

### TC-046 — Estructura de tabla coincide con el spec
**Prioridad:** Alta | **Tipo:** Funcional | **Estado:** Pendiente

**Query de verificación:**
```sql
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_name = 'geocode_queries'
ORDER BY ordinal_position;
```

**Resultado esperado:**

| column_name | data_type | max_length | nullable |
|-------------|-----------|------------|----------|
| id | uuid | — | NO |
| address | character varying | 500 | NO |
| request_payload | jsonb | — | NO |
| response_payload | jsonb | — | NO |
| latitude | numeric | — | NO |
| longitude | numeric | — | NO |
| queried_at | timestamp with time zone | — | NO |

**Resultado actual:** _______________

---

### TC-047 — Precisión de lat/lng almacenada correctamente
**Prioridad:** Media | **Tipo:** Funcional | **Estado:** Pendiente

**Pasos:**
1. Geocodificar una dirección y anotar `latitude` y `longitude` del response
2. Consultar en la DB:
   ```sql
   SELECT latitude, longitude FROM geocode_queries ORDER BY queried_at DESC LIMIT 1;
   ```

**Resultado esperado:**
- `latitude`: hasta 8 decimales (DECIMAL(10,8))
- `longitude`: hasta 8 decimales (DECIMAL(11,8))
- Los valores coinciden con los del response del endpoint

**Resultado actual:** _______________

---

### TC-048 ⚠️ — Consultas de historial usan índice en queried_at
**Prioridad:** Media | **Tipo:** Funcional | **Estado:** Pendiente

> ⚠️ Issue conocido: no hay índice definido en `queried_at` (ver `code-quality-review.md` issue #10). Con volumen alto de datos, la consulta hará seq scan.

**Query de verificación:**
```sql
EXPLAIN ANALYZE
SELECT * FROM geocode_queries
WHERE queried_at >= '2026-05-01'
ORDER BY queried_at DESC
LIMIT 10;
```

**Resultado esperado (post-fix):**
- El plan incluye `Index Scan` en lugar de `Seq Scan`

**Resultado actual:** _______________

---

## Resumen de casos

| Suite | Casos | Alta | Media | Baja |
|-------|-------|------|-------|------|
| 1 — Autocomplete | TC-001 a TC-009 | 4 | 4 | 1 |
| 2 — Geocode | TC-010 a TC-018 | 5 | 3 | 0 |
| 3 — History | TC-019 a TC-030 | 6 | 4 | 0 |
| 4 — CORS y Seguridad | TC-031 a TC-035 | 5 | 0 | 0 |
| 5 — Flujo completo | TC-036 a TC-045 | 4 | 5 | 1 |
| 6 — Base de Datos | TC-046 a TC-048 | 1 | 2 | 0 |
| **Total** | **48** | **25** | **18** | **2** |

### Casos marcados como ⚠️ (bloqueados por issues conocidos)

| ID | Issue relacionado | Criticidad |
|----|-------------------|------------|
| TC-005 | Sin validación de longitud mínima en autocomplete | 🔴 Crítico |
| TC-015 | Acceso a `results[0]` sin guard | 🔴 Crítico |
| TC-016 | Sin validación de longitud de `Address` | 🔴 Crítico |
| TC-033 | API key en query string de Geocoding URL | 🔴 Crítico |
| TC-048 | Sin índice en `queried_at` | 🟡 Advertencia |

Estos 5 casos se recomienda ejecutar **después** de que los issues críticos del `code-quality-review.md` sean corregidos.
