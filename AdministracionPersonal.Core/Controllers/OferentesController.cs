using Microsoft.AspNetCore.Mvc;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Services;

namespace AdministracionPersonal.Core.Controllers;

/// <summary>
/// Core2 y Core8 — Gestión de oferentes.
/// Responsable: Andrew (Core2, Core7, Core8)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OferentesController : ControllerBase
{
    private readonly IBitacoraService _bitacora;
    private readonly ILogger<OferentesController> _logger;

    public OferentesController(IBitacoraService bitacora, ILogger<OferentesController> logger)
    {
        _bitacora = bitacora;
        _logger = logger;
    }

    /// <summary>
    /// Core2 — Lista los oferentes aptos para un puesto específico.
    /// </summary>
    /// <param name="idPuesto">ID del puesto a consultar.</param>
    /// <returns>Lista de oferentes aptos para el puesto.</returns>
    /// <response code="200">Lista obtenida correctamente.</response>
    /// <response code="400">ID de puesto inválido.</response>
    /// <response code="404">No se encontraron oferentes aptos para el puesto.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("aptos/{idPuesto:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OferenteDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ObtenerOferentesAptos(int idPuesto)
    {
        // TODO Core2: Implementar según criterios de aceptación.
        // 1. Validar que idPuesto > 0; si no, retornar BadRequest.
        // 2. Consultar la vista vw_oferentes_aptos_puesto filtrando por id_puesto.
        // 3. Si no hay resultados, retornar NotFound con mensaje descriptivo.
        // 4. Registrar en bitácora: tipo=SELECT, entidad="oferente",
        //    descripcion=$"El usuario consulta oferentes aptos para puesto {idPuesto}".
        // 5. Retornar Ok(ApiResponse<IEnumerable<OferenteDto>>.Ok(resultado)).
        throw new NotImplementedException("Core2: ObtenerOferentesAptos pendiente de implementar.");
    }

    /// <summary>
    /// Core8 — Obtiene el detalle completo de un oferente por su ID.
    /// </summary>
    /// <param name="idOferente">ID del oferente.</param>
    /// <returns>Detalle completo del oferente.</returns>
    /// <response code="200">Detalle obtenido correctamente.</response>
    /// <response code="404">Oferente no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{idOferente:int}")]
    [ProducesResponseType(typeof(ApiResponse<OferenteDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ObtenerDetalleOferente(int idOferente)
    {
        // TODO Core8: Implementar según criterios de aceptación.
        // 1. Validar que idOferente > 0; si no, retornar BadRequest.
        // 2. Consultar la vista vw_detalle_oferente filtrando por id_oferente.
        // 3. Si no existe, retornar NotFound con mensaje descriptivo.
        // 4. Registrar en bitácora: tipo=SELECT, entidad="oferente",
        //    descripcion=$"El usuario consulta detalle del oferente {idOferente}".
        // 5. Retornar Ok(ApiResponse<OferenteDto>.Ok(resultado)).
        throw new NotImplementedException("Core8: ObtenerDetalleOferente pendiente de implementar.");
    }
}
