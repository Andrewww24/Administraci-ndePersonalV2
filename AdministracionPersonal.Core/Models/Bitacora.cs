namespace AdministracionPersonal.Core.Models;

public class Bitacora
{
    public int IdBitacora { get; set; }
    public DateTime Fecha { get; set; }
    public int? IdUsuario { get; set; }
    /// <summary>INSERT | UPDATE | DELETE | SELECT | ERROR</summary>
    public string Tipo { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string? DatosAnteriores { get; set; }
    public string? DatosNuevos { get; set; }
    public string? Descripcion { get; set; }
}
