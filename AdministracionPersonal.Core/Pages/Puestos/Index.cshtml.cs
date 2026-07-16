using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionPersonal.Core.Pages.Puestos;

/// <summary>
/// Core6 — Pantalla de listado de puestos activos.
/// Responsable: Fran (Core1, Core3, Core6, Core9)
/// Consume: GET /api/puestos (Core1)
/// </summary>
public class IndexModel : PageModel
{
    // TODO Core6 (Fran): Implementar lógica de listado.
    // 1. En OnGetAsync(), llamar GET /api/puestos con HttpClient (incluir JWT en header).
    // 2. Deserializar la respuesta en una lista de PuestoDto.
    // 3. Exponer la lista como propiedad pública para que la vista la itere.
    // 4. Al hacer clic en un puesto, redirigir a Oferentes/Index?idPuesto={id}.

    public Task OnGetAsync()
    {
        // TODO Core6 (Fran): Implementar.
        return Task.CompletedTask;
    }
}
