# Pantallas — Core5, Core6, Core7, Core9

Estas historias corresponden a pantallas (frontend). Está PENDIENTE decidir si se
implementarán como:

  A) Razor Pages / MVC Views dentro de este mismo proyecto ASP.NET.
  B) Un proyecto de frontend separado (React, Vue, etc.) que consuma los Web Services.

## Decisión pendiente de equipo

Coordinar con el profesor/equipo y actualizar este README con la decisión tomada.

## Stubs de pantalla

### Core5 — Login del sistema Core
- Consume: Core4 (POST /api/auth/login)
- Responsable: [NOMBRE DEL INTEGRANTE CORE5]

### Core6 — Listado de puestos
- Consume: Core1 (GET /api/puestos)
- Responsable: [NOMBRE DEL INTEGRANTE CORE6]

### Core7 — Listado de oferentes aptos
- Consume: Core2 (GET /api/oferentes/aptos/{idPuesto})
- Responsable: [NOMBRE DEL INTEGRANTE CORE7]

### Core9 — Detalle de oferente + botón Crear empleado
- Consume: Core8 (GET /api/oferentes/{idOferente})
- Consume: Core3 (POST /api/empleados)
- Responsable: [NOMBRE DEL INTEGRANTE CORE9]
