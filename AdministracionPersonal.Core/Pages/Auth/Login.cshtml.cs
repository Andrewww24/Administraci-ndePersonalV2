using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdministracionPersonal.Core.Models;

namespace AdministracionPersonal.Core.Pages.Auth;

/// <summary>
/// Core5 — Pantalla de login del sistema Core.
/// Responsable: Kendall (Core4, Core5)
/// Consume: POST /api/auth/login (Core4)
/// </summary>
public class LoginModel : PageModel
{
    // Claves de sesión usadas por el resto de pantallas internas (Core6, Core7, Core9).
    public const string SesionToken = "CoreJwt";
    public const string SesionNombre = "CoreNombre";
    public const string SesionIdUsuario = "CoreIdUsuario";

    private const string MsgCredenciales = "Usuario y/o contraseña incorrectos.";

    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IHttpClientFactory httpFactory, IConfiguration config, ILogger<LoginModel> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    [BindProperty]
    public string Usuario { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public IActionResult OnGet(string? motivo = null)
    {
        // Permite que otras pantallas redirijan aquí con un mensaje.
        // Ej: /Auth/Login?motivo=expirada
        ErrorMessage = motivo switch
        {
            "expirada" => "Su sesión ha expirado. Por favor inicie sesión nuevamente.",
            "requerida" => "Por favor inicie sesión para utilizar el sistema.",
            _ => null
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Validación en pantalla, para no golpear el servicio con campos vacíos.
        // El criterio de aceptación exige exactamente el mismo mensaje.
        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = MsgCredenciales;
            return Page();
        }

        try
        {
            var client = _httpFactory.CreateClient();

            // La URL base se toma de configuración; si no está definida, se usa la del request actual.
            var baseUrl = _config["CoreApi:BaseUrl"];
            client.BaseAddress = new Uri(
                string.IsNullOrWhiteSpace(baseUrl) ? $"{Request.Scheme}://{Request.Host}" : baseUrl);

            var respuesta = await client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest { Usuario = Usuario, Password = Password });

            var cuerpo = await respuesta.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOpts);

            if (respuesta.IsSuccessStatusCode && cuerpo is { Success: true, Data: not null })
            {
                HttpContext.Session.SetString(SesionToken, cuerpo.Data.Token);
                HttpContext.Session.SetString(SesionNombre, cuerpo.Data.NombreCompleto);
                HttpContext.Session.SetInt32(SesionIdUsuario, cuerpo.Data.IdUsuario);

                // HU Core5: tras el login exitoso se pasa al listado de puestos (Core6).
                return RedirectToPage("/Puestos/Index");
            }

            ErrorMessage = cuerpo?.Message ?? MsgCredenciales;
            Password = string.Empty;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo contactar el servicio de autenticación (Core4).");
            ErrorMessage = "No fue posible contactar el servicio de autenticación. Intente de nuevo.";
            Password = string.Empty;
            return Page();
        }
    }
}
