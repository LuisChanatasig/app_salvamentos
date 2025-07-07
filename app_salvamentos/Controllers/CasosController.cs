using app_salvamentos.Models;
using app_salvamentos.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace app_salvamentos.Controllers
{
    public class CasosController : Controller
    {
        private readonly CasosService _casoService;
        private readonly ILogger<CasosController> _logger;

        public CasosController(CasosService casoService, ILogger<CasosController> logger)
        {
            _casoService = casoService;
            _logger = logger;
        }
        public IActionResult CasosRegistrados()
        {
            return View();
        }  
        public IActionResult CrearCasos()
        {
            return View();
        }
        public IActionResult CasosRegistradosModal()
        {
            return PartialView("CasosRegistradosModal"); // ✅ no debe ser View()
        }

        /// <summary>
        /// Crea un nuevo caso con asegurado, vehículo y documentos relacionados.
        /// </summary>
        /// <param name="dto">Objeto DTO con todos los datos necesarios para la creación del caso.</param>
        /// <returns>Una redirección a otra acción con mensajes en TempData.</returns>
        [HttpPost("crear")]
        // [ValidateAntiForgeryToken] // Considera añadir esto para protección CSRF en formularios HTML
        public async Task<IActionResult> CrearCaso([FromBody] CrearCasoDto dto) // Manteniendo [FromBody] si el cliente envía JSON
        {
            // 1. Validación del modelo (automática por [FromBody] y atributos de validación en DTO)
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validación del modelo fallida al crear caso. Errores: {Errors}", ModelState);
                TempData["ErrorMessage"] = "Datos de entrada inválidos. Por favor, revise los campos.";
                // Si quieres mostrar los errores de validación en la misma vista, puedes devolver View(dto)
                // y manejar ModelState en la vista. Para una redirección, TempData es más simple.
                return RedirectToAction(nameof(CrearCaso)); // Redirige de vuelta al formulario GET
            }

            try
            {
                // 2. Llamada al servicio para crear el caso
                var nuevoCasoId = await _casoService.CrearCasoCompletoAsync(dto);

                // 3. Respuesta exitosa (Redirección con TempData)
                _logger.LogInformation("Caso creado exitosamente con ID: {NuevoCasoId}", nuevoCasoId);
                TempData["SuccessMessage"] = $"¡Caso {dto.NumeroAvaluo} creado exitosamente con ID: {nuevoCasoId}!";
                // Redirige a la acción que lista los casos (asumiendo que existe o se creará)
                return RedirectToAction(nameof(CasosRegistrados));
            }
            // 4. Manejo de excepciones específicas del servicio, mapeando a mensajes en TempData
            catch (UsuarioAuditoriaInvalidoException ex)
            {
                _logger.LogWarning(ex, "Intento de creación de caso con usuario de auditoría inválido. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(CrearCaso));
            }
            catch (IdentificacionAseguradoDuplicadaException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear caso: Identificación de asegurado duplicada. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(CrearCaso));
            }
            catch (PlacaVehiculoDuplicadaException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear caso: Placa de vehículo duplicada. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(CrearCaso));
            }
            catch (NumeroChasisVehiculoDuplicadoException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear caso: Número de chasis de vehículo duplicado. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(CrearCaso));
            }
            catch (NumeroMotorVehiculoDuplicadoException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear caso: Número de motor de vehículo duplicado. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(CrearCaso));
            }
            catch (NumeroAvaluoCasoDuplicadoException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear caso: Número de avalúo de caso duplicado. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(CrearCaso));
            }
            catch (EstadoCasoInvalidoException ex)
            {
                _logger.LogWarning(ex, "Datos inválidos al crear caso: Estado de caso inválido. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(CrearCaso));
            }
            catch (TipoDocumentoAseguradoInvalidoException ex)
            {
                _logger.LogWarning(ex, "Datos inválidos al crear caso: Tipo de documento de asegurado inválido. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(CrearCaso));
            }
            catch (TipoDocumentoCasoInvalidoException ex)
            {
                _logger.LogWarning(ex, "Datos inválidos al crear caso: Tipo de documento de caso inválido. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(CrearCaso));
            }
            catch (ErrorInternoSPCasoException ex)
            {
                _logger.LogError(ex, "Error interno del SP al crear el caso. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error interno del servidor al crear el caso. Por favor, contacte a soporte.";
                return RedirectToAction(nameof(CrearCaso));
            }
            catch (CasoCreationException ex) // Captura cualquier otra excepción específica de CasoCreationException
            {
                _logger.LogError(ex, "Error específico no mapeado del servicio al crear caso. Código: {ErrorCode}, Mensaje: {Message}", ex.ErrorCode, ex.Message);
                TempData["ErrorMessage"] = $"Ocurrió un error al crear el caso: {ex.Message}";
                return RedirectToAction(nameof(CrearCaso));
            }
            catch (Exception ex) // Captura cualquier otra excepción inesperada
            {
                _logger.LogError(ex, "Error inesperado al crear el caso. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al procesar la solicitud para crear el caso.";
                return RedirectToAction(nameof(CrearCaso));
            }
        }

    }
}
