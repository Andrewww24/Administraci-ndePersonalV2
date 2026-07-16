using Microsoft.AspNetCore.Mvc;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Services;

namespace AdministracionPersonal.Core.Controllers;

/// <summary>
/// Core1 — Gestión de puestos.
/// Responsable: Fran (Core1, Core3, Core6, Core9)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PuestosController : ControllerBase
{
    private readonly IBitacoraService _bitacora;
    private readonly ILogger<PuestosController> _logger;

    public PuestosController(IBitacoraService bitacora, ILogger<PuestosController> logger)
    {
        _bitacora = bitacora;
        _logger = logger;
    }

    /// <summary>
    /// Core1 — Lista todos los puestos activos (disponible = 1).
    /// </summary>
    /// <returns>Lista de puestos con código y nombre.</returns>
    /// <response code="200">Lista de puestos obtenida correctamente.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PuestoDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ObtenerPuestosActivos()
    {
        // TODO Core1: Implementar según criterios de aceptación.
        // 1. Consultar la vista vw_puestos_disponibles con Dapper.
        // 2. Mapear resultado a IEnumerable<PuestoDto> (solo IdPuesto, Codigo, Nombre).
        // 3. Registrar en bitácora: tipo=SELECT, entidad="puesto",
        //    descripcion="El usuario consulta lista de puestos activos".
        // 4. Retornar Ok(ApiResponse<IEnumerable<PuestoDto>>.Ok(resultado)).
        // En caso de error: retornar StatusCode(500, ApiResponse<object>.Fail(ex.Message)).
        throw new NotImplementedException("Core1: ObtenerPuestosActivos pendiente de implementar.");
    }
}
