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

    public string? Distrito { get; set; }
    public string? Canton { get; set; }
    public string? Provincia { get; set; }
    public string? Correos { get; set; }
    public string? Telefonos { get; set; }
    public string? RutaCurriculum { get; set; }
    public string? PuestosPostulados { get; set; }
}

/// <summary>
/// Proyección liviana de oferente para Core2 (listado de aptos por puesto): solo
/// identificación y nombre, más el id para poder pedir el detalle completo (Core8) después.
/// </summary>
public class OferenteAptoDto
{
    public int IdOferente { get; set; }
    public string Identificacion { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
}
