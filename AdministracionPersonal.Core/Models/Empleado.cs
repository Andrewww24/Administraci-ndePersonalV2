namespace AdministracionPersonal.Core.Models;

public class CrearEmpleadoRequest
{
    public int IdOferente { get; set; }
    public int IdPuesto { get; set; }
    public DateTime FechaIngreso { get; set; }
}

public class EmpleadoDto
{
    public int IdEmpleado { get; set; }
    public string NumeroEmpleado { get; set; } = string.Empty;
    public int IdOferente { get; set; }
    public int IdPuesto { get; set; }
    public DateTime FechaIngreso { get; set; }
}
