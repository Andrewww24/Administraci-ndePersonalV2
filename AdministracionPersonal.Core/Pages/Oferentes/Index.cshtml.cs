using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Pages.Auth;

namespace AdministracionPersonal.Core.Pages.Oferentes;

/// <summary>
/// Core7 — Pantalla de listado de oferentes aptos para un puesto.
/// Responsable: Andrew (Core2, Core7, Core8)
/// Consume: GET /api/Oferentes/aptos/{codigo} (Core2)
/// </summary>
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IHttpClientFactory httpFactory, IConfiguration config, ILogger<IndexModel> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Codigo { get; set; }

    // Solo se reenvía hacia el enlace de Core9 (detalle de oferente + crear empleado);
    // Core2 se sigue consultando por Codigo, no por este valor.
    [BindProperty(SupportsGet = true)]
    public int IdPuesto { get; set; }

    public List<OferenteAptoDto> Oferentes { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<IActionResult> OnGetAsync()
    {
        var token = HttpContext.Session.GetString(LoginModel.SesionToken);
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login", new { motivo = "requerida" });

        if (string.IsNullOrWhiteSpace(Codigo))
        {
            ErrorMessage = "Indique un código de puesto para consultar los oferentes aptos.";
            return Page();
        }

        try
        {
            var client = _httpFactory.CreateClient();

            var baseUrl = _config["CoreApi:BaseUrl"];
            client.BaseAddress = new Uri(
                string.IsNullOrWhiteSpace(baseUrl) ? $"{Request.Scheme}://{Request.Host}" : baseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var respuesta = await client.GetAsync($"/api/Oferentes/aptos/{Uri.EscapeDataString(Codigo)}");
            var cuerpo = await respuesta.Content.ReadFromJsonAsync<ApiResponse<List<OferenteAptoDto>>>(JsonOpts);

            if (respuesta.IsSuccessStatusCode && cuerpo is { Success: true, Data: not null })
            {
                Oferentes = cuerpo.Data;
            }
            else
            {
                ErrorMessage = cuerpo?.Message ?? "No fue posible obtener los oferentes aptos para este puesto.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo contactar el servicio de oferentes aptos (Core2).");
            ErrorMessage = "No fue posible contactar el servicio de oferentes. Intente de nuevo.";
        }

        return Page();
    }
}
