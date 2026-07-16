namespace AdministracionPersonal.Core.Models;

public class OferenteDto
{
    public int IdOferente { get; set; }
    public string Identificacion { get; set; } = string.Empty;
    public string TipoIdentificacion { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string? Direccion { get; set; }
    public int? IdDistrito { get; set; }
    public DateTime FechaRegistro { get; set; }
}
