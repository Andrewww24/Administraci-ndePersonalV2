using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionPersonal.Core.Pages.Auth;

/// <summary>
/// Apoyo (Kendall) — Cierre de sesión del sistema Core.
/// Limpia la sesión (JWT, nombre, id de usuario) y regresa al login (Core5)
/// sin mostrar el mensaje de "sesión requerida", ya que fue un cierre explícito.
/// </summary>
public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        HttpContext.Session.Clear();
        return RedirectToPage("/Auth/Login");
    }
}
