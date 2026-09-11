using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Reservas_Temporales.Repositorios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["TokenAuthentication:Issuer"],
            ValidAudience = builder.Configuration["TokenAuthentication:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["TokenAuthentication:SecretKey"]!))
        };

        options.Events = new JwtBearerEvents
        // El JWT viaja en una cookie, no en el header Authorization (esto es una app MVC, no una API).
        {
            OnMessageReceived = context =>
            {
                // Check if the token is present in the query string
                context.Token = context.Request.Cookies["access_token"];
                return Task.CompletedTask;
            },
            // En vez del 401/403 crudo de una API, redirigimos a pantallas normales del sitio.
            OnChallenge = context =>
            {
                context.HandleResponse();
                var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect($"/Usuario/Login?returnUrl={returnUrl}");
                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                context.Response.Redirect("/Usuario/AccesoDenegado");
                return Task.CompletedTask;
            },
            // Útiles solo para debug durante el desarrollo (ver la consola donde corre `dotnet run`).
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Error validando el token JWT: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token válido para: " + context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<IRepositorioUsuario, RepositorioUsuario>();
builder.Services.AddScoped<IRepositorioPropietario, RepositorioPropietario>();
builder.Services.AddScoped<IRepositorioInquilino, RepositorioInquilino>();
builder.Services.AddScoped<IRepositorioTipoInmueble, RepositorioTipoInmueble>();
builder.Services.AddScoped<IRepositorioInmueble, RepositorioInmueble>();
builder.Services.AddScoped<IRepositorioImagenInmueble, RepositorioImagenInmueble>();
builder.Services.AddScoped<IRepositorioReserva, RepositorioReserva>();
builder.Services.AddScoped<IRepositorioPago, RepositorioPago>();
builder.Services.AddScoped<RepositorioInformes>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrador", policy => policy.RequireRole("Administrador"));
    // Exige login en TODAS las acciones salvo las marcadas [AllowAnonymous] (como UsuarioController.Login).
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();

// UseStaticFiles sirve cualquier archivo de wwwroot en el momento (incluidas las imágenes
// subidas en runtime, como /uploads/...). MapStaticAssets, más abajo, solo optimiza y sirve
// los archivos que ya existían al compilar (CSS, JS, imágenes del proyecto) — por eso conviven
// los dos: uno para lo estático de siempre, otro para lo que se sube después.
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();