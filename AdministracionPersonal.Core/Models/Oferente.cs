using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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
/// Datos recibidos desde el formulario público de WordPress para registrar
/// al oferente, su postulación y el currículum adjunto.
/// </summary>
public class RegistrarOferenteRequest
{
    [Required(ErrorMessage = "La identificación es requerida.")]
    [StringLength(30, ErrorMessage = "La identificación no puede superar 30 caracteres.")]
    public string Identificacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de identificación es requerido.")]
    public string TipoIdentificacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [StringLength(150, ErrorMessage = "El nombre completo no puede superar 150 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es requerida.")]
    [DataType(DataType.Date)]
    public DateTime? FechaNacimiento { get; set; }

    /// <summary>
    /// Uno o varios correos separados por coma, punto y coma o salto de línea.
    /// </summary>
    public string? Correos { get; set; }

    /// <summary>
    /// Uno o varios teléfonos separados por coma, punto y coma o salto de línea.
    /// Al menos uno es requerido por la HU Aut3.
    /// </summary>
    [Required(ErrorMessage = "Al menos un teléfono es requerido.")]
    public string Telefonos { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un puesto válido.")]
    public int IdPuesto { get; set; }

    [Required(ErrorMessage = "El currículum es requerido.")]
    public IFormFile? Curriculum { get; set; }
}

/// <summary>
/// Resultado mínimo del registro público de una postulación.
/// </summary>
public class PostulacionCreadaDto
{
    public int IdOferente { get; set; }
    public int IdPostulacion { get; set; }
    public int IdPuesto { get; set; }
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