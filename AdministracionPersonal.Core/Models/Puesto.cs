namespace AdministracionPersonal.Core.Models;

public class Puesto
{
    public int IdPuesto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Salario { get; set; }
    public int IdArea { get; set; }
    public int? IdPuestoJefe { get; set; }
    public bool Disponible { get; set; }
    public string? DescripcionPublica { get; set; }
    public DateTime? FechaPublicacion { get; set; }
}

public class PuestoDto
{
    public int IdPuesto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}
