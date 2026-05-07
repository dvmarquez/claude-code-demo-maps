# /code-review — Revisión de Código con Subagentes

## Propósito
Ejecuta una revisión completa del código usando subagentes especializados: 
uno para accesibilidad del frontend y otro para calidad del código backend.

## Pasos a seguir

### 1. Identificar archivos modificados
- Ejecutar `git diff main --name-only` para obtener los archivos cambiados
- Separar archivos de frontend (carpeta `frontend/`) y backend (carpeta `backend/`)

### 2. Subagente — Accessibility Review (Frontend)
Revisar los componentes React modificados evaluando:

- **Semántica HTML** — uso correcto de elementos semánticos (header, main, nav, etc.)
- **ARIA labels** — presencia de atributos aria-label, aria-describedby donde corresponda
- **Contraste de colores** — clases de Tailwind que puedan generar bajo contraste
- **Navegación por teclado** — elementos interactivos accesibles con Tab y Enter
- **Textos alternativos** — imágenes e íconos con alt o aria-label
- **Formularios** — labels asociados correctamente a inputs

Generar reporte en: `docs/reviews/accessibility-review.md`

### 3. Subagente — Code Quality Review (Backend)
Revisar los archivos .NET modificados evaluando:

- **Manejo de errores** — try/catch apropiados, retorno de ProblemDetails
- **Async/Await** — uso correcto sin bloqueos síncronos
- **Seguridad** — API keys no expuestas, validación de inputs
- **Performance** — consultas a base de datos optimizadas, uso de índices
- **Logs** — uso apropiado de ILogger en puntos clave
- **Convenciones** — cumplimiento de las convenciones definidas en CLAUDE.md

Generar reporte en: `docs/reviews/code-quality-review.md`

### 4. Resumen ejecutivo
Al finalizar ambos subagentes, presentar un resumen con:
- Total de issues encontrados por categoría
- Issues críticos que deben resolverse antes de merge
- Sugerencias de mejora opcionales

## Reglas
- Revisar únicamente archivos modificados en el branch actual
- No modificar código durante la revisión, solo reportar
- Clasificar cada issue como: Crítico, Advertencia o Sugerencia