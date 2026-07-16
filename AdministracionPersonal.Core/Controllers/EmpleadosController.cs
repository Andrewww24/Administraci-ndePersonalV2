using Microsoft.AspNetCore.Mvc;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Services;

namespace AdministracionPersonal.Core.Controllers;

/// <summary>
/// Core3 — Creación de empleados.
/// Responsable: Fran (Core1, Core3, Core6, Core9)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmpleadosController : ControllerBase
{
    private readonly IBitacoraService _bitacora;
    private readonly ILogger<EmpleadosController> _logger;

    public EmpleadosController(IBitacoraService bitacora, ILogger<EmpleadosController> logger)
    {
        _bitacora = bitacora;
        _logger = logger;
    }

    /// <summary>
    /// Core3 — Crea un nuevo empleado a partir de un oferente existente.
    /// </summary>
    /// <param name="request">Datos para crear el empleado (idOferente, idPuesto, fechaIngreso).</param>
    /// <returns>El empleado creado.</returns>
    /// <response code="201">Empleado creado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">Oferente o puesto no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EmpleadoDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> CrearEmpleado([FromBody] CrearEmpleadoRequest request)
    {
        // TODO Core3: Implementar según criterios de aceptación.
        // 1. Validar los campos del request (IdOferente > 0, IdPuesto > 0, FechaIngreso válida).
        // 2. Verificar que el oferente exista en tabla oferente; si no, 404.
        // 3. Verificar que el puesto exista y esté disponible; si no, 400.
        // 4. Generar un numero_empleado único (consultar lógica de negocio con el profesor).
        // 5. Insertar en tabla empleado con Dapper.
        // 6. Insertar en accion_personal con tipo_accion = 'CONTRATACION'.
        // 7. Registrar en bitácora: tipo=INSERT, entidad="empleado",
        //    datosNuevos=request, descripcion="Creación de nuevo empleado".
        // 8. Retornar Created(resultado) con ApiResponse<EmpleadoDto>.Ok(empleado, "Empleado creado").
        throw new NotImplementedException("Core3: CrearEmpleado pendiente de implementar.");
    }
}
