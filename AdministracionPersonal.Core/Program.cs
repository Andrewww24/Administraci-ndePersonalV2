using AdministracionPersonal.Core.Repositories;
using AdministracionPersonal.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Servicios ────────────────────────────────────────────────────────────────

builder.Services.AddControllers();

// Razor Pages (Core5, Core6, Core7, Core9 — pantallas internas)
// CAMBIO: rutas adicionales para la pantalla de login (Core5).
// Queda accesible en las tres direcciones, sin redirecciones ni /index.html:
//   http://localhost:5124/index   <- la que abre el navegador al arrancar
//   http://localhost:5124/        <- raíz del sitio
//   http://localhost:5124/Auth/Login  <- ruta por convención, la usan los RedirectToPage
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Auth/Login", "index");
    options.Conventions.AddPageRoute("/Auth/Login", "");
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Administración de Personal — Core API", Version = "v1" });

    // Soporte para JWT en Swagger UI
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Incluir comentarios XML en Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// Conexión MySQL vía Dapper
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

// Servicio de bitácora (transversal a todas las historias)
builder.Services.AddScoped<IBitacoraService, BitacoraService>();

// Core4/Core5 (Kendall): verificación AES-GCM de contraseñas.
builder.Services.AddScoped<IPasswordCryptoService, PasswordCryptoService>();

// HttpClient para que la pantalla de login (Core5) consuma el web service Core4.
builder.Services.AddHttpClient();

// Sesión para guardar el JWT tras el login (Core5).
// CAMBIO: se define un vencimiento por inactividad, consistente con la HU Seg7 del Alcance 1.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key no configurado en appsettings.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// CORS permisivo (para desarrollo académico)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ─── Pipeline ─────────────────────────────────────────────────────────────────

var app = builder.Build();

// CAMBIO: el manejador de excepciones va de PRIMERO. Un middleware solo captura
// los errores de lo que se ejecuta después de él; al final del pipeline no servía
// para los middlewares anteriores.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error is not null)
        {
            var msg = app.Environment.IsDevelopment()
                ? error.Error.Message
                : "Ocurrió un error interno. Contacte al administrador.";
            await context.Response.WriteAsJsonAsync(new { success = false, message = msg });
        }
    });
});

// CAMBIO: archivos estáticos antes del enrutamiento.
app.UseStaticFiles();

app.UseRouting();

app.UseCors();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// CAMBIO: Swagger deja libre la raíz y queda en /swagger, que es la dirección
// que abre launchSettings.json (antes daba 404 por el RoutePrefix vacío).
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Core API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();
app.MapRazorPages();

app.Run();