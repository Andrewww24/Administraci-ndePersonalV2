using Microsoft.Extensions.Http;

namespace AdministracionPersonal.Core.Services;

public class CoreApiClientFactory : ICoreApiClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CoreApiClientFactory(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public HttpClient CrearCliente()
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No hay un HttpContext activo para determinar la URL base del Core API.");

        var client = _httpClientFactory.CreateClient("CoreApi");
        client.BaseAddress = new Uri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}");
        return client;
    }
}
