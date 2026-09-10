using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Reservas_Temporales.Repositorios;
using Reservas_Temporales.Seguridad;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<TokenService>();

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
                context.Response.Redirect($"/Account/Login?returnUrl={returnUrl}");
                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                context.Response.Redirect("/Account/AccesoDenegado");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<ConexionBD>();
builder.Services.AddScoped<RepositorioUsuario>();
builder.Services.AddScoped<RepositorioPropietario>();
builder.Services.AddScoped<RepositorioInquilino>();
builder.Services.AddScoped<RepositorioTipoInmueble>();
builder.Services.AddScoped<RepositorioInmueble>();
builder.Services.AddScoped<RepositorioImagenInmueble>();
builder.Services.AddScoped<RepositorioReserva>();
builder.Services.AddScoped<RepositorioPago>();
builder.Services.AddScoped<RepositorioInformes>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrador", policy => policy.RequireRole("Administrador"));
    // Exige login en TODAS las acciones salvo las marcadas [AllowAnonymous] (como AccountController.Login).
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
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();