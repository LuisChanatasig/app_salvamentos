
using app_salvamentos.Models;
using app_salvamentos.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

//// Configuración de la base de datos
builder.Services.AddDbContext<AppAutopiezasContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("conexion")));

// Registrar un IDbConnection para Dapper
builder.Services.AddTransient<IDbConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("conexion")));
//Servicios
builder.Services.AddScoped<AutenticacionService>();
builder.Services.AddScoped<DocumentRecognitionService>();


//Configuracion tiempo de sesion
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10); // Cambia el valor a lo que prefieras
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Agregar esta línea ANTES de UseAuthorization
app.UseSession();

// Configuración de endpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Autenticacion}/{action=InicioSesion}/{id?}");
});
app.Run();
