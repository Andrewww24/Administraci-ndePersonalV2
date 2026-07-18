using Dapper;
using Microsoft.AspNetCore.Mvc;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Repositories;
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
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IBitacoraService _bitacora;
    private readonly ILogger<PuestosController> _logger;

    public PuestosController(IDbConnectionFactory dbFactory, IBitacoraService bitacora, ILogger<PuestosController> logger)
    {
        _dbFactory = dbFactory;
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
        const string sql = @"
            SELECT id_puesto AS IdPuesto, codigo AS Codigo, nombre AS Nombre
            FROM vw_puestos_disponibles
            ORDER BY nombre";

        try
        {
            using var connection = _dbFactory.CreateConnection();
            var puestos = await connection.QueryAsync<PuestoDto>(sql);

            await _bitacora.RegistrarAsync(
                tipo: "SELECT",
                entidad: "puesto",
                descripcion: "El usuario consulta lista de puestos activos");

            return Ok(ApiResponse<IEnumerable<PuestoDto>>.Ok(puestos, "Puestos activos obtenidos correctamente."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener los puestos activos.");
            await _bitacora.RegistrarAsync(
                tipo: "ERROR",
                entidad: "puesto",
                descripcion: $"Error al consultar puestos activos: {ex.Message}");

            return StatusCode(500, ApiResponse<object>.Fail("Ocurrió un error al obtener los puestos activos."));
        }
    }
}
