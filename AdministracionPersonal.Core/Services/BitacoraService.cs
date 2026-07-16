using System.Text.Json;
using Dapper;
using AdministracionPersonal.Core.Repositories;

namespace AdministracionPersonal.Core.Services;

public class BitacoraService : IBitacoraService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<BitacoraService> _logger;

    public BitacoraService(IDbConnectionFactory dbFactory, ILogger<BitacoraService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task RegistrarAsync(
        string tipo,
        string entidad,
        string descripcion,
        int? idUsuario = null,
        object? datosAnteriores = null,
        object? datosNuevos = null)
    {
        const string sql = @"
            INSERT INTO bitacora (fecha, id_usuario, tipo, entidad, datos_anteriores, datos_nuevos, descripcion)
            VALUES (@Fecha, @IdUsuario, @Tipo, @Entidad, @DatosAnteriores, @DatosNuevos, @Descripcion)";

        try
        {
            using var connection = _dbFactory.CreateConnection();
            await connection.ExecuteAsync(sql, new
            {
                Fecha = DateTime.Now,
                IdUsuario = idUsuario,
                Tipo = tipo,
                Entidad = entidad,
                DatosAnteriores = datosAnteriores is null ? null : JsonSerializer.Serialize(datosAnteriores),
                DatosNuevos = datosNuevos is null ? null : JsonSerializer.Serialize(datosNuevos),
                Descripcion = descripcion
            });
        }
        catch (Exception ex)
        {
            // No propagar error de bitácora para no interrumpir el flujo principal
            _logger.LogError(ex, "Error al registrar en bitácora: {Entidad} - {Descripcion}", entidad, descripcion);
        }
    }
}
