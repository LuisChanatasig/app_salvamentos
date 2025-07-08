$(document).ready(function () {
    // Variables globales
    const form = $('#crearCasoForm');
    const steps = $('.step');
    let currentStep = 1;

    // Objeto para almacenar los documentos cargados (ahora guarda el objeto File)
    // Se usan arrays separados para cada tipo de documento para facilitar el mapeo a FormData
    let loadedDocuments = {
        asegurado: [], // [{ TipoDocumentoId: int, File: FileObject, Observaciones: string }]
        caso: [],      // [{ TipoDocumentoId: int, File: FileObject, Observaciones: string }]
        fotosDano: [], // [{ TipoDocumentoId: int, File: FileObject, Observaciones: string }]
        valoresComercialesFiles: [] // [{ TipoDocumentoId: int, File: FileObject, Observaciones: string }]
    };

    // Referencias a elementos del formulario de documentos
    const documentoTipoSelect = $('#documentoTipo');
    const archivosDocumentoInput = $('#archivosDocumento');
    const observacionesDocumentoTextarea = $('#observacionesDocumento');
    const listaDocumentosDiv = $('#listaDocumentos');

    // Inyectar el UsuarioId desde el modelo Razor (definido en la vista .cshtml)
    const usuarioIdFromModel = typeof initialUsuarioId !== 'undefined' ? initialUsuarioId : 1; // Usar 1 como fallback si no está inyectado

    // Variable para la URL de creación de casos, que será inyectada desde la vista Razor
    // Asegúrate de definir 'crearCasoUrl' en tu vista .cshtml antes de cargar este script.
    const crearCasoApiUrl = typeof crearCasoUrl !== 'undefined' ? crearCasoUrl : '/Casos/CrearCaso';


    // ====================================================================
    // Funciones de Navegación entre Pasos
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
                $(this).removeClass('is-valid');
                if (!firstInvalidField) firstInvalidField = this;
            } else {
                $(this).addClass('is-valid');
                $(this).removeClass('is-invalid');
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
    // Manejo de Documentos (Carga y Visualización)
    // ====================================================================

    // Función para leer un archivo como Base64 (para previsualización)
    function readFileAsBase64(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result.split(',')[1]); // Solo la parte Base64
            reader.onerror = error => reject(error);
            reader.readAsDataURL(file);
        });
    }

    // Obtener ícono según tipo de archivo
    function getIconByType(type) {
        if (type.includes('pdf')) return 'ri-file-pdf-line';
        if (type.includes('word') || type.includes('document')) return 'ri-file-word-line';
        if (type.includes('excel') || type.includes('spreadsheet')) return 'ri-file-excel-line';
        if (type.includes('image')) return 'ri-image-line';
        return 'ri-file-line';
    }

    // Función para añadir documentos a la lista
    $('#btnAgregarArchivos').on('click', async function () {
        const tipoDocumentoId = documentoTipoSelect.val();
        const tipoDocumentoText = documentoTipoSelect.find('option:selected').text();
        const selectedOptionElement = documentoTipoSelect.find('option:selected');

        let ambitoDocumentoRaw = selectedOptionElement.attr('data-ambito');
        let ambitoDocumento = ambitoDocumentoRaw ? ambitoDocumentoRaw.toLowerCase() : undefined;

        // Validar que se haya seleccionado una opción de tipo de documento
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

        // Validar que ambitoDocumento sea 'asegurado' o 'caso' (en minúsculas)
        if (ambitoDocumento !== 'asegurado' && ambitoDocumento !== 'caso') {
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

        // Validar que se haya seleccionado al menos un archivo
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
                continue; // Saltar este archivo y pasar al siguiente
            }

            try {
                const newDoc = {
                    TipoDocumentoId: parseInt(tipoDocumentoId),
                    NombreArchivo: file.name,
                    File: file, // Guardamos el objeto File directamente
                    Observaciones: observaciones,
                    AmbitoDocumento: ambitoDocumentoRaw
                };

                // Asignar un índice temporal para la UI
                const uiIndex = loadedDocuments[ambitoDocumento].length;
                newDoc.uiIndex = uiIndex;

                if (ambitoDocumento === 'asegurado') {
                    loadedDocuments.asegurado.push(newDoc);
                } else if (ambitoDocumento === 'caso') {
                    loadedDocuments.caso.push(newDoc);
                }

                // Añadir a la UI
                const docItem = `
                    <div class="col-md-4 col-sm-6 mb-3" data-ui-index="${uiIndex}" data-ambito="${ambitoDocumentoRaw}">
                        <div class="card border card-animate">
                            <div class="card-body">
                                <div class="d-flex align-items-center">
                                    <div class="flex-shrink-0 me-3">
                                        <i class="${getIconByType(file.type)} fs-2 text-primary"></i>
                                    </div>
                                    <div class="flex-grow-1">
                                        <h6 class="mb-1 text-truncate">${file.name}</h6>
                                        <small class="text-muted">${tipoDocumentoText} (${ambitoDocumentoRaw})</small>
                                    </div>
                                    <div class="flex-shrink-0">
                                        <button type="button" class="btn btn-sm btn-light p-0 remove-doc-btn" data-bs-toggle="tooltip" data-bs-placement="top" title="Eliminar">
                                            <i class="ri-delete-bin-line text-danger"></i>
                                        </button>
                                        <button type="button" class="btn btn-sm btn-light p-0 preview-doc-btn ms-1" data-bs-toggle="tooltip" data-bs-placement="top" title="Previsualizar" data-file-type="${file.type}">
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

                // Re-inicializar tooltips para los nuevos elementos
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
                console.error("Error al leer archivo:", error);
                Swal.fire({
                    icon: 'error',
                    title: 'Error de Archivo',
                    text: `No se pudo leer el archivo ${file.name}.`,
                    confirmButtonText: 'Aceptar'
                });
            }
        }

        // Limpiar campos después de agregar
        archivosDocumentoInput.val('');
        observacionesDocumentoTextarea.val('');
        documentoTipoSelect.val(''); // Limpiar select estándar
    });

    // Manejar eliminación de documentos de la UI y del array
    listaDocumentosDiv.on('click', '.remove-doc-btn', function () {
        const card = $(this).closest('.col-md-4');
        const uiIndex = card.data('ui-index');
        let ambitoRaw = card.data('ambito');
        let ambito = ambitoRaw ? ambitoRaw.toLowerCase() : undefined;

        if (ambito !== 'asegurado' && ambito !== 'caso') {
            console.error("Error al eliminar: Ámbito de documento inválido o indefinido en la UI. Valor obtenido:", ambitoRaw);
            Swal.fire({
                icon: 'error',
                title: 'Error Interno',
                text: 'No se pudo determinar el tipo de documento para eliminar. Por favor, recargue la página.',
                confirmButtonText: 'Aceptar'
            });
            return;
        }

        Swal.fire({
            title: '¿Estás seguro?',
            text: "El documento se eliminará de la lista de carga.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Sí, eliminarlo!',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed) {
                const realIndex = loadedDocuments[ambito].findIndex(doc => doc.uiIndex === uiIndex);
                if (realIndex > -1) {
                    loadedDocuments[ambito].splice(realIndex, 1);
                }
                card.remove();
                Swal.fire('Eliminado!', 'El documento ha sido removido de la lista.', 'success');
            }
        });
    });

    // Manejar previsualización de documentos
    listaDocumentosDiv.on('click', '.preview-doc-btn', function () {
        const card = $(this).closest('.col-md-4');
        const uiIndex = card.data('ui-index');
        let ambitoRaw = card.data('ambito');
        let ambito = ambitoRaw ? ambitoRaw.toLowerCase() : undefined;

        if (ambito !== 'asegurado' && ambito !== 'caso') {
            console.error("Error al previsualizar: Ámbito de documento inválido o indefinido en la UI. Valor obtenido:", ambitoRaw);
            Swal.fire({
                icon: 'error',
                title: 'Error Interno',
                text: 'No se pudo determinar el tipo de documento para previsualizar. Por favor, recargue la página.',
                confirmButtonText: 'Aceptar'
            });
            return;
        }

        const doc = loadedDocuments[ambito].find(d => d.uiIndex === uiIndex);

        if (!doc || !doc.File) { // Asegurarse de que el objeto File exista
            Swal.fire({
                icon: 'error',
                title: 'Error de Previsualización',
                text: 'Documento no encontrado en la memoria o archivo no disponible.',
                confirmButtonText: 'Aceptar'
            });
            return;
        }

        const file = doc.File;
        const reader = new FileReader();
        const previewContent = $('#previewContent');
        previewContent.empty();

        reader.onload = function (e) {
            if (file.type.startsWith('image/')) {
                previewContent.append(`<img src="${e.target.result}" class="img-fluid" style="max-height: 80vh;" alt="Previsualización de imagen">`);
            } else if (file.type === 'application/pdf') {
                previewContent.append(`<embed src="${e.target.result}" type="application/pdf" width="100%" height="600px" />`);
            } else {
                previewContent.append('<p class="alert alert-warning">No se puede previsualizar este tipo de archivo.</p>');
            }
            // Mostrar el modal después de cargar el contenido
            new bootstrap.Modal(document.getElementById('previewModal')).show();
        };
        reader.readAsDataURL(file);
    });

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
    // Envío del Formulario (AJAX con FormData)
    // ====================================================================

    form.on('submit', async function (e) {
        e.preventDefault(); // Evita el envío normal del formulario

        // Validar el último paso antes de enviar
        if (!validateStep(currentStep)) {
            return; // validateStep ya muestra un Swal.fire
        }

        // REMOVIDO: Validación para que se haya subido al menos un documento.
        // Los documentos ahora son opcionales.
        // La lógica original era:
        // const totalDocuments = loadedDocuments.asegurado.length + loadedDocuments.caso.length;
        // if (totalDocuments === 0) {
        //     Swal.fire({
        //         icon: 'error',
        //         title: 'Documentos requeridos',
        //         text: 'Debes subir al menos un documento para continuar.',
        //         confirmButtonText: 'Aceptar',
        //         confirmButtonColor: '#f06548'
        //     });
        //     return;
        // }

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

        // NOTA: Si tienes arrays separados en loadedDocuments para FotosDano y ValoresComercialesFiles
        // y quieres que se mapeen a propiedades separadas en CrearCasoInputDto,
        // deberías iterar sobre ellos de manera similar.
        // Por ejemplo, si loadedDocuments.fotosDano existe:
        // loadedDocuments.fotosDano.forEach((doc, index) => {
        //     formDataToSend.append(`FotosDano[${index}].TipoDocumentoId`, doc.TipoDocumentoId);
        //     formDataToSend.append(`FotosDano[${index}].File`, doc.File, doc.File.name);
        //     formDataToSend.append(`FotosDano[${index}].Observaciones`, doc.Observaciones);
        // });
        // Y lo mismo para ValoresComercialesFiles.

        // Añadir el UsuarioId inyectado desde el modelo
        formDataToSend.append('UsuarioId', usuarioIdFromModel);

        try {
            const response = await fetch(crearCasoApiUrl, {
                method: 'POST',
                // NO establecer Content-Type. El navegador lo establecerá automáticamente para FormData.
                // Si usas AntiForgeryToken, descomenta y asegúrate de que el token esté en la vista
                // headers: {
                //     'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                // },
                body: formDataToSend // Enviar el objeto FormData directamente
            });

            if (response.ok) {
                // El controlador redirige con TempData, por lo que el navegador seguirá la redirección.
                // SweetAlert2 se mostrará en la página de destino.
                window.location.href = response.url;
            } else {
                // Si el controlador devuelve un error HTTP (ej. 400, 401, 409, 500)
                // y no redirige, entonces procesamos la respuesta JSON de error.
                // Asegúrate de que tu controlador devuelva un JSON de error si no hay redirección.
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
            console.error("Error en la solicitud AJAX:", error);
            Swal.fire({
                icon: 'error',
                title: 'Error de Conexión',
                text: 'No se pudo conectar con el servidor. Inténtelo de nuevo.',
                confirmButtonText: 'Aceptar'
            });
        } finally {
            Swal.hideLoading(); // Ocultar spinner
        }
    });
});
