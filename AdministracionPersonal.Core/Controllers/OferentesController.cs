using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Repositories;
using AdministracionPersonal.Core.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace AdministracionPersonal.Core.Controllers;

/// <summary>
/// Core2 y Core8 — Gestión de oferentes.
/// Incluye el registro público requerido por Aut3 para integrar WordPress
/// con las tablas principales del sistema.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OferentesController : ControllerBase
{
    private static readonly char[] SeparadoresContactos = [',', ';', '\r', '\n'];

    private readonly IBitacoraService _bitacora;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<OferentesController> _logger;

    public OferentesController(
        IBitacoraService bitacora,
        IDbConnectionFactory dbFactory,
        IWebHostEnvironment environment,
        ILogger<OferentesController> logger)
    {
        _bitacora = bitacora;
        _dbFactory = dbFactory;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Aut3 — Registra desde WordPress los datos del oferente, contactos,
    /// postulación y currículum en las tablas principales del sistema.
    /// </summary>
    /// <param name="request">Datos enviados como multipart/form-data.</param>
    /// <returns>Identificadores del oferente y de la postulación creada.</returns>
    /// <response code="201">Datos guardados de manera satisfactoria.</response>
    /// <response code="400">Los datos enviados no son válidos.</response>
    /// <response code="404">El puesto no existe o no está disponible.</response>
    /// <response code="409">El oferente ya está postulado al puesto seleccionado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<PostulacionCreadaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegistrarOferente([FromForm] RegistrarOferenteRequest request)
    {
        var identificacion = request.Identificacion.Trim();
        var nombreCompleto = request.NombreCompleto.Trim();
        var tipoIdentificacion = NormalizarTipoIdentificacion(request.TipoIdentificacion);
        var correos = SepararContactos(request.Correos);
        var telefonos = SepararContactos(request.Telefonos);
        var curriculum = request.Curriculum;

        if (tipoIdentificacion is null)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "El tipo de identificación debe ser CEDULA, DIMEX o PASAPORTE."));
        }

        if (request.FechaNacimiento is null || request.FechaNacimiento.Value.Date > DateTime.Today)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "La fecha de nacimiento es requerida y no puede ser futura."));
        }

        if (telefonos.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "Al menos un teléfono es requerido."));
        }

        var correoInvalido = correos.FirstOrDefault(correo => !CorreoValido(correo));
        if (correoInvalido is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(
                $"El correo '{correoInvalido}' no tiene un formato válido o supera 150 caracteres."));
        }

        var telefonoInvalido = telefonos.FirstOrDefault(telefono => !TelefonoValido(telefono));
        if (telefonoInvalido is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(
                $"El teléfono '{telefonoInvalido}' no tiene un formato válido o supera 20 caracteres."));
        }

        if (curriculum is null || curriculum.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("El currículum es requerido."));
        }

        if (curriculum.Length > int.MaxValue)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "El archivo de currículum supera el tamaño permitido por el sistema."));
        }

        var nombreArchivoOriginal = Path.GetFileName(curriculum.FileName);
        if (string.IsNullOrWhiteSpace(nombreArchivoOriginal) || nombreArchivoOriginal.Length > 255)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "El nombre del archivo de currículum no es válido o supera 255 caracteres."));
        }

        using var connection = _dbFactory.CreateConnection();
        connection.Open();

        var puestoDisponible = await connection.QueryFirstOrDefaultAsync<int?>(
            @"SELECT id_puesto
              FROM puesto
              WHERE id_puesto = @IdPuesto
                AND disponible = 1",
            new { request.IdPuesto });

        if (puestoDisponible is null)
        {
            return NotFound(ApiResponse<object>.Fail(
                "El puesto seleccionado no existe o ya no está disponible."));
        }

        var idOferenteExistente = await connection.QueryFirstOrDefaultAsync<int?>(
            @"SELECT id_oferente
              FROM oferente
              WHERE identificacion = @Identificacion",
            new { Identificacion = identificacion });

        if (idOferenteExistente is not null)
        {
            var yaPostulado = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(1)
                  FROM postulacion
                  WHERE id_oferente = @IdOferente
                    AND id_puesto = @IdPuesto",
                new
                {
                    IdOferente = idOferenteExistente.Value,
                    request.IdPuesto
                });

            if (yaPostulado > 0)
            {
                return Conflict(ApiResponse<object>.Fail(
                    "El oferente ya se encuentra postulado para el puesto seleccionado."));
            }
        }

        string? rutaFisicaGuardada = null;

        using var transaction = connection.BeginTransaction();

        try
        {
            var idOferente = idOferenteExistente;

            if (idOferente is null)
            {
                const string insertarOferente = @"
                    INSERT INTO oferente
                        (identificacion, tipo_identificacion, nombre_completo, fecha_nacimiento)
                    VALUES
                        (@Identificacion, @TipoIdentificacion, @NombreCompleto, @FechaNacimiento);";

                await connection.ExecuteAsync(
                    insertarOferente,
                    new
                    {
                        Identificacion = identificacion,
                        TipoIdentificacion = tipoIdentificacion,
                        NombreCompleto = nombreCompleto,
                        FechaNacimiento = request.FechaNacimiento.Value.Date
                    },
                    transaction);

                idOferente = await connection.ExecuteScalarAsync<int>(
                    "SELECT LAST_INSERT_ID();",
                    transaction: transaction);
            }

            const string insertarCorreo = @"
                INSERT INTO oferente_correo (id_oferente, correo)
                SELECT @IdOferente, @Correo
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM oferente_correo
                    WHERE id_oferente = @IdOferente
                      AND correo = @Correo
                );";

            foreach (var correo in correos)
            {
                await connection.ExecuteAsync(
                    insertarCorreo,
                    new { IdOferente = idOferente.Value, Correo = correo },
                    transaction);
            }

            const string insertarTelefono = @"
                INSERT INTO oferente_telefono (id_oferente, telefono)
                SELECT @IdOferente, @Telefono
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM oferente_telefono
                    WHERE id_oferente = @IdOferente
                      AND telefono = @Telefono
                );";

            foreach (var telefono in telefonos)
            {
                await connection.ExecuteAsync(
                    insertarTelefono,
                    new { IdOferente = idOferente.Value, Telefono = telefono },
                    transaction);
            }

            const string insertarPostulacion = @"
                INSERT INTO postulacion (id_oferente, id_puesto, estado)
                VALUES (@IdOferente, @IdPuesto, 'RECIBIDA');";

            await connection.ExecuteAsync(
                insertarPostulacion,
                new
                {
                    IdOferente = idOferente.Value,
                    request.IdPuesto
                },
                transaction);

            var idPostulacion = await connection.ExecuteScalarAsync<int>(
                "SELECT LAST_INSERT_ID();",
                transaction: transaction);

            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var carpetaCurriculums = Path.Combine(webRoot, "curriculums");
            Directory.CreateDirectory(carpetaCurriculums);

            var extension = Path.GetExtension(nombreArchivoOriginal);
            var nombreArchivoGuardado = $"{Guid.NewGuid():N}{extension}";
            rutaFisicaGuardada = Path.Combine(carpetaCurriculums, nombreArchivoGuardado);
            var rutaRelativa = $"/curriculums/{nombreArchivoGuardado}";

            await using (var stream = new FileStream(
                rutaFisicaGuardada,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await curriculum.CopyToAsync(stream);
            }

            var tipoArchivo = string.IsNullOrWhiteSpace(curriculum.ContentType)
                ? "application/octet-stream"
                : curriculum.ContentType.Trim();

            if (tipoArchivo.Length > 100)
            {
                tipoArchivo = tipoArchivo[..100];
            }

            const string insertarCurriculum = @"
                INSERT INTO curriculum_oferente
                    (id_oferente, id_postulacion, nombre_archivo, ruta_archivo,
                     tipo_archivo, tamano_bytes)
                VALUES
                    (@IdOferente, @IdPostulacion, @NombreArchivo, @RutaArchivo,
                     @TipoArchivo, @TamanoBytes);";

            await connection.ExecuteAsync(
                insertarCurriculum,
                new
                {
                    IdOferente = idOferente.Value,
                    IdPostulacion = idPostulacion,
                    NombreArchivo = nombreArchivoOriginal,
                    RutaArchivo = rutaRelativa,
                    TipoArchivo = tipoArchivo,
                    TamanoBytes = checked((int)curriculum.Length)
                },
                transaction);

            transaction.Commit();

            var respuesta = new PostulacionCreadaDto
            {
                IdOferente = idOferente.Value,
                IdPostulacion = idPostulacion,
                IdPuesto = request.IdPuesto
            };

            await _bitacora.RegistrarAsync(
                tipo: "INSERT",
                entidad: "postulacion",
                descripcion: $"Registro público de postulación para el oferente {identificacion}",
                idUsuario: null,
                datosNuevos: new
                {
                    respuesta.IdOferente,
                    respuesta.IdPostulacion,
                    respuesta.IdPuesto,
                    Identificacion = identificacion
                });

            return CreatedAtAction(
                nameof(ObtenerDetalleOferente),
                new { identificacion },
                ApiResponse<PostulacionCreadaDto>.Ok(
                    respuesta,
                    "Datos guardados de manera satisfactoria"));
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            TryRollback(transaction);
            EliminarArchivoSiExiste(rutaFisicaGuardada);

            _logger.LogWarning(
                ex,
                "Registro duplicado al postular al oferente {Identificacion} al puesto {IdPuesto}.",
                identificacion,
                request.IdPuesto);

            return Conflict(ApiResponse<object>.Fail(
                "El oferente ya se encuentra postulado para el puesto seleccionado."));
        }
        catch (Exception ex)
        {
            TryRollback(transaction);
            EliminarArchivoSiExiste(rutaFisicaGuardada);

            _logger.LogError(
                ex,
                "Error al registrar la postulación del oferente {Identificacion}.",
                identificacion);

            await _bitacora.RegistrarAsync(
                tipo: "ERROR",
                entidad: "postulacion",
                descripcion: $"Error al registrar la postulación pública del oferente {identificacion}: {ex.Message}",
                idUsuario: null);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail(
                    "Ocurrió un error al guardar los datos del oferente."));
        }
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
    /// Core8 — Obtiene el detalle completo de un oferente a partir de su identificación.
    /// </summary>
    /// <param name="identificacion">Identificación del oferente (cédula, DIMEX o pasaporte).</param>
    /// <returns>Detalle completo del oferente.</returns>
    /// <response code="200">Detalle obtenido correctamente.</response>
    /// <response code="400">Identificación inválida.</response>
    /// <response code="404">Oferente no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{identificacion}")]
    [ProducesResponseType(typeof(ApiResponse<OferenteDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ObtenerDetalleOferente(string identificacion)
    {
        if (string.IsNullOrWhiteSpace(identificacion))
            return BadRequest(ApiResponse<object>.Fail("La identificación del oferente es requerida."));

        try
        {
            using var connection = _dbFactory.CreateConnection();

            var idOferente = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT id_oferente FROM oferente WHERE identificacion = @Identificacion",
                new { Identificacion = identificacion });

            if (idOferente is null)
                return NotFound(ApiResponse<object>.Fail($"No existe un oferente con identificación {identificacion}."));

            var detalle = await connection.QueryFirstOrDefaultAsync<OferenteDto>(
                @"SELECT id_oferente AS IdOferente, identificacion AS Identificacion,
                         tipo_identificacion AS TipoIdentificacion, nombre_completo AS NombreCompleto,
                         fecha_nacimiento AS FechaNacimiento, direccion AS Direccion,
                         id_distrito AS IdDistrito, fecha_registro AS FechaRegistro,
                         distrito AS Distrito, canton AS Canton, provincia AS Provincia,
                         correos AS Correos, telefonos AS Telefonos,
                         puestos_postulados AS PuestosPostulados, ruta_curriculum AS RutaCurriculum
                  FROM vw_detalle_oferente
                  WHERE id_oferente = @IdOferente",
                new { IdOferente = idOferente });

            if (detalle is null)
                return NotFound(ApiResponse<object>.Fail($"No existe un oferente con identificación {identificacion}."));

            await _bitacora.RegistrarAsync("SELECT", "oferente", $"El usuario consulta detalle del oferente {identificacion}");

            return Ok(ApiResponse<OferenteDto>.Ok(detalle));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener detalle del oferente con identificación {Identificacion}", identificacion);
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
        }
    }

    private static List<string> SepararContactos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return [];
        }

        return valor
            .Split(
                SeparadoresContactos,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(contacto => !string.IsNullOrWhiteSpace(contacto))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool CorreoValido(string correo)
    {
        if (correo.Length > 150)
        {
            return false;
        }

        return new EmailAddressAttribute().IsValid(correo);
    }

    private static bool TelefonoValido(string telefono)
    {
        if (telefono.Length > 20 ||
            !Regex.IsMatch(telefono, @"^[0-9+()\-\s]+$", RegexOptions.CultureInvariant))
        {
            return false;
        }

        var cantidadDigitos = telefono.Count(char.IsDigit);
        return cantidadDigitos is >= 8 and <= 15;
    }

    private static string? NormalizarTipoIdentificacion(string tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
        {
            return null;
        }

        var texto = tipo.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var sinTildes = new string(
            texto
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray())
            .Normalize(NormalizationForm.FormC);

        return sinTildes switch
        {
            "CEDULA" or "CEDULA DE IDENTIDAD" => "CEDULA",
            "DIMEX" => "DIMEX",
            "PASAPORTE" => "PASAPORTE",
            _ => null
        };
    }

    private static void TryRollback(System.Data.IDbTransaction transaction)
    {
        try
        {
            transaction.Rollback();
        }
        catch
        {
            // La transacción puede haberse cerrado por el error original.
        }
    }

    private static void EliminarArchivoSiExiste(string? rutaFisica)
    {
        if (string.IsNullOrWhiteSpace(rutaFisica))
        {
            return;
        }

        try
        {
            if (System.IO.File.Exists(rutaFisica))
            {
                System.IO.File.Delete(rutaFisica);
            }
        }
        catch
        {
            // No ocultar el error original si no se logra limpiar el archivo.
        }
    }
}