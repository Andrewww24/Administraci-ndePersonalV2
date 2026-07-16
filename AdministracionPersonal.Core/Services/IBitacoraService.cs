namespace AdministracionPersonal.Core.Services;

public interface IBitacoraService
{
    /// <summary>
    /// Registra una entrada en la bitácora del sistema.
    /// </summary>
    /// <param name="tipo">INSERT | UPDATE | DELETE | SELECT | ERROR</param>
    /// <param name="entidad">Nombre de la tabla o entidad afectada.</param>
    /// <param name="descripcion">Descripción legible de la acción.</param>
    /// <param name="idUsuario">ID del usuario que realiza la acción (null si no autenticado).</param>
    /// <param name="datosAnteriores">Objeto serializado como JSON con el estado previo (para UPDATE/DELETE).</param>
    /// <param name="datosNuevos">Objeto serializado como JSON con el nuevo estado (para INSERT/UPDATE).</param>
    Task RegistrarAsync(
        string tipo,
        string entidad,
        string descripcion,
        int? idUsuario = null,
        object? datosAnteriores = null,
        object? datosNuevos = null);
}
