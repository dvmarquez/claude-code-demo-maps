# /spec — Spec-Driven Feature Planning

## Propósito
Genera un spec técnico detallado a partir de una descripción en lenguaje natural, 
crea un nuevo Git branch para el desarrollo y prepara el plan de implementación.

## Pasos a seguir

### 1. Analizar el requerimiento
- Leer cuidadosamente la descripción del feature proporcionada
- Identificar los componentes afectados: backend, frontend, base de datos
- Validar que el requerimiento es consistente con la arquitectura definida en CLAUDE.md

### 2. Crear el Git branch
- Nombre del branch en formato: `feature/descripcion-corta-en-kebab-case`
- Ejecutar: `git checkout -b feature/nombre-del-branch`
- Confirmar al usuario el branch creado

### 3. Generar el spec técnico
Crear el archivo `docs/specs/NOMBRE_FEATURE.md` con esta estructura:

#### Descripción
Resumen del feature en 2-3 oraciones.

#### Cambios en Base de Datos
- Nuevas tablas o columnas necesarias
- Migraciones requeridas

#### Cambios en Backend
- Nuevos endpoints o modificaciones
- DTOs necesarios
- Lógica de negocio

#### Cambios en Frontend
- Componentes nuevos o modificados
- Hooks nuevos o modificados
- Cambios en UI/UX

#### Plan de Implementación
Lista ordenada de tareas concretas con el orden sugerido de implementación.

#### Criterios de Aceptación
Lista de condiciones que deben cumplirse para considerar el feature completo.

### 4. Presentar resumen
- Mostrar al usuario el branch creado
- Mostrar la ruta del archivo spec generado
- Preguntar si aprueba el plan antes de proceder con la implementación

## Reglas
- Nunca iniciar la implementación sin aprobación explícita del usuario
- El spec debe ser consistente con CLAUDE.md
- Si hay ambigüedades en el requerimiento, preguntar antes de generar el spec