// Asegúrate de que jQuery, SweetAlert2 y Choices.js estén cargados antes de este script.

$(document).ready(function () {
    let currentStep = 1;
    const totalSteps = 2;
    const form = $('#crearCasoForm');
    const steps = $('.step');
    const btnNext = $('#btnNextStep');
    const btnBack = $('#btnBack');
    const btnAgregarArchivos = $('#btnAgregarArchivos');
    const archivosDocumentoInput = $('#archivosDocumento');
    const categoriaDocumentoSelect = $('#documentoTipo');
    const observacionesDocumentoTextarea = $('#observacionesDocumento');
    const listaDocumentosDiv = $('#listaDocumentos');
    const previewModal = new bootstrap.Modal(document.getElementById('previewModal'));
    const previewContent = $('#previewContent');

    // Array para almacenar los documentos cargados
    let loadedDocuments = {
        asegurado: [],
        caso: []
    };

    // Inyectar el UsuarioId desde el modelo Razor (definido en la vista .cshtml)
    // Asegúrate de que 'initialUsuarioId' esté disponible globalmente o se pase aquí.
    // Ejemplo: var initialUsuarioId = @Model.UsuarioId; en la vista.
    const usuarioIdFromModel = typeof initialUsuarioId !== 'undefined' ? initialUsuarioId : 1; // Usar 1 como fallback si no está inyectado

    // Inicializar Choices.js para los selects
    if (typeof Choices !== 'undefined') {
        new Choices(categoriaDocumentoSelect[0], {
            searchEnabled: false,
            removeItemButton: true,
            allowHTML: true // Permite optgroup labels
        });
        new Choices($('#CasoEstadoId')[0], {
            searchEnabled: false,
            removeItemButton: true
        });
    }

    // Función para mostrar el paso actual
    function showStep(stepNumber) {
        steps.addClass('d-none'); // Oculta todos los pasos
        $(`#step${stepNumber}`).removeClass('d-none'); // Muestra el paso actual
    }

    // Función para validar el paso actual (mejorada para dar feedback específico)
    function validateStep(stepNumber) {
        let isValid = true;
        let firstInvalidField = null;

        // Limpiar validaciones previas
        $(`#step${stepNumber} .is-invalid`).removeClass('is-invalid');
        $(`#step${stepNumber} .choices.is-invalid`).removeClass('is-invalid');

        // Validar campos de input y textarea
        $(`#step${stepNumber} :input[required]`).each(function () {
            if (!this.checkValidity()) {
                isValid = false;
                $(this).addClass('is-invalid');
                if (!firstInvalidField) firstInvalidField = this;
            }
        });

        // Validar selects de Choices.js
        $(`#step${stepNumber} select[required]`).each(function () {
            const choicesInstance = Choices.getInstance(this);
            if (choicesInstance && !choicesInstance.getValue(true)) {
                isValid = false;
                $(this).next('.choices').addClass('is-invalid'); // Añadir clase a la envoltura de Choices.js
                if (!firstInvalidField) firstInvalidField = this;
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

    // Manejar el botón Siguiente
    btnNext.on('click', function () {
        if (validateStep(currentStep)) {
            currentStep++;
            showStep(currentStep);
            // Si hay nuevos selects en el paso 2 que necesitan Choices.js, inicialízalos aquí
            // (En este caso, ya se inicializan al cargar la página, pero es un buen lugar para recordar)
        }
    });

    // Manejar el botón Volver
    btnBack.on('click', function () {
        currentStep--;
        showStep(currentStep);
    });

    // Mostrar el primer paso al cargar la página
    showStep(currentStep);

    // ====================================================================
    // Lógica de Carga de Documentos
    // ====================================================================

    // Función para leer un archivo como Base64
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
    btnAgregarArchivos.on('click', async function () {
        const tipoDocumentoId = categoriaDocumentoSelect.val();
        const tipoDocumentoText = categoriaDocumentoSelect.find('option:selected').text();
        const ambitoDocumento = categoriaDocumentoSelect.find('option:selected').data('ambito');
        const observaciones = observacionesDocumentoTextarea.val();
        const files = archivosDocumentoInput[0].files;

        if (!tipoDocumentoId || files.length === 0 || !ambitoDocumento) {
            Swal.fire({
                icon: 'warning',
                title: 'Campos Incompletos',
                text: 'Por favor, seleccione un tipo de documento y al menos un archivo.',
                confirmButtonText: 'Aceptar',
                confirmButtonColor: '#f7b84b'
            });
            return;
        }

        for (const file of files) {
            // Verificar si el archivo ya existe en la lista de documentos cargados para el mismo ámbito
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
                const contenidoBase64 = await readFileAsBase64(file);
                const newDoc = {
                    TipoDocumentoId: parseInt(tipoDocumentoId),
                    NombreArchivo: file.name,
                    ContenidoBase64: contenidoBase64,
                    Observaciones: observaciones,
                    AmbitoDocumento: ambitoDocumento
                };

                // Asignar un índice temporal para la UI, ya que splice puede cambiar los índices reales
                const uiIndex = loadedDocuments[ambitoDocumento].length;
                newDoc.uiIndex = uiIndex; // Guardar el índice en el array para facilitar la eliminación

                if (ambitoDocumento === 'ASEGURADO') {
                    loadedDocuments.asegurado.push(newDoc);
                } else if (ambitoDocumento === 'CASO') {
                    loadedDocuments.caso.push(newDoc);
                }

                // Añadir a la UI
                const docItem = `
                    <div class="col-md-4 col-sm-6 mb-3" data-ui-index="${uiIndex}" data-ambito="${ambitoDocumento}">
                        <div class="card border card-animate">
                            <div class="card-body">
                                <div class="d-flex align-items-center">
                                    <div class="flex-shrink-0 me-3">
                                        <i class="${getIconByType(file.type)} fs-2 text-primary"></i>
                                    </div>
                                    <div class="flex-grow-1">
                                        <h6 class="mb-1 text-truncate">${file.name}</h6>
                                        <small class="text-muted">${tipoDocumentoText} (${ambitoDocumento})</small>
                                    </div>
                                    <div class="flex-shrink-0">
                                        <button type="button" class="btn btn-sm btn-light p-0 remove-doc-btn" data-bs-toggle="tooltip" data-bs-placement="top" title="Eliminar">
                                            <i class="ri-delete-bin-line text-danger"></i>
                                        </button>
                                        <button type="button" class="btn btn-sm btn-light p-0 preview-doc-btn ms-1" data-bs-toggle="tooltip" data-bs-placement="top" title="Previsualizar">
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
        // Para Choices.js, usa la instancia para resetear
        const categoriaChoices = Choices.getInstance(categoriaDocumentoSelect[0]);
        if (categoriaChoices) {
            categoriaChoices.setChoiceByValue('');
        }
    });

    // Manejar eliminación de documentos de la UI y del array
    listaDocumentosDiv.on('click', '.remove-doc-btn', function () {
        const card = $(this).closest('.col-md-4');
        const uiIndex = card.data('ui-index'); // Usar el índice de la UI
        const ambito = card.data('ambito');

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
                // Encontrar el índice real en el array loadedDocuments
                const realIndex = loadedDocuments[ambito].findIndex(doc => doc.uiIndex === uiIndex);
                if (realIndex > -1) {
                    loadedDocuments[ambito].splice(realIndex, 1); // Eliminar del array
                }
                card.remove(); // Eliminar de la UI
                Swal.fire('Eliminado!', 'El documento ha sido removido de la lista.', 'success');
            }
        });
    });

    // Manejar previsualización de documentos
    listaDocumentosDiv.on('click', '.preview-doc-btn', function () {
        const card = $(this).closest('.col-md-4');
        const uiIndex = card.data('ui-index');
        const ambito = card.data('ambito');
        const doc = loadedDocuments[ambito].find(d => d.uiIndex === uiIndex); // Buscar por uiIndex

        if (!doc) {
            Swal.fire({
                icon: 'error',
                title: 'Error de Previsualización',
                text: 'Documento no encontrado en la memoria.',
                confirmButtonText: 'Aceptar'
            });
            return;
        }

        previewContent.empty(); // Limpiar contenido previo

        const fileExtension = doc.NombreArchivo.split('.').pop().toLowerCase();
        const base64Data = `data:${getFileMimeType(fileExtension)};base64,${doc.ContenidoBase64}`;

        if (['jpg', 'jpeg', 'png', 'gif'].includes(fileExtension)) {
            previewContent.append(`<img src="${base64Data}" class="img-fluid" style="max-height: 80vh;" alt="${doc.NombreArchivo}">`);
        } else if (fileExtension === 'pdf') {
            previewContent.append(`<iframe src="${base64Data}" width="100%" height="600px" style="border: none;"></iframe>`);
        } else {
            previewContent.append(`<p class="alert alert-warning">No se puede previsualizar este tipo de archivo: <strong>.${fileExtension}</strong></p>`);
        }
        previewModal.show();
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
    // Envío del Formulario (AJAX)
    // ====================================================================

    form.on('submit', async function (e) {
        e.preventDefault(); // Evita el envío normal del formulario

        // Validar el último paso antes de enviar
        if (!validateStep(currentStep)) {
            return; // validateStep ya muestra un Swal.fire
        }

        // Validar que se haya subido al menos un documento (si es requerido por tu lógica de negocio)
        const totalDocuments = loadedDocuments.asegurado.length + loadedDocuments.caso.length;
        if (totalDocuments === 0) {
            Swal.fire({
                icon: 'error',
                title: 'Documentos requeridos',
                text: 'Debes subir al menos un documento para continuar.',
                confirmButtonText: 'Aceptar',
                confirmButtonColor: '#f06548'
            });
            return;
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

        // Recolectar datos del formulario en un objeto DTO
        const formData = new FormData(this);
        const casoDto = {
            // Asegurado
            NombreCompleto: formData.get('NombreCompleto'),
            Identificacion: formData.get('Identificacion'),
            Telefono: formData.get('Telefono'),
            Email: formData.get('Email'),
            Direccion: formData.get('Direccion'),

            // Vehículo
            Placa: formData.get('Placa'),
            Marca: formData.get('Marca'),
            Modelo: formData.get('Modelo'),
            Transmision: formData.get('Transmision'),
            Combustible: formData.get('Combustible'),
            Cilindraje: formData.get('Cilindraje'),
            Anio: formData.get('Anio') ? parseInt(formData.get('Anio')) : null,
            NumeroChasis: formData.get('NumeroChasis'),
            NumeroMotor: formData.get('NumeroMotor'),
            TipoVehiculo: formData.get('TipoVehiculo'),
            Clase: formData.get('Clase'),
            Color: formData.get('Color'),
            ObservacionesVehiculo: formData.get('ObservacionesVehiculo'),

            // Caso
            NumeroAvaluo: formData.get('NumeroAvaluo'),
            NumeroReclamo: formData.get('NumeroReclamo'),
            FechaSiniestro: formData.get('FechaSiniestro'),
            CasoEstadoId: parseInt(formData.get('CasoEstadoId')),

            // Documentos (ya están en loadedDocuments)
            DocumentosAsegurado: loadedDocuments.asegurado.map(doc => ({
                TipoDocumentoId: doc.TipoDocumentoId,
                NombreArchivo: doc.NombreArchivo,
                ContenidoBinario: doc.ContenidoBase64, // Enviar como Base64
                Observaciones: doc.Observaciones
            })),
            DocumentosCaso: loadedDocuments.caso.map(doc => ({
                TipoDocumentoId: doc.TipoDocumentoId,
                NombreArchivo: doc.NombreArchivo,
                ContenidoBinario: doc.ContenidoBase64, // Enviar como Base64
                Observaciones: doc.Observaciones
            })),

            // Auditoría
            UsuarioId: usuarioIdFromModel // Usar el UsuarioId inyectado desde el modelo
        };

        try {
            const response = await fetch('/api/Caso/crear', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    // Si usas AntiForgeryToken, descomenta y asegúrate de que el token esté en la vista
                    // 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                body: JSON.stringify(casoDto)
            });

            if (response.ok) {
                // El controlador redirige con TempData, por lo que el navegador seguirá la redirección.
                // SweetAlert2 se mostrará en la página de destino.
                window.location.href = response.url;
            } else {
                // Si el controlador devuelve un error HTTP (ej. 400, 401, 409, 500)
                // y no redirige, entonces procesamos la respuesta JSON de error.
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

    // Script para la validación de Bootstrap 5 (se mantiene para la validación HTML5)
    var forms = document.querySelectorAll('.needs-validation');
    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('submit', function (event) {
                if (!form.checkValidity()) {
                    event.preventDefault();
                    event.stopPropagation();
                }
                form.classList.add('was-validated');
            }, false);
        });
});
