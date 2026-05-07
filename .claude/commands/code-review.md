# /code-review — Revisión de Código con Subagentes

## Propósito
Ejecuta una revisión completa del código usando subagentes especializados: 
uno para accesibilidad del frontend y otro para calidad del código backend,
y al finalizar crea un Pull Request automáticamente con el resumen de los resultados.

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

### 5. Crear Pull Request
Una vez generados ambos reportes, seguir estos pasos:

#### 5.1 Generar el body del PR
Construir el body dinámicamente basado en:
- Los archivos modificados detectados en el paso 1
- Los endpoints implementados en el backend
- Los componentes y hooks creados en el frontend
- El resumen de issues de ambos reportes de revisión

El body debe seguir esta estructura:

```
## ¿Qué hace este PR?
{descripción generada en base a los cambios reales implementados}

## Cambios incluidos
### Backend
{lista de endpoints y archivos creados/modificados}

### Frontend
{lista de componentes y hooks creados/modificados}

### Base de Datos
{migraciones aplicadas}

## Revisiones realizadas
- ✅ Accessibility Review — {X} issues encontrados — ver docs/reviews/accessibility-review.md
- ✅ Code Quality Review — {X} issues encontrados — ver docs/reviews/code-quality-review.md

## Testing
{checklist generado en base a los criterios de aceptación del spec}
```

#### 5.2 Crear el PR
```bash
gh pr create \
  --title "feat: {título generado en base a los cambios reales}" \
  --body "{body generado en el paso 5.1}" \
  --base main
```

Confirmar al usuario la URL del PR creado.

## Reglas
- Revisar únicamente archivos modificados en el branch actual
- No modificar código durante la revisión, solo reportar
- Clasificar cada issue como: Crítico, Advertencia o Sugerencia
- El body del PR debe reflejar los cambios reales, no un template genérico
