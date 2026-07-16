using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionPersonal.Core.Pages.Oferentes;

/// <summary>
/// Core7 — Pantalla de listado de oferentes aptos para un puesto.
/// Responsable: Andrew (Core2, Core7, Core8)
/// Consume: GET /api/oferentes/aptos/{idPuesto} (Core2)
/// </summary>
public class IndexModel : PageModel
{
    // TODO Core7 (Andrew): Implementar lógica de listado.
    // 1. Recibir idPuesto como query parameter (ej. /Oferentes?idPuesto=5).
    // 2. En OnGetAsync(), llamar GET /api/oferentes/aptos/{idPuesto} con HttpClient.
    // 3. Deserializar la respuesta en una lista de OferenteDto.
    // 4. Exponer la lista como propiedad pública para que la vista la itere.
    // 5. Al hacer clic en un oferente, redirigir a Oferentes/Detalle?idOferente={id}.

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public int IdPuesto { get; set; }

    public Task OnGetAsync()
    {
        // TODO Core7 (Andrew): Implementar.
        return Task.CompletedTask;
    }
}
