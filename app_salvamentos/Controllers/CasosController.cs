using app_salvamentos.Models;
using app_salvamentos.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http; // Necesario para HttpContext.Session y IFormFile
using Microsoft.AspNetCore.Hosting; // Necesario para IWebHostEnvironment
using System; // Necesario para Guid
using System.IO; // Necesario para Path y FileStream
using System.Linq; // Necesario para FirstOrDefault
using System.Collections.Generic; // Necesario para List

// Asegúrate de que las excepciones personalizadas estén en un namespace accesible o definidas.
// Por ejemplo, si están en Servicios, asegúrate de tener el using adecuado.
// using app_salvamentos.Servicios.Excepciones; // Si tienes un namespace específico para excepciones

namespace app_salvamentos.Controllers
{
    // Si tus DTOs CrearCasoInputDto y DocumentoDto están en app_salvamentos.Models,
    // y tus DTOs de servicio (CrearCasoDto, DocumentoCreacionDto) están en app_salvamentos.Servicios,
    // es importante que los uses con sus namespaces completos o alias si hay ambigüedad.
    // Para simplificar, he asumido que los DTOs de entrada del formulario están en Models
    // y los DTOs para el servicio están en Servicios.

    public class CasosController : Controller
    {
        private readonly CasosService _casoService;
        private readonly ILogger<CasosController> _logger;
        private readonly SeleccionablesService _seleccionablesService;
        private readonly IWebHostEnvironment _env; // DECLARACIÓN: Propiedad para IWebHostEnvironment

        // CONSTRUCTOR: Inyectar IWebHostEnvironment
        public CasosController(CasosService casoService, ILogger<CasosController> logger, SeleccionablesService seleccionablesService, IWebHostEnvironment env)
        {
            _casoService = casoService;
            _logger = logger;
            _seleccionablesService = seleccionablesService;
            _env = env; // ASIGNACIÓN: Inicializar _env
        }

        /// <summary>
        /// Muestra la vista principal con el listado de casos registrados.
        /// </summary>
        /// <param name="sortColumn">Columna por la que ordenar (ej. 'numero_avaluo', 'created_at').</param>
        /// <param name="sortDirection">Dirección del ordenamiento ('ASC' o 'DESC').</param>
        /// <returns>Una vista completa con la lista de objetos CasoListadoDto.</returns>
        [HttpGet("registrados")] // Puedes ajustar la ruta según tus necesidades
        public async Task<IActionResult> CasosRegistrados(
            string sortColumn = "created_at",
            string sortDirection = "DESC")
        {
            try
            {
                _logger.LogInformation("Solicitando casos para CasosRegistrados (vista principal). Columna de ordenamiento: {SortColumn}, Dirección: {SortDirection}", sortColumn, sortDirection);

                // Llama al servicio para obtener la lista de casos
                var casos = await _casoService.ListarCasosAsync(sortColumn, sortDirection);

                // Pasa la lista de casos a la vista principal
                return View("CasosRegistrados", casos);
            }
            catch (Servicios.ListarCasosException ex) // Usar el namespace completo para la excepción
            {
                _logger.LogError(ex, "Error al cargar los casos para CasosRegistrados. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = "No se pudieron cargar los casos. Por favor, inténtelo de nuevo más tarde.";
                return View("CasosRegistrados", new List<Models.CasoListadoDto>()); // Devuelve una lista vacía en caso de error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en CasosRegistrados. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar los casos.";
                return View("CasosRegistrados", new List<Models.CasoListadoDto>());
            }
        }

        /// <summary>
        /// Muestra el formulario para crear un nuevo caso.
        /// </summary>
        /// <returns>La vista del formulario de creación de caso.</returns>
        [HttpGet]
        public async Task<IActionResult> CrearCasos()
        {
            var viewModel = new Models.CrearCasoViewModel();
            try
            {
                //Cargar estados de caso no se si necesite luego
                var estadosCasoDto = await _seleccionablesService.ListarEstadosCasoAsync(); // Asumo un método ListarEstadosCasoAsync
                viewModel.EstadosCaso = estadosCasoDto.Select(e => new SelectListItem
                {
                    Value = e.estado_id.ToString(),
                    Text = e.nombre_estado
                }).ToList();

                // Cargar tipos de documento para asegurado
                var tiposDocumentoAseguradoDto = await _seleccionablesService.ListarTiposDocumentoAsync(soloActivos: true, ambito: "ASEGURADO");
                viewModel.TiposDocumentoAsegurado = tiposDocumentoAseguradoDto.Select(t => new SelectListItem
                {
                    Value = t.tipo_documento_id.ToString(),
                    Text = t.nombre_tipo
                }).ToList();

                // Cargar tipos de documento para caso
                var tiposDocumentoCasoDto = await _seleccionablesService.ListarTiposDocumentoAsync(soloActivos: true, ambito: "CASO");
                viewModel.TiposDocumentoCaso = tiposDocumentoCasoDto.Select(t => new SelectListItem
                {
                    Value = t.tipo_documento_id.ToString(),
                    Text = t.nombre_tipo
                }).ToList();

                // Aquí podrías obtener el UsuarioId del usuario logueado si tienes autenticación
                // viewModel.UsuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 1; // Ejemplo
                viewModel.UsuarioId = 1; // Valor por defecto para demostración

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar datos para el formulario de creación de caso.");
                TempData["ErrorMessage"] = "No se pudieron cargar los datos necesarios para el formulario. Por favor, inténtelo de nuevo.";
                return View(viewModel); // Devuelve el ViewModel aunque esté incompleto
            }
        }

        /// <summary>
        /// Muestra el listado de casos en una vista parcial, con ordenamiento dinámico.
        /// </summary>
        /// <param name="sortColumn">Columna por la que ordenar (ej. 'numero_avaluo', 'created_at').</param>
        /// <param name="sortDirection">Dirección del ordenamiento ('ASC' o 'DESC').</param>
        /// <returns>Una vista parcial con la lista de objetos CasoListadoDto.</returns>
        [HttpGet] // Puedes definir una ruta específica si lo necesitas, ej. [HttpGet("casos-modal")]
        public async Task<IActionResult> CasosRegistradosModal(
            string sortColumn = "created_at",
            string sortDirection = "DESC")
        {
            try
            {
                _logger.LogInformation("Solicitando casos para CasosRegistradosModal. Columna de ordenamiento: {SortColumn}, Dirección: {SortDirection}", sortColumn, sortDirection);

                // Llama al servicio para obtener la lista de casos
                var casos = await _casoService.ListarCasosAsync(sortColumn, sortDirection);

                // Pasa la lista de casos a la vista parcial
                return PartialView("CasosRegistradosModal", casos);
            }
            catch (Servicios.ListarCasosException ex)
            {
                _logger.LogError(ex, "Error al cargar los casos para CasosRegistradosModal. Mensaje: {Message}", ex.Message);
                // Puedes pasar un modelo vacío o un mensaje de error a la vista parcial
                TempData["ErrorMessage"] = "No se pudieron cargar los casos. Por favor, inténtelo de nuevo más tarde.";
                return PartialView("CasosRegistradosModal", new List<Models.CasoListadoDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en CasosRegistradosModal. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar los casos.";
                return PartialView("CasosRegistradosModal", new List<Models.CasoListadoDto>());
            }
        }

        /// <summary>
        /// Crea un nuevo caso con asegurado, vehículo y documentos relacionados.
        /// </summary>
        /// <param name="inputDto">Objeto DTO con todos los datos necesarios para la creación del caso, incluyendo archivos.</param>
        /// <returns>Una redirección a otra acción con mensajes en TempData.</returns>
        [HttpPost]
        // [ValidateAntiForgeryToken] // Considera añadir esto para protección CSRF en formularios HTML
        public async Task<IActionResult> CrearCaso([FromForm] Models.CrearCasoInputDto inputDto) // CAMBIO: Recibe CrearCasoInputDto desde el formulario
        {
            // 1. Validación del modelo
            if (!ModelState.IsValid)
            {
                var errores = new List<string>();

                foreach (var kvp in ModelState)
                {
                    var key = kvp.Key;
                    var erroresCampo = kvp.Value.Errors;

                    foreach (var error in erroresCampo)
                    {
                        errores.Add($"Campo: {key}, Error: {error.ErrorMessage}");
                    }
                }

                _logger.LogWarning("Validación del modelo fallida al crear caso. Errores:\n{Errores}", string.Join("\n", errores));

                TempData["ErrorMessage"] = "Datos de entrada inválidos. Por favor, revise los campos.";

                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel); // Helper para recargar dropdowns
                return View(viewModel);
            }


            try
            {
                // 2. Procesar y guardar archivos físicos
                var casoDtoForService = new Models.CrearCasoDto // DTO que el servicio espera
                {
                    // Mapear propiedades escalares
                    NombreCompleto = inputDto.NombreCompleto,
                    Identificacion = inputDto.Identificacion,
                    Telefono = inputDto.Telefono,
                    Email = inputDto.Email,
                    Direccion = inputDto.Direccion,
                    Placa = inputDto.Placa,
                    Marca = inputDto.Marca,
                    Modelo = inputDto.Modelo,
                    Transmision = inputDto.Transmision,
                    Combustible = inputDto.Combustible,
                    Cilindraje = inputDto.Cilindraje,
                    Anio = inputDto.Anio,
                    NumeroChasis = inputDto.NumeroChasis,
                    NumeroMotor = inputDto.NumeroMotor,
                    TipoVehiculo = inputDto.TipoVehiculo,
                    Clase = inputDto.Clase,
                    Color = inputDto.Color,
                    ObservacionesVehiculo = inputDto.ObservacionesVehiculo,
                    NumeroAvaluo = inputDto.NumeroAvaluo,
                    NumeroReclamo = inputDto.NumeroReclamo,
                    FechaSiniestro = inputDto.FechaSiniestro,
                    CasoEstadoId = inputDto.CasoEstadoId
                };

                // Guardar documentos del asegurado
                foreach (var docInput in inputDto.DocumentosAsegurado)
                {
                    if (docInput.File != null && docInput.File.Length > 0)
                    {
                        // Usar inputDto.NumeroAvaluo como caseIdentifier
                        var filePath = await SaveFileAsync(docInput.File, inputDto.NumeroAvaluo, "asegurados");
                        casoDtoForService.DocumentosAsegurado.Add(new Models.DocumentoDto
                        {
                            TipoDocumentoId = docInput.TipoDocumentoId,
                            NombreArchivo = docInput.File.FileName,
                            RutaFisica = filePath,
                            Observaciones = docInput.Observaciones
                        });
                    }
                }

                // Guardar documentos del caso
                foreach (var docInput in inputDto.DocumentosCaso)
                {
                    if (docInput.File != null && docInput.File.Length > 0)
                    {
                        // Usar inputDto.NumeroAvaluo como caseIdentifier
                        var filePath = await SaveFileAsync(docInput.File, inputDto.NumeroAvaluo, "casos");
                        casoDtoForService.DocumentosCaso.Add(new Models.DocumentoDto
                        {
                            TipoDocumentoId = docInput.TipoDocumentoId,
                            NombreArchivo = docInput.File.FileName,
                            RutaFisica = filePath,
                            Observaciones = docInput.Observaciones
                        });
                    }
                }




                // 3. Llamada al servicio para crear el caso
                var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0; // Obtener UsuarioId de la sesión
                var nuevoCasoId = await _casoService.CrearCasoCompletoAsync(casoDtoForService, usuarioId);

                // 4. Respuesta exitosa (Redirección con TempData)
                _logger.LogInformation("Caso creado exitosamente con ID: {NuevoCasoId}", nuevoCasoId);
                TempData["SuccessMessage"] = $"¡Caso {inputDto.NumeroAvaluo} creado exitosamente con ID: {nuevoCasoId}!";
                return RedirectToAction(nameof(CasosRegistrados)); // Redirige a la acción que lista los casos
            }
            // 5. Manejo de excepciones específicas del servicio, mapeando a mensajes en TempData
            catch (Servicios.UsuarioAuditoriaInvalidoException ex) // Usar el namespace completo
            {
                _logger.LogWarning(ex, "Intento de creación de caso con usuario de auditoría inválido. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
            catch (Servicios.IdentificacionAseguradoDuplicadaException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear caso: Identificación de asegurado duplicada. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
            catch (Servicios.PlacaVehiculoDuplicadaException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear caso: Placa de vehículo duplicada. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
            catch (Servicios.NumeroChasisVehiculoDuplicadoException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear caso: Número de chasis de vehículo duplicado. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
            catch (Servicios.NumeroMotorVehiculoDuplicadoException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear caso: Número de motor de vehículo duplicado. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
            catch (Servicios.NumeroAvaluoCasoDuplicadoException ex)
            {
                _logger.LogWarning(ex, "Conflicto al crear caso: Número de avalúo de caso duplicado. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
            catch (Servicios.EstadoCasoInvalidoException ex)
            {
                _logger.LogWarning(ex, "Datos inválidos al crear caso: Estado de caso inválido. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
            catch (Servicios.TipoDocumentoAseguradoInvalidoException ex)
            {
                _logger.LogWarning(ex, "Datos inválidos al crear caso: Tipo de documento de asegurado inválido. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
            catch (Servicios.TipoDocumentoCasoInvalidoException ex)
            {
                _logger.LogWarning(ex, "Datos inválidos al crear caso: Tipo de documento de caso inválido. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
            catch (Servicios.ErrorInternoSPCasoException ex)
            {
                _logger.LogError(ex, "Error interno del SP al crear el caso. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error interno del servidor al crear el caso. Por favor, contacte a soporte.";
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
            catch (Servicios.CasoCreationException ex)
            {
                _logger.LogError(ex, "Error específico no mapeado del servicio al crear caso. Código: {ErrorCode}, Mensaje: {Message}", ex.ErrorCode, ex.Message);
                TempData["ErrorMessage"] = $"Ocurrió un error al crear el caso: {ex.Message}";
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear el caso. Mensaje: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al procesar la solicitud para crear el caso." + ex.Message;
                var viewModel = new Models.CrearCasoViewModel();
                await CargarDatosParaCrearCasoViewModel(viewModel);
                return View(viewModel);
            }
        }

        /// <summary>
        /// Helper para guardar un archivo IFormFile en una ruta física, creando una carpeta única para cada caso.
        /// </summary>
        /// <param name="file">El archivo IFormFile a guardar.</param>
        /// <param name="caseIdentifier">Un identificador único para el caso (ej. NumeroAvaluo o CasoId).</param>
        /// <param name="documentTypeSubfolder">Subcarpeta adicional para organizar por tipo de documento (ej. "asegurado", "caso").</param>
        /// <returns>La ruta relativa del archivo guardado dentro de wwwroot/uploads/caseIdentifier/documentTypeSubfolder.</returns>
        /// <exception cref="ArgumentException">Lanzada si el archivo es nulo o vacío.</exception>
        public async Task<string> SaveFileAsync(IFormFile file, string caseIdentifier, string documentTypeSubfolder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("El archivo es nulo o está vacío.", nameof(file));
            }
            if (string.IsNullOrEmpty(caseIdentifier))
            {
                throw new ArgumentException("El identificador del caso no puede ser nulo o vacío.", nameof(caseIdentifier));
            }
            if (string.IsNullOrEmpty(documentTypeSubfolder))
            {
                throw new ArgumentException("La subcarpeta del tipo de documento no puede ser nula o vacía.", nameof(documentTypeSubfolder));
            }

            // Crear la ruta completa del directorio de destino: wwwroot/uploads/CASE_IDENTIFIER/DOCUMENT_TYPE_SUBFOLDER/
            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
            var caseFolder = Path.Combine(uploadsRoot, caseIdentifier);
            var finalUploadsFolder = Path.Combine(caseFolder, documentTypeSubfolder);

            if (!Directory.Exists(finalUploadsFolder))
            {
                Directory.CreateDirectory(finalUploadsFolder);
            }

            // Generar un nombre de archivo único para evitar colisiones
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(finalUploadsFolder, uniqueFileName);

            // Guardar el archivo físicamente
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Devolver la ruta relativa del archivo guardado
            // Por ejemplo: /uploads/AV-2024-001/asegurado/guid_nombrearchivo.pdf
            return Path.Combine("uploads", caseIdentifier, documentTypeSubfolder, uniqueFileName).Replace("\\", "/");
        }
        /// <summary>
        /// Helper para recargar los datos necesarios para el ViewModel de CrearCaso.
        /// </summary>
        /// <param name="viewModel">El ViewModel a poblar.</param>
        private async Task CargarDatosParaCrearCasoViewModel(Models.CrearCasoViewModel viewModel)
        {
            try
            {
                viewModel.EstadosCaso = (await _seleccionablesService.ListarEstadosCasoAsync())
                                        .Select(e => new SelectListItem { Value = e.estado_id.ToString(), Text = e.nombre_estado })
                                        .ToList();

                viewModel.TiposDocumentoAsegurado = (await _seleccionablesService.ListarTiposDocumentoAsync(soloActivos: true, ambito: "ASEGURADO"))
                                                    .Select(t => new SelectListItem { Value = t.tipo_documento_id.ToString(), Text = t.nombre_tipo })
                                                    .ToList();

                viewModel.TiposDocumentoCaso = (await _seleccionablesService.ListarTiposDocumentoAsync(soloActivos: true, ambito: "CASO"))
                                                .Select(t => new SelectListItem { Value = t.tipo_documento_id.ToString(), Text = t.nombre_tipo })
                                                .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recargar datos para CrearCasoViewModel.");
                // No relanzar, solo loguear. El error principal ya se está manejando.
            }
        }
    }
}
