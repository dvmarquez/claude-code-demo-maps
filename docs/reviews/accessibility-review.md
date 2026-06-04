# Accessibility Review
Fecha: 2026-06-04
Branch: feature/maps-autocomplete-geocode-history

## Resumen
- Total issues: 14
- Críticos: 2
- Advertencias: 7
- Sugerencias: 5

---

## Issues Encontrados

### [CRÍTICO] Spinner de carga no anunciado a lectores de pantalla

- Archivo: `src/components/SearchBox.jsx`
- Líneas: 84–88
- Problema: El spinner visual (div con `animate-spin`) que aparece durante autocompletado y geocodificación es puramente decorativo — no tiene `aria-label`, `role="status"` ni ningún texto `sr-only`. La región `aria-live` existente (línea 97) solo anuncia el número de sugerencias disponibles, no el estado de carga en sí. Los usuarios de lector de pantalla no reciben ninguna señal de que el sistema está procesando su solicitud.
- Solución sugerida: Agregar un `<span className="sr-only">` dentro del contenedor del spinner con texto contextual ("Buscando sugerencias..." o "Obteniendo coordenadas, por favor espere."), o bien emitir ese texto a través de la región `aria-live` existente. Como alternativa, agregar `aria-busy={isLoadingAutocomplete || isGeocoding}` al `<input>`.

---

### [CRÍTICO] Ausencia de enlace "Saltar al contenido principal" (skip link)

- Archivo: `src/pages/HomePage.jsx`
- Líneas: 32–35 (bloque `<header>`)
- Problema: La página no tiene un enlace de salto al inicio del DOM. Cada vez que se carga o recarga la página, el usuario de teclado debe tabular por todos los elementos del `<header>` antes de llegar al contenido principal. Con la estructura actual esto es relativamente corto, pero constituye una violación de WCAG 2.4.1 (Bypass Blocks, nivel A).
- Solución sugerida: Agregar como primer hijo del componente raíz `<a href="#main-content" className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 ...">Saltar al contenido</a>` y añadir `id="main-content"` al elemento `<main>`.

---

### [ADVERTENCIA] Mensaje de error de geocodificación no vinculado al input mediante `aria-describedby`

- Archivo: `src/components/SearchBox.jsx`
- Líneas: 64–83 (input), 91–94 (párrafo de error)
- Problema: Cuando `geocodeError` es verdadero se renderiza un `<p>` con el mensaje de error, pero el `<input>` no tiene `aria-describedby` apuntando a ese párrafo. Los lectores de pantalla no asociarán automáticamente el error con el campo de búsqueda; el usuario puede re-enfocar el input y no escuchar el mensaje de error.
- Solución sugerida: Asignar `id="geocode-error"` al `<p>` de error y agregar `aria-describedby={geocodeError ? 'geocode-error' : undefined}` al `<input>`.

---

### [ADVERTENCIA] El mapa no anuncia cambios de ubicación a lectores de pantalla

- Archivo: `src/components/MapPortlet.jsx` y `src/pages/HomePage.jsx`
- Líneas: 41–46 (HomePage — bloque de coordenadas), 11–17 (MapPortlet — iframe)
- Problema: Cuando el usuario selecciona una sugerencia, el mapa y el bloque de coordenadas se actualizan silenciosamente. El bloque de latitud/longitud en `HomePage` (línea 42) no está envuelto en una región `aria-live`, por lo que los usuarios de lector de pantalla no son notificados de que se obtuvo un resultado nuevo. El iframe tampoco actualiza su `title` con la nueva ubicación.
- Solución sugerida: Envolver el bloque de coordenadas en `<div aria-live="polite" aria-atomic="true">` para que el lector de pantalla anuncie el resultado. Complementariamente, actualizar el `title` del iframe de forma dinámica con la dirección buscada.

---

### [ADVERTENCIA] El título del iframe del mapa es demasiado genérico

- Archivo: `src/components/MapPortlet.jsx`
- Línea: 13
- Problema: El atributo `title="Mapa"` del `<iframe>` no describe la ubicación que está siendo visualizada. WCAG 4.1.2 requiere que los elementos de interfaz tengan nombres que describan su propósito. Un usuario de lector de pantalla que navegue por frames escuchará solo "Mapa" sin saber qué lugar se muestra.
- Solución sugerida: Recibir la dirección o coordenadas como prop y construir un título dinámico: `title={address ? \`Mapa de ubicación: ${address}\` : "Mapa — ubicación predeterminada"}`.

---

### [ADVERTENCIA] Celdas de dirección truncadas sin texto completo accesible

- Archivo: `src/components/HistoryTable.jsx`
- Línea: 42
- Problema: Las celdas de la columna "Dirección" aplican `max-w-xs truncate`, lo que puede ocultar visualmente parte del texto. No se incluye atributo `title` ni ningún mecanismo alternativo para que usuarios de teclado o lector de pantalla accedan al texto completo de la dirección.
- Solución sugerida: Agregar `title={item.address}` al elemento `<td>`. Aunque `title` es un mecanismo de bajo nivel, es suficiente para este caso de uso tabular.

---

### [ADVERTENCIA] Botones de paginación sin contexto de acción en su nombre accesible

- Archivo: `src/components/HistoryTable.jsx`
- Líneas: 59–65 (botón "Anterior"), 70–77 (botón "Siguiente")
- Problema: Los botones tienen solo el texto "Anterior" y "Siguiente". Un usuario que navegue la página mediante lista de elementos interactivos (sin contexto visual) no sabe a qué sección pertenece esta paginación ni cuántas páginas hay en total.
- Solución sugerida: Agregar `aria-label` descriptivos: `aria-label="Ir a la página anterior"` y `aria-label={`Ir a la página siguiente, página ${page + 1} de ${totalPages}`}`. Considerar también envolver el bloque en `<nav aria-label="Paginación del historial">`.

---

### [ADVERTENCIA] Botón "Limpiar" en DateFilter sin descripción accesible

- Archivo: `src/components/DateFilter.jsx`
- Línea: 29–34
- Problema: El texto "Limpiar" es ambiguo si se navega la página por lista de botones sin contexto visual. No está claro qué se limpia.
- Solución sugerida: Agregar `aria-label="Limpiar filtro de fechas"` al botón.

---

### [ADVERTENCIA] Los inputs de fecha no están agrupados semánticamente

- Archivo: `src/components/DateFilter.jsx`
- Líneas: 3–37
- Problema: El texto "Filtrar por fecha:" es un `<span>`, no un `<legend>`. Los dos inputs de fecha no están agrupados en un `<fieldset>` con `<legend>`, por lo que un lector de pantalla anuncia cada campo de forma independiente sin el contexto de que ambos conforman un rango de fechas relacionado. WCAG 1.3.1 (Info and Relationships) requiere que la relación entre controles relacionados sea determinable programáticamente.
- Solución sugerida: Reemplazar el `<div>` contenedor y el `<span>` por `<fieldset>` y `<legend>`:
  ```jsx
  <fieldset className="flex flex-wrap items-center gap-4 border-0 p-0 m-0">
    <legend className="text-sm font-medium text-gray-600 float-left">Filtrar por fecha:</legend>
    ...
  </fieldset>
  ```

---

### [SUGERENCIA] El input de búsqueda no tiene un elemento `<label>` visible

- Archivo: `src/components/SearchBox.jsx`
- Línea: 64–68
- Problema: El input usa `aria-label="Buscar dirección"` e `id="address-search"`, pero no tiene un elemento `<label htmlFor="address-search">` en el DOM. Aunque `aria-label` es técnicamente suficiente para cumplir WCAG 2.1 AA, las guías de autoría ARIA recomiendan preferir un `<label>` visible ya que mejora la orientación para usuarios con discapacidades cognitivas y amplía el área de clic hacia el campo.
- Solución sugerida: Agregar `<label htmlFor="address-search" className="sr-only">Buscar dirección</label>` inmediatamente antes del input, o un label visible encima de él, y mantener el `aria-label` solo si no se agrega el label visible.

---

### [SUGERENCIA] Las secciones no tienen `aria-labelledby` apuntando a sus encabezados

- Archivo: `src/pages/HomePage.jsx`
- Líneas: 38, 49, 54
- Problema: Los tres elementos `<section>` contienen `<h2>` que los identifican, pero no tienen `aria-labelledby`. Algunos lectores de pantalla (especialmente en modo de navegación de landmarks) anuncian el nombre del landmark cuando el usuario entra a él, pero solo si `aria-labelledby` o `aria-label` está presente. Sin esto, los tres `<section>` aparecen como "región" anónima.
- Solución sugerida: Agregar `id` a cada `<h2>` y `aria-labelledby` al `<section>` correspondiente. Ejemplo: `<section aria-labelledby="heading-buscar">` con `<h2 id="heading-buscar">Buscar dirección</h2>`.

---

### [SUGERENCIA] Animación del spinner no respeta `prefers-reduced-motion`

- Archivo: `src/components/SearchBox.jsx`
- Línea: 86–87
- Problema: La clase `animate-spin` de Tailwind ejecuta una animación CSS continua de rotación. Tailwind v4 soporta la variante `motion-safe:` pero no aplica automáticamente el media query a `animate-spin`. Usuarios con epilepsia fotosensible o que experimentan mareos por movimiento continuo pueden verse afectados.
- Solución sugerida: Cambiar la clase a `motion-safe:animate-spin` en el div del spinner, o agregar una regla en `index.css`:
  ```css
  @media (prefers-reduced-motion: reduce) {
    .animate-spin { animation: none; }
  }
  ```

---

### [SUGERENCIA] Texto `text-gray-500` en 14px tiene contraste mínimamente suficiente

- Archivos: `src/components/DateFilter.jsx` (línea 6), `src/components/HistoryTable.jsx` (líneas 26, 49), `src/pages/HomePage.jsx` (línea 34)
- Problema: El color `text-gray-500` (#6B7280) sobre fondo blanco produce un ratio de contraste de ~4.63:1. WCAG 2.1 AA exige 4.5:1 para texto no-grande, por lo que técnicamente pasa, pero con un margen de solo 0.13:1. Variaciones en calibración de monitor, renderizado de fuentes o condiciones de iluminación pueden hacer que este texto falle en la práctica. Afecta textos descriptivos a 14px (text-sm) sin negrita.
- Solución sugerida: Reemplazar `text-gray-500` por `text-gray-600` (#4B5563, ratio ~7.0:1) en textos de soporte, etiquetas de formulario y mensajes informativos para garantizar un margen de contraste cómodo.

---

### [SUGERENCIA] No existe estilo global de `focus-visible` como fallback

- Archivo: `src/index.css`
- Problema: El archivo global `index.css` no define ningún estilo de indicador de foco en `:focus-visible`. Los componentes individuales aplican `focus:ring-2 focus:ring-blue-500` en sus clases de Tailwind, pero si algún elemento interactivo futuro omite esas clases, quedará sin indicador de foco visible, incumpliendo WCAG 2.4.7 (Focus Visible, nivel AA).
- Solución sugerida: Agregar una regla base defensiva en `index.css`:
  ```css
  :focus-visible {
    outline: 2px solid #2563EB;
    outline-offset: 2px;
  }
  ```

---

## Componentes Revisados

| Componente | Estado | Issues |
|---|---|---|
| App.jsx | Sin issues | — |
| HomePage.jsx | Crítico/Advertencia | Skip link ausente (crítico), mapa sin live region, secciones sin aria-labelledby |
| SearchBox.jsx | Crítico/Advertencia | Spinner sin anuncio (crítico), error sin aria-describedby, label sin elemento visible |
| MapPortlet.jsx | Advertencia | Sin live region al cambiar mapa, title genérico del iframe |
| HistoryTable.jsx | Advertencia | Celdas truncadas sin title, botones de paginación sin contexto |
| DateFilter.jsx | Advertencia | Sin fieldset/legend, botón limpiar sin aria-label, contraste límite |
| index.css | Sugerencia | Sin focus-visible global, sin prefers-reduced-motion |

---

## Nota sobre el estado actual vs revisión anterior

La versión actual del código resolvió todos los issues críticos de la revisión anterior del 2026-05-22:
- El input de búsqueda ya tiene `aria-label`, `id`, `role="combobox"` y `aria-autocomplete`.
- El dropdown ya tiene `role="listbox"` con items `role="option"` y `aria-selected`.
- La navegación por teclado (flechas, Enter, Escape) está implementada en `handleKeyDown`.
- La región `aria-live="polite"` en SearchBox anuncia el número de sugerencias.
- El loading skeleton en HistoryTable ya tiene `role="status"` y texto `sr-only`.
- Los `<th>` en HistoryTable ya tienen `scope="col"`.
- Los `<label>` en DateFilter ya están asociados con `htmlFor`/`id`.
