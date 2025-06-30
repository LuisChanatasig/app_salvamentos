using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using app_salvamentos.Servicios;

namespace app_salvamentos.Controllers
{
    public class AutenticacionController : Controller

    {

        private readonly AutenticacionService _authService;

        public AutenticacionController(AutenticacionService authService)
        {
            _authService = authService;
        }

        [HttpGet]            // Ya no lleva plantilla
        public IActionResult InicioSesion()
        {
            return View();
        }


        /// <summary>
        /// Método para iniciar sesión del usuario.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Usuario y contraseña son requeridos." });

            var result = await _authService.ValidarCredencialesAsync(request.Email, request.Password);

            switch (result.Codigo)
            {
                case 0:
                    // Guardar en sesión
                    HttpContext.Session.SetInt32("UsuarioId", result.UsuarioId);
                    HttpContext.Session.SetInt32("RolId", result.PerfilId);
                    HttpContext.Session.SetString("UsuarioLogin", result.UsuarioLogin); //nombres del usuario
                    HttpContext.Session.SetString("PerfilNombre", result.PerfilNombre);
                   

                    return Ok(new
                    {
                        usuarioId = result.UsuarioId,
                        perfilId = result.PerfilId,
                        usuarioLogin = result.UsuarioLogin,
                        perfilNombre = result.PerfilNombre
                    });

                case 1:
                    return Unauthorized(new { message = "Usuario no existe o está inactivo." });

                case 2:
                    return Unauthorized(new { message = "Cuenta bloqueada. Intente más tarde." });

                case 3:
                    return Unauthorized(new { message = "Contraseña incorrecta." });

                case 4:
                    return Unauthorized(new { message = "Contraseña expirada. Cambie su contraseña." });

                default:
                    return StatusCode(500, new { message = "Error de autenticación desconocido." });
            }
        }
    }
}
