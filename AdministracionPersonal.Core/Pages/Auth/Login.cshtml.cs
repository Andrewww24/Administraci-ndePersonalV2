using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionPersonal.Core.Pages.Auth;

/// <summary>
/// Core5 — Pantalla de login del sistema Core.
/// Responsable: Kendall (Core4, Core5)
/// Consume: POST /api/auth/login (Core4)
/// </summary>
public class LoginModel : PageModel
{
    // TODO Core5 (Kendall): Implementar lógica de login.
    // 1. Capturar usuario y contraseña del formulario.
    // 2. Llamar al endpoint POST /api/auth/login (Core4) con HttpClient.
    // 3. Si la respuesta es exitosa: guardar el JWT en sesión/cookie y redirigir a Puestos/Index.
    // 4. Si falla: mostrar ErrorMessage con el mensaje del API (cuenta bloqueada, credenciales inválidas, etc.).
    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public IActionResult OnPost()
    {
        throw new NotImplementedException("Core5: Login pendiente de implementar.");
    }
}
