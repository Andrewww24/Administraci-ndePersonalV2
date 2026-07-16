# Proyecto: Sistema Core - Esqueleto Base (Alcance 2)
## Administración de Personal V2 — Servicios Médicos SA — CUC

## 1. Contexto

Este es el **Alcance 2** de un proyecto académico de "Administración de Personal" para el
Colegio Universitario de Cartago. El **Alcance 1 ya está completo** (sistema Web interno con
ASP.NET, Dapper y MySQL).

Ahora se necesita construir el **sistema "Core"**: un conjunto de Web Services y pantallas
propias que serán desarrollados por **varios integrantes del equipo en paralelo**, cada uno
responsable de una o más historias de usuario.

**Importante: esta tarea es SOLO para crear el esqueleto/andamiaje del proyecto.**
No se debe implementar la lógica de negocio de ninguna historia de usuario todavía —
el objetivo es dejar la base lista (estructura, configuración, convenciones y stubs
vacíos) para que cada miembro del equipo pueda tomar su historia asignada y trabajar
sin pisarse con los demás ni tener que resolver decisiones de arquitectura por su cuenta.

## 2. Objetivo de esta tarea

Crear, desde cero, el **esqueleto de un proyecto ASP.NET 8 Web API** en Visual Studio
Code, con:

- Estructura de carpetas/proyectos de la solución, ya organizada por responsabilidad,
  para que el trabajo de cada persona quede aislado en sus propios archivos.
- Configuración base funcionando (conexión a MySQL vía Dapper, inyección de
  dependencias, Swagger, manejo de errores, appsettings).
- Un helper/servicio de bitácora reutilizable (ver sección 6), ya que aplica a
  **todas** las historias de usuario.
- Un controlador y un método **stub** (vacío, con `// TODO` y la firma correcta) por
  cada historia de usuario Core1–Core9 (ver sección 7), de modo que cada integrante
  del equipo abra su archivo correspondiente y empiece a programar directamente.
- **NO** implementar la lógica interna de ningún endpoint todavía (ni siquiera uno de
  ejemplo) — solo la firma, la ruta, los parámetros esperados y el TODO describiendo
  qué debe hacer.
- Verificar que el proyecto compila y levanta correctamente con los stubs vacíos.

## 3. Stack tecnológico obligatorio

- **.NET 8** (ASP.NET Core Web API).
- **MySQL** como motor de base de datos (ya existe y tiene datos, ver sección 5).
- **Dapper** como micro-ORM para acceso a datos (NO Entity Framework).
- Autenticación simple basada en JWT o similar ligero (dejar el mecanismo listo pero
  simple; es un proyecto académico, no enterprise).
- Documentación de la API con Swagger/OpenAPI (`Swashbuckle.AspNetCore`).

## 4. Estructura de solución sugerida

```
/AdministracionPersonal.Core
  /AdministracionPersonal.Core.Api          <- Proyecto Web API (controllers, Program.cs)
  /AdministracionPersonal.Core.Application  <- Servicios / lógica de negocio (vacíos por ahora)
  /AdministracionPersonal.Core.Infrastructure <- Repositorios Dapper, conexión MySQL
  /AdministracionPersonal.Core.Domain        <- Modelos / DTOs
  AdministracionPersonal.Core.sln
```

Si para un equipo estudiantil resulta excesivo, proponer algo más simple (un solo
proyecto con carpetas `Controllers/`, `Services/`, `Repositories/`, `Models/`) es
bienvenido — priorizar que cada historia de usuario tenga un lugar claro y único
donde vivir, sobre arquitectura elaborada.

Dentro de `Controllers/` (o el equivalente), un archivo por historia de usuario:
```
Controllers/
  PuestosController.cs        <- Core1
  OferentesController.cs      <- Core2, Core8
  EmpleadosController.cs      <- Core3
  AuthController.cs           <- Core4
```
(Ajustar agrupación si tiene más sentido de otra forma; lo importante es que quede
explícito qué archivo le corresponde a cada historia para repartir el trabajo.)

## 5. Base de datos existente (ya está lista, NO crear tablas nuevas)

Base de datos MySQL/MariaDB `db_personal_sitios`. Tablas y vistas relevantes ya
existen:

### Tabla `puesto`
```sql
id_puesto, codigo, nombre, salario, id_area, id_puesto_jefe, disponible (tinyint),
descripcion_publica, fecha_publicacion
```

### Tabla `oferente`
```sql
id_oferente, identificacion, tipo_identificacion (CEDULA/DIMEX/PASAPORTE),
nombre_completo, fecha_nacimiento, direccion, id_distrito, fecha_registro
```

### Tabla `postulacion`
```sql
id_postulacion, id_oferente, id_puesto, fecha_postulacion,
estado (RECIBIDA/EN_REVISION/APTO/...), observacion
```

### Tabla `requisito_puesto`
```sql
id_requisito, id_puesto, nombre
```

### Tabla `oferente_requisito`
```sql
id_oferente, id_requisito, cumple (tinyint), observacion, fecha_revision
```

### Tabla `curriculum_oferente`
```sql
id_curriculum, id_oferente, id_postulacion, nombre_archivo, ruta_archivo,
tipo_archivo, tamano_bytes, fecha_carga
```

### Tabla `empleado`
```sql
id_empleado, numero_empleado, id_oferente, id_puesto, fecha_ingreso
```

### Tabla `accion_personal`
```sql
id_accion, tipo_accion (CONTRATACION/ASCENSO/TRASLADO/DESPIDO/OTRO),
fecha_accion, descripcion, id_empleado, id_aprobador
```

### Tabla `usuario` (ya existe, del Alcance 1, se reutiliza para Core4/Core5)
```sql
id_usuario, usuario, nombre_completo, correo, password_hash,
estado (ACTIVO/INACTIVO/BLOQUEADO), intentos_login, fecha_creacion,
fecha_ultimo_login, fecha_bloqueo
```
Nota: `password_hash` está encriptado con AES-GCM (formato `AESGCM:iv:tag:cipherdata`
en base64, separado por `:`). Hay que replicar/reutilizar el mismo esquema de
verificación que usa el sistema del Alcance 1.

### Tabla `bitacora` (reutilizar, ya existe)
```sql
id_bitacora, fecha, id_usuario, tipo (INSERT/UPDATE/DELETE/SELECT/ERROR),
entidad, datos_anteriores (json), datos_nuevos (json), descripcion
```

### Vistas ya creadas (usarlas en vez de reescribir los joins)
- `vw_puestos_disponibles` → puestos con `disponible = 1`, incluye nombre de área.
- `vw_oferentes_aptos_puesto` → oferentes aptos para un puesto, filtrable por `id_puesto`.
- `vw_detalle_oferente` → detalle completo de un oferente, filtrable por `id_oferente`.
- `vw_postulaciones_detalle` → detalle de postulaciones con datos de oferente y puesto.

## 6. Manejo de bitácoras (regla transversal, aplica a TODAS las historias)

Cada acción importante (crear, actualizar, eliminar) debe registrar una fila en
`bitacora` con fecha, usuario, tipo, y descripción (JSON del registro anterior/nuevo
según el caso, o texto simple para consultas: `"El usuario consulta <elemento>"`).

Como parte del esqueleto, crear un servicio reutilizable (ej. `IBitacoraService`)
inyectable en cualquier controlador/servicio, para que cada quien lo use al
implementar su historia sin tener que diseñar esa parte por su cuenta.

## 7. Historias de usuario que deben quedar como stubs (una por persona del equipo)

| ID | Descripción | Tipo |
|---|---|---|
| **Core1** | Listar puestos activos (código y nombre) | Web service |
| **Core2** | Listar oferentes aptos para un puesto (recibe `id_puesto`) | Web service |
| **Core3** | Crear un nuevo empleado a partir de un oferente | Web service |
| **Core4** | Autenticar usuario (usuario + contraseña) | Web service |
| **Core5** | Pantalla de login del sistema Core (consume Core4) | Pantalla |
| **Core6** | Pantalla de listado de puestos para seleccionar (consume Core1) | Pantalla |
| **Core7** | Pantalla de listado de oferentes aptos (consume Core2) | Pantalla |
| **Core8** | Detalle completo de un oferente (recibe `id_oferente`) | Web service |
| **Core9** | Pantalla de detalle de oferente + botón "Crear empleado" (consume Core3 y Core8) | Pantalla |

Para las historias marcadas como "Pantalla", dejar claro en el stub si van a vivir en
el mismo proyecto (ej. como Razor Pages o vistas MVC) o en un proyecto de frontend
aparte — de no estar decidido, dejar un placeholder simple (una carpeta `Views/` o
`Pages/` vacía con un README indicando qué falta decidir) sin bloquear el resto.

## 8. Convenciones de respuesta

- Respuestas exitosas: JSON con DTOs (no exponer las entidades de base de datos
  directamente).
- Errores: estructura consistente, por ejemplo:
  ```json
  { "success": false, "message": "Descripción del error" }
  ```
- Códigos HTTP apropiados (200, 201, 400, 401, 404, 500).
- Todos los endpoints deben quedar documentados en Swagger, incluso los stubs (para
  que se vea el contrato esperado aunque no tengan lógica todavía).

## 9. Qué necesito que hagas (Claude Code)

1. Crear la estructura de carpetas/proyectos con `dotnet new`.
2. Configurar la conexión a MySQL vía Dapper (connection string en
   `appsettings.Development.json`, sin credenciales reales — las agrego yo aparte).
3. Configurar Swagger.
4. Crear el helper/servicio de bitácora (`IBitacoraService` o similar), sin lógica de
   negocio específica de ninguna historia, solo el mecanismo genérico de inserción.
5. Crear un controlador por historia (ver tabla de la sección 7) con el/los método(s)
   correspondientes como **stub**: firma correcta, ruta, parámetros, atributos de
   Swagger, y un `// TODO: implementar según criterios de aceptación de Core#` — sin
   ninguna lógica interna.
6. Verificar que la solución compila y el servidor levanta sin errores con Swagger
   mostrando todos los endpoints (aunque respondan "not implemented" o similar).
7. Explicarme brevemente cómo correr el proyecto localmente desde VS Code y cómo cada
   integrante del equipo debería clonar/trabajar su rama sin chocar con los demás
   (sugerencia de convención de branches por historia, ej. `feature/core1-puestos`).

## 10. Fuera de alcance por ahora

- Implementar la lógica de cualquier historia de usuario (ni siquiera una de ejemplo).
- WordPress y su integración (parte de autoservicio del oferente, HU Aut1-Aut3).
- Autenticación robusta tipo OAuth/Identity — dejar algo simple.
- Tests automatizados.
