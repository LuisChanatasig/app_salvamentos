
using app_salvamentos.Configuration;
using app_salvamentos.Models;
using app_salvamentos.Servicios;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

//// Configuración de la base de datos
builder.Services.AddDbContext<AppAutopiezasContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("conexion")));

// Registrar un IDbConnection para Dapper
builder.Services.AddTransient<IDbConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("conexion")));

// Configurar el FileStorageSettings
builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorageSettings"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<FileStorageSettings>>().Value);


//Servicios
builder.Services.AddScoped<AutenticacionService>();
builder.Services.AddScoped<DocumentRecognitionService>();
builder.Services.AddScoped<SeleccionablesService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<CasosService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AnalisisService>();




//Configuracion tiempo de sesion
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120); // Cambia el valor a lo que prefieras
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

// Aquí mapeas la carpeta física que contiene tus imágenes
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(@"C:\Archivos_imagenes\App_Salvamento"),
    RequestPath = "/imagenes"
});

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
