using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionPersonal.Core.Pages.Oferentes;

/// <summary>
/// Core9 — Pantalla de detalle del oferente con botón "Crear empleado".
/// Responsable: Fran (Core1, Core3, Core6, Core9)
/// Consume: GET /api/oferentes/{idOferente} (Core8)
///          POST /api/empleados (Core3)
/// </summary>
public class DetalleModel : PageModel
{
    // TODO Core9 (Fran): Implementar lógica de detalle + creación de empleado.
    // 1. Recibir idOferente como query parameter (ej. /Oferentes/Detalle?idOferente=3).
    // 2. En OnGetAsync(), llamar GET /api/oferentes/{idOferente} con HttpClient (Core8).
    // 3. Mostrar los datos del oferente en la vista.
    // 4. En OnPostCrearEmpleadoAsync(), llamar POST /api/empleados con HttpClient (Core3).
    //    Enviar { idOferente, idPuesto, fechaIngreso }.
    // 5. Si la respuesta es exitosa: redirigir con mensaje de confirmación.
    // 6. Si falla: mostrar el error en pantalla.

    [BindProperty(SupportsGet = true)]
    public int IdOferente { get; set; }

    public Task OnGetAsync()
    {
        // TODO Core9 (Fran): Implementar.
        return Task.CompletedTask;
    }

    public IActionResult OnPostCrearEmpleado()
    {
        // TODO Core9 (Fran): Implementar botón "Crear empleado".
        throw new NotImplementedException("Core9: CrearEmpleado pendiente de implementar.");
    }
}
