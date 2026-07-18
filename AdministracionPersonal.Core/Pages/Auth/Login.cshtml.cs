using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdministracionPersonal.Core.Controllers;
using AdministracionPersonal.Core.Models;

namespace AdministracionPersonal.Core.Pages.Auth;

/// <summary>
/// Core5 — Pantalla de login del sistema Core.
/// Responsable: Kendall (Core4, Core5)
/// Consume: POST /api/auth/login (Core4)
/// </summary>
public class LoginModel : PageModel
{
    private readonly IHttpClientFactory _httpFactory;

    public LoginModel(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    [BindProperty]
    public string Usuario { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // 1. Capturar usuario y contraseña del formulario (BindProperty).
        try
        {
            // 2. Llamar al endpoint POST /api/auth/login (Core4) con HttpClient.
            var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");

            var respuesta = await client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest { Usuario = Usuario, Password = Password });

            var cuerpo = await respuesta.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOpts);

            if (respuesta.IsSuccessStatusCode && cuerpo is { Success: true, Data: not null })
            {
                // 3. Éxito: guardar el JWT en sesión y redirigir a Puestos/Index.
                HttpContext.Session.SetString("CoreJwt", cuerpo.Data.Token);
                HttpContext.Session.SetString("CoreNombre", cuerpo.Data.NombreCompleto);
                return RedirectToPage("/Puestos/Index");
            }

            // 4. Falla: mostrar el mensaje del API.
            ErrorMessage = cuerpo?.Message ?? "Usuario y/o contraseña incorrectos.";
            return Page();
        }
        catch
        {
            ErrorMessage = "No fue posible contactar el servicio de autenticación.";
            return Page();
        }
    }
}
