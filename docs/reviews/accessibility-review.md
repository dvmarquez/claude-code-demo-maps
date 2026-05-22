# Accessibility Review — Maps Demo UI

Fecha: 2026-05-22
Branch: feature/maps-autocomplete-geocode-history

---

## Resumen

- Total issues: 18
- Críticos: 7
- Advertencias: 7
- Sugerencias: 4

---

## Issues por componente

### SearchBox.jsx

**[CRÍTICO] Input de búsqueda sin label accesible**
- Línea: 42–53
- Problema: El `<input>` usa `placeholder` como única indicación de propósito. No hay `<label>` asociado via `htmlFor`/`id`, ni `aria-label`. Los lectores de pantalla no anuncian el propósito del campo al recibir foco; el placeholder desaparece al escribir.
- Recomendación: Agregar `<label htmlFor="address-search" className="sr-only">Buscar dirección</label>` e `id="address-search"` al input. Alternativamente, `aria-label="Buscar dirección"` directamente en el input.

**[CRÍTICO] Dropdown de sugerencias sin gestión de teclado**
- Líneas: 67–80
- Problema: El `<ul>` de sugerencias solo responde a eventos de ratón (`onMouseDown`). No hay soporte para navegación con flechas, selección con Enter ni cierre con Escape. Usuarios de teclado no pueden seleccionar ninguna sugerencia, bloqueando la funcionalidad principal.
- Recomendación: Implementar `onKeyDown` en el input que maneje `ArrowDown`/`ArrowUp`, `Enter` y `Escape`. Usar índice de item activo en estado local con `aria-activedescendant`.

**[CRÍTICO] Dropdown sin `aria-live` — usuarios de screen reader no son notificados**
- Líneas: 67–80
- Problema: Cuando el dropdown aparece, no hay región `aria-live` que notifique al usuario cuántas sugerencias hay disponibles.
- Recomendación: Agregar elemento oculto con `aria-live="polite"` que anuncie el número de sugerencias (ej: "3 sugerencias disponibles. Use las flechas para navegar.").

**[CRÍTICO] `<li>` de sugerencias sin rol ni accesibilidad por teclado**
- Líneas: 69–78
- Problema: Cada item es un `<li>` sin `role="option"`, sin `tabIndex`, sin `aria-selected`. El patrón combobox/listbox requiere `role="listbox"` en el `<ul>` y `role="option"` en cada `<li>`.
- Recomendación: Agregar `role="listbox"` al `<ul>` y `role="option"` a cada `<li>`. Conectar con el input usando `aria-controls` y `aria-activedescendant`.

**[ADVERTENCIA] Spinner de carga sin texto alternativo**
- Líneas: 54–58
- Problema: El spinner es un `<div>` con animación CSS sin `aria-label` ni región `aria-live`. Un usuario de screen reader no sabe que el sistema está trabajando.
- Recomendación: Agregar `role="status"` y `aria-label="Buscando..."` al contenedor, o `<span className="sr-only">Buscando...</span>` dentro del div.

**[ADVERTENCIA] Mensaje de error no asociado al input**
- Líneas: 61–64
- Problema: El párrafo de error no está vinculado al input mediante `aria-describedby`.
- Recomendación: Asignar `id="geocode-error"` al párrafo y agregar `aria-describedby="geocode-error"` al input cuando el error exista.

**[ADVERTENCIA] Input deshabilitado durante geocodificación sin anuncio de estado**
- Línea: 49
- Problema: El input se deshabilita con `disabled={isGeocoding}` pero no hay feedback textual accesible que explique por qué.
- Recomendación: Agregar `<span aria-live="polite" className="sr-only">` que anuncie "Obteniendo coordenadas, por favor espere." cuando `isGeocoding` sea `true`.

---

### MapPortlet.jsx

**[ADVERTENCIA] `<iframe>` con título genérico**
- Línea: 13
- Problema: `title="Mapa"` es insuficientemente descriptivo. El usuario de screen reader no sabe qué mapa es ni qué ubicación muestra.
- Recomendación: Usar título dinámico como `title="Mapa de ubicación: {address}"`.

**[ADVERTENCIA] Mapa inaccesible para usuarios de teclado — sin alternativa textual**
- Líneas: 10–19
- Problema: Google Maps embed no es operable por teclado. No se ofrece alternativa textual de las coordenadas mostradas.
- Recomendación: Agregar elemento oculto con las coordenadas en texto: "Mapa mostrando latitud X, longitud Y".

**[SUGERENCIA] Animaciones del mapa sin consideración de `prefers-reduced-motion`**
- Línea: 11–18
- Problema: El iframe puede contener animaciones de Google que no respetan la preferencia del usuario.
- Recomendación: Documentar la limitación conocida del enfoque iframe embed.

---

### HistoryTable.jsx

**[CRÍTICO] Estado de carga sin anuncio accesible**
- Líneas: 4–11
- Problema: Los skeleton loaders no tienen `role="status"` ni texto para screen readers. El área parece vacía para usuarios de AT.
- Recomendación: Envolver los skeletons en `<div role="status"><span className="sr-only">Cargando historial de consultas...</span>`.

**[CRÍTICO] Tabla sin `scope` en encabezados de columna**
- Líneas: 29–34
- Problema: Los `<th>` no tienen `scope="col"`. Los lectores de pantalla no pueden asociar correctamente encabezados con celdas.
- Recomendación: Agregar `scope="col"` a todos los `<th>`.

**[ADVERTENCIA] Celda de dirección truncada sin texto completo accesible**
- Línea: 39
- Problema: `max-w-xs truncate` corta el texto visualmente sin exponer el valor completo a AT.
- Recomendación: Agregar `title={item.address}` a la `<td>`.

**[ADVERTENCIA] Botones de paginación sin contexto descriptivo**
- Líneas: 56–74
- Problema: "Anterior" y "Siguiente" son ambiguos para usuarios que navegan por botones sin contexto visual.
- Recomendación: Agregar `aria-label="Ir a la página anterior"` y `aria-label="Ir a la página siguiente"`.

**[SUGERENCIA] Tabla sin `caption` o `aria-label`**
- Línea: 27
- Problema: La `<table>` no está programáticamente asociada a su encabezado de sección.
- Recomendación: Agregar `aria-label="Historial de consultas de geocodificación"` a la `<table>`.

---

### DateFilter.jsx

**[CRÍTICO] Labels no asociados programáticamente a sus inputs**
- Líneas: 6–13 y 15–23
- Problema: Los `<label>` no tienen `htmlFor` y los `<input>` no tienen `id`. Los lectores de pantalla no anuncian el label al enfocar el input.
- Recomendación: Agregar `htmlFor="date-from"` / `id="date-from"` y `htmlFor="date-to"` / `id="date-to"`.

**[ADVERTENCIA] Botón "Limpiar" sin contexto descriptivo**
- Líneas: 26–33
- Problema: El texto "Limpiar" es ambiguo fuera de contexto visual.
- Recomendación: Agregar `aria-label="Limpiar filtros de fecha"`.

**[SUGERENCIA] Inputs de fecha sin instrucciones de formato**
- Líneas: 7–13 y 16–24
- Problema: No hay comunicación textual de restricciones como `min={dateFrom}`.

---

### HomePage.jsx

**[SUGERENCIA] Resultado de coordenadas sin región `aria-live`**
- Líneas: 41–46
- Problema: El bloque de latitud/longitud aparece condicionalmente sin `aria-live`. Los usuarios de screen reader no son notificados cuando las coordenadas se actualizan.
- Recomendación: Envolver en `<div aria-live="polite" aria-atomic="true">`.

---

## Resumen por componente

| Componente     | Críticos | Advertencias | Sugerencias |
|----------------|----------|--------------|-------------|
| SearchBox.jsx  | 4        | 3            | 0           |
| MapPortlet.jsx | 0        | 2            | 1           |
| HistoryTable   | 2        | 2            | 1           |
| DateFilter.jsx | 1        | 1            | 1           |
| HomePage.jsx   | 0        | 0            | 1           |
| **Total**      | **7**    | **8**        | **4**       |

---

## Conclusión

El issue más crítico es la ausencia total de soporte de teclado en el dropdown de `SearchBox`, que bloquea la funcionalidad principal para usuarios sin ratón (WCAG 2.1.1, nivel A). Le siguen la falta de labels asociados en `DateFilter` (WCAG 1.3.1, nivel A) y la ausencia de `scope` en `HistoryTable` (WCAG 1.3.1).

**Correcciones prioritarias antes de merge:**
1. Navegación por teclado en el dropdown con `role="listbox"` / `role="option"` y `aria-activedescendant`
2. `aria-live="polite"` en el dropdown de sugerencias
3. Labels con `htmlFor` en DateFilter
4. `scope="col"` en encabezados de HistoryTable
5. `role="status"` con texto oculto en skeleton loader de HistoryTable
6. `aria-label` o `id` en el input de búsqueda
7. `aria-describedby` para el mensaje de error en SearchBox
