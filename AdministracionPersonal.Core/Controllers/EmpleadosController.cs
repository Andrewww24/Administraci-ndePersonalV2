using Dapper;
using Microsoft.AspNetCore.Mvc;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Repositories;
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
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IBitacoraService _bitacora;
    private readonly ILogger<EmpleadosController> _logger;

    public EmpleadosController(IDbConnectionFactory dbFactory, IBitacoraService bitacora, ILogger<EmpleadosController> logger)
    {
        _dbFactory = dbFactory;
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
        if (request.IdOferente <= 0 || request.IdPuesto <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Debe indicar un oferente y un puesto válidos."));
        }

        var fechaIngreso = request.FechaIngreso == default ? DateTime.Today : request.FechaIngreso.Date;

        using var connection = _dbFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var oferenteExiste = await connection.ExecuteScalarAsync<int?>(
                "SELECT id_oferente FROM oferente WHERE id_oferente = @IdOferente",
                new { request.IdOferente }, transaction);

            if (oferenteExiste is null)
            {
                return NotFound(ApiResponse<object>.Fail("El oferente indicado no existe."));
            }

            var disponible = await connection.ExecuteScalarAsync<int?>(
                "SELECT disponible FROM puesto WHERE id_puesto = @IdPuesto FOR UPDATE",
                new { request.IdPuesto }, transaction);

            if (disponible is null)
            {
                return NotFound(ApiResponse<object>.Fail("El puesto indicado no existe."));
            }

            if (disponible == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("El puesto indicado no está disponible."));
            }

            var yaEsEmpleado = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM empleado WHERE id_oferente = @IdOferente",
                new { request.IdOferente }, transaction);

            if (yaEsEmpleado > 0)
            {
                return BadRequest(ApiResponse<object>.Fail("El oferente ya se encuentra registrado como empleado."));
            }

            var siguienteId = await connection.ExecuteScalarAsync<int>(
                "SELECT IFNULL(MAX(id_empleado), 0) + 1 FROM empleado FOR UPDATE",
                transaction: transaction);
            var numeroEmpleado = $"EMP-{siguienteId:D6}";

            const string sqlInsertEmpleado = @"
                INSERT INTO empleado (numero_empleado, id_oferente, id_puesto, fecha_ingreso)
                VALUES (@NumeroEmpleado, @IdOferente, @IdPuesto, @FechaIngreso)";

            await connection.ExecuteAsync(sqlInsertEmpleado, new
            {
                NumeroEmpleado = numeroEmpleado,
                request.IdOferente,
                request.IdPuesto,
                FechaIngreso = fechaIngreso
            }, transaction);

            var idEmpleado = await connection.ExecuteScalarAsync<int>(
                "SELECT LAST_INSERT_ID()", transaction: transaction);

            var idAprobador = request.IdAprobador ?? idEmpleado;

            const string sqlAccionPersonal = @"
                INSERT INTO accion_personal (tipo_accion, fecha_accion, descripcion, id_empleado, id_aprobador)
                VALUES ('CONTRATACION', @FechaAccion, @Descripcion, @IdEmpleado, @IdAprobador)";

            await connection.ExecuteAsync(sqlAccionPersonal, new
            {
                FechaAccion = fechaIngreso,
                Descripcion = $"Contratación registrada mediante el sistema Core (número de empleado {numeroEmpleado}).",
                IdEmpleado = idEmpleado,
                IdAprobador = idAprobador
            }, transaction);

            transaction.Commit();

            var empleadoCreado = new EmpleadoDto
            {
                IdEmpleado = idEmpleado,
                NumeroEmpleado = numeroEmpleado,
                IdOferente = request.IdOferente,
                IdPuesto = request.IdPuesto,
                FechaIngreso = fechaIngreso
            };

            await _bitacora.RegistrarAsync(
                tipo: "INSERT",
                entidad: "empleado",
                descripcion: "Creación de nuevo empleado",
                datosNuevos: empleadoCreado);

            return StatusCode(201, ApiResponse<EmpleadoDto>.Ok(empleadoCreado, "Empleado creado con éxito"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el empleado.");
            await _bitacora.RegistrarAsync(
                tipo: "ERROR",
                entidad: "empleado",
                descripcion: $"Error al crear empleado a partir del oferente {request.IdOferente}: {ex.Message}");

            return StatusCode(500, ApiResponse<object>.Fail("Ocurrió un error al crear el empleado."));
        }
    }
}
