using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Pages.Auth;

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

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<IndexModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public List<PuestoDto> Puestos { get; private set; } = new();

    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Mensaje de éxito dejado por Core9 (ej. "Empleado creado con éxito") antes de
    /// redirigir a esta pantalla tras crear un empleado. Usa la misma clave TempData
    /// ("MensajeExito") que DetalleModel para que el valor viaje entre ambas páginas.
    /// </summary>
    [TempData]
    public string? MensajeExito { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // HU Seg1: si no hay sesión iniciada, redirigir al login con el mensaje correspondiente.
        var token = HttpContext.Session.GetString(LoginModel.SesionToken);
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage("/Auth/Login", new { motivo = "requerida" });
        }

        try
        {
            var client = CrearCliente(token);
            var respuesta = await client.GetFromJsonAsync<ApiResponse<List<PuestoDto>>>("api/puestos", JsonOptions);
            Puestos = respuesta?.Data ?? new List<PuestoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar los puestos activos desde el Core API.");
            ErrorMessage = "No fue posible obtener el listado de puestos activos. Intente nuevamente más tarde.";
        }

        return Page();
    }

    private HttpClient CrearCliente(string token)
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = _configuration["CoreApi:BaseUrl"];
        client.BaseAddress = new Uri(
            string.IsNullOrWhiteSpace(baseUrl) ? $"{Request.Scheme}://{Request.Host}" : baseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
