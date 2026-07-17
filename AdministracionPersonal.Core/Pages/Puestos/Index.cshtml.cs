using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Services;

namespace AdministracionPersonal.Core.Pages.Puestos;

/// <summary>
/// Core6 — Pantalla de listado de puestos activos.
/// Responsable: Fran (Core1, Core3, Core6, Core9)
/// Consume: GET /api/puestos (Core1)
/// </summary>
public class IndexModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ICoreApiClientFactory _apiClientFactory;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ICoreApiClientFactory apiClientFactory, ILogger<IndexModel> logger)
    {
        _apiClientFactory = apiClientFactory;
        _logger = logger;
    }

    public List<PuestoDto> Puestos { get; private set; } = new();

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            var client = _apiClientFactory.CrearCliente();
            var respuesta = await client.GetFromJsonAsync<ApiResponse<List<PuestoDto>>>(
                "api/puestos", JsonOptions);

            Puestos = respuesta?.Data ?? new List<PuestoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar los puestos activos desde el Core API.");
            ErrorMessage = "No fue posible obtener el listado de puestos activos. Intente nuevamente más tarde.";
        }
    }
}
