# Accessibility Review — Maps Demo UI

**Fecha:** 2026-05-07
**Branch:** feature/maps-autocomplete-geocode-history
**Archivos revisados:**
- `src/main.jsx`
- `src/App.jsx`
- `src/pages/HomePage.jsx`
- `src/components/SearchBox.jsx`
- `src/components/MapPortlet.jsx`
- `src/components/DateFilter.jsx`
- `src/components/HistoryTable.jsx`

---

## Resumen

| Categoría | Cantidad |
|-----------|----------|
| 🔴 Críticos | 6 |
| 🟡 Advertencias | 6 |
| 🟢 Sugerencias | 5 |
| **Total** | **17** |

---

## Issues encontrados

### SearchBox

#### 🔴 Crítico — Input sin label visible ni aria-label
**Archivo:** `src/components/SearchBox.jsx:42`
**Problema:** El `<input type="text">` solo tiene `placeholder="Buscar dirección..."`. No existe un `<label>` asociado via `htmlFor`/`id` ni un atributo `aria-label`. El placeholder desaparece al escribir y no es equivalente a un label para lectores de pantalla.
**Recomendación:** Agregar `id="search-address"` al input y un `<label htmlFor="search-address">` visible (o `sr-only`), o al menos `aria-label="Buscar dirección"` en el input.

#### 🔴 Crítico — Dropdown sin aria-live, role="listbox" ni role="option"
**Archivo:** `src/components/SearchBox.jsx:67-80`
**Problema:** El `<ul>` con sugerencias aparece y desaparece dinámicamente pero no tiene `aria-live` ni `role="listbox"`. Los usuarios de lectores de pantalla no son notificados cuando aparecen sugerencias ni cuántas hay disponibles. Los `<li>` tampoco tienen `role="option"`.
**Recomendación:** Agregar `role="listbox"` al `<ul>` y `role="option"` a cada `<li>`. Agregar un elemento con `aria-live="polite"` que anuncie la cantidad de sugerencias disponibles.

#### 🔴 Crítico — Dropdown no navegable con teclado
**Archivo:** `src/components/SearchBox.jsx:67-80`
**Problema:** Las sugerencias solo son seleccionables via `onMouseDown`. No hay manejo de eventos de teclado (`ArrowDown`, `ArrowUp`, `Escape`, `Enter`). Los usuarios que navegan exclusivamente con teclado no pueden seleccionar una sugerencia.
**Recomendación:** Agregar `onKeyDown` al input que gestione `ArrowDown`/`ArrowUp` para mover foco entre sugerencias, `Escape` para cerrar, y `Enter` para confirmar. Cada `<li>` debe tener `tabIndex={-1}` y `onKeyDown` propio. Combinar con `aria-activedescendant`.

#### 🔴 Crítico — Spinner de carga sin anuncio a lector de pantalla
**Archivo:** `src/components/SearchBox.jsx:54-58`
**Problema:** El spinner es puramente visual sin texto alternativo ni `aria-label`. Los usuarios de lectores de pantalla no saben que hay una operación en curso. Tampoco hay `aria-busy` en el input durante `isGeocoding`.
**Recomendación:** Agregar `aria-busy={isLoadingAutocomplete || isGeocoding}` al input, un `<span className="sr-only">` con texto "Buscando..." o "Obteniendo coordenadas...", y `aria-hidden="true"` al div del spinner.

#### 🟡 Advertencia — aria-expanded ausente en el combobox
**Archivo:** `src/components/SearchBox.jsx:42`
**Problema:** El input actúa como combobox pero no tiene `aria-expanded` para indicar el estado del dropdown ni `aria-haspopup="listbox"`.
**Recomendación:** Agregar `role="combobox"`, `aria-expanded={showDropdown && suggestions.length > 0}`, `aria-haspopup="listbox"`, y `aria-autocomplete="list"` al input.

#### 🟡 Advertencia — Mensaje de error no vinculado al input via aria-describedby
**Archivo:** `src/components/SearchBox.jsx:61-65`
**Problema:** El párrafo de error no está asociado al input mediante `aria-describedby`. Un lector de pantalla puede no conectar el mensaje de error con el campo que lo produjo.
**Recomendación:** Agregar `id="search-error"` al `<p>` del error y `aria-describedby="search-error"` al input condicionalmente cuando `geocodeError` existe.

---

### DateFilter

#### 🔴 Crítico — Labels no asociados a sus inputs (falta htmlFor/id)
**Archivo:** `src/components/DateFilter.jsx:6-25`
**Problema:** Los `<label>` "Desde" y "Hasta" no tienen `htmlFor` y los `<input type="date">` no tienen `id`. La asociación es solo visual. Los lectores de pantalla no establecen la relación semántica.
**Recomendación:** Agregar `htmlFor="date-from"` al label "Desde" e `id="date-from"` al primer input. Ídem para el segundo con `htmlFor="date-to"` e `id="date-to"`.

#### 🟡 Advertencia — Contraste insuficiente en labels con text-gray-500
**Archivo:** `src/components/DateFilter.jsx:6,16`
**Problema:** `text-gray-500` sobre fondo blanco produce un ratio de ~4.48:1, inferior al mínimo WCAG AA de 4.5:1 para texto normal. Los labels "Desde" y "Hasta" son texto pequeño.
**Recomendación:** Cambiar `text-gray-500` a `text-gray-600` en los labels del filtro.

#### 🟢 Sugerencia — Botón "Limpiar" sin descripción contextual
**Archivo:** `src/components/DateFilter.jsx:27-33`
**Problema:** El botón "Limpiar" no especifica qué está limpiando para usuarios de lectores de pantalla.
**Recomendación:** Agregar `aria-label="Limpiar filtro de fechas"` al botón.

---

### HistoryTable

#### 🟡 Advertencia — Estado de carga (skeleton) sin anuncio accesible
**Archivo:** `src/components/HistoryTable.jsx:4-11`
**Problema:** Los divs animados con `animate-pulse` no tienen texto ni región `aria-live` que anuncie que el contenido está cargando.
**Recomendación:** Agregar `<p className="sr-only" aria-live="polite">Cargando historial...</p>` dentro del estado de carga.

#### 🟡 Advertencia — Estado vacío sin aria-live
**Archivo:** `src/components/HistoryTable.jsx:14-17`
**Problema:** El párrafo "No hay consultas registradas." puede no ser anunciado al actualizarse dinámicamente (por ejemplo, al filtrar por fecha).
**Recomendación:** Envolver en un contenedor con `aria-live="polite"` o agregar el atributo directamente al `<p>`.

#### 🟡 Advertencia — Botones de paginación sin contexto para lectores de pantalla
**Archivo:** `src/components/HistoryTable.jsx:56-74`
**Problema:** Los botones "Anterior" y "Siguiente" no comunican la página actual ni el destino.
**Recomendación:** Agregar `aria-label="Ir a la página anterior"` / `aria-label="Ir a la página siguiente"`. Envolver la paginación en `<nav aria-label="Paginación del historial">`.

#### 🟢 Sugerencia — Celda de dirección truncada sin title completo
**Archivo:** `src/components/HistoryTable.jsx:39`
**Problema:** La celda con `max-w-xs truncate` no permite ver el texto completo visualmente.
**Recomendación:** Agregar `title={item.address}` a la `<td>`.

#### 🟢 Sugerencia — Tabla sin caption ni aria-label
**Archivo:** `src/components/HistoryTable.jsx:27`
**Problema:** La `<table>` no tiene `<caption>` ni `aria-label`. Los lectores de pantalla anuncian "tabla" sin contexto.
**Recomendación:** Agregar `aria-label="Historial de consultas de geocodificación"` a la `<table>`.

---

### MapPortlet

#### 🟢 Sugerencia — iframe con title genérico
**Archivo:** `src/components/MapPortlet.jsx:13`
**Problema:** `title="Mapa"` es funcional pero genérico. Cuando hay una ubicación activa no comunica qué lugar se muestra.
**Recomendación:** Recibir prop `address` y usar `title={address ? \`Mapa de ${address}\` : 'Mapa de ubicación predeterminada'}`.

---

### HomePage

#### 🟢 Sugerencia — Resultado de geocodificación sin aria-live
**Archivo:** `src/pages/HomePage.jsx:41-46`
**Problema:** El bloque con latitud/longitud aparece dinámicamente tras seleccionar una sugerencia pero no tiene `aria-live`. Los lectores de pantalla no notifican el nuevo resultado.
**Recomendación:** Envolver en `<div aria-live="polite" aria-atomic="true">`. Mantener el div en el DOM cuando `currentResult` es null para que la región viva ya esté registrada.

---

### index.html

#### 🔴 Crítico — lang="en" con contenido en español
**Archivo:** `index.html:2`
**Problema:** El atributo `lang="en"` no corresponde al idioma del contenido. Los lectores de pantalla pronunciarán el texto en español con reglas fonológicas del inglés.
**Recomendación:** Cambiar a `<html lang="es">`.

---

## Componentes sin issues

- `src/App.jsx` — Sin issues. Componente de envoltura trivial.
- `src/main.jsx` — Sin issues. Uso correcto de `StrictMode` y `QueryClientProvider`.
