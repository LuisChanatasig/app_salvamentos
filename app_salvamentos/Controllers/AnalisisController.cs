using app_salvamentos.Servicios;
using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace app_salvamentos.Controllers
{
    public class AnalisisController : Controller
    {
        private readonly CasosService _casoService;
         private readonly SeleccionablesService _seleccionablesService;
        private readonly ILogger<CasosController> _logger;

        public AnalisisController(CasosService casoService, ILogger<CasosController> logger,SeleccionablesService seleccionablesService)
        {
            _casoService = casoService;
            _logger = logger;
           _seleccionablesService = seleccionablesService; // Descomentar si lo inyectas
        }

        /// <summary>
        /// Muestra la vista de análisis de un caso específico.
        /// </summary>
        /// <param name="id">El ID del caso a analizar.</param>
        /// <returns>La vista con los detalles del caso o una redirección si el caso no se encuentra.</returns>
        [HttpGet] // Ruta para la acción de análisis de caso
        public async Task<IActionResult> AnálisisCaso(string id) // El ID del caso
        {
            try
            {
                _logger.LogInformation("Solicitando análisis para caso ID: {CasoId}", id);

                // Llama al servicio para obtener los detalles completos del caso
                var casoDetalle = await _casoService.ObtenerCasoPorIdAsync(id);

                // Llama al servicio para obtener los tipos de documento relevantes para 'CASO'
                var tiposDocumentoDto = await _seleccionablesService.ListarTiposDocumentoPorAmbitoAsync("CASO");
                var tiposDocumentoSelectList = tiposDocumentoDto.Select(td => new SelectListItem
                {
                    Value = td.tipo_documento_id.ToString(),
                    Text = td.nombre_tipo
                }).ToList();

                // Crea el ViewModel para pasar a la vista
                var viewModel = new app_salvamentos.Models.AnalisisCasoViewModel
                {
                    CasoDetalle = casoDetalle,
                    TiposDocumentoCaso = tiposDocumentoSelectList
                };

                // Pasa el ViewModel a la vista
                return View("AnálisisCaso", viewModel);
            }
            catch (CasoNoEncontradoException ex)
            {
                _logger.LogWarning(ex, "Intento de acceder a análisis de caso no existente. ID: {CasoId}", id);
                TempData["ErrorMessage"] = ex.Message; // Mensaje de error para el usuario
                return RedirectToAction(nameof(Views_Casos_CasosRegistrados)); // Redirige al listado de casos
            }
            catch (CasoServiceException ex)
            {
                _logger.LogError(ex, "Error del servicio al obtener detalles del caso ID: {CasoId}. Mensaje: {Message}", id, ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error al cargar los detalles del caso. Por favor, inténtelo de nuevo más tarde.";
                return RedirectToAction(nameof(Views_Casos_CasosRegistrados));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al cargar la vista de análisis para caso ID: {CasoId}. Mensaje: {Message}", id, ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar el análisis del caso.";
                return RedirectToAction(nameof(Views_Casos_CasosRegistrados));
            }
        }
    }
}
