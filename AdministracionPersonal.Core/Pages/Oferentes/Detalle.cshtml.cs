using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Services;

namespace AdministracionPersonal.Core.Pages.Oferentes;

/// <summary>
/// Core9 — Pantalla de detalle del oferente con botón "Crear empleado".
/// Responsable: Fran (Core1, Core3, Core6, Core9)
/// Consume: GET /api/oferentes/{identificacion} (Core8 — implementado por Andrew,
///          identifica al oferente por su número de identificación, no por id numérico)
///          POST /api/empleados (Core3)
/// </summary>
public class DetalleModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ICoreApiClientFactory _apiClientFactory;
    private readonly ILogger<DetalleModel> _logger;

    public DetalleModel(ICoreApiClientFactory apiClientFactory, ILogger<DetalleModel> logger)
    {
        _apiClientFactory = apiClientFactory;
        _logger = logger;
    }

    /// <summary>Identificación del oferente a mostrar (llega desde el listado de oferentes aptos, Core7).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Identificacion { get; set; }

    /// <summary>Id del puesto para el que se está considerando al oferente.</summary>
    [BindProperty(SupportsGet = true)]
    public int IdPuesto { get; set; }

    /// <summary>Id numérico del oferente, resuelto tras consultar Core8. Se conserva en un
    /// campo oculto del formulario para poder llamar a Core3 sin volver a consultar Core8.</summary>
    [BindProperty]
    public int IdOferenteEncontrado { get; set; }

    public OferenteDto? Oferente { get; private set; }

    public string? ErrorMessage { get; private set; }

    [TempData]
    public string? MensajeExito { get; set; }

    public async Task OnGetAsync()
    {
        await CargarOferenteAsync();
    }

    public async Task<IActionResult> OnPostCrearEmpleadoAsync()
    {
        if (IdOferenteEncontrado <= 0 || IdPuesto <= 0)
        {
            ErrorMessage = "No se pudo determinar el oferente o el puesto seleccionado.";
            await CargarOferenteAsync();
            return Page();
        }

        try
        {
            var client = _apiClientFactory.CrearCliente();
            var request = new CrearEmpleadoRequest
            {
                IdOferente = IdOferenteEncontrado,
                IdPuesto = IdPuesto,
                FechaIngreso = DateTime.Today
            };

            var respuestaHttp = await client.PostAsJsonAsync("api/empleados", request);
            var respuesta = await respuestaHttp.Content.ReadFromJsonAsync<ApiResponse<EmpleadoDto>>(JsonOptions);

            if (!respuestaHttp.IsSuccessStatusCode || respuesta is null || !respuesta.Success)
            {
                ErrorMessage = respuesta?.Message ?? "No fue posible crear el empleado.";
                await CargarOferenteAsync();
                return Page();
            }

            MensajeExito = "Empleado creado con éxito";
            return RedirectToPage("/Puestos/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el empleado desde el detalle del oferente {Identificacion}.", Identificacion);
            ErrorMessage = "Ocurrió un error al crear el empleado. Intente nuevamente más tarde.";
            await CargarOferenteAsync();
            return Page();
        }
    }

    private async Task CargarOferenteAsync()
    {
        if (string.IsNullOrWhiteSpace(Identificacion))
        {
            ErrorMessage = "No se indicó una identificación de oferente válida.";
            return;
        }

        try
        {
            var client = _apiClientFactory.CrearCliente();
            var respuesta = await client.GetFromJsonAsync<ApiResponse<OferenteDto>>(
                $"api/oferentes/{Uri.EscapeDataString(Identificacion)}", JsonOptions);

            Oferente = respuesta?.Data;

            if (Oferente is null)
            {
                ErrorMessage = "No se encontró la información del oferente solicitado.";
            }
            else
            {
                IdOferenteEncontrado = Oferente.IdOferente;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar el detalle del oferente {Identificacion}.", Identificacion);
            ErrorMessage = "No fue posible obtener el detalle del oferente. Intente nuevamente más tarde.";
        }
    }
}
