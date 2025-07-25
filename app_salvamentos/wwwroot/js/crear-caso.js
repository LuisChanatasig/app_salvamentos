$(document).ready(function () {
    // ====================================================================
    // Variables Globales e Inicialización
    // ====================================================================
    const form = $('#crearCasoForm');
    const formA = $('#actualizarCasoForm'); // Asumiendo que esto es para un formulario de actualización
    const steps = $('.step');
    let currentStep = 1;

    // Objeto para almacenar los documentos cargados en memoria
    // Contiene arrays separados para cada categoría de documento
    let loadedDocuments = {
        asegurado: [],          // [{ TipoDocumentoId: int, File: FileObject, Observaciones: string, uiId: uuid }]
        caso: [],               // [{ TipoDocumentoId: int, File: FileObject, Observaciones: string, uiId: uuid }]
        fotosDano: [],          // [{ TipoDocumentoId: int, File: FileObject, Observaciones: string, uiId: uuid }]
        valoresComercialesFiles: [] // [{ TipoDocumentoId: int, File: FileObject, Observaciones: string, uiId: uuid }]
    };

    // Referencias a elementos del formulario de documentos
    const documentoTipoSelect = $('#documentoTipo'); // Asegúrate de que este ID sea correcto en tu HTML
    const archivosDocumentoInput = $('#archivosDocumento'); // Asegúrate de que este ID sea correcto
    const observacionesDocumentoTextarea = $('#observacionesDocumento'); // Asegúrate de que este ID sea correcto
    const listaDocumentosDiv = $('#listaDocumentos'); // Aquí es donde se añaden las tarjetas de documentos

    // Instancia del modal de Bootstrap para previsualización
    const previewContent = $('#previewContent');
    const previewModal = new bootstrap.Modal(document.getElementById('previewModal'));

    // Inyectar el UsuarioId desde el modelo Razor (definido en la vista .cshtml)
    // Asegúrate de que 'initialUsuarioId' se define en tu Razor View:
    // <script>var initialUsuarioId = @Model.UsuarioId;</script> (o similar)
    const usuarioIdFromModel = typeof initialUsuarioId !== 'undefined' ? initialUsuarioId : 1;

    // URL para la creación/actualización de casos. Deben ser inyectadas desde la vista Razor.
    // Ejemplo en tu Razor View:
    // <script>var crearCasoApiUrl = '@Url.Action("CrearCaso", "Casos")';</script>
    // <script>var actualizarCasoApiUrl = '@Url.Action("ModificarCaso", "Casos")';</script>
    // Si no las inyectas, usarán un fallback.


    // ====================================================================
    // Funciones de Navegación entre Pasos del Formulario
    // ====================================================================

    function showStep(stepNumber) {
        steps.addClass('d-none'); // Oculta todos los pasos
        $(`#step${stepNumber}`).removeClass('d-none'); // Muestra el paso actual
        currentStep = stepNumber;
    }

    // Función para validar el paso actual (adaptada para selects estándar)
    function validateStep(stepNumber) {
        let isValid = true;
        let firstInvalidField = null;

        // Limpiar validaciones previas
        $(`#step${stepNumber} .is-invalid`).removeClass('is-invalid');

        // Validar campos de input, textarea y select estándar
        $(`#step${stepNumber} :input[required], #${stepNumber} select[required]`).each(function () {
            if (!this.checkValidity()) {
                isValid = false;
                $(this).addClass('is-invalid');
                $(this).removeClass('is-valid'); // Elimina la clase válida si se estableció previamente
                if (!firstInvalidField) firstInvalidField = this;
            } else {
                $(this).addClass('is-valid');
                $(this).removeClass('is-invalid'); // Elimina la clase inválida si se estableció previamente
            }
        });

        if (!isValid && firstInvalidField) {
            // Desplazarse al primer campo inválido
            firstInvalidField.scrollIntoView({ behavior: 'smooth', block: 'center' });
            // Mostrar mensaje de error específico
            const labelText = $(firstInvalidField).closest('.mb-3').find('.form-label').text().trim() || "Campo requerido";
            Swal.fire({
                icon: 'error',
                title: 'Error de Validación',
                text: `Por favor, complete el campo obligatorio: "${labelText}".`,
                confirmButtonText: 'Aceptar'
            });
        }

        return isValid;
    }

    // ====================================================================
    // Funciones Auxiliares para Documentos
    // ====================================================================

    /**
     * Retorna la clase de ícono (Remix Icon) según el tipo MIME del archivo.
     * @param {string} type - El tipo MIME del archivo (ej. 'application/pdf', 'image/jpeg').
     * @returns {string} - La clase CSS del ícono.
     */
    function getIconByType(type) {
        if (type.includes('pdf')) return 'ri-file-pdf-line';
        if (type.includes('word') || type.includes('document')) return 'ri-file-word-line';
        if (type.includes('excel') || type.includes('spreadsheet')) return 'ri-file-excel-line';
        if (type.includes('image')) return 'ri-image-line';
        return 'ri-file-line';
    }

    /**
     * Extrae la extensión de un nombre de archivo o URL.
     * @param {string} filename - El nombre del archivo o URL.
     * @returns {string} - La extensión en minúsculas.
     */
    function getFileExtension(filename) {
        return filename.split('.').pop().toLowerCase();
    }

    /**
     * Retorna el tipo MIME común para una extensión de archivo dada.
     * @param {string} extension - La extensión del archivo (ej. 'pdf', 'jpg').
     * @returns {string} - El tipo MIME correspondiente.
     */
    function getFileMimeType(extension) {
        switch (extension) {
            case 'jpg':
            case 'jpeg': return 'image/jpeg';
            case 'png': return 'image/png';
            case 'gif': return 'image/gif';
            case 'pdf': return 'application/pdf';
            case 'doc':
            case 'docx': return 'application/msword';
            case 'xls':
            case 'xlsx': return 'application/vnd.ms-excel';
            default: return 'application/octet-stream';
        }
    }

    // ====================================================================
    // Evento: Añadir Documentos (Botón 'Agregar Archivos')
    // ====================================================================

    $('#btnAgregarArchivos').on('click', async function () {
        const tipoDocumentoId = documentoTipoSelect.val();
        const tipoDocumentoText = documentoTipoSelect.find('option:selected').text();
        const selectedOptionElement = documentoTipoSelect.find('option:selected');

        let ambitoDocumentoRaw = selectedOptionElement.attr('data-ambito');
        let ambitoDocumento = ambitoDocumentoRaw ? ambitoDocumentoRaw.toLowerCase() : undefined;

        // --- Validaciones de entrada ---
        if (!tipoDocumentoId) {
            documentoTipoSelect.addClass('is-invalid');
            Swal.fire({
                icon: 'warning',
                title: 'Selección Requerida',
                text: 'Por favor, seleccione un tipo de documento de la lista.',
                confirmButtonText: 'Aceptar',
                confirmButtonColor: '#f7b84b'
            });
            return;
        } else {
            documentoTipoSelect.removeClass('is-invalid');
        }

        // Validación extendida para ambitoDocumento, considerando tus nuevas categorías
        // Comprueba si el ámbito existe como clave en loadedDocuments
        if (!ambitoDocumento || !loadedDocuments.hasOwnProperty(ambitoDocumento)) {
            documentoTipoSelect.addClass('is-invalid');
            console.error("Error de validación: El ámbito del documento es inválido o indefinido. Valor obtenido:", ambitoDocumentoRaw);
            Swal.fire({
                icon: 'error',
                title: 'Error de Configuración',
                text: 'La opción de tipo de documento seleccionada no tiene un ámbito válido. Por favor, recargue la página o contacte a soporte.',
                confirmButtonText: 'Aceptar'
            });
            return;
        } else {
            documentoTipoSelect.removeClass('is-invalid');
        }

        const observaciones = observacionesDocumentoTextarea.val();
        const files = archivosDocumentoInput[0].files;

        if (files.length === 0) {
            Swal.fire({
                icon: 'warning',
                title: 'Archivos Requeridos',
                text: 'Por favor, seleccione al menos un archivo para subir.',
                confirmButtonText: 'Aceptar',
                confirmButtonColor: '#f7b84b'
            });
            return;
        }

        // --- Procesamiento de archivos seleccionados ---
        for (const file of files) {
            // Verificar si el archivo ya existe en la lista de documentos cargados para el mismo ámbito y tipo
            const yaExiste = loadedDocuments[ambitoDocumento].some(
                doc => doc.NombreArchivo === file.name && doc.TipoDocumentoId === parseInt(tipoDocumentoId)
            );
            if (yaExiste) {
                Swal.fire({
                    icon: 'info',
                    title: 'Archivo Duplicado',
                    text: `El archivo "${file.name}" con este tipo de documento ya ha sido añadido.`,
                    confirmButtonText: 'Aceptar'
                });
                continue; // Saltar este archivo
            }

            try {
                const newDoc = {
                    TipoDocumentoId: parseInt(tipoDocumentoId),
                    NombreArchivo: file.name,
                    File: file, // Guardamos el objeto File directamente para futura referencia
                    Observaciones: observaciones,
                    AmbitoDocumento: ambitoDocumentoRaw // Mantén la cadena original para mostrar
                };

                // Asignar un ID único para la UI, clave para eliminar y previsualizar en memoria
                const uiId = crypto.randomUUID();
                newDoc.uiId = uiId;

                // Añadir el documento al array en memoria según su ámbito
                loadedDocuments[ambitoDocumento].push(newDoc);
                console.log(`Documento '${file.name}' agregado al ámbito '${ambitoDocumento}'.`);
                console.log("Estado actual de loadedDocuments:", loadedDocuments);

                // --- Añadir a la UI (card Bootstrap) ---
                const docItem = `
                <div class="col-md-4 col-sm-6 mb-3" data-ui-id="${uiId}" data-ambito="${ambitoDocumentoRaw}">
                    <div class="card border card-animate">
                        <div class="card-body">
                            <div class="d-flex align-items-center">
                                <div class="flex-shrink-0 me-3">
                                    <i class="${getIconByType(file.type)} fs-2 text-primary"></i>
                                </div>
                                <div class="flex-grow-1">
                                    <h6 class="mb-1" style="white-space: normal; word-break: break-word; overflow-wrap: break-word;">
                                        ${file.name}
                                    </h6>
                                    <small class="text-muted">${tipoDocumentoText} (${ambitoDocumentoRaw})</small>
                                </div>
                                <div class="flex-shrink-0">
                                    <button type="button" class="btn btn-sm btn-light p-0 remove-doc-btn" data-bs-toggle="tooltip" data-bs-placement="top" title="Eliminar" data-ui-id="${uiId}" data-ambito="${ambitoDocumentoRaw}">
                                        <i class="ri-delete-bin-line text-danger"></i>
                                    </button>
                                    <button type="button" class="btn btn-sm btn-light p-0 preview-doc-btn ms-1" data-bs-toggle="tooltip" data-bs-placement="top" title="Previsualizar" data-ui-id="${uiId}" data-ambito="${ambitoDocumentoRaw}" data-file-type="${file.type}">
                                        <i class="ri-eye-line text-info"></i>
                                    </button>
                                </div>
                            </div>
                            ${observaciones ? `<p class="text-muted mt-2 mb-0 text-wrap"><small>Obs: ${observaciones}</small></p>` : ''}
                        </div>
                    </div>
                </div>
                `;
                listaDocumentosDiv.append(docItem);

                // Re-inicializar tooltips para los nuevos elementos (importante para que funcionen)
                $('[data-bs-toggle="tooltip"]').tooltip('dispose').tooltip();

                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'success',
                    title: 'Archivo agregado',
                    showConfirmButton: false,
                    timer: 1500
                });

            } catch (error) {
                console.error("Error al procesar archivo:", error);
                Swal.fire({
                    icon: 'error',
                    title: 'Error de Archivo',
                    text: `No se pudo procesar el archivo ${file.name}.`,
                    confirmButtonText: 'Aceptar'
                });
            }
        }

        // Limpiar campos después de agregar todos los archivos
        archivosDocumentoInput.val('');
        observacionesDocumentoTextarea.val('');
        documentoTipoSelect.val(''); // Limpiar select estándar
    });

    // ====================================================================
    // Evento: Previsualizar Documentos (Delegación de Eventos)
    // ====================================================================

    // Usamos delegación de eventos en 'listaDocumentosDiv' porque los botones '.preview-doc-btn'
    // se añaden dinámicamente al DOM.
    listaDocumentosDiv.on('click', '.preview-doc-btn', async function () {
        const button = $(this);
        const card = button.closest('.col-md-4');
        const uiId = card.data('ui-id'); // ID para documentos recién cargados (en memoria)
        let ambitoRaw = card.data('ambito');
        let ambito = ambitoRaw ? ambitoRaw.toLowerCase() : undefined;

        // Obtener información para documentos existentes (ya guardados en DB)
        const documentoId = button.data('doc-id');
        const rutaFisica = button.data('ruta-fisica');
        // No necesitamos data-file-type aquí, getFileMimeType lo inferirá de rutaFisica o file.type

        previewContent.empty(); // Limpiar contenido previo del modal de previsualización

        // Validación básica para ámbito
        if (!ambito || !loadedDocuments.hasOwnProperty(ambito)) { // Comprobación mejorada
            console.error('Ámbito inválido o no especificado al intentar visualizar documento');
            Swal.fire({
                icon: 'error',
                title: 'Error Interno',
                text: 'No se pudo determinar el ámbito del documento. Recargue la página.',
                confirmButtonText: 'Aceptar'
            });
            return;
        }

        // --- Lógica para documentos existentes (ya guardados en el servidor) ---
        if (documentoId && rutaFisica) {
            const encodedPath = encodeURIComponent(rutaFisica);
            const previewUrl = `${API_CONTROLLER_BASE_URL_PREVIEW}?rutaRelativa=${encodedPath}`;
            const ext = getFileExtension(rutaFisica); // Usa la ruta física para la extensión
            const mimeType = getFileMimeType(ext);

            switch (mimeType) {
                case 'application/pdf':
                    previewContent.append(`<embed src="${previewUrl}" type="application/pdf" width="100%" height="600px" />`);
                    break;
                case 'image/jpeg':
                case 'image/png':
                case 'image/gif':
                    previewContent.append(`<img src="${previewUrl}" class="img-fluid" style="max-height: 80vh;" alt="Previsualización de imagen">`);
                    break;
                case 'application/msword':
                case 'application/vnd.openxmlformats-officedocument.wordprocessingml.document': // .docx
                    previewContent.append(`<p class="alert alert-info">📄 Archivo Word. <a href="${previewUrl}" target="_blank">Abrir/Descargar</a></p>`);
                    break;
                case 'application/vnd.ms-excel':
                case 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': // .xlsx
                    previewContent.append(`<p class="alert alert-info">📊 Archivo Excel. <a href="${previewUrl}" target="_blank">Abrir/Descargar</a></p>`);
                    break;
                default:
                    previewContent.append(`<p class="alert alert-warning">No se puede previsualizar este tipo de archivo. <a href="${previewUrl}" target="_blank">Descargar</a></p>`);
                    break;
            }
            previewModal.show(); // Muestra el modal

        } else if (uiId) { // --- Lógica para documentos recién subidos (en memoria, no guardados aún) ---
            const documentosAmbito = loadedDocuments[ambito] || [];
            const documento = documentosAmbito.find(doc => doc.uiId === uiId); // Buscar por uiId

            if (documento && documento.File) {
                const file = documento.File;
                const reader = new FileReader();

                reader.onload = function (e) {
                    const fileUrl = e.target.result;
                    if (file.type.startsWith('image/')) {
                        previewContent.append(`<img src="${fileUrl}" class="img-fluid" style="max-height: 80vh;" alt="Previsualización de imagen">`);
                    } else if (file.type === 'application/pdf') {
                        previewContent.append(`<embed src="${fileUrl}" type="application/pdf" width="100%" height="600px" />`);
                    } else {
                        previewContent.append('<p class="alert alert-warning">No se puede previsualizar este tipo de archivo.</p>');
                    }
                    previewModal.show(); // Muestra el modal
                };
                reader.readAsDataURL(file);
            } else {
                Swal.fire('Error', 'No se encontró el archivo en memoria para previsualizar.', 'error');
            }
        } else {
            // En caso de que no se encuentre ni uiId ni docId/rutaFisica
            Swal.fire({
                icon: 'error',
                title: 'Error de Previsualización',
                text: 'No se pudo identificar el documento para previsualizar.',
                confirmButtonText: 'Aceptar'
            });
        }
    });
    // ====================================================================
    // Evento: Eliminar Documentos (Delegación de Eventos)
    // ====================================================================

    listaDocumentosDiv.on('click', '.remove-doc-btn', function () {
        const button = $(this);
        const cardToRemove = button.closest('.col-md-4'); // Obtiene toda la tarjeta a eliminar

        // Corrección 1: Leer correctamente el uiId
        const uiIdToRemove = cardToRemove.data('ui-id');
        const documentoIdToRemove = cardToRemove.data('doc-id');

        // Corrección 2: Leer correctamente el ámbito
        let ambitoToRemove = cardToRemove.data('ambito');
        if (!ambitoToRemove || typeof ambitoToRemove !== 'string' || ambitoToRemove.trim() === '') {
            console.error("Error: El atributo data-ambito no se encontró en la tarjeta del documento. No se puede eliminar.");
            Swal.fire({
                icon: 'error',
                title: 'Error de Eliminación',
                text: 'No se pudo identificar la categoría del documento para eliminarlo. Por favor, recargue la página.',
                confirmButtonText: 'Aceptar'
            });
            return;
        }
        ambitoToRemove = ambitoToRemove.toLowerCase();

        Swal.fire({
            title: '¿Estás seguro?',
            text: "¡El documento será eliminado permanentemente (si ya está en la base de datos) o de la lista temporal!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed) {
                // Documento ya guardado en base de datos
                if (parseInt(documentoIdToRemove) > 0) {
                    console.log(`Intentando eliminar de la BD el documento ID: ${documentoIdToRemove}`);
                    $.ajax({
                        url: `${API_DOCUMENTOS_BASE_URL}?documentoId=${documentoIdToRemove}`,
                        type: "POST",
                        headers: {
                            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                        },
                        success: function (response) {
                            if (response.success) {
                                cardToRemove.remove();

                                if (window.loadedDocuments.hasOwnProperty(ambitoToRemove)) {
                                    window.loadedDocuments[ambitoToRemove] = window.loadedDocuments[ambitoToRemove].filter(
                                        doc => doc.documento_id !== parseInt(documentoIdToRemove)
                                    );
                                    console.log(`Documento con ID ${documentoIdToRemove} eliminado (DB) del ámbito ${ambitoToRemove}.`);
                                    console.log("Estado actual de loadedDocuments:", window.loadedDocuments);
                                }

                                Swal.fire(
                                    '¡Eliminado!',
                                    'El documento ha sido eliminado de la base de datos.',
                                    'success'
                                );
                            } else {
                                const errorMessage = response.message || "Hubo un problema al eliminar el documento de la base de datos.";
                                console.error("Error al eliminar documento de la BD:", errorMessage);
                                Swal.fire('Error!', errorMessage, 'error');
                            }
                        },
                        error: function (xhr, status, error) {
                            const errorMessage = xhr.responseJSON && xhr.responseJSON.message
                                ? xhr.responseJSON.message
                                : "Hubo un problema al eliminar el documento de la base de datos.";
                            console.error("Error AJAX al eliminar documento de la BD:", errorMessage);
                            Swal.fire('Error!', errorMessage, 'error');
                        }
                    });
                }
                // Documento solo en memoria
                else if (uiIdToRemove) {
                    console.log(`Intentando eliminar de la memoria el documento UI ID: ${uiIdToRemove}`);
                    cardToRemove.remove();

                    // ✅ Corrección aquí
                    if (window.loadedDocuments.hasOwnProperty(ambitoToRemove)) {
                        window.loadedDocuments[ambitoToRemove] = window.loadedDocuments[ambitoToRemove].filter(
                            doc => doc.uiId !== uiIdToRemove
                        );
                        console.log(`Documento con uiId ${uiIdToRemove} eliminado (memoria) del ámbito ${ambitoToRemove}.`);
                        console.log("Estado actual de loadedDocuments:", window.loadedDocuments);
                    }

                    Swal.fire(
                        '¡Eliminado!',
                        'El documento ha sido removido de la lista temporal.',
                        'success'
                    );
                } else {
                    Swal.fire(
                        'Error!',
                        'No se pudo identificar el documento para eliminar.',
                        'error'
                    );
                }
            }
        });
    });

    // ====================================================================
    // Navegación de Pasos
    // ====================================================================

    $('#btnNextStep').on('click', function () {
        if (validateStep(currentStep)) {
            showStep(currentStep + 1);
        }
    });

    $('#btnBack').on('click', function () {
        if (currentStep > 1) {
            showStep(currentStep - 1);
        }
    });

    // ====================================================================
    // Envío del Formulario de Creación (AJAX con FormData)
    // ====================================================================

    // Asegurarse de que el formulario de creación exista en la página
    if (form.length > 0) {
        form.on('submit', async function (e) {
            e.preventDefault(); // Evita el envío normal del formulario

            // Validar el último paso antes de enviar
            if (!validateStep(currentStep)) {
                return; // validateStep ya muestra un Swal.fire
            }

            // Mostrar spinner o mensaje de carga
            Swal.fire({
                title: 'Creando Caso...',
                text: 'Por favor, espere mientras se procesa su solicitud.',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            // Crear un objeto FormData y añadir todos los campos del formulario
            const formDataToSend = new FormData(this); // 'this' es el formulario HTML

            // Añadir los archivos cargados dinámicamente al FormData
            // Asegurado
            loadedDocuments.asegurado.forEach((doc, index) => {
                formDataToSend.append(`DocumentosAsegurado[${index}].TipoDocumentoId`, doc.TipoDocumentoId);
                formDataToSend.append(`DocumentosAsegurado[${index}].File`, doc.File, doc.File.name); // El objeto File
                formDataToSend.append(`DocumentosAsegurado[${index}].Observaciones`, doc.Observaciones);
            });

            // Caso
            loadedDocuments.caso.forEach((doc, index) => {
                formDataToSend.append(`DocumentosCaso[${index}].TipoDocumentoId`, doc.TipoDocumentoId);
                formDataToSend.append(`DocumentosCaso[${index}].File`, doc.File, doc.File.name); // El objeto File
                formDataToSend.append(`DocumentosCaso[${index}].Observaciones`, doc.Observaciones);
            });

            // Fotos Daño
            loadedDocuments.fotosDano.forEach((doc, index) => {
                formDataToSend.append(`FotosDano[${index}].TipoDocumentoId`, doc.TipoDocumentoId);
                formDataToSend.append(`FotosDano[${index}].File`, doc.File, doc.File.name);
                formDataToSend.append(`FotosDano[${index}].Observaciones`, doc.Observaciones);
            });

            // Valores Comerciales Files
            loadedDocuments.valoresComercialesFiles.forEach((doc, index) => {
                formDataToSend.append(`ValoresComercialesFiles[${index}].TipoDocumentoId`, doc.TipoDocumentoId);
                formDataToSend.append(`ValoresComercialesFiles[${index}].File`, doc.File, doc.File.name);
                formDataToSend.append(`ValoresComercialesFiles[${index}].Observaciones`, doc.Observaciones);
            });


            console.log("--- Contenido de formDataToSend (para depuración) ---");
            for (let pair of formDataToSend.entries()) {
                const [key, value] = pair;
                if (value instanceof File) {
                    console.log(`${key}: File (Name: ${value.name}, Type: ${value.type}, Size: ${value.size} bytes)`);
                } else {
                    console.log(`${key}: ${value}`);
                }
            }
            console.log("--- Fin de la inspección ---");
            console.log("URL de envío para crear caso:", crearCasoApiUrl);

            try {
                const response = await fetch(crearCasoApiUrl, {
                    method: 'POST',
                    // NO establecer Content-Type. El navegador lo establecerá automáticamente para FormData.
                    headers: {
                        'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                    },
                    body: formDataToSend // Enviar el objeto FormData directamente
                });

                if (response.ok) {
                    // Comprueba si la respuesta realmente contiene una redirectUrl o una nueva URL
                    // La propiedad 'response.url' contendrá la URL final después de las redirecciones.
                    window.location.href = response.url;
                } else {
                    const errorData = await response.json();
                    Swal.fire({
                        icon: 'error',
                        title: 'Error al Crear Caso',
                        text: errorData.message || 'Ocurrió un error desconocido.',
                        footer: errorData.detail ? `<small>${errorData.detail}</small>` : '',
                        confirmButtonText: 'Aceptar'
                    });
                }
            } catch (error) {
                console.error("Error en la solicitud AJAX (crear caso):", error);
                Swal.fire({
                    icon: 'error',
                    title: 'Error de Conexión',
                    text: 'No se pudo conectar con el servidor. Inténtelo de nuevo. Detalles: ' + error.message,
                    confirmButtonText: 'Aceptar'
                });
            } finally {
                Swal.hideLoading(); // Ocultar spinner
            }
        });
    }


    // ====================================================================
    // Envío del Formulario de Actualización (AJAX con FormData)
    // ====================================================================
    // Asegurarse de que el formulario de actualización exista en la página
    if (formA.length > 0) {
        formA.on('submit', async function (e) {
            e.preventDefault(); // Evita el envío normal del formulario

            if (!validateStep(currentStep)) {
                return;
            }

            Swal.fire({
                title: 'Actualizando Caso...',
                text: 'Por favor, espere mientras se procesa su solicitud.',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            const formDataToSend = new FormData(this); // 'this' es el formulario HTML

            // --- Parte importante: Consolidar y Añadir AmbitoDocumento ---
            // Una sola lista para todos los nuevos documentos
            let allNewDocuments = [];

            // Recorre los documentos de cada categoría y añade el ámbito
            Object.keys(loadedDocuments).forEach(ambitoKey => {
                loadedDocuments[ambitoKey].forEach(doc => {
                    allNewDocuments.push({
                        TipoDocumentoId: doc.TipoDocumentoId,
                        File: doc.File,
                        Observaciones: doc.Observaciones,
                        AmbitoDocumento: ambitoKey.toUpperCase() // Asignas el ámbito en MAYÚSCULAS
                    });
                });
            });


            // Ahora, añade todos los documentos consolidados a 'NewDocuments' en el FormData
            // Asegúrate de que tu controlador C# espera una lista llamada 'NewDocuments'
            allNewDocuments.forEach((doc, index) => {
                formDataToSend.append(`NewDocuments[${index}].TipoDocumentoId`, doc.TipoDocumentoId);
                formDataToSend.append(`NewDocuments[${index}].File`, doc.File, doc.File.name); // El objeto File
                formDataToSend.append(`NewDocuments[${index}].Observaciones`, doc.Observaciones);
                formDataToSend.append(`NewDocuments[${index}].AmbitoDocumento`, doc.AmbitoDocumento); // ¡Aquí se envía el ámbito!
            });

            console.log("--- Contenido de formDataToSend (para depuración - Actualización) ---");
            for (let pair of formDataToSend.entries()) {
                const [key, value] = pair;
                if (value instanceof File) {
                    console.log(`${key}: File (Name: ${value.name}, Type: ${value.type}, Size: ${value.size} bytes)`);
                } else {
                    console.log(`${key}: ${value}`);
                }
            }
            console.log("--- Fin de la inspección ---");
            console.log("URL de envío para actualizar caso:", actualizarCasoApiUrl);

            try {
                const response = await fetch(actualizarCasoApiUrl, {
                    method: 'PUT', // Asegúrate de que el método HTTP sea PUT para modificar
                    headers: {
                        'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                    },
                    body: formDataToSend
                });

                if (response.ok) {
                    // Manejo de éxito, por ejemplo, redirigir
                    Swal.fire({
                        icon: 'success',
                        title: '¡Caso Actualizado!',
                        text: 'El caso ha sido modificado exitosamente.',
                        confirmButtonText: 'Aceptar'
                    }).then(() => {
                        // Podrías redirigir a una página de detalles del caso o a la lista de casos
                        // Intenta obtener la URL de redirección si la API la proporciona
                        // De lo contrario, usa un fallback predeterminado
                        window.location.href = response.redirectUrl || '/Casos/CasosRegistrados';
                    });
                } else {
                    const errorData = await response.json();
                    Swal.fire({
                        icon: 'error',
                        title: 'Error al Actualizar el Caso',
                        text: errorData.message || 'Ocurrió un error desconocido.',
                        footer: errorData.detail ? `<small>${errorData.detail}</small>` : '',
                        confirmButtonText: 'Aceptar'
                    });
                }
            } catch (error) {
                console.error("Error en la solicitud AJAX (actualizar caso):", error);
                Swal.fire({
                    icon: 'error',
                    title: 'Error de Conexión',
                    text: 'No se pudo conectar con el servidor. Inténtelo de nuevo. Detalles: ' + error.message,
                    confirmButtonText: 'Aceptar'
                });
            } finally {
                Swal.hideLoading();
            }
        });
    }

}); // Fin de la función $(document).ready