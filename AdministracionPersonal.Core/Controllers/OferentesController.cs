using Microsoft.AspNetCore.Mvc;
using Dapper;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Repositories;
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
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<OferentesController> _logger;

    public OferentesController(IBitacoraService bitacora, IDbConnectionFactory dbFactory, ILogger<OferentesController> logger)
    {
        _bitacora = bitacora;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Core2 — Lista los oferentes aptos para un puesto específico.
    /// </summary>
    /// <param name="codigo">Código del puesto a consultar (columna `codigo` de la tabla `puesto`).</param>
    /// <returns>Lista de oferentes aptos para el puesto, con identificación y nombre.</returns>
    /// <response code="200">Lista obtenida correctamente.</response>
    /// <response code="400">Código de puesto inválido.</response>
    /// <response code="404">No existe el puesto, o no tiene oferentes aptos.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("aptos/{codigo}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OferenteAptoDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ObtenerOferentesAptos(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return BadRequest(ApiResponse<object>.Fail("El código de puesto es requerido."));

        try
        {
            using var connection = _dbFactory.CreateConnection();

            var idPuesto = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT id_puesto FROM puesto WHERE codigo = @Codigo",
                new { Codigo = codigo });

            if (idPuesto is null)
                return NotFound(ApiResponse<object>.Fail($"No existe un puesto con código {codigo}."));

            var oferentes = await connection.QueryAsync<OferenteAptoDto>(
                @"SELECT id_oferente AS IdOferente, identificacion AS Identificacion, nombre_completo AS NombreCompleto
                  FROM vw_oferentes_aptos_puesto
                  WHERE id_puesto = @IdPuesto",
                new { IdPuesto = idPuesto });

            var resultado = oferentes.ToList();
            if (resultado.Count == 0)
                return NotFound(ApiResponse<object>.Fail($"No hay oferentes aptos para el puesto {codigo}."));

            await _bitacora.RegistrarAsync("SELECT", "oferente", $"El usuario consulta oferentes aptos para puesto {codigo}");

            return Ok(ApiResponse<IEnumerable<OferenteAptoDto>>.Ok(resultado));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener oferentes aptos para puesto {Codigo}", codigo);
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
        }
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
