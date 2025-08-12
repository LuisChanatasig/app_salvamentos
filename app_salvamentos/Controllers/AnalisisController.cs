using app_salvamentos.Configuration;
using app_salvamentos.Models;
using app_salvamentos.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace app_salvamentos.Controllers
{
    public class AnalisisController : Controller
    {
        private readonly CasosService _casoService;
        private readonly SeleccionablesService _seleccionablesService;
        private readonly EmailService _emailService;
        private readonly AnalisisService _financieroService; // Asegúrate de que este servicio esté inyectado si lo necesitas
        private readonly ILogger<CasosController> _logger;
        private readonly FileStorageSettings _fileStorageSettings;

        public AnalisisController(CasosService casoService, ILogger<CasosController> logger, SeleccionablesService seleccionablesService, AnalisisService financieroService, FileStorageSettings fileStorageSettings, EmailService emailService)
        {
            _casoService = casoService;
            _logger = logger;
            _seleccionablesService = seleccionablesService; // Descomentar si lo inyectas
            _financieroService = financieroService;
            _fileStorageSettings = fileStorageSettings;
            _emailService = emailService;
        }


        [HttpGet]
        public async Task<IActionResult> CasosAnalizados([FromQuery] int? estadoId = 2) // Hacemos 'estadoId' nullable
        {
            // Puedes establecer valores por defecto para el ordenamiento si no vienen en la URL
            string sortColumn = "created_at";
            string sortDirection = "DESC";

            // Llama a tu servicio para obtener la lista de casos
            // Pasamos el estadoId, que será null si no se proporciona en la URL,
            // permitiendo que el SP liste todos los casos.
            IEnumerable<CasoListadoDto> casos = await _casoService.ListarCasosEstadoAsync(
                sortColumn,
                sortDirection,
                estadoId
            );

            // Pasa la lista de casos a la vista
            return View(casos);
        }

        /// <summary>
        /// Muestra la vista de análisis de un caso específico.
        /// </summary>
        /// <param name="id">El ID del caso a analizar.</param>
        /// <returns>La vista con los detalles del caso o una redirección si el caso no se encuentra.</returns>
        [HttpGet] // Ruta para la acción de análisis de caso
        public async Task<IActionResult> AnálisisCaso(int id) // Cambiado a int id
        {
            try
            {
                _logger.LogInformation("Solicitando análisis para caso ID: {CasoId}", id);

                // Llama al servicio para obtener los detalles completos del caso
                var casoDetalle = await _casoService.ObtenerCasoPorIdAsync(id);

                // Llama al servicio para obtener los tipos de documento relevantes para 'CASO'
                var tiposDocumentoDto = await _seleccionablesService.ListarTiposDocumentoAsync(ambito: "CASO");
                var tiposDocumentoDtoA = await _seleccionablesService.ListarTiposDocumentoAsync(ambito: "ASEGURADO");

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
                var viewModel = new AnalisisCasoViewModel
                {
                    CasoDetalle = casoDetalle,
                    TiposDocumentoCaso = tiposDocumentoSelectList,
                    TiposDocumentoAsegurado = tiposDocumentoSelectListA
                };

                // Pasa el ViewModel a la vista
                return View("AnálisisCaso", viewModel); // Asegúrate de que el nombre de la vista sea correcto
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
        ///  
        /// </summary>
        /// <param name="datosInput"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarDatosFinancieros([FromForm] DatosCasoFinancieroInputDto datosInput)
        {
            if (!Request.HasFormContentType)
            {
                _logger.LogWarning("La solicitud no tiene datos de formulario. ContentType: {ContentType}", Request.ContentType);
                return BadRequest(new { error = "La solicitud no contiene datos de formulario." });
            }

            // 1) Loggear qué archivos vinieron realmente
            _logger.LogInformation(
                "📁 Request.Form.Files → Count={Count}; Names=[{Names}]",
                Request.Form.Files.Count,
                string.Join(", ", Request.Form.Files.Select(f => f.Name))
            );
            if (!ModelState.IsValid)
            {
                foreach (var kvp in ModelState)
                {
                    var campo = kvp.Key;
                    var errores = kvp.Value.Errors;

                    foreach (var error in errores)
                    {
                        _logger.LogWarning("Error en el campo '{Campo}': {Mensaje}", campo, error.ErrorMessage);
                    }
                }

                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .SelectMany(kvp => kvp.Value.Errors.Select(e => new { campo = kvp.Key, mensaje = e.ErrorMessage }))
                    .ToList();

                return BadRequest(new
                {
                    error = "Datos del formulario inválidos.",
                    errors = errors
                });
            }





            if (datosInput == null || datosInput.CasoId <= 0)
            {
                _logger.LogWarning("El objeto datosInput llegó como null o CasoId no válido después del binding.");
                return BadRequest(new { error = "No se recibió información válida para guardar el caso." });
            }

            // Lista para guardar las rutas físicas completas de los archivos guardados exitosamente
            // para poder eliminarlos si la transacción de la DB falla.
            var successfullyUploadedFilePaths = new List<string>();

            try
            {
                string numeroReclamo = "Documentos_Reclamo_" + datosInput.NumeroReclamo;

                if (string.IsNullOrWhiteSpace(numeroReclamo))
                {
                    _logger.LogError("No se pudo obtener el Número de Reclamo para el CasoId {CasoId}.", datosInput.CasoId);
                    return BadRequest(new { error = "No se encontró el número de reclamo asociado a este caso." });
                }

                string mainIdentifierFolder = numeroReclamo.Replace(" ", "_").Replace("/", "-");

                // Inicializa las listas de documentos del input DTO si son nulas
                datosInput.DocumentosCasoInput ??= new List<DocumentoFormInput>();
                datosInput.DocumentosAseguradoInput ??= new List<DocumentoFormInput>();
                datosInput.DocumentosValorComercialInput ??= new List<DocumentoFormInput>();
                datosInput.DocumentosDanoInput ??= new List<DocumentoFormInput>();

                // Crea las listas de DocumentoDto que se pasarán al servicio
                var documentosCasoParaServicio = new List<DocumentoDto>();
                var documentosAseguradoParaServicio = new List<DocumentoDto>();
                var documentosValorComercialParaServicio = new List<DocumentoDto>();
                var documentosDanoParaServicio = new List<DocumentoDto>();

                // ===========================================================================================================
                // Procesamiento de documentos: Guarda archivos físicamente e inserta sus rutas en la lista temporal
                // ===========================================================================================================

                await ProcessAndSaveDocuments(datosInput.DocumentosCasoInput, mainIdentifierFolder, "casos", datosInput.CasoId, documentosCasoParaServicio, successfullyUploadedFilePaths);
                await ProcessAndSaveDocuments(datosInput.DocumentosAseguradoInput, mainIdentifierFolder, "asegurados", datosInput.CasoId, documentosAseguradoParaServicio, successfullyUploadedFilePaths);
                await ProcessAndSaveDocuments(datosInput.DocumentosValorComercialInput, mainIdentifierFolder, "valores_comerciales", datosInput.CasoId, documentosValorComercialParaServicio, successfullyUploadedFilePaths);
                await ProcessAndSaveDocuments(datosInput.DocumentosDanoInput, mainIdentifierFolder, "danos", datosInput.CasoId, documentosDanoParaServicio, successfullyUploadedFilePaths);


                _logger.LogInformation(
    "🔍 datosInput.DocumentosValorComercialInput → Count={Count}; Elementos=[{Indices}]",
    datosInput.DocumentosValorComercialInput?.Count ?? 0,
    datosInput.DocumentosValorComercialInput?
        .Select((d, i) => $"{i}:{d.File?.FileName ?? "(no File)"}")
        .ToArray() ?? Array.Empty<string>()
);
                // ===========================================================================================================
                // Preparar DTO final para el servicio de DB y registrar datos
                // ===========================================================================================================

                var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

                var finalDtoForService = new DatosCasoFinancieroDto
                {
                    CasoId = datosInput.CasoId,
                    UsuarioId = usuarioId,
                    AseguradoId = datosInput.AseguradoId,
                    VehiculoId = datosInput.VehiculoId,
                    NombreCompleto = datosInput.NombreCompleto,
                    FechaSiniestro = datosInput.FechaSiniestro,
                    MetodoAvaluo = datosInput.MetodoAvaluo,
                    DireccionAvaluo = datosInput.DireccionAvaluo,
                    ComentariosAvaluo = datosInput.ComentariosAvaluo,
                    NotasAvaluo = datosInput.NotasAvaluo,
                    FechaSolicitudAvaluo = datosInput.FechaSolicitudAvaluo,

                    Vehiculo = datosInput.Vehiculo,

                    Resumen = datosInput.Resumen,
                    ValoresComerciales = datosInput.ValoresComerciales,
                    Danos = datosInput.Danos,
                    Partes = datosInput.Partes,

                    DocumentosCaso = documentosCasoParaServicio,
                    DocumentosAsegurado = documentosAseguradoParaServicio,
                    DocumentosValorComercial = documentosValorComercialParaServicio,
                    DocumentosDano = documentosDanoParaServicio,
                };
                string redirectUrl = Url.Action("CasosAnalizados", "Analisis");

                // *** ESTE ES EL PUNTO CRÍTICO ***
                // Si _financieroService.RegistrarDatosCasoFinancieroAsync falla, se ejecutará el catch.
                await _financieroService.RegistrarDatosCasoFinancieroAsync(finalDtoForService);

                return Ok(new { mensaje = "Datos financieros y documentos registrados correctamente.", redirectToUrl = redirectUrl });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error en la base de datos al registrar datos financieros. Intentando revertir archivos.");
                // Revertir archivos si la DB falla
                DeleteUploadedFiles(successfullyUploadedFilePaths);
                return StatusCode(500, new { error = "Error en la base de datos", detalle = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error inesperado al guardar el análisis del caso. Intentando revertir archivos.");
                // Revertir archivos si hay cualquier otro error
                DeleteUploadedFiles(successfullyUploadedFilePaths);
                return StatusCode(500, new { error = "Ocurrió un error inesperado al guardar el análisis del caso.", detalle = ex.Message });
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
            int? associatedId, // Puede ser CasoId, AseguradoId, etc.
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
                        Observaciones = docInput.Observaciones,

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
                    // No lanzar la excepción para que el proceso de "rollback" continúe para otros archivos.
                }
            }
        }



        /// <summary>
        /// Helper para guardar un archivo IFormFile en una ruta física, creando una carpeta única para cada caso/reclamo,
        /// y subcarpetas por tipo de documento, directamente bajo BaseUploadPath.
        /// </summary>
        /// <param name="file">El archivo IFormFile a guardar.</param>
        /// <param name="mainIdentifierFolder">El identificador principal para la carpeta del caso/reclamo (ej. "Analisis_Financiero_XXX").</param>
        /// <param name="documentTypeAlias">Alias para determinar la subcarpeta (ej. "asegurados", "casos", "danos", "valores_comerciales", "partes").</param>
        /// <returns>La ruta relativa del archivo guardado (ej. "Analisis_Financiero_XXX/Asegurados/archivo.jpg").</returns>
        /// <exception cref="ArgumentException">Lanzada si el archivo es nulo o vacío.</exception>
        public async Task<string> SaveFileAsync(IFormFile file, string mainIdentifierFolder, string documentTypeAlias)
        {
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
                "valores_comerciales" => _fileStorageSettings.DocumentsValoresSubPath, // *** Se mantiene este alias para consistencia ***
                _ => "OtrosDocumentos" // Fallback para tipos no definidos
            };

            // Construye la ruta completa de la carpeta física donde se guardará el archivo:
            // BaseUploadPath\mainIdentifierFolder\documentTypeSubFolder\
            // Ejemplo: C:\Users\...\app_autopartes_imagenes\Analisis_Financiero_XXX\Asegurados\
            string uploadFolderPath = Path.Combine(basePhysicalUploadPath, mainIdentifierFolder, documentTypeSubFolder);

            // Crea la carpeta si no existe
            if (!Directory.Exists(uploadFolderPath))
            {
                Directory.CreateDirectory(uploadFolderPath);
            }

            // Validar extensión del archivo
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var extensionesPermitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".xml" }; // Tus extensiones permitidas
            if (!extensionesPermitidas.Contains(extension))
            {
                _logger.LogWarning("Archivo con extensión no permitida: {Extension}", extension);
                throw new InvalidOperationException($"Extensión de archivo '{extension}' no permitida.");
            }

            // Generar el nombre original
            var safeFileName = Path.GetFileName(file.FileName);
            var fullPath = Path.Combine(uploadFolderPath, safeFileName);

            // Si ya existe, simplemente lo borramos para que FileMode.Create lo sobreescriba
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            // Y luego guardamos normalmente
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            _logger.LogInformation("Archivo guardado (o actualizado) en: {FullPath}", fullPath);

            // Calcular la ruta relativa que se guardará en la base de datos:
            // Ejemplo: "Analisis_Financiero_XXX/Asegurados/archivo.jpg"
            // Esta ruta es relativa a tu BaseUploadPath y es la que usarás para acceder al archivo desde la web.
            string relativePath = Path.Combine(mainIdentifierFolder, documentTypeSubFolder, safeFileName)
                                       .Replace("\\", "/"); // Reemplaza '\' por '/' para URLs web

            return relativePath; // Esta es la ruta que se almacenará en la base de datos
        }


        /// <summary>
        /// Envía el correo de avalúo para un caso específico.
        /// </summary>
        /// <param name="casoId">El ID del caso.</param>
        /// <returns>Un ActionResult indicando el resultado de la operación.</returns>
        /// <summary>
        /// Prepara el contenido de un email de avalúo y devuelve los datos necesarios para un enlace mailto:.
        /// No envía el email directamente.
        /// </summary>
        /// <param name="casoId">El ID del caso para el cual se preparará el email.</param>
        /// <returns>Un JSON con el MailtoUri, HtmlBody, PlainTextBody, Subject, RecipientEmail y AttachmentPathsNotSent.</returns>
        [HttpGet] // La ruta completa será /Analisis/preparar-email-avaluo/{id}
        public async Task<IActionResult> PrepararEmailAvaluo(int id) // Cambiado el nombre del parámetro de casoId a id
        {
            _logger.LogInformation("Solicitud recibida para preparar contenido de email de avalúo para el caso ID: {CasoId}", id);

            try
            {
                // Obtener el número de reclamo para pasarlo al servicio de preparación de email
                // Esto es necesario porque el PrepareAvaluoEmailAsync lo requiere.
                var casoDetalle = await _casoService.ObtenerCasoPorIdAsync(id);
                if (casoDetalle == null)
                {
                    _logger.LogWarning("Caso con ID {CasoId} no encontrado al intentar preparar el email.", id);
                    return NotFound(new { message = $"Caso con ID {id} no encontrado para preparar el email." });
                }

                // Llama al servicio de preparación de emails
                var emailPrepareResult = await _emailService.PrepareAvaluoEmailAsync(id, casoDetalle.numero_reclamo ?? "N/A");

                _logger.LogInformation("Contenido de email de avalúo para el caso ID: {CasoId} preparado con éxito.", id);

                // Devuelve el DTO con toda la información al frontend
                return Ok(emailPrepareResult);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error de operación al preparar email de avalúo para caso ID {CasoId}: {Message}", id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al preparar email de avalúo para caso ID {CasoId}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "Ocurrió un error interno al intentar preparar el contenido del email de avalúo." });
            }
        }
    }
}
