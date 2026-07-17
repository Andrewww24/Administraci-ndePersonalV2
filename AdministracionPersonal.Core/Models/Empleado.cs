namespace AdministracionPersonal.Core.Models;

public class CrearEmpleadoRequest
{
    public int IdOferente { get; set; }
    public int IdPuesto { get; set; }
    public DateTime FechaIngreso { get; set; }

    /// <summary>
    /// Id del empleado que aprueba la acción de personal de contratación.
    /// La tabla accion_personal exige un aprobador (FK a empleado). Si no se indica,
    /// se registra al propio empleado recién creado como aprobador de su contratación,
    /// ya que aún no existe un mecanismo de sesión de usuario (Core4/Core5 fuera de alcance).
    /// </summary>
    public int? IdAprobador { get; set; }
}

public class EmpleadoDto
{
    public int IdEmpleado { get; set; }
    public string NumeroEmpleado { get; set; } = string.Empty;
    public int IdOferente { get; set; }
    public int IdPuesto { get; set; }
    public DateTime FechaIngreso { get; set; }
}
