using app_salvamentos.Models;
using app_salvamentos.Servicios; // Necesario para TempData
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace app_salvamentos.Controllers
{
  public class UsuarioController : Controller // Se ha cambiado de ControllerBase a Controller para acceder a TempData
    {
        private readonly UsuarioService _usuarioService;
        private readonly ILogger<UsuarioController> _logger;
        private readonly SeleccionablesService _seleccionablesService; // Asegúrate de que este servicio esté inyectado correctamente
        // Constructor con nombre corregido
        public UsuarioController(UsuarioService usuarioService, ILogger<UsuarioController> logger,SeleccionablesService seleccionablesService)
        {
            _usuarioService = usuarioService;
            _logger = logger;
            _seleccionablesService = seleccionablesService; // Asignación del servicio de selección
        }

        /// <summary>
        /// Muestra el listado de usuarios.
        /// </summary>
        /// <param name="sortColumn">Columna por la que ordenar (ej. 'usuario_login', 'created_at').</param>
        /// <param name="sortDirection">Dirección del ordenamiento ('ASC' o 'DESC').</param>
        /// <returns>La vista con la lista de usuarios.</returns>
        [HttpGet("listado")] // Ejemplo de ruta para el listado
        public async Task<IActionResult> ListarUsuarios(
            string sortColumn = "created_at",
            string sortDirection = "DESC")
        {
            try
            {
                var usuarios = await _usuarioService.ListarUsuariosAsync(
                    sortColumn,
                    sortDirection
                );

                // No hay paginación ni filtros en el servicio, así que se pasa la lista directamente
                return View(usuarios);
            }
            catch (ListarUsuariosException ex)
            {
                _logger.LogError(ex, "Error al obtener el listado de usuarios.");
                TempData["ErrorMessage"] = "Ocurrió un error al cargar el listado de usuarios."+ex; // Mensaje amigable
                // Devuelve una vista con una lista vacía en caso de error
                return View(new List<UsuarioListadoDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener el listado de usuarios."+ex);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar el listado de usuarios." +ex;
                return View(new List<UsuarioListadoDto>());
            }
        }


        /// <summary>
        /// Muestra el formulario para crear un nuevo usuario, incluyendo la lista de perfiles.
        /// </summary>
        /// <returns>La vista con el modelo necesario para la creación de usuario.</returns>
        [HttpGet] // Ruta para la acción GET que muestra el formulario
        public async Task<IActionResult> CrearUsuario()
        {
            try
            {
                _logger.LogInformation("Cargando vista de creación de usuario y perfiles.");

                // Obtener la lista de perfiles activos del servicio SeleccionablesService
                var perfilesDto = await _seleccionablesService.ListarPerfilesAsync();

                // Mapear PerfilDto a SelectListItem para usar en un dropdown en la vista
                var perfilesSelectList = perfilesDto.Select(p => new SelectListItem
                {
                    Value = p.perfil_id.ToString(), // El ID del perfil como valor
                    Text = p.perfil_nombre           // El nombre del perfil como texto a mostrar
                }).ToList();

                // Crear el modelo de vista y asignar la lista de perfiles
                var modelo = new CrearUsuarioViewModel
                {
                    Perfiles = perfilesSelectList
                };

                return View(modelo); // Devuelve la vista con el modelo
            }
            catch (ListarPerfilesException ex) // Captura la excepción específica de ListarPerfilesAsync
            {
                _logger.LogError(ex, "Error al cargar la lista de perfiles para la vista de creación de usuario.");
                TempData["ErrorMessage"] = "No se pudieron cargar los perfiles. Por favor, inténtelo de nuevo más tarde.";
                // Si no se pueden cargar los perfiles, aún puedes devolver la vista, quizás con una lista vacía
                return View(new CrearUsuarioViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al cargar la vista de creación de usuario.");
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar la página de creación de usuario.";
                return View(new CrearUsuarioViewModel());
            }
        }

        /// <summary>
        /// Crea un nuevo usuario en el sistema.
        /// </summary>
        /// <param name="nuevoUsuario">Datos del nuevo usuario.</param>
        /// <returns>El ID del usuario creado.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)] // Documenta el tipo de respuesta exitosa
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Documenta errores de validación
        [ProducesResponseType(StatusCodes.Status409Conflict)] // Documenta errores de conflicto
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Documenta errores internos
        public async Task<IActionResult> CrearUsuario([FromForm] CrearUsuarioViewModel viewModel) // <-- Recibe el ViewModel del formulario
        {
            // La validación del modelo con [ApiController] es a menudo automática,
            // pero mantener el if (!ModelState.IsValid) explícito es claro.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validación fallida para la creación de usuario.");

                var errorMessages = new List<string>();
                foreach (var modelStateEntry in ModelState.Values)
                {
                    foreach (var error in modelStateEntry.Errors)
                    {
                        errorMessages.Add(error.ErrorMessage);
                    }
                }
                TempData["ValidationErrors"] = errorMessages;
                TempData["ErrorMessage"] = "Por favor, corrija los errores de validación."; // General message

                // Recargar los perfiles para que el dropdown no esté vacío al redirigir
                await CargarPerfilesEnViewModel(viewModel);
                return View(viewModel); // Or RedirectToAction if you're going to a different action
            }
            try
            {
                // 1. Generar el salt y hashear la contraseña
                Guid passwordSalt = Guid.NewGuid(); // Puedes seguir generando este salt si tu DB lo requiere, aunque BCrypt usa su propio
                string passwordHash = HashPassword(viewModel.Password, passwordSalt); // Llama a la función HashPassword actualizada

                // 2. Crear el DTO para el servicio
                var nuevoUsuarioDto = new NuevoUsuarioDto
                {
                    UsuarioLogin = viewModel.UsuarioLogin,
                    UsuarioEmail = viewModel.UsuarioEmail,
                    UsuarioPasswordHash = passwordHash, // Este será el hash generado por BCrypt
                    PerfilId = viewModel.PerfilId,
                    PasswordSalt = passwordSalt // Almacena el salt si tu DB lo requiere, aunque BCrypt no lo usa para el hashing
                };

                _logger.LogInformation("Intentando crear un nuevo usuario con login: {Login}", nuevoUsuarioDto.UsuarioLogin);
                var nuevoId = await _usuarioService.CrearNuevoUsuarioAsync(nuevoUsuarioDto);

                _logger.LogInformation("Usuario creado exitosamente con ID: {Id}", nuevoId);
                TempData["SuccessMessage"] = "¡Usuario creado exitosamente!";
                return RedirectToAction(nameof(ListarUsuarios)); // Redirige de nuevo al formulario o a una página de éxito
            }
            catch (DuplicadoUsuarioLoginException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear usuario: El login ya existe. Login: {Login}", viewModel.UsuarioLogin);
                TempData["ErrorMessage"] = ex.Message;
                await CargarPerfilesEnViewModel(viewModel); // Recargar perfiles para la vista
                return View(viewModel); // Retorna la misma vista con el error
            }
            catch (DuplicadoUsuarioEmailException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear usuario: El email ya existe. Email: {Email}", viewModel.UsuarioEmail);
                TempData["ErrorMessage"] = ex.Message;
                await CargarPerfilesEnViewModel(viewModel); // Recargar perfiles para la vista
                return View(viewModel); // Retorna la misma vista con el error
            }
            catch (ErrorCreacionUsuarioException ex)
            {
                _logger.LogError(ex, "Error crítico al crear usuario con login: {Login}. Mensaje: {ErrorMessage}", viewModel.UsuarioLogin, ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al procesar su solicitud. Por favor, inténtelo de nuevo más tarde.";
                await CargarPerfilesEnViewModel(viewModel); // Recargar perfiles para la vista
                return View(viewModel); // Retorna la misma vista con el error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción inesperada en CrearUsuario para login: {Login}", viewModel.UsuarioLogin);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado. Por favor, inténtelo de nuevo más tarde.";
                await CargarPerfilesEnViewModel(viewModel); // Recargar perfiles para la vista
                return View(viewModel); // Retorna la misma vista con el error
            }
        }

        // Helper para cargar perfiles en el ViewModel (útil para errores de validación)
        private async Task CargarPerfilesEnViewModel(CrearUsuarioViewModel viewModel)
        {
            try
            {
                var perfilesDto = await _seleccionablesService.ListarPerfilesAsync();
                viewModel.Perfiles = perfilesDto.Select(p => new SelectListItem
                {
                    Value = p.perfil_id.ToString(),
                    Text = p.perfil_nombre
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recargar la lista de perfiles para el ViewModel.");
                // No relanzar, solo loguear. El error principal ya se está manejando.
            }
        }


        // Placeholder para la función de hashing de contraseñas.
        
        private string HashPassword(string password, Guid salt) // El 'salt' de Guid ya no sería necesario para BCrypt
        {
            // BCrypt genera y gestiona su propio salt internamente dentro del hash.
            // Por lo tanto, el 'salt' que pasas como Guid ya no se usa directamente aquí para el hashing de BCrypt.
            // Si tu columna 'password_salt' en la DB es obligatoria, podrías almacenarla,
            // pero para la función de hashing de BCrypt, solo necesitas la contraseña.
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        // ==============================================================================================
        // Acciones para VER DETALLES de Usuario
        // ==============================================================================================

        /// <summary>
        /// Muestra los detalles de un usuario específico.
        /// </summary>
        /// <param name="id">El ID del usuario.</param>
        /// <returns>La vista con los detalles del usuario o NotFound si no existe.</returns>
        [HttpGet("detalle/{id}")]
        public async Task<IActionResult> DetalleUsuario(int id)
        {
            try
            {
                _logger.LogInformation("Cargando detalles para usuario ID: {Id}", id);
                var usuario = await _usuarioService.ObtenerUsuarioPorIdAsync(id);
                return View(usuario);
            }
            catch (UsuarioNoEncontradoException ex)
            {
                _logger.LogWarning(ex, "Intento de acceder a detalles de usuario no existente. ID: {Id}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(ListarUsuarios)); // Redirige al listado con mensaje de error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener detalles de usuario ID: {Id}", id);
                TempData["ErrorMessage"] = "Ocurrió un error al cargar los detalles del usuario.";
                return RedirectToAction(nameof(ListarUsuarios));
            }
        }

        // ==============================================================================================
        // Acciones para EDITAR Usuario
        // ==============================================================================================

        /// <summary>
        /// Muestra el formulario para editar un usuario existente.
        /// </summary>
        /// <param name="id">El ID del usuario a editar.</param>
        /// <returns>La vista con el formulario de edición pre-llenado.</returns>
        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            try
            {
                _logger.LogInformation("Cargando formulario de edición para usuario ID: {Id}", id);
                var usuarioDto = await _usuarioService.ObtenerUsuarioPorIdAsync(id);
                var perfilesDto = await _seleccionablesService.ListarPerfilesAsync();

                var perfilesSelectList = perfilesDto.Select(p => new SelectListItem
                {
                    Value = p.perfil_id.ToString(),
                    Text = p.perfil_nombre
                }).ToList();

                var viewModel = new EditarUsuarioViewModel // Asegúrate de usar el namespace correcto
                {
                    UsuarioId = usuarioDto.usuario_id,
                    UsuarioLogin = usuarioDto.usuario_login,
                    UsuarioEmail = usuarioDto.usuario_email,
                    PerfilId = usuarioDto.perfil_id,
                    UsuarioEstado = usuarioDto.usuario_estado,
                    Perfiles = perfilesSelectList
                };

                return View(viewModel);
            }
            catch (UsuarioNoEncontradoException ex)
            {
                _logger.LogWarning(ex, "Intento de editar usuario no existente. ID: {Id}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(ListarUsuarios));
            }
            catch (ListarPerfilesException ex)
            {
                _logger.LogError(ex, "Error al cargar la lista de perfiles para la edición de usuario ID: {Id}", id);
                TempData["ErrorMessage"] = "No se pudieron cargar los perfiles para la edición. " + ex.Message;
                return RedirectToAction(nameof(ListarUsuarios));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al cargar el formulario de edición para usuario ID: {Id}", id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar el formulario de edición.";
                return RedirectToAction(nameof(ListarUsuarios));
            }
        }

        /// <summary>
        /// Procesa la solicitud POST para actualizar un usuario existente.
        /// </summary>
        /// <param name="viewModel">Datos del usuario actualizados desde el formulario.</param>
        /// <returns>Redirección o mensajes de error/éxito.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken] // Siempre recomendado para formularios POST
        public async Task<IActionResult> EditarUsuario([FromForm] EditarUsuarioViewModel viewModel) // Asegúrate de usar el namespace correcto
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validación fallida al editar usuario ID: {Id}", viewModel.UsuarioId);

                foreach (var estado in ModelState)
                {
                    foreach (var error in estado.Value.Errors)
                    {
                        _logger.LogWarning("Campo inválido: '{Campo}' - Error: {Error}", estado.Key, error.ErrorMessage);
                    }
                }

                TempData["ErrorMessage"] = "Por favor, corrija los errores de validación.";

                // Recargar perfiles para que el dropdown no esté vacío al redirigir
                await CargarPerfilesEnEditarViewModel(viewModel);

                return View(viewModel);
            }

            try
            {
                var usuarioEditarDto = new Models.UsuarioEditarDto // Asegúrate de usar el namespace correcto
                {
                    usuario_id = viewModel.UsuarioId,
                    usuario_login = viewModel.UsuarioLogin,
                    usuario_email = viewModel.UsuarioEmail,
                    perfil_id = viewModel.PerfilId,
                    usuario_estado = viewModel.UsuarioEstado
                };

                _logger.LogInformation("Intentando editar usuario ID: {Id}", usuarioEditarDto.usuario_id);
                await _usuarioService.EditarUsuarioAsync(usuarioEditarDto);

                _logger.LogInformation("Usuario ID {Id} editado exitosamente.", usuarioEditarDto.usuario_id);
                TempData["SuccessMessage"] = "¡Usuario actualizado exitosamente!";
                return RedirectToAction(nameof(ListarUsuarios)); // Redirige al listado
            }
            catch (UsuarioNoEncontradoException ex)
            {
                _logger.LogWarning(ex, "Error al editar: Usuario no encontrado. ID: {Id}", viewModel.UsuarioId);
                TempData["ErrorMessage"] = ex.Message;
                await CargarPerfilesEnEditarViewModel(viewModel);
                return View(viewModel);
            }
            catch (DuplicadoUsuarioLoginException ex)
            {
                _logger.LogWarning(ex, "Error al editar: Login duplicado. Login: {Login}", viewModel.UsuarioLogin);
                TempData["ErrorMessage"] = ex.Message;
                await CargarPerfilesEnEditarViewModel(viewModel);
                return View(viewModel);
            }
            catch (DuplicadoUsuarioEmailException ex)
            {
                _logger.LogWarning(ex, "Error al editar: Email duplicado. Email: {Email}", viewModel.UsuarioEmail);
                TempData["ErrorMessage"] = ex.Message;
                await CargarPerfilesEnEditarViewModel(viewModel);
                return View(viewModel);
            }
            catch (PerfilNoEncontradoException ex)
            {
                _logger.LogWarning(ex, "Error al editar: Perfil no encontrado. Perfil ID: {PerfilId}", viewModel.PerfilId);
                TempData["ErrorMessage"] = ex.Message;
                await CargarPerfilesEnEditarViewModel(viewModel);
                return View(viewModel);
            }
            catch (ErrorActualizarUsuarioException ex)
            {
                _logger.LogError(ex, "Error específico del SP al editar usuario ID: {Id}. Mensaje: {Message}", viewModel.UsuarioId, ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error al actualizar el usuario. Por favor, inténtelo de nuevo más tarde.";
                await CargarPerfilesEnEditarViewModel(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar usuario ID: {Id}. Mensaje: {Message}", viewModel.UsuarioId, ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al actualizar el usuario.";
                await CargarPerfilesEnEditarViewModel(viewModel);
                return View(viewModel);
            }
        }

        // Helper para cargar perfiles en el EditarUsuarioViewModel
        private async Task CargarPerfilesEnEditarViewModel(EditarUsuarioViewModel viewModel)
        {
            try
            {
                var perfilesDto = await _seleccionablesService.ListarPerfilesAsync();
                viewModel.Perfiles = perfilesDto.Select(p => new SelectListItem
                {
                    Value = p.perfil_id.ToString(),
                    Text = p.perfil_nombre
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recargar la lista de perfiles para el EditarUsuarioViewModel.");
            }
        }

        // ==============================================================================================
        // Acciones para ELIMINAR Usuario (Soft Delete)
        // ==============================================================================================

        /// <summary>
        /// Procesa la solicitud POST para eliminar (soft delete) un usuario.
        /// </summary>
        /// <param name="id">El ID del usuario a eliminar.</param>
        /// <returns>Un resultado JSON indicando éxito o fracaso.</returns>
        [HttpPost("eliminar")]
        // [ValidateAntiForgeryToken] // Si el AJAX lo envía, descomentar
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            try
            {
                _logger.LogInformation("Intentando eliminar (soft delete) usuario ID: {Id}", id);
                await _usuarioService.EliminarUsuarioAsync(id);
                _logger.LogInformation("Usuario ID {Id} eliminado (soft delete) exitosamente.", id);
                return Json(new { success = true, message = "Usuario eliminado exitosamente (estado cambiado a inactivo)." });
            }
            catch (UsuarioNoEncontradoException ex)
            {
                _logger.LogWarning(ex, "Error al eliminar: Usuario no encontrado. ID: {Id}", id);
                return Json(new { success = false, message = ex.Message });
            }
            catch (ErrorEliminarUsuarioException ex)
            {
                _logger.LogError(ex, "Error específico del SP al eliminar usuario ID: {Id}. Mensaje: {Message}", id, ex.Message);
                return Json(new { success = false, message = "Ocurrió un error al eliminar el usuario. Por favor, inténtelo de nuevo más tarde." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar usuario ID: {Id}. Mensaje: {Message}", id, ex.Message);
                return Json(new { success = false, message = "Ocurrió un error inesperado al eliminar el usuario." });
            }
        }


    }
}
