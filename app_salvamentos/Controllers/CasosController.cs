using app_salvamentos.Models;
using app_salvamentos.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using app_salvamentos.Configuration;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Options;
// Asegúrate de que las excepciones personalizadas estén en un namespace accesible o definidas.


namespace app_salvamentos.Controllers
{
  

    public class CasosController : Controller
    {
        private readonly CasosService _casoService;
        private readonly ILogger<CasosController> _logger;
        private readonly SeleccionablesService _seleccionablesService;
        private readonly IWebHostEnvironment _env; // DECLARACIÓN: Propiedad para IWebHostEnvironment
        private readonly FileStorageSettings _fileStorageSettings;
        private readonly IConfiguration _configuration;



        // CONSTRUCTOR: Inyectar IWebHostEnvironment
        public CasosController(CasosService casoService, ILogger<CasosController> logger, SeleccionablesService seleccionablesService, IWebHostEnvironment env, IOptions<FileStorageSettings> fileStorageOptions, IConfiguration configuration)
        {
            _casoService = casoService;
            _logger = logger;
            _seleccionablesService = seleccionablesService;
            _env = env; // ASIGNACIÓN: Inicializar _env
            _fileStorageSettings = fileStorageOptions.Value; // ✅ ESTO AHORA FUNCIONA
            _configuration = configuration;
        }

        /// <summary>
        /// Muestra la vista principal con el listado de casos registrados.
        /// </summary>
        /// <param name="sortColumn">Columna por la que ordenar (ej. 'numero_avaluo', 'created_at').</param>
        /// <param name="sortDirection">Dirección del ordenamiento ('ASC' o 'DESC').</param>
        /// <returns>Una vista completa con la lista de objetos CasoListadoDto.</returns>
        [HttpGet] // Puedes ajustar la ruta según tus necesidades
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
            catch (ListarCasosException ex) // Usar el namespace completo para la excepción
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
            var viewModel = new CrearCasoViewModel();
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
                viewModel.UsuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 1; // Ejemplo

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar datos para el formulario de creación de caso.");
                TempData["ErrorMessage"] = "No se pudieron cargar los datos necesarios para el formulario. Por favor, inténtelo de nuevo.";
                return View(viewModel); // Devuelve el ViewModel aunque esté incompleto
            }
        }

        [HttpGet]
        public async Task<IActionResult> ModificarCasos(int id)
        {
            try
            {
                _logger.LogInformation("Solicitando análisis para caso ID: {CasoId}", id);

                // Llama al servicio para obtener los detalles completos del caso
                var casoDetalle = await _casoService.ObtenerCasoPorIdAsync(id);

                // Llama al servicio para obtener los tipos de documento relevantes para 'CASO'
                var tiposDocumentoDto = await _seleccionablesService.ListarTiposDocumentoAsync(ambito: "CASO");
                var tiposDocumentoDtoA = await _seleccionablesService.ListarTiposDocumentoAsync(ambito: "ASEGURADO");

                //Cargar estados de caso no se si necesite luego
                var estadosCasoDto = await _seleccionablesService.ListarEstadosCasoAsync(); // Asumo un método ListarEstadosCasoAsync

                var estadosCasoDtoList =  estadosCasoDto.Select(e => new SelectListItem
                {
                    Value = e.estado_id.ToString(),
                    Text = e.nombre_estado
                }).ToList();
                var tiposDocumentoSelectList = tiposDocumentoDto.Select(td => new SelectListItem
                {
                    Value = td.tipo_documento_id.ToString(),
                    Text = td.nombre_tipo
                }).ToList();
                var tiposDocumentoSelectListA = tiposDocumentoDtoA.Select(td => new SelectListItem
                {
                    Value = td.tipo_documento_id.ToString(),
                    Text = td.nombre_tipo
                }).ToList();

                // Crea el ViewModel para pasar a la vista
                var viewModel = new ModificarCasoViewModel
                {
                    CasoDetalle = casoDetalle,
                    EstadosCaso = estadosCasoDtoList,
                    TiposDocumentoCaso = tiposDocumentoSelectList,
                    TiposDocumentoAsegurado = tiposDocumentoSelectListA
                };

                // Pasa el ViewModel a la vista
                return View("ModificarCasos", viewModel); // Asegúrate de que el nombre de la vista sea correcto
            }
            catch (CasoNoEncontradoException ex)
            {
                _logger.LogWarning(ex, "Intento de acceder a análisis de caso no existente. ID: {CasoId}", id);
                TempData["ErrorMessage"] = ex.Message; // Mensaje de error para el usuario
                return RedirectToAction("CasosRegistrados", "Casos"); // Redirige al listado de casos (ajusta la acción/controlador si es diferente)
            }
            catch (CasoServiceException ex)
            {
                _logger.LogError(ex, "Error del servicio al obtener detalles del caso ID: {CasoId}. Mensaje: {Message}", id, ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error al cargar los detalles del caso. Por favor, inténtelo de nuevo más tarde." + ex; // Quité 'ex' directo
                return RedirectToAction("CasosRegistrados", "Casos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al cargar la vista de análisis para caso ID: {CasoId}. Mensaje: {Message}", id, ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar el análisis del caso.";
                return RedirectToAction("CasosRegistrados", "Casos");
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
            string sortDirection = "DESC",
            int? estado_id =  1 )
        {
            try
            {
                _logger.LogInformation("Solicitando casos para CasosRegistradosModal. Columna de ordenamiento: {SortColumn}, Dirección: {SortDirection}", sortColumn, sortDirection);

                // Llama al servicio para obtener la lista de casos
                var casos = await _casoService.ListarCasosEstadoAsync(sortColumn, sortDirection, estado_id);

                // Pasa la lista de casos a la vista parcial
                return PartialView("CasosRegistradosModal", casos);
            }
            catch (ListarCasosException ex)
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCaso([FromForm] CrearCasoInputDto inputDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)
                                        .ToList();

                _logger.LogWarning("Validación del modelo fallida al crear caso. Errores: {Errores}", string.Join("; ", errors));

                return BadRequest(new { message = "Datos de entrada inválidos. Por favor, revise los campos.", details = errors });
            }

            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

            // *** IMPORTANTE ***
            // Asegúrate que inputDto.NumeroReclamo YA VIENE FORMATEADO
            // como lo esperas para la carpeta raíz (ej. "REC-2024-001").
            // Si no, deberás formatearlo aquí (ej. inputDto.NumeroReclamo.Replace(" ", "_").Replace("/", "-"))
            // o en el cliente antes de enviarlo.
            string reclamoIdentifierFolder = "Documentos_Reclamo_" + inputDto.NumeroReclamo.Replace(" ", "_").Replace("/", "-");

            // Lista para guardar las rutas físicas completas de los archivos guardados exitosamente
            // para poder eliminarlos si la transacción de la DB falla.
            var successfullyUploadedFilePaths = new List<string>();

            try
            {
                _logger.LogInformation("Ruta base configurada: {Ruta}", _fileStorageSettings.BaseUploadPath ?? "NO CONFIGURADA");

                // Inicializa las listas de documentos del input DTO si son nulas
                inputDto.DocumentosAsegurado ??= new List<DocumentoFormInput>();
                inputDto.DocumentosCaso ??= new List<DocumentoFormInput>();

                // Prepara el DTO para el servicio, con las listas de DocumentoDto vacías inicialmente
                var casoDtoForService = new CrearCasoDto
                {
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
                    NumeroReclamo = inputDto.NumeroReclamo,
                    FechaSiniestro = inputDto.FechaSiniestro,
                    CasoEstadoId = inputDto.CasoEstadoId,
                    // Inicializa estas listas aquí, se llenarán con el helper
                    DocumentosAsegurado = new List<DocumentoDto>(),
                    DocumentosCaso = new List<DocumentoDto>()
                };

                // ===========================================================================================================
                // Procesamiento de documentos: Guarda archivos físicamente e inserta sus rutas en la lista temporal
                // ===========================================================================================================

                // Guardar documentos del asegurado
                await ProcessAndSaveDocuments(inputDto.DocumentosAsegurado, reclamoIdentifierFolder, "asegurados", null, casoDtoForService.DocumentosAsegurado, successfullyUploadedFilePaths);

                // Guardar documentos del caso
                await ProcessAndSaveDocuments(inputDto.DocumentosCaso, reclamoIdentifierFolder, "casos", null, casoDtoForService.DocumentosCaso, successfullyUploadedFilePaths);

                // ===========================================================================================================
                // Ahora que ya tienes todos los archivos guardados y DTO completo, llama al servicio de DB
                // ===========================================================================================================

                // *** ESTE ES EL PUNTO CRÍTICO ***
                // Si _casoService.CrearCasoCompletoAsync falla, se ejecutará el catch.
                var nuevoCasoId = await _casoService.CrearCasoCompletoAsync(casoDtoForService, usuarioId);

                TempData["SuccessMessage"] = $"¡Caso {nuevoCasoId} creado exitosamente!";
                return RedirectToAction(nameof(CasosRegistrados));
            }
            catch (Exception ex) // Captura cualquier excepción (SqlException o cualquier otra)
            {
                _logger.LogError(ex, "Error al crear caso. Mensaje: {Message}. Intentando revertir archivos.", ex.Message);
                // Revertir archivos si hay cualquier error en la creación del caso en DB
                DeleteUploadedFiles(successfullyUploadedFilePaths);
                // Dependiendo de tu UI, podrías querer devolver un JSON con el error para AJAX
                return StatusCode(500, new { message = "Ocurrió un error inesperado al crear el caso.", detail = ex.Message });
            }
        }


        /// <summary>
        /// Modifica un caso existente, incluyendo sus datos principales y la adición de nuevos documentos.
        /// </summary>
        /// <param name="viewModel">Objeto ViewModel con los datos del caso a modificar, incluyendo posibles nuevos documentos.</param>
        /// <returns>Un IActionResult indicando el resultado de la operación.</returns>
        [HttpPut] // Usa HttpPut para operaciones de modificación
        [ValidateAntiForgeryToken] // Solo si usas un formulario web y anti-forgery tokens
        public async Task<IActionResult> ModificarCaso([FromForm] ModificarCasoViewModel viewModel) // O [FromBody] si no hay archivos
        {
            // 1. Validación del modelo
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                                       .SelectMany(v => v.Errors)
                                       .Select(e => e.ErrorMessage)
                                       .ToList();

                _logger.LogWarning("Validación del modelo fallida al modificar caso. Errores: {Errores}", string.Join("; ", errors));

                return BadRequest(new { message = "Datos de entrada inválidos. Por favor, revise los campos.", details = errors });
            }

            // 2. Obtener el ID del usuario actual (ej. desde la sesión o JWT)
            // Asegúrate de que tu lógica de autenticación y autorización proporciona este ID.
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
            if (usuarioId <= 0)
            {
                _logger.LogWarning("Usuario no autenticado o ID de usuario no encontrado en la sesión.");
                return Unauthorized(new { message = "Usuario no autenticado." });
            }

            // 3. Validar que el CasoId esté presente para la modificación
            // Recuerda que ModificarCasoViewModel.CasoDetalle.CasoId debe ser el ID del caso a modificar.
            if (viewModel.CasoDetalle == null || viewModel.CasoDetalle.caso_id <= 0) // Sin .HasValue ni .Value
            {
                _logger.LogWarning("Intento de modificar un caso sin proporcionar un CasoId válido.");
                return BadRequest(new { message = "El ID del caso a modificar es requerido y debe ser válido." });
            }

            // 4. Preparar la carpeta para los documentos (basado en NumeroReclamo)
            // Asegúrate que viewModel.CasoDetalle.NumeroReclamo YA VIENE FORMATEADO
            // o formatéalo aquí si es necesario.
            string reclamoIdentifierFolder = "Documentos_Reclamo_" + viewModel.CasoDetalle.numero_reclamo.Replace(" ", "_").Replace("/", "-");

            // Lista para guardar las rutas físicas completas de los archivos guardados exitosamente
            // para poder eliminarlos si la transacción de la DB falla.
            var successfullyUploadedFilePaths = new List<string>();

            try
            {
                _logger.LogInformation("Ruta base configurada: {Ruta}", _fileStorageSettings.BaseUploadPath ?? "NO CONFIGURADA");

                // 5. Mapear el ViewModel a CrearCasoDto (que ahora incluye CasoId)
                // Aquí usamos CrearCasoDto porque es el DTO que tu servicio espera para ambas operaciones.
                var casoDtoForService = new ModificarCasoDto
                {
                    CasoId = viewModel.CasoDetalle.caso_id, // Esencial para la modificación
                    NombreCompleto = viewModel.CasoDetalle.nombre_asegurado,
                    Identificacion = viewModel.CasoDetalle.identificacion,
                    Telefono = viewModel.CasoDetalle.telefono,
                    Email = viewModel.CasoDetalle.email,
                    Direccion = viewModel.CasoDetalle.direccion,
                    Placa = viewModel.CasoDetalle.placa,
                    Marca = viewModel.CasoDetalle.marca,
                    Modelo = viewModel.CasoDetalle.modelo,
                    Transmision = viewModel.CasoDetalle.transmision,
                    Combustible = viewModel.CasoDetalle.combustible,
                    Cilindraje = viewModel.CasoDetalle.cilindraje,
                    Anio = viewModel.CasoDetalle.anio,
                    NumeroChasis = viewModel.CasoDetalle.numero_chasis,
                    NumeroMotor = viewModel.CasoDetalle.numero_motor,
                    TipoVehiculo = viewModel.CasoDetalle.tipo_vehiculo,
                    Clase = viewModel.CasoDetalle.clase,
                    Color = viewModel.CasoDetalle.color,
                    ObservacionesVehiculo = viewModel.CasoDetalle.observaciones_vehiculo,
                    NumeroReclamo = viewModel.CasoDetalle.numero_reclamo,
                    FechaSiniestro = viewModel.CasoDetalle.fecha_siniestro ?? DateTime.Now,
                    CasoEstadoId = viewModel.CasoDetalle.caso_estado_id,
                    // Las listas de documentos se llenarán con los nuevos archivos cargados
                    DocumentosAsegurado = new List<DocumentoDto>(),
                    DocumentosCaso = new List<DocumentoDto>(),
                    UsuarioId = usuarioId // Pasa el usuario ID si tu DTO lo espera
                };

                // 6. Procesar y guardar nuevos documentos cargados
                // Asumo que tu ModificarCasoViewModel.AllDocuments contiene los IFormFile.
                // Y que ProcessAndSaveDocuments sabe cómo diferenciar y manejar los documentos.
                // Es crucial que tu UI/ViewModel solo envíe los *nuevos* documentos aquí si tu SP
                // solo los inserta. Si AllDocuments tiene existentes y nuevos, necesitas filtrar.
                // 6. Procesar y guardar nuevos documentos cargados
                // Asegúrate de que DocumentoFormInput tiene la propiedad 'ambito_documento' y 'File'.
                // Dentro de tu método 'ModificarCaso' en el controlador:

                // 6. Procesar y guardar nuevos documentos cargados
                // 6. Procesar y guardar los *nuevos* documentos cargados
                // Ahora usamos viewModel.NewDocuments para los archivos a subir
                await ProcessAndSaveDocuments(
                    viewModel.NewDocuments.Where(d => d.AmbitoDocumento == "ASEGURADO" && d.File != null).ToList(),
                    reclamoIdentifierFolder,
                    "asegurados",
                    null, // Pass null for 'associatedId' if not directly used for file saving logic
                    casoDtoForService.DocumentosAsegurado, // Target list for DocumentoDto
                    successfullyUploadedFilePaths // List for rollback
                );

                await ProcessAndSaveDocuments(
                    viewModel.NewDocuments.Where(d => d.AmbitoDocumento == "CASO" && d.File != null).ToList(),
                    reclamoIdentifierFolder,
                    "casos",
                    null, // Pass null for 'associatedId'
                    casoDtoForService.DocumentosCaso, // Target list for DocumentoDto
                    successfullyUploadedFilePaths // List for rollback
                );
                // 7. Llamar al servicio para modificar el caso en la base de datos
                var (resultado, mensajeCambios) = await _casoService.ModificarCasoCompletoAsync(casoDtoForService, usuarioId);

                // 8. Manejo de resultados exitosos
                if (resultado == 1)
                {
                    _logger.LogInformation("Caso ID {CasoId} modificado exitosamente. Mensaje: {Mensaje}", casoDtoForService.CasoId, mensajeCambios);

                    // AQUÍ ES DONDE REEMPLAZAMOS LA LÍNEA
                    return Ok(new
                    {
                        message = "Caso modificado exitosamente.",
                        detail = mensajeCambios, // 'detail' mejor que 'details' para ser coherente con los errores
                        redirectUrl = Url.Action("CasosRegistrados", "Casos")
                    });

                }
                else
                {
                    // Esto debería ser capturado por tus excepciones personalizadas del servicio,
                    // pero es un fallback si el SP devuelve otro código de error no mapeado.
                    _logger.LogError("SP 'sp_ModificarCasoCompleto' para caso ID {CasoId} devolvió resultado inesperado: {Resultado}. Mensaje: {Mensaje}", casoDtoForService.CasoId, resultado, mensajeCambios);
                    return StatusCode(500, new { message = "Error inesperado al modificar el caso.", details = mensajeCambios });
                }
            }
            catch (CasoNotFoundException ex)
            {
                _logger.LogWarning(ex, "Caso no encontrado para modificar (ID: {CasoId}): {Message}", viewModel.CasoDetalle.caso_id, ex.Message);
                DeleteUploadedFiles(successfullyUploadedFilePaths); // Revertir archivos si la DB no lo procesó
                return NotFound(new { message = ex.Message });
            }
            catch (UsuarioAuditoriaInvalidoException ex)
            {
                _logger.LogWarning(ex, "Error de auditoría al modificar caso (ID: {CasoId}): {Message}", viewModel.CasoDetalle.caso_id, ex.Message);
                DeleteUploadedFiles(successfullyUploadedFilePaths);
                return BadRequest(new { message = ex.Message });
            }
            catch (IdentificacionAseguradoDuplicadaException ex)
            {
                _logger.LogWarning(ex, "Identificación duplicada al modificar caso (ID: {CasoId}): {Message}", viewModel.CasoDetalle.caso_id, ex.Message);
                DeleteUploadedFiles(successfullyUploadedFilePaths);
                return Conflict(new { message = ex.Message }); // 409 Conflict para recursos duplicados
            }
            // Agrega más bloques catch para tus otras excepciones personalizadas
            catch (PlacaVehiculoDuplicadaException ex)
            {
                _logger.LogWarning(ex, "Placa duplicada al modificar caso (ID: {CasoId}): {Message}", viewModel.CasoDetalle.caso_id, ex.Message);
                DeleteUploadedFiles(successfullyUploadedFilePaths);
                return Conflict(new { message = ex.Message });
            }
            catch (NumeroChasisVehiculoDuplicadoException ex)
            {
                _logger.LogWarning(ex, "Número de chasis duplicado al modificar caso (ID: {CasoId}): {Message}", viewModel.CasoDetalle.caso_id, ex.Message);
                DeleteUploadedFiles(successfullyUploadedFilePaths);
                return Conflict(new { message = ex.Message });
            }
            catch (NumeroMotorVehiculoDuplicadoException ex)
            {
                _logger.LogWarning(ex, "Número de motor duplicado al modificar caso (ID: {CasoId}): {Message}", viewModel.CasoDetalle.caso_id, ex.Message);
                DeleteUploadedFiles(successfullyUploadedFilePaths);
                return Conflict(new { message = ex.Message });
            }
            catch (EstadoCasoInvalidoException ex)
            {
                _logger.LogWarning(ex, "Estado de caso inválido al modificar caso (ID: {CasoId}): {Message}", viewModel.CasoDetalle.caso_id, ex.Message);
                DeleteUploadedFiles(successfullyUploadedFilePaths);
                return BadRequest(new { message = ex.Message });
            }
            catch (ErrorInternoSPCasoException ex)
            {
                _logger.LogError(ex, "Error interno del SP al modificar caso (ID: {CasoId}): {Message}", viewModel.CasoDetalle.caso_id, ex.Message);
                DeleteUploadedFiles(successfullyUploadedFilePaths);
                return StatusCode(500, new { message = "Error interno del sistema al modificar el caso.", details = ex.Message });
            }
            catch (CasoModificationException ex)
            {
                _logger.LogError(ex, "Error al modificar caso (ID: {CasoId}). Código: {ResultCode}. Mensaje: {Message}", viewModel.CasoDetalle.caso_id, ex.ResultCode, ex.Message);
                DeleteUploadedFiles(successfullyUploadedFilePaths);
                return StatusCode(500, new { message = "Error al modificar el caso.", details = ex.Message });
            }
            catch (Exception ex) // Captura cualquier otra excepción inesperada
            {
                _logger.LogError(ex, "Error inesperado al modificar el caso ID {CasoId}.", viewModel.CasoDetalle.caso_id);
                DeleteUploadedFiles(successfullyUploadedFilePaths); // Revertir archivos siempre
                return StatusCode(500, new { message = "Ocurrió un error inesperado al modificar el caso.", detail = ex.Message });
            }
        }



        /// <summary>
        /// Desactiva un documento por su ID (borrado lógico en DB), registra la acción en el histórico.
        /// </summary>
        /// <param name="documentoId">El ID del documento a desactivar.</param>
        /// <returns>Un ActionResult indicando el resultado de la operación.</returns>
        [HttpPost]
        public async Task<IActionResult> BorrarDocumento(int documentoId)
        {
            if (documentoId <= 0)
            {
                _logger.LogWarning("Intento de desactivar un documento con ID inválido: {DocumentoId}.", documentoId);
                return BadRequest(new { message = "ID de documento inválido." });
            }

            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
            if (usuarioId <= 0)
            {
                _logger.LogWarning("Usuario no autenticado al intentar desactivar documento ID {DocumentoId}.", documentoId);
                return Unauthorized(new { message = "Usuario no autenticado. Por favor, inicie sesión." });
            }

            try
            {
                var (resultado, mensaje, _) = await _casoService.BorrarDocumentoAsync(documentoId, usuarioId);

                if (resultado == 1)
                {
                    _logger.LogInformation("Documento ID {DocumentoId} desactivado correctamente por usuario {UsuarioId}.", documentoId, usuarioId);

                    // 🔄 Ya no se borra el archivo físico, solo se desactiva lógicamente
                    return Ok(new { message = mensaje });
                }
                else if (resultado == 0)
                {
                    _logger.LogWarning("No se pudo desactivar documento ID {DocumentoId}. Mensaje: {Mensaje}", documentoId, mensaje);
                    return NotFound(new { message = mensaje });
                }
                else
                {
                    _logger.LogError("Error al desactivar documento ID {DocumentoId}. Mensaje: {Mensaje}", documentoId, mensaje);
                    return StatusCode(500, new { message = "Error interno al desactivar el documento.", details = mensaje });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al desactivar documento ID {DocumentoId}.", documentoId);
                return StatusCode(500, new { message = "Error inesperado al procesar la solicitud.", details = ex.Message });
            }
        }

        // ===========================================================================================================
        // Métodos Auxiliares para Reutilización y Rollback
        // ===========================================================================================================

        // Método auxiliar para procesar y guardar documentos de una lista específica
        private async Task ProcessAndSaveDocuments(
            List<DocumentoFormInput> docInputs,
            string mainIdentifierFolder,
            string documentTypeAlias,
            int? associatedId, // Puede ser CasoId, AseguradoId, etc. (En este CrearCaso, no lo usamos para asignar ID de entidad)
            List<DocumentoDto> targetList,
            List<string> uploadedPhysicalPaths)
        {
            foreach (var docInput in docInputs)
            {
                if (docInput.File != null && docInput.File.Length > 0)
                {
                    // Llama directamente al método SaveFileAsync del mismo controlador
                    var relativeFilePath = await SaveFileAsync(docInput.File, mainIdentifierFolder, documentTypeAlias);

                    // Construye la ruta física completa para el rollback
                    string fullPhysicalPath = Path.Combine(_fileStorageSettings.BaseUploadPath, relativeFilePath.Replace("/", "\\"));
                    uploadedPhysicalPaths.Add(fullPhysicalPath); // Añade a la lista de rollback

                    // Añade el DTO a la lista de destino
                    targetList.Add(new DocumentoDto
                    {
                        TipoDocumentoId = docInput.TipoDocumentoId,
                        NombreArchivo = docInput.File.FileName,
                        RutaFisica = relativeFilePath, // Se guarda la ruta relativa en DB
                        Observaciones = docInput.Observaciones
                 
                    });
                }
            }
        }

        // Método auxiliar para eliminar archivos en caso de fallo
        private void DeleteUploadedFiles(List<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation("Archivo revertido/eliminado con éxito: {FilePath}", filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al intentar revertir/eliminar archivo: {FilePath}", filePath);
                }
            }
        }

        /// <summary>
        /// Helper para guardar un archivo IFormFile en una ruta física, creando una carpeta única para cada reclamo,
        /// y subcarpetas por tipo de documento, directamente bajo BaseUploadPath.
        /// </summary>
        /// <param name="file">El archivo IFormFile a guardar.</param>
        /// <param name="reclamoIdentifier">El identificador único para el reclamo (ej. "Reclamo_REC-2024").</param>
        /// <param name="documentTypeAlias">Alias para determinar la subcarpeta (ej. "asegurados", "casos", "danos", "valores_comerciales", "partes").</param>
        /// <returns>La ruta relativa del archivo guardado (ej. "Reclamo_REC-2024/Asegurados/archivo.jpg").</returns>
        /// <exception cref="ArgumentException">Lanzada si el archivo es nulo o vacío.</exception>
        public async Task<string> SaveFileAsync(IFormFile file, string reclamoIdentifier, string documentTypeAlias)
        {
            // La ruta base física donde se guardarán todos los archivos (ej. "C:\...\app_autopartes_imagenes")
            string basePhysicalUploadPath = _fileStorageSettings.BaseUploadPath;

            if (string.IsNullOrWhiteSpace(basePhysicalUploadPath))
            {
                _logger.LogError("BaseUploadPath no está configurado en appsettings.json.");
                throw new InvalidOperationException("Ruta base de carga de archivos no configurada.");
            }

            // Determina la subcarpeta específica del tipo de documento (ej. "Asegurados", "Casos", "Danos")
            string documentTypeSubFolder = documentTypeAlias switch
            {
                "asegurados" => _fileStorageSettings.DocumentsAseguradosSubPath,
                "casos" => _fileStorageSettings.DocumentsCasosSubPath,
                "danos" => _fileStorageSettings.DocumentsDanosSubPath,
                "valores_comerciales" => _fileStorageSettings.DocumentsValoresSubPath, // *** Corregido a "valores_comerciales" para consistencia ***
                _ => "OtrosDocumentos" // Fallback para tipos no definidos
            };

            // Construye la ruta completa de la carpeta física donde se guardará el archivo:
            // C:\Users\...\app_autopartes_imagenes\Reclamo_REC-2024\Asegurados\
            string uploadFolderPath = Path.Combine(basePhysicalUploadPath, reclamoIdentifier, documentTypeSubFolder);

            // Crea la carpeta si no existe
            if (!Directory.Exists(uploadFolderPath))
            {
                Directory.CreateDirectory(uploadFolderPath);
            }

            // Validar extensión del archivo
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var extensionesPermitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".xml" }; // Define tus extensiones permitidas
            if (!extensionesPermitidas.Contains(extension))
            {
                _logger.LogWarning("Archivo con extensión no permitida: {Extension}", extension);
                throw new InvalidOperationException($"Extensión de archivo '{extension}' no permitida.");
            }

            // Generar un nombre de archivo seguro para evitar colisiones
            var originalFileName = Path.GetFileName(file.FileName);
            string fileNameOnly = Path.GetFileNameWithoutExtension(originalFileName);
            string safeFileName = originalFileName;
            string fullPath = Path.Combine(uploadFolderPath, safeFileName);
            int count = 1;

            // Si el archivo ya existe, añade un contador (ej. "archivo (1).jpg")
            while (System.IO.File.Exists(fullPath))
            {
                safeFileName = $"{fileNameOnly} ({count}){extension}";
                fullPath = Path.Combine(uploadFolderPath, safeFileName);
                count++;
            }

            // Guardar el archivo en disco
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Archivo guardado exitosamente en: {FullPath}", fullPath);

            // Calcular la ruta relativa que se guardará en la base de datos:
            // Ej: "Reclamo_REC-2024/Asegurados/archivo.jpg"
            // Esta ruta es relativa a tu BaseUploadPath y es la que usarás para acceder al archivo desde la web.
            string relativePath = Path.Combine(reclamoIdentifier, documentTypeSubFolder, safeFileName)
                                       .Replace("\\", "/"); // Reemplaza '\' por '/' para URLs web

            return relativePath; // Esta es la ruta que se almacenará en la base de datos
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

        /// <summary>
        /// Obtiene una vista previa temporal de un archivo dado su ruta física.
        /// </summary>
        /// <param name="rutaFisica"></param>
        /// <returns></returns>
        [HttpGet("ObtenerArchivo")] // Este será el endpoint que llamará tu frontend
        public async Task<IActionResult> ObtenerArchivo([FromQuery] string rutaRelativa)
        {
            if (string.IsNullOrEmpty(rutaRelativa))
            {
                return BadRequest("La ruta relativa del archivo no puede estar vacía.");
            }

            // Obtener la ruta base de almacenamiento desde appsettings.json
            var baseUploadPath = _configuration["FileStorageSettings:BaseUploadPath"];

            // Construir la ruta física completa en el servidor
            // Path.Combine se encarga de usar el separador de directorio correcto para el sistema operativo
            string fullPath = Path.Combine(baseUploadPath, rutaRelativa);

            // **IMPORTANTE**: Normalizar la ruta para evitar problemas con barras inclinadas y contra-barras
            fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            if (!System.IO.File.Exists(fullPath))
            {
                // Puedes loggear esto para depuración
                // _logger.LogWarning($"Archivo no encontrado en la ruta: {fullPath}");
                return NotFound($"El archivo '{rutaRelativa}' no fue encontrado en el servidor.");
            }

            // Determinar el tipo MIME del archivo para que el navegador lo interprete correctamente
            string mimeType = GetMimeType(fullPath);

            // Obtener el nombre original del archivo para que la descarga (si ocurre) tenga el nombre correcto
            string fileName = Path.GetFileName(fullPath);

            // Devolver el archivo como FileContentResult
            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, mimeType, fileName); // El tercer parámetro establece el nombre para la descarga
        }

        // Función auxiliar para determinar el tipo MIME (puedes tener una más robusta)
        private string GetMimeType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".doc" or ".docx" => "application/msword",
                ".xls" or ".xlsx" => "application/vnd.ms-excel",
                _ => "application/octet-stream", // Tipo por defecto si no se reconoce
            };
        }
    }
}
