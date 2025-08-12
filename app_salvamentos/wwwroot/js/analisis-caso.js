$(document).ready(function () {

    window.loadedValorFiles = {};   // un objeto, en lugar de un array
    let loadedDamagePhotos = [];

    function parseDecimal(value) {
        if (value === null || value === undefined) return 0;
        const cleanedValue = String(value).replace(/\./g, '').replace(/,/g, '.');
        const parsed = parseFloat(cleanedValue);
        return isNaN(parsed) ? 0 : parsed;
    }

    function readFileAsBase64(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result.split(',')[1]); // Solo la parte Base64
            reader.onerror = error => reject(error);
            reader.readAsDataURL(file);
        });
    }

    // Referencias a elementos del formulario de documentos
    const categoriaDocumentoSelect = $('#categoriaDocumento');
    const archivosDocumentoInput = $('#archivosDocumento');
    const observacionesDocumentoTextarea = $('#observacionesDocumento');
    const listaDocumentosDiv = $('#listaDocumentos'); // Aquí es donde se añaden las tarjetas de documentos

    // Instancia del modal de Bootstrap para previsualización
    const previewContent = $('#previewContent');
    const previewModal = new bootstrap.Modal(document.getElementById('previewModal'));

  
    // ====================================================================================
    // Lógica de Carga y Gestión de Documentos Generales (Caso, Asegurado, etc.)
    // ====================================================================================

    // Precargar documentos existentes en la UI (si loadedDocuments ya tiene datos)
    function loadExistingDocumentsToUI() {
        listaDocumentosDiv.empty(); // Limpiar el contenedor antes de recargar, para evitar duplicados en actualizaciones

        Object.keys(window.loadedDocuments).forEach(ambitoKey => {
            window.loadedDocuments[ambitoKey].forEach(doc => {
                // `doc` ya viene normalizado desde el .cshtml (uiIndex, tipo_mime, nombre_tipo_documento, ambito_documento)
                const fileMime = doc.tipo_mime;
                const docItem = `
                <div class="col-md-4 col-sm-6 mb-3 fade-in-up" data-ui-index="${doc.uiIndex}" data-doc-id="${doc.documento_id || ''}" data-ambito="${doc.ambito_documento.toLowerCase()}">
                    <div class="card border card-animate">
                        <div class="card-body">
                            <div class="d-flex align-items-center">
                                <div class="flex-shrink-0 me-3">
                                    <i class="${getIconByType(fileMime)} fs-2 ${getColorByType(fileMime)}"></i>
                                </div>
                                <div class="flex-grow-1">
                                    <h6 class="mb-1 text-truncate" style="max-width: 150px;">${doc.nombre_archivo}</h6>
                                    <small class="text-muted">${doc.nombre_tipo_documento} (${doc.ambito_documento})</small>
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
                            ${doc.observaciones ? `<p class="text-muted mt-2 mb-0 text-wrap"><small>Obs: ${doc.observaciones}</small></p>` : ''}
                        </div>
                    </div>
                </div>
                `;
                listaDocumentosDiv.append(docItem);
            });
        });
        // Re-inicializa los tooltips de Bootstrap después de añadir todos los elementos
        $('[data-bs-toggle="tooltip"]').tooltip("dispose").tooltip();
    }

    // Llamar a la función para cargar documentos existentes al iniciar (ahora es seguro)
    loadExistingDocumentsToUI();


    $('#btnAgregarDoc').on('click', async function () {
        const tipoDocumentoId = categoriaDocumentoSelect.val();
        const tipoDocumentoText = categoriaDocumentoSelect.find("option:selected").text();
        const selectedOptionElement = categoriaDocumentoSelect.find("option:selected");

        // Lee el ámbito del atributo data-ambito, convirtiéndolo a minúsculas para coincidir con las claves de loadedDocuments
        let ambitoFijo = selectedOptionElement.data("ambito") ? selectedOptionElement.data("ambito").toLowerCase() : 'caso'; // Default a 'caso' si no hay ámbito

        // Validaciones iniciales
        if (!tipoDocumentoId) {
            categoriaDocumentoSelect.addClass("is-invalid");
            Swal.fire({ icon: "warning", title: "Selección Requerida", text: "Por favor, seleccione un tipo de documento de la lista.", confirmButtonText: "Aceptar", confirmButtonColor: "#f7b84b", });
            return;
        } else {
            categoriaDocumentoSelect.removeClass("is-invalid");
        }

        const observaciones = observacionesDocumentoTextarea.val();
        const files = archivosDocumentoInput[0].files;

        if (files.length === 0) {
            Swal.fire({ icon: "warning", title: "Archivos Requeridos", text: "Por favor, seleccione al menos un archivo para subir.", confirmButtonText: "Aceptar", confirmButtonColor: "#f7b84b", });
            return;
        }

        // --- Procesamiento de archivos seleccionados ---
        for (const file of files) {
            const uiId = crypto.randomUUID(); // Generar un ID único para la UI

            // Re-evaluar el ámbito si hay lógica específica por tipoDocumentoId
            // Esto sobrescribe el ámbito del data-ambito si hay una regla específica aquí
            let finalAmbito = ambitoFijo; // Empezamos con el ámbito del data-ambito
            if (parseInt(tipoDocumentoId) === 6) { // ID para 'Fotos del Siniestro'
                finalAmbito = "dano";
            } else if (parseInt(tipoDocumentoId) === 13) { // ID para 'Evidencia de Valores Comerciales'
                finalAmbito = "valorcomercial";
            }
            // Importante: convertir a minúsculas para coincidir con las claves de window.loadedDocuments
            finalAmbito = finalAmbito.toLowerCase();


            if (!window.loadedDocuments.hasOwnProperty(finalAmbito)) {
                console.error(`Error: El ámbito '${finalAmbito}' no es una categoría válida en loadedDocuments.`);
                Swal.fire({
                    icon: 'error',
                    title: 'Error Interno',
                    text: 'Error en la configuración del tipo de documento. Contacte a soporte.',
                    confirmButtonText: 'Aceptar'
                });
                return;
            }

            const yaExiste = window.loadedDocuments[finalAmbito].some(
                doc => (doc.nombre_archivo === file.name || (doc.File && doc.File.name === file.name)) &&
                    doc.tipo_documento_id === parseInt(tipoDocumentoId)
            );

            if (yaExiste) {
                Swal.fire({ icon: "info", title: "Archivo Duplicado", text: `El archivo "${file.name}" con este tipo de documento ya ha sido añadido en el ámbito ${finalAmbito}.`, confirmButtonText: "Aceptar", confirmButtonColor: "#f7b84b", });
                continue; // Saltar este archivo
            }

            try {
                const newDoc = {
                    tipo_documento_id: parseInt(tipoDocumentoId),
                    nombre_tipo_documento: tipoDocumentoText,
                    nombre_archivo: file.name,
                    observaciones: observaciones,
                    ambito_documento: finalAmbito.toUpperCase(), // Guardar en mayúsculas para backend si lo espera así
                    File: file, // **IMPORTANTE**: Guardar el objeto File original
                    uiIndex: uiId,
                    tipo_mime: file.type // El tipo MIME del File es lo más preciso
                };

                window.loadedDocuments[finalAmbito].push(newDoc);
                console.log(`Documento '${file.name}' agregado al ámbito '${finalAmbito}'.`);
                console.log("Estado actual de loadedDocuments:", window.loadedDocuments);

                // --- Añadir a la UI (card Bootstrap) ---
                const docItem = `
                <div class="col-md-4 col-sm-6 mb-3 fade-in-up" data-ui-index="${uiId}" data-ambito="${finalAmbito}">
                    <div class="card border card-animate">
                        <div class="card-body">
                            <div class="d-flex align-items-center">
                                <div class="flex-shrink-0 me-3">
                                    <i class="${getIconByType(newDoc.tipo_mime)} fs-2 ${getColorByType(newDoc.tipo_mime)}"></i>
                                </div>
                                <div class="flex-grow-1">
                                    <h6 class="mb-1 text-truncate">${newDoc.nombre_archivo}</h6>
                                    <small class="text-muted">${newDoc.nombre_tipo_documento} (${newDoc.ambito_documento})</small>
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
                            ${newDoc.observaciones ? `<p class="text-muted mt-2 mb-0 text-wrap"><small>Obs: ${newDoc.observaciones}</small></p>` : ''}
                        </div>
                    </div>
                </div>
                `;
                listaDocumentosDiv.append(docItem);

                // Re-inicializar tooltips para los nuevos elementos
                $('[data-bs-toggle="tooltip"]').tooltip('dispose').tooltip();

                Swal.fire({ toast: true, position: "top-end", icon: "success", title: "Archivo agregado", showConfirmButton: false, timer: 1500, });

            } catch (error) {
                console.error("Error al procesar archivo:", error);
                Swal.fire({ icon: "error", title: "Error de Archivo", text: `No se pudo procesar el archivo ${file.name}.`, confirmButtonText: "Aceptar", });
            }
        }

        // Limpiar campos después de agregar todos los archivos
        archivosDocumentoInput.val('');
        observacionesDocumentoTextarea.val('');
        categoriaDocumentoSelect.val('');
    });


    // ====================================================================
    // Evento: Eliminar Documentos (Delegación de Eventos)
    // ====================================================================

    listaDocumentosDiv.on("click", ".remove-doc-btn", function () {
        const card = $(this).closest(".col-md-4");
        const uiId = card.data("ui-index");
        const documentoId = card.data("doc-id"); // Para documentos ya guardados en DB
        const ambito = card.data("ambito"); // El ámbito del documento (ej. 'caso', 'asegurado')

        Swal.fire({
            title: "¿Estás seguro?",
            text: "¡El documento será eliminado!",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            cancelButtonColor: "#3085d6",
            confirmButtonText: "Sí, eliminarlo!",
            cancelButtonText: "Cancelar",
        }).then((result) => {
            if (result.isConfirmed) {
                if (documentoId) {
                    // Si tiene documentoId, es un documento guardado en la BD
                    $.ajax({
                        url: `${API_DOCUMENTOS_BASE_URL}?documentoId=${documentoId}`, // Endpoint en C#
                        type: "POST",
                        headers: {
                            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() // Si usas anti-forgery tokens
                        },
                        success: function (response) {
                            card.remove(); // Elimina la tarjeta de la UI
                            // Eliminar del array loadedDocuments en memoria
                            if (ambito && window.loadedDocuments.hasOwnProperty(ambito)) {
                                window.loadedDocuments[ambito] = window.loadedDocuments[ambito].filter(doc => doc.documento_id !== documentoId);
                                console.log(`Documento con ID ${documentoId} eliminado (DB) del ámbito ${ambito}.`);
                            }
                            Swal.fire("Eliminado!", "El documento ha sido eliminado de la base de datos.", "success");
                        },
                        error: function (xhr, status, error) {
                            console.error("Error al eliminar documento de la BD:", xhr.responseText);
                            const errorMessage = xhr.responseJSON && xhr.responseJSON.message ? xhr.responseJSON.message : "Hubo un problema al eliminar el documento de la base de datos.";
                            Swal.fire("Error!", errorMessage, "error");
                        }
                    });
                } else if (typeof uiId !== 'undefined') {
                    // Si solo tiene uiId, es un documento agregado localmente (en memoria)
                    if (ambito && window.loadedDocuments.hasOwnProperty(ambito)) {
                        window.loadedDocuments[ambito] = window.loadedDocuments[ambito].filter(doc => doc.uiIndex !== uiId);
                        console.log(`Documento con uiId ${uiId} eliminado (memoria) del ámbito ${ambito}.`);
                    }
                    card.remove(); // Elimina la tarjeta de la UI
                    Swal.fire("Eliminado!", "El documento ha sido removido de la lista temporal.", "success");
                } else {
                    Swal.fire("Error!", "No se pudo identificar el documento para eliminar.", "error");
                }
            }
        });
    });

    // ====================================================================
    // Evento: Previsualizar Documentos (Delegación de Eventos)
    // ====================================================================

    listaDocumentosDiv.on("click", ".preview-doc-btn", function () {
        const card = $(this).closest(".col-md-4");
        const uiId = card.data("ui-index");
        const documentoId = card.data("doc-id"); // Este es el ID de la DB
        const ambito = card.data("ambito");

        let doc;
        // Buscar el documento en el ámbito correcto
        if (ambito && window.loadedDocuments.hasOwnProperty(ambito)) {
            if (typeof uiId !== 'undefined') {
                doc = window.loadedDocuments[ambito].find((d) => d.uiIndex === uiId);
            } else if (documentoId) {
                doc = window.loadedDocuments[ambito].find((d) => d.documento_id === documentoId);
            }
        }

        if (!doc) {
            Swal.fire({ icon: "error", title: "Error de Previsualización", text: "Documento no encontrado en la memoria o no se pudo identificar.", confirmButtonText: "Aceptar", });
            return;
        }

        previewContent.empty(); // Limpiar contenido previo del modal

        let previewSource;
        let fileMime = doc.tipo_mime; // Ya debería venir normalizado
        let fileName = doc.nombre_archivo;

        // PRIORIDAD 1: Documentos recién agregados (en memoria, tienen el objeto File)
        if (doc.File) {
            fileMime = doc.File.type; // Obtiene el tipo MIME directamente del objeto File (más preciso)
            const reader = new FileReader();
            reader.onload = function (e) {
                previewSource = e.target.result;
                displayPreview(fileMime, previewSource, fileName);
                previewModal.show();
            };
            reader.readAsDataURL(doc.File);
            return; // Salir para esperar que FileReader termine
        }
        // PRIORIDAD 2: Documentos ya guardados en DB (tienen documento_id y ruta_fisica)
        else if (doc.documento_id && doc.ruta_fisica) {
            // Genera la URL para obtener el archivo desde el servidor (tu controlador)
            previewSource = `${API_CASOS_BASE_URL}?rutaRelativa=${encodeURIComponent(doc.ruta_fisica)}`;
        }
        else {
            Swal.fire({ icon: "error", title: "Error de Previsualización", text: "No se encontró contenido o URL para previsualizar este documento.", confirmButtonText: "Aceptar", });
            return;
        }

        // Si llega aquí, significa que la fuente de previsualización ya fue determinada (desde RutaPublica o ruta_fisica)
        displayPreview(fileMime, previewSource, fileName);
        previewModal.show();
    });

    // ====================================================================
    // Función displayPreview
    // ====================================================================
    function displayPreview(mimeType, source, fileName) {
        previewContent.empty(); // Limpiar antes de añadir nuevo contenido

        if (mimeType.includes("image")) {
            previewContent.append(`<img src="${source}" class="img-fluid" style="max-height: 80vh;" alt="${fileName}">`);
        } else if (mimeType.includes("pdf")) {
            previewContent.append(`<iframe src="${source}" width="100%" height="600px" style="border: none;"></iframe>`);
        } else {
            previewContent.append(`<p class="alert alert-warning">No se puede previsualizar este tipo de archivo: <strong>.${fileName.split(".").pop().toLowerCase()}</strong></p><a href="${source}" target="_blank" class="btn btn-primary mt-2">Descargar</a>`);
        }
    }

    // ====================================================================
    // Lógica para la sección de Valores Comerciales
    // ====================================================================

    const valorAseguradoInput = document.getElementById("valorAsegurado");
    const valorMatriculaPendienteInput = document.getElementById(
        "valorMatriculaPendiente"
    );
    const valorPatioTuercaInput = document.getElementById("valorPatioTuerca");
    const valorMarketplaceInput = document.getElementById("valorMarketplace");
    const valorHugoVargasInput = document.getElementById("valorHugoVargas");
    const valorAEADEInput = document.getElementById("valorAEADE");
    const valorOtrosInput = document.getElementById("valorOtros");
    const promedioCalculadoInput = document.getElementById("promedioCalculado");
    const promedioNetoInput = document.getElementById("promedioNeto");
    const precioComercialSugerido = document.getElementById("precioComercialSugerido"); // Asegúrate de que este elemento existe
    const precioEstimadoVentaVehiculo = document.getElementById("precioEstimadoVentaVehiculo"); // Asegúrate de que este elemento existe


    // Asegúrate de que `loadedValorFiles` está declarado al inicio del script.
    // Esto ya está resuelto con la declaración global `let loadedValorFiles = {...};`

    $(".file-input-valor").on("change", async function () {
        const inputId = $(this).attr("id");
        const file = this.files[0];
        const previewBtn = $(`button[data-file-id="${inputId}"].preview-valor-btn`);
        const removeBtn = $(`button[data-file-id="${inputId}"].remove-valor-btn`);
        const fileNameDisplay = $(`#fileName${inputId.replace("file", "")}`);

        if (file) {
            try {
                const contenidoBase64 = await readFileAsBase64(file);
               
                fileNameDisplay.text(file.name);
                previewBtn.removeClass("d-none");
                removeBtn.removeClass("d-none");
                Swal.fire({
                    toast: true,
                    position: "top-end",
                    icon: "success",
                    title: `Archivo ${file.name} cargado`,
                    showConfirmButton: false,
                    timer: 1500,
                });
            } catch (error) {
                console.error("Error al leer archivo de valor comercial:", error);
                Swal.fire({
                    icon: "error",
                    title: "Error de Archivo",
                    text: `No se pudo leer el archivo ${file.name}.`,
                    confirmButtonText: "Aceptar",
                });
                $(this).val("");
                loadedValorFiles[inputId] = null;
                fileNameDisplay.text("");
                previewBtn.addClass("d-none");
                removeBtn.addClass("d-none");
            }
        } else {
            $(this).val("");
            loadedValorFiles[inputId] = null;
            fileNameDisplay.text("");
            previewBtn.addClass("d-none");
            removeBtn.addClass("d-none");
        }
    });

    $(".preview-valor-btn").on("click", function () {
        const fileId = $(this).data("file-id");
        const fileData = loadedValorFiles[fileId];

        if (!fileData) {
            Swal.fire({
                icon: "error",
                title: "Error de Previsualización",
                text: "Archivo no encontrado en la memoria.",
                confirmButtonText: "Aceptar",
            });
            return;
        }

        previewContent.empty();
        const base64Data = `data:${fileData.mime_type};base64,${fileData.contenido_base64}`;

        if (fileData.mime_type.includes("image")) {
            previewContent.append(
                `<img src="${base64Data}" class="img-fluid" style="max-height: 80vh;" alt="${fileData.nombre_archivo}">`
            );
        } else if (fileData.mime_type.includes("pdf")) {
            previewContent.append(
                `<iframe src="${base64Data}" width="100%" height="600px" style="border: none;"></iframe>`
            );
        } else {
            previewContent.append(
                `<p class="alert alert-warning">No se puede previsualizar este tipo de archivo: <strong>${fileData.nombre_archivo.split(
                    "."
                )
                    .pop()
                    .toLowerCase()}</strong></p>`
            );
        }
        previewModal.show();
    });

    $(".remove-valor-btn").on("click", function () {
        const fileId = $(this).data("file-id");
        const fileInput = $(`#${fileId}`);
        const previewBtn = $(`button[data-file-id="${fileId}"].preview-valor-btn`);
        const removeBtn = $(`button[data-file-id="${fileId}"].remove-valor-btn`);
        const fileNameDisplay = $(`#fileName${fileId.replace("file", "")}`);

        Swal.fire({
            title: "¿Estás seguro?",
            text: "El archivo se eliminará de la carga.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#3085d6",
            cancelButtonColor: "#d33",
            confirmButtonText: "Sí, eliminarlo!",
            cancelButtonText: "Cancelar",
        }).then((result) => {
            if (result.isConfirmed) {
                fileInput.val("");
                loadedValorFiles[fileId] = null;
                fileNameDisplay.text("");
                previewBtn.addClass("d-none");
                removeBtn.addClass("d-none");
                Swal.fire("Eliminado!", "El archivo ha sido removido.", "success");
            }
        });
    });

    function recalcularValoresComerciales() {
        let sumValoresComerciales = 0;
        let countValoresComerciales = 0;

        // **CORRECCIÓN:** Usa parseDecimal para obtener los valores numéricos
        const valoresComerciales = [
            parseDecimal(valorPatioTuercaInput.value),
            parseDecimal(valorMarketplaceInput.value),
            parseDecimal(valorHugoVargasInput.value),
            parseDecimal(valorAEADEInput.value),
            parseDecimal(valorOtrosInput.value),
        ];

        valoresComerciales.forEach((val) => {
            if (val > 0) { // Solo si el valor es positivo, lo incluimos en el promedio
                sumValoresComerciales += val;
                countValoresComerciales++;
            }
        });

        let promedio = 0;
        if (countValoresComerciales > 0) {
            promedio = sumValoresComerciales / countValoresComerciales;
        }
        promedioCalculadoInput.value = promedio.toFixed(2);

        let promedioV = 0;
        const precioComercialSugeridoVal = parseDecimal(precioComercialSugerido.value); // **CORRECCIÓN**
        if (countValoresComerciales > 0 && precioComercialSugeridoVal >= 0) { // **CORRECCIÓN**
            promedioV = promedio - precioComercialSugeridoVal; // **CORRECCIÓN**
        }

        // Asegúrate de que precioEstimadoVentaVehiculo es un elemento HTML válido
        if (precioEstimadoVentaVehiculo) {
            precioEstimadoVentaVehiculo.value = promedioV.toFixed(2);
        }


        const valorMatricula = parseDecimal(valorMatriculaPendienteInput.value); // **CORRECCIÓN**
        const promedioNetoCalc = promedio - valorMatricula; // Variable diferente para evitar conflicto con promedioNetoInput
        promedioNetoInput.value = promedioNetoCalc.toFixed(2);

        actualizarResumenFinal();
    }

    valorMatriculaPendienteInput.addEventListener(
        "input",
        recalcularValoresComerciales
    );
    valorAseguradoInput.addEventListener("input", actualizarResumenFinal);

    document.querySelectorAll(".valor-comercial-input").forEach((input) => {
        input.addEventListener("input", recalcularValoresComerciales);
    });


    // Referencias a los nuevos elementos HTML
    const numMultasInput = $("#num_multas");
    const individualMultasContainer = $("#individual_multas_container");
    const totalMultasDisplay = $("#total_multas_display");

    /**
     * Genera dinámicamente los campos de entrada para cada multa individual
     * basándose en el número de multas ingresado.
     */
    function renderMultasInputs() {
        const numMultas = parseInt(numMultasInput.val()) || 0;
        individualMultasContainer.empty();

        if (numMultas > 0) {
            let multaHtml = '<div class="row">';
            for (let i = 0; i < numMultas; i++) {
                multaHtml += `
            <div class="col-md-4 mb-3">
                <label class="form-label">Valor Multa ${i + 1}</label>
                <input
                    type="number"
                    class="form-control individual-multa-valor"
                    placeholder="Valor"
                    data-multa-index="${i}"
                    min="0"
                    value="0"
                />
            </div>
            <div class="col-md-4 mb-3">
                <label class="form-label">Archivo Multa ${i + 1}</label>
                <input
                    type="file"
                    class="form-control individual-multa-file"
                    data-multa-index="${i}"
                    accept=".pdf,image/*"
                />
            </div>
            `;
            }
            multaHtml += '</div>';
            individualMultasContainer.append(multaHtml);

            // Vuelve a enlazar el evento de cálculo de total
            individualMultasContainer.find(".individual-multa-valor")
                .off("input")
                .on("input", calculateTotalMultas);
        }
        calculateTotalMultas();
    }

    /**
     * Calcula la suma total de los valores de las multas individuales
     * y actualiza el campo de visualización del total.
     */
    function calculateTotalMultas() {
        let total = 0;
        individualMultasContainer.find(".individual-multa-valor").each(function () {
            const valor = parseDecimal($(this).val()); // Usa tu función parseDecimal
            total += valor;
        });
        totalMultasDisplay.val(total.toFixed(2));
    }

  

    // ====================================================================
    // Event Listeners e Inicialización (Multas)
    // ====================================================================

    // Enlaza la generación cada vez que cambie el número de multas
    numMultasInput.on("change input", renderMultasInputs);
    renderMultasInputs();
    calculateTotalMultas();


    // ====================================================================
    // Lógica para la sección de Partes y Salvamento
    // ====================================================================

    document.getElementById("btnAgregarParte").addEventListener("click", () => {
        const tbody = document.getElementById("tablaPartes");
        const fila = document.createElement("tr");

        fila.innerHTML = `
        <td><input type="text" class="form-control parte-nombre-input" placeholder="Ej. Parachoques" /></td>
        <td><input type="number" class="form-control parte-valor-nuevo-input" placeholder="0" /></td>
        <td><input type="number" class="form-control parte-valor-depreciado-input" placeholder="0" /></td> <td><button type="button" class="btn btn-sm btn-danger btnEliminarParte">Eliminar</button></td>
    `;

        tbody.appendChild(fila);
        // Llamada inicial para asegurar que el cálculo se haga si ya hay filas
        recalcularSalvamento();
        actualizarResumenFinal();
    });

    document
        .getElementById("tablaPartes")
        .addEventListener("click", function (e) {
            if (e.target.classList.contains("btnEliminarParte")) {
                e.target.closest("tr").remove();
                recalcularSalvamento();
                actualizarResumenFinal();
            }
        });

    document
        .getElementById("porcentajeDano")
        .addEventListener("input", recalcularSalvamento);
    document
        .getElementById("porcentajeDano")
        .addEventListener("input", actualizarResumenFinal);

    document
        .getElementById("tablaPartes")
        .addEventListener("input", function (e) {
            if (e.target.classList.contains("parte-valor-depreciado-input")) { // **CORRECCIÓN**: Usar la clase correcta
                recalcularSalvamento();
                actualizarResumenFinal();
            }
        });
    function recalcularSalvamento() {
        const porcentaje =
            parseFloat(document.getElementById("porcentajeDano").value) || 0;
        let totalDepreciado = 0;

        document.querySelectorAll(".parte-valor-depreciado-input").forEach((input) => { // ¡CAMBIO AQUÍ!
            totalDepreciado += parseFloat(input.value) || 0;
        });

        const valorSalvamento = ((100 - porcentaje) / 100) * totalDepreciado;
        document.getElementById("valorSalvamento").value =
            valorSalvamento.toFixed(2);
    }
    // ====================================================================
    // Lógica para Fotos de Daños (Modificada)
    // ====================================================================

    const inputFotos = document.getElementById("fotosDanoInput");
    const previewDanos = document.getElementById("previewDanos");
    const countDanosBadge = $("#countDanos");

    // loadedDamagePhotos ya está declarado al inicio del script.

    function actualizarContadorFotos() {
        countDanosBadge.text(loadedDamagePhotos.length);
        // Swal.fire({ // Descomentar si quieres este toast cada vez que cambian las fotos
        //     toast: true,
        //     position: "top-end",
        //     icon: "success",
        //     title: `Total: ${loadedDamagePhotos.length} imagen(es)`,
        //     showConfirmButton: false,
        //     timer: 1500,
        // });
    }

    inputFotos.addEventListener("change", async function () {
        const files = this.files;

        for (let file of files) {
            const yaExiste = loadedDamagePhotos.some(
                (photo) => photo.nombre_archivo === file.name
            );
            if (yaExiste) {
                Swal.fire({
                    icon: "info",
                    title: "Archivo Duplicado",
                    text: `La foto "${file.name}" ya ha sido añadida.`,
                    confirmButtonText: "Aceptar",
                });
                continue;
            }

            try {
                const contenidoBase64 = await readFileAsBase64(file);
                const newPhoto = {
                    nombre_archivo: file.name,
                    contenido_base64: contenidoBase64,
                    mime_type: file.type,
                    observaciones: "", // Se llenará en el textarea
                    file: file // **IMPORTANTE**: Guardar el objeto File original aquí
                };

                const uiIndex = loadedDamagePhotos.length;
                newPhoto.uiIndex = uiIndex;

                loadedDamagePhotos.push(newPhoto);

                const photoItem = `
                    <div class="col-md-3 col-sm-6 mb-3 dano-item-preview" data-ui-index="${uiIndex}">
                        <div class="card border card-animate">
                            <div class="card-body p-2 text-center">
                                <div class="d-flex align-items-center mb-2">
                                    <div class="flex-shrink-0 me-2">
                                        <i class="ri-image-line fs-3 text-primary"></i>
                                    </div>
                                    <div class="flex-grow-1 text-start">
                                        <h6 class="mb-0 text-truncate">${file.name}</h6>
                                        <small class="text-muted">Foto de Daño</small>
                                    </div>
                                    <div class="flex-shrink-0">
                                        <button type="button" class="btn btn-sm btn-light p-0 remove-dano-btn" data-bs-toggle="tooltip" data-bs-placement="top" title="Eliminar">
                                            <i class="ri-delete-bin-line text-danger"></i>
                                        </button>
                                        <button type="button" class="btn btn-sm btn-light p-0 preview-dano-btn ms-1" data-bs-toggle="tooltip" data-bs-placement="top" title="Previsualizar">
                                            <i class="ri-eye-line text-info"></i>
                                        </button>
                                    </div>
                                </div>
                                <img src="data:${file.type};base64,${contenidoBase64}" class="img-thumbnail mb-2" style="height: 120px; object-fit: cover; width: 100%;" alt="${file.name}" />
                                <textarea class="form-control observacion-foto-input mt-2" rows="2" placeholder="Observación del daño" data-ui-index="${uiIndex}"></textarea>
                            </div>
                        </div>
                    </div>
                `;
                previewDanos.insertAdjacentHTML("beforeend", photoItem);

                $('[data-bs-toggle="tooltip"]').tooltip("dispose").tooltip();
                actualizarContadorFotos();

                Swal.fire({
                    toast: true,
                    position: "top-end",
                    icon: "success",
                    title: "Foto de daño agregada",
                    showConfirmButton: false,
                    timer: 1500,
                });
            } catch (error) {
                console.error("Error al leer archivo de daño:", error);
                Swal.fire({
                    icon: "error",
                    title: "Error de Archivo",
                    text: `No se pudo leer la foto ${file.name}.`,
                    confirmButtonText: "Aceptar",
                });
            }
        }
        this.value = null; // Limpiar el input file
    });

    previewDanos.addEventListener("click", (e) => {
        if (e.target.closest(".remove-dano-btn")) {
            const card = e.target.closest(".dano-item-preview"); // **CORRECCIÓN**: Usar la clase del item
            const uiIndex = card.dataset.uiIndex;

            Swal.fire({
                title: "¿Estás seguro?",
                text: "La foto se eliminará de la lista de carga.",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#3085d6",
                cancelButtonColor: "#d33",
                confirmButtonText: "Sí, eliminarla!",
                cancelButtonText: "Cancelar",
            }).then((result) => {
                if (result.isConfirmed) {
                    const realIndex = loadedDamagePhotos.findIndex(
                        (photo) => photo.uiIndex === parseInt(uiIndex)
                    );
                    if (realIndex > -1) {
                        loadedDamagePhotos.splice(realIndex, 1);
                    }
                    card.remove();
                    actualizarContadorFotos();
                    Swal.fire(
                        "Eliminada!",
                        "La foto ha sido removida de la lista.",
                        "success"
                    );
                }
            });
        }
    });

    previewDanos.addEventListener("click", (e) => {
        if (e.target.closest(".preview-dano-btn")) {
            const card = e.target.closest(".dano-item-preview"); // **CORRECCIÓN**: Usar la clase del item
            const uiIndex = card.dataset.uiIndex;

            const photo = loadedDamagePhotos.find(
                (p) => p.uiIndex === parseInt(uiIndex)
            );

            if (!photo) {
                Swal.fire({
                    icon: "error",
                    title: "Error de Previsualización",
                    text: "Foto no encontrada en la memoria.",
                    confirmButtonText: "Aceptar",
                });
                return;
            }

            previewContent.empty();
            const base64Data = `data:${photo.mime_type};base64,${photo.contenido_base64}`;

            if (photo.mime_type.includes("image")) {
                previewContent.append(
                    `<img src="${base64Data}" class="img-fluid" style="max-height: 80vh;" alt="${photo.nombre_archivo}">`
                );
            } else if (photo.mime_type.includes("pdf")) { // Good defensive practice though accept="image/*"
                previewContent.append(
                    `<iframe src="${base64Data}" width="100%" height="600px" style="border: none;"></iframe>`
                );
            } else {
                previewContent.append(
                    `<p class="alert alert-warning">No se puede previsualizar este tipo de archivo: <strong>.${photo.nombre_archivo.split(
                        "."
                    )
                        .pop()
                        .toLowerCase()}</strong></p>`
                );
            }
            previewModal.show();
        }
    });

    previewDanos.addEventListener("input", (e) => {
        if (e.target.classList.contains("observacion-foto-input")) { // **CORRECCIÓN**: Usar la clase correcta
            const uiIndex = e.target.dataset.uiIndex;
            const photo = loadedDamagePhotos.find(
                (p) => p.uiIndex === parseInt(uiIndex)
            );
            if (photo) {
                photo.observaciones = e.target.value;
            }
        }
    });

    document
        .getElementById("btnValidarFotos")
        .addEventListener("click", function () {
            const observaciones = document.querySelectorAll(".observacion-foto-input"); // **CORRECCIÓN**: Usar la clase correcta
            let todasLlenas = true;
            observaciones.forEach((obs) => {
                // Solo validar si hay al menos una foto cargada
                if (loadedDamagePhotos.length > 0 && !obs.value.trim()) {
                    obs.classList.add("is-invalid");
                    todasLlenas = false;
                } else {
                    obs.classList.remove("is-invalid");
                }
            });

            if (todasLlenas) {
                Swal.fire({
                    icon: "success",
                    title: "Listo",
                    text: "Todas las observaciones están completas.",
                });
            } else {
                Swal.fire({
                    icon: "error",
                    title: "Faltan observaciones",
                    text: "Completa las observaciones para todas las fotos (si hay fotos cargadas).",
                });
            }
        });

    // ====================================================================
    // Lógica para actualizar el Resumen Final
    // ====================================================================

    function actualizarResumenFinal() {
        document.getElementById("resumenValorAsegurado").innerText = `$${(
            parseDecimal(valorAseguradoInput.value)
        ).toLocaleString("es-EC", { // **CORRECCIÓN**: Usar parseDecimal
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        })}`;
        document.getElementById("resumenPromedioComercial").innerText = `$${(
            parseDecimal(promedioCalculadoInput.value)
        ).toLocaleString("es-EC", { // **CORRECCIÓN**: Usar parseDecimal
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        })}`;
        document.getElementById("resumenValorMatricula").innerText = `$${(
            parseDecimal(valorMatriculaPendienteInput.value)
        ).toLocaleString("es-EC", { // **CORRECCIÓN**: Usar parseDecimal
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        })}`;
        document.getElementById("resumenPromedioNeto").innerText = `$${(
            parseDecimal(promedioNetoInput.value)
        ).toLocaleString("en-US", { // **CORRECCIÓN**: Usar parseDecimal
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        })}`;
        document.getElementById("resumenPorcentajeDano").innerText = `${(
            parseDecimal(document.getElementById("porcentajeDano").value)
        ).toFixed(0)}%`; // **CORRECCIÓN**: Usar parseDecimal
        document.getElementById("resumenValorSalvamento").innerText = `$${(
            parseDecimal(document.getElementById("valorSalvamento").value)
        ).toLocaleString("en-US", { // **CORRECCIÓN**: Usar parseDecimal
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        })}`;
    }

    // Llamar a las funciones de cálculo y actualización al cargar la página
    recalcularValoresComerciales();
    recalcularSalvamento();
    actualizarResumenFinal();
    function agregarFilaParte(parte = {}) {
        const nombre = parte.NombreParte || "";
        const valorNuevo = parte.ValorNuevo ?? "";
        const valorDepreciado = parte.ValorDepreciado ?? "";

        const filaHtml = `
        <tr>
            <td><input type="text" class="form-control parte-nombre-input" value="${nombre}" /></td>
            <td><input type="number" class="form-control parte-valor-nuevo-input" value="${valorNuevo}" /></td>
            <td><input type="number" class="form-control parte-valor-depreciado-input" value="${valorDepreciado}" /></td>
            <td><button type="button" class="btn btn-sm btn-danger btnEliminarParte">Eliminar</button></td>
        </tr>
    `;

        $('#tablaPartes').append(filaHtml);
    }

    $(document).ready(function () {
        const partes = window.datosCaso?.Partes || [];

        partes.forEach(parte => agregarFilaParte(parte));

        // También enganchamos el botón de agregar
        $('#btnAgregarParte').on('click', () => agregarFilaParte());

        // Eliminar fila
        $('#tablaPartes').on('click', '.btnEliminarParte', function () {
            $(this).closest('tr').remove();
        });

        // Lógica para recalcular al iniciar
        recalcularSalvamento();
    });
  
    class CasoFinancieroManager {
        constructor() {
            // 1. Inicialización de propiedades
            this.casoId = window.casoId || null;
            this.usuarioId = window.usuarioId || null;
            this.aseguradoId = window.aseguradoId || null;
            this.vehiculoId = window.vehiculoId || null;
            this.currentTab = 'resumen';
            this.lastSaveTime = null;
            this.saveTimeout = null;
            this.isDirty = false;
            this.isAutoSaving = false;
            this.crearCasoApiUrl = window.crearCasoApiUrl;

            // 2. Configuración inicial
            this.init();
        }

        // 3. Métodos de inicialización
        init() {
            this.initializeEventListeners();
            this.inicializarManejadoresArchivosValorComercial();
            this.startAutoSave();
            this.setupGlobalDocumentHandlers();
        }

        initializeEventListeners() {
            const $document = $(document);

            // Cambios en formularios
            $document.on('input change', 'input, select, textarea', () => {
                if (!this.isAutoSaving) this.markAsDirty();
            });

            // Navegación entre tabs
            $document.on('click', '.btn-next-tab, .btn-prev-tab', (e) => {
                this.handleTabChange(e).catch(console.error);
            });

            // Submit del formulario
            $('#crearAnalisisForm').off('submit').on('submit', (e) => {
                e.preventDefault();
                if (!this.isAutoSaving) this.saveData(false);
            });

            // Auto-guardado antes de cerrar
            let beforeUnloadTimeout;
            $(window).on('beforeunload', () => {
                clearTimeout(beforeUnloadTimeout);
                if (this.isDirty) {
                    beforeUnloadTimeout = setTimeout(() => this.saveDataSync(), 100);
                }
            });
        }

        inicializarManejadoresArchivosValorComercial() {
            const tiposDocumentos = [
                { id: 'filePatioTuerca', key: 'filePatioTuerca', label: 'Patio Tuerca', tipoDocId: 13 },
                { id: 'fileAEADE', key: 'fileAEADE', label: 'AEADE', tipoDocId: 13 },
                { id: 'fileMarketplace', key: 'fileMarketplace', label: 'Marketplace', tipoDocId: 13 },
                { id: 'fileHugoVargas', key: 'fileHugoVargas', label: 'Hugo Vargas', tipoDocId: 13 },
                { id: 'fileOtros', key: 'fileOtros', label: 'Otros', tipoDocId: 13 }
            ];

            window.loadedValorFiles = window.loadedValorFiles || {};

            tiposDocumentos.forEach(({ id, key, label, tipoDocId }) => {
                $(document).on('change', `#${id}`, (e) => {
                    const file = e.target.files[0];
                    if (file) {
                        window.loadedValorFiles[key] = {
                            file,
                            File: file, // compatibilidad
                            tipo_documento_id: tipoDocId,
                            observaciones: `Documento de ${label}`,
                            ambito_documento: "VALORCOMERCIAL",
                            nombre_archivo: file.name,
                            ruta_fisica: '',
                            tipo_mime: file.type
                        };
                    } else {
                        delete window.loadedValorFiles[key];
                    }
                });
            });
        }


        setupGlobalDocumentHandlers() {
            window.loadedDocuments = window.loadedDocuments || { caso: [], asegurado: [] };
            window.loadedDamagePhotos = window.loadedDamagePhotos || [];
        }

        // 4. Manejo de estado y auto-guardado
        markAsDirty() {
            if (this.isDirty) return;

            this.isDirty = true;
            clearTimeout(this.saveTimeout);

            this.saveTimeout = setTimeout(() => {
                if (this.isDirty && !this.isAutoSaving) {
                    this.saveData(true);
                }
            }, 10000);
        }

        startAutoSave() {
            setInterval(() => {
                if (this.isDirty && !this.isAutoSaving &&
                    document.visibilityState === 'visible') {
                    this.saveData(true);
                }
            }, 30000);
        }

        // 5. Manejo de tabs
        async handleTabChange(e) {
            const $target = $(e.target);
            const isNext = $target.hasClass('btn-next-tab');
            const targetTab = $target.data('target')?.replace('#', '') || this.getNextTab(isNext);

            if (this.isDirty) {
                await this.saveData(true);
            }

            this.currentTab = targetTab;
            this.switchTab(targetTab);
        }

        getNextTab(isNext) {
            const tabs = ['resumen', 'documentos', 'valores', 'danos', 'partes', 'resumenfinal'];
            const currentIndex = tabs.indexOf(this.currentTab);

            if (isNext && currentIndex < tabs.length - 1) {
                return tabs[currentIndex + 1];
            } else if (!isNext && currentIndex > 0) {
                return tabs[currentIndex - 1];
            }

            return this.currentTab;
        }

        switchTab(tabName) {
            $('.tab-pane').removeClass('show active');
            $(`#${tabName}`).addClass('show active');
            $('.nav-link').removeClass('active');
            $(`[href="#${tabName}"]`).addClass('active');
        }

        // 6. Métodos principales de guardado
        async saveData(esGuardadoParcial = true, showLoading = true) {
            if (!this.isDirty && esGuardadoParcial) return { success: true };

            this.isAutoSaving = true;
            if (showLoading) this.showSaveIndicator();

            try {
                const formData = this.buildFormData(esGuardadoParcial);
                const response = await this.sendData(formData, esGuardadoParcial);

                this.handleSaveSuccess(response, esGuardadoParcial, showLoading);
                return { success: true, data: response };
            } catch (error) {
                this.handleSaveError(error, esGuardadoParcial, showLoading);
                return { success: false, error };
            } finally {
                this.isAutoSaving = false;
                if (showLoading && esGuardadoParcial) {
                    setTimeout(() => this.hideSaveIndicator(), 2000);
                }
            }
        }

        buildFormData(esGuardadoParcial) {
            const formData = new FormData();

            // Datos básicos
            this.appendBasicFormData(formData, esGuardadoParcial);

            // Datos estructurados
            this.appendStructuredData(formData);

            // Documentos
            this.appendDocumentData(formData);

            // 4) Si hay documentos de valor comercial, forzamos la pestaña 'valores'
            const valorDocsCount = Object.values(window.loadedValorFiles).filter(Boolean).length;
            if (valorDocsCount > 0) {
                console.log(`🔧 Forzando TabActual = 'valores' porque hay ${valorDocsCount} docs de valor comercial`);
                formData.set('TabActual', 'valores');
            }


            // Debug en desarrollo
            if (window.location.hostname === 'localhost') {
                this.debugFormData(formData);
            }

            return formData;
        }

        appendBasicFormData(formData, esGuardadoParcial) {
            formData.append('EsGuardadoParcial', esGuardadoParcial);
            formData.append('TabActual', this.currentTab);
            formData.append('casoId', this.casoId || '');
            formData.append('usuarioId', this.usuarioId || '');
            formData.append('aseguradoId', this.aseguradoId || '');
            formData.append('vehiculoId', this.vehiculoId || '');
            formData.append('NumeroReclamo', $('#numeroReclamo').val() || '');

            const basicFields = [
                'nombreCompleto', 'metodoAvaluo', 'direccionAvaluo',
                'comentariosAvaluo', 'notasAvaluo', 'fechaSiniestro',
                'fechaSolicitudAvaluo'
            ];

            basicFields.forEach(field => {
                const value = $(`#${field}`).val();

                // Validar fechas sólo para campos que son fechas
                const isDateField = field.toLowerCase().includes('fecha');
                if (value) {
                    if (isDateField) {
                        const d = new Date(value);
                        if (!isNaN(d) && d.getFullYear() >= 1753) {
                            formData.append(field.charAt(0).toUpperCase() + field.slice(1), value);
                        } else {
                            console.warn(`Fecha inválida para ${field}: ${value}`);
                        }
                    } else {
                        formData.append(field.charAt(0).toUpperCase() + field.slice(1), value);
                    }
                }
            });
        }

        appendStructuredData(formData) {
            formData.append("Vehiculo", JSON.stringify(this.getVehiculoData()));
            formData.append("Resumen", JSON.stringify(this.getResumenFinancieroData()));
            formData.append("ValoresComerciales", JSON.stringify(this.getValoresComercialesData()));
            formData.append("Danos", JSON.stringify(this.getDanosData()));
            formData.append("Partes", JSON.stringify(this.getPartesData()));
        }

        getVehiculoData() {
            return {
                placa: $('#vehiculoPlaca').val(),
                marca: this.nullIfEmpty($('#vehiculoMarca').val()),
                modelo: this.nullIfEmpty($('#vehiculoModelo').val()),
                transmision: this.nullIfEmpty($('#vehiculoTransmision').val()),
                combustible: this.nullIfEmpty($('#vehiculoCombustible').val()),
                cilindraje: this.nullIfEmpty($('#vehiculoCilindraje').val()),
                anio: this.parseIntOrNull($('#vehiculoAnio').val()),
                numeroChasis: this.nullIfEmpty($('#vehiculoNumeroChasis').val()),
                numeroMotor: this.nullIfEmpty($('#vehiculoNumeroMotor').val()),
                tipoVehiculo: this.nullIfEmpty($('#tipoVehiculo').val()),
                clase: this.nullIfEmpty($('#vehiculoClase').val()),
                color: this.nullIfEmpty($('#vehiculoColor').val()),
                observaciones: this.nullIfEmpty($('#vehiculoObservaciones').val()),
                gravamen: this.nullIfEmpty($('#vehiculoGravamen').val()),
                placasMetalicas: this.nullIfEmpty($('#vehiculoPlacasMetalicas').val()),
                radioVehiculo: this.nullIfEmpty($('#vehiculoRadio').val()),
                estadoVehiculo: this.nullIfEmpty($('#vehiculoEstado').val())
            };
        }

        getResumenFinancieroData() {
            return {
                fechaLimitePagoSri: this.nullIfEmpty($('#fechalimitepago').val()),
                numeroMultas: this.parseIntOrNull($('#num_multas').val()),
                valorMultasTotal: this.parseDecimal($('#total_multas_display').val()),
                valorAsegurado: this.parseDecimal($('#valorAsegurado').val()),
                valorMatriculaPendiente: this.parseDecimal($('#valorMatriculaPendiente').val()),
                promedioCalculado: this.parseDecimal($('#promedioCalculado').val()),
                promedioNeto: this.parseDecimal($('#promedioNeto').val()),
                porcentajeDano: this.parseDecimal($('#porcentajeDano').val()),
                valorSalvamento: this.parseDecimal($('#valorSalvamento').val()),
                precioComercialSugerido: this.parseDecimal($('#precioComercialSugerido').val()),
                precioBase: this.parseDecimal($('#precioBase').val()),
                precioEstimadoVentaVehiculo: this.parseDecimal($('#precioEstimadoVentaVehiculo').val())
            };
        }

        getValoresComercialesData() {
            const valores = [];
            const addValor = (fuente, selector) => {
                const valor = this.parseDecimal($(selector).val());
                if (valor > 0) {
                    valores.push({ Fuente: fuente, Valor: valor });
                }
            };

            addValor("Patio Tuerca", "#valorPatioTuerca");
            addValor("AEADE", "#valorAEADE");
            addValor("Marketplace", "#valorMarketplace");
            addValor("Hugo Vargas", "#valorHugoVargas");
            addValor("Otros", "#valorOtros");

            return valores;
        }

        getDanosData() {
            if (!window.loadedDamagePhotos || !Array.isArray(window.loadedDamagePhotos)) return [];

            return window.loadedDamagePhotos.map(item => ({
                Observaciones: item.observaciones || "",
                nombre_archivo: item.nombre_archivo || "",
            }));
        }

        getPartesData() {
            const partes = [];
            const parseDecimal = (val) => {
                const num = parseFloat(val?.toString().replace(",", "."));
                return isNaN(num) ? 0 : num;
            };

            $('#tablaPartes tr').each(function () {
                const $row = $(this);
                const nombreParte = $row.find('.parte-nombre-input').val()?.trim();
                const valorNuevo = parseDecimal($row.find('.parte-valor-nuevo-input').val());
                const valorDepreciado = parseDecimal($row.find('.parte-valor-depreciado-input').val());

                if (nombreParte || valorNuevo !== 0 || valorDepreciado !== 0) {
                    partes.push({
                        NombreParte: nombreParte || "",
                        ValorNuevo: valorNuevo,
                        ValorDepreciado: valorDepreciado
                    });
                }
            });

            return partes;
        }

        // MÉTODO CORREGIDO: appendDocumentData
        appendDocumentData(formData) {
            console.log('=== INICIANDO PROCESAMIENTO DE DOCUMENTOS ===');

            const allDocs = this.prepareCaseDocuments();

            // Documentos del caso
            const casoDocs = allDocs.filter(d => d.ambito_documento === "CASO");
            this.appendDocumentList(formData, "DocumentosCasoInput", casoDocs);
            console.log(`Documentos del caso preparados: ${casoDocs.length}`);

            // Documentos del asegurado
            const aseguradosDocs = allDocs.filter(d => d.ambito_documento === "ASEGURADO");
            this.appendDocumentList(formData, "DocumentosAseguradoInput", aseguradosDocs);
            console.log(`Documentos del asegurado preparados: ${aseguradosDocs.length}`);

            // Documentos de daño
            const danosDocs = this.prepareDamageDocuments();
            this.appendDocumentList(formData, "DocumentosDanoInput", danosDocs);
            console.log(`Documentos de daños preparados: ${danosDocs.length}`);

            // Documentos de valores comerciales
            const valoresDocs = this.prepareCommercialValueDocuments();
            this.appendDocumentList(formData, "DocumentosValorComercialInput", valoresDocs);
            console.log(`Documentos de valor comercial preparados: ${valoresDocs.length}`);

            console.log("⚙️ prepareCommercialValueDocuments →", valoresDocs);

        }

        // MÉTODO NUEVO: prepareCaseDocuments
        prepareCaseDocuments() {
            if (!window.loadedDocuments) {
                console.warn('window.loadedDocuments no existe');
                return [];
            }

            const allDocuments = [];

            // Procesar documentos de todos los ámbitos
            Object.keys(window.loadedDocuments).forEach(ambito => {
                if (Array.isArray(window.loadedDocuments[ambito])) {
                    console.log(`Procesando ámbito '${ambito}': ${window.loadedDocuments[ambito].length} documentos`);

                    window.loadedDocuments[ambito].forEach(doc => {
                        // Verificar que el documento tenga un archivo válido
                        const hasValidFile = (doc.File instanceof File) || (doc.file instanceof File);

                        allDocuments.push({
                            file: doc.File || doc.file || null,
                            documento_id: doc.documento_id || null,
                            tipo_documento_id: doc.tipo_documento_id || 1,
                            observaciones: doc.observaciones || '',
                            ambito_documento: doc.ambito_documento || ambito.toUpperCase(),
                            nombre_archivo: doc.nombre_archivo || '',
                            ruta_fisica: doc.ruta_fisica || '',
                            hasValidFile: hasValidFile
                        });
                    });
                }
            });

            console.log(`Total documentos del caso: ${allDocuments.length}`);
            return allDocuments;
        }


        // MÉTODO CORREGIDO: prepareDamageDocuments
        prepareDamageDocuments() {
            if (!window.loadedDamagePhotos || !Array.isArray(window.loadedDamagePhotos)) {
                console.warn('window.loadedDamagePhotos no existe o no es array');
                return [];
            }

            console.log(`Procesando ${window.loadedDamagePhotos.length} fotos de daños`);

            return window.loadedDamagePhotos.map(item => {
                const hasValidFile = (item.file instanceof File) || (item.File instanceof File);

                return {
                    file: item.file || item.File || null,
                    documento_id: item.documento_id || null,
                    tipo_documento_id: item.tipo_documento_id || 6,
                    observaciones: item.observaciones || `Observación de daño`,
                    ambito_documento: item.ambito_documento || "DANO",
                    nombre_archivo: item.nombre_archivo || '',
                    ruta_fisica: item.ruta_fisica || '',
                    hasValidFile: hasValidFile
                };
            });
        }

        // MÉTODO CORREGIDO: prepareCommercialValueDocuments
        prepareCommercialValueDocuments() {
            if (!window.loadedValorFiles) {
                console.warn('window.loadedValorFiles no existe');
                return [];
            }

            const valorDocs = Object.values(window.loadedValorFiles).filter(Boolean);
            console.log(`Procesando ${valorDocs.length} documentos de valor comercial`);

            return valorDocs.map(item => {
                const hasValidFile = (item.file instanceof File) || (item.File instanceof File);

                return {
                    file: item.file || item.File || null,
                    documento_id: item.documento_id || null,
                    tipo_documento_id: item.tipo_documento_id || 13,
                    observaciones: item.observaciones || `Documento de Valor Comercial`,
                    ambito_documento: item.ambito_documento || "VALORCOMERCIAL",
                    nombre_archivo: item.nombre_archivo || '',
                    ruta_fisica: item.ruta_fisica || '',
                    hasValidFile: hasValidFile
                };
            });
        }

        // MÉTODO MEJORADO: appendDocumentList
        appendDocumentList(formData, key, documents) {
            if (!Array.isArray(documents)) {
                console.warn(`${key}: No hay documentos o no es un array`);
                return;
            }

            console.log(`\n=== PROCESANDO ${key} (${documents.length} documentos) ===`);

            documents.forEach((doc, index) => {
                console.log(`${key}[${index}]:`, {
                    hasFile: !!(doc.file instanceof File),
                    fileName: doc.nombre_archivo,
                    documentId: doc.documento_id,
                    tipoDoc: doc.tipo_documento_id,
                    ambito: doc.ambito_documento,
                    fileSize: doc.file instanceof File ? doc.file.size : 'N/A'
                });

                // Solo agregar el archivo si es un File válido
                if (doc.file instanceof File) {
                    formData.append(`${key}[${index}].File`, doc.file);
                    console.log(`✅ Archivo adjuntado: ${key}[${index}].File = ${doc.file.name} (${doc.file.size} bytes)`);
                } else if (doc.documento_id) {
                    console.log(`ℹ️  Documento existente en BD: ${key}[${index}].DocumentoId = ${doc.documento_id}`);
                } else {
                    console.warn(`⚠️  Sin archivo ni documento_id: ${key}[${index}]`);
                }

                // Agregar metadatos siempre
                if (doc.documento_id) {
                    formData.append(`${key}[${index}].DocumentoId`, doc.documento_id);
                }

                formData.append(`${key}[${index}].TipoDocumentoId`, doc.tipo_documento_id || 1);
                formData.append(`${key}[${index}].Observaciones`, doc.observaciones || '');
                formData.append(`${key}[${index}].AmbitoDocumento`, doc.ambito_documento || '');
                formData.append(`${key}[${index}].NombreArchivo`, doc.nombre_archivo || '');
                formData.append(`${key}[${index}].RutaFisica`, doc.ruta_fisica || '');
            });
        }

        // 7. Métodos auxiliares
        nullIfEmpty(value) {
            return (value === null || value === undefined || (typeof value === 'string' && value.trim() === '')) ? null : value;
        }

        parseIntOrNull(value) {
            const parsed = parseInt(value);
            return isNaN(parsed) ? null : parsed;
        }

        parseDecimal(value) {
            if (value === null || value === undefined) return 0;
            const cleanedValue = String(value).replace(/\./g, '').replace(/,/g, '.');
            const parsed = parseFloat(cleanedValue);
            return isNaN(parsed) ? 0 : parsed;
        }

        formatDecimalInput(inputElement) {
            const rawValue = parseDecimal(inputElement.value);
            inputElement.value = rawValue.toLocaleString('es-EC', {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            });
        }

        // 8. Comunicación con el servidor
        async sendData(formData, esGuardadoParcial) {
            return $.ajax({
                url: this.crearCasoApiUrl,
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val(),
                    'X-Auto-Save': esGuardadoParcial.toString()
                }
            });
        }

        saveDataSync() {
            if (navigator.sendBeacon) {
                try {
                    const formData = this.buildFormData(true);
                    const url = this.crearCasoApiUrl + '?sync=true';
                    navigator.sendBeacon(url, formData);
                } catch (error) {
                    console.error('Error en guardado sincrónico:', error);
                }
            }
        }

        // 9. Manejo de resultados
        handleSaveSuccess(response, esGuardadoParcial, showLoading) {
            this.isDirty = false;
            this.lastSaveTime = new Date();

            if (response.casoId) this.casoId = response.casoId;
            if (response.aseguradoId) this.aseguradoId = response.aseguradoId;
            if (response.vehiculoId) this.vehiculoId = response.vehiculoId;

            if (showLoading) {
                if (esGuardadoParcial) {
                    this.showSaveSuccess('Guardado automáticamente');
                } else {
                    Swal.fire('✅ Éxito', response.mensaje || 'Caso guardado correctamente', 'success').then(() => {
                        if (response.redirectToUrl) {
                            window.location.href = response.redirectToUrl;
                        }
                    });
                }
            }
        }

        handleSaveError(error, esGuardadoParcial, showLoading) {
            console.error('Error al guardar:', error);

            if (showLoading) {
                if (esGuardadoParcial) {
                    this.showSaveError('Error en guardado automático');
                } else {
                    this.processValidationErrors(error);
                    Swal.fire('❌ Error', this.getErrorMessage(error), 'error');
                }
            }
        }

        getErrorMessage(error) {
            if (error.responseJSON?.errors) {
                return Object.values(error.responseJSON.errors)
                    .flat()
                    .join('<br>');
            }
            return error.responseJSON?.detalle ||
                error.responseJSON?.error ||
                error.responseText ||
                'Error inesperado al guardar';
        }

        processValidationErrors(error) {
            $('.is-invalid').removeClass('is-invalid');
            $('.invalid-feedback').remove();

            if (error.responseJSON?.errors) {
                Object.entries(error.responseJSON.errors).forEach(([field, messages]) => {
                    const $input = $(`[name="${field}"], #${field}`);
                    if ($input.length) {
                        $input.addClass('is-invalid');
                        $input.after(`<div class="invalid-feedback">${messages.join('<br>')}</div>`);
                    }
                });
            }
        }

        // 10. UI Feedback
        showSaveIndicator() {
            let $indicator = $('#save-indicator');
            if ($indicator.length === 0) {
                $indicator = $(`
            <div id="save-indicator" class="position-fixed" style="top: 20px; right: 20px; z-index: 9999;">
                <div class="alert alert-info alert-dismissible fade show" role="alert">
                    <div class="d-flex align-items-center">
                        <div class="spinner-border spinner-border-sm me-2" role="status"></div>
                        <span>Guardando...</span>
                    </div>
                </div>
            </div>
        `);
                $('body').append($indicator);
            }
            $indicator.show();
        }

        showSaveSuccess(message) {
            const $indicator = $('#save-indicator');
            if ($indicator.length) {
                $indicator.html(`
            <div class="alert alert-success alert-dismissible fade show" role="alert">
                <div class="d-flex align-items-center">
                    <i class="ri-check-line me-2"></i>
                    <span>${message}</span>
                </div>
            </div>
        `);
            }
        }

        showSaveError(message) {
            const $indicator = $('#save-indicator');
            if ($indicator.length) {
                $indicator.html(`
            <div class="alert alert-danger alert-dismissible fade show" role="alert">
                <div class="d-flex align-items-center">
                    <i class="ri-error-warning-line me-2"></i>
                    <span>${message}</span>
                </div>
            </div>
        `);
            }
        }

        hideSaveIndicator() {
            $('#save-indicator').fadeOut();
        }

        // 11. Debug mejorado
        debugFormData(formData) {
            console.log("=== DEBUG FORM DATA COMPLETO ===");
            console.log("Auto-guardado - Tab:", this.currentTab);

            const fileSummary = [];
            const otherFields = [];

            for (let pair of formData.entries()) {
                if (pair[1] instanceof File) {
                    const fileInfo = `📎 ${pair[0]} → ${pair[1].name} (${pair[1].size} bytes)`;
                    fileSummary.push(fileInfo);
                    console.log(`FILE: ${fileInfo}`);
                } else {
                    otherFields.push(`${pair[0]}: ${pair[1]}`);
                    console.log(`FIELD: ${pair[0]} = ${pair[1]}`);
                }
            }

            console.log(`\n=== 📁 RESUMEN DE ARCHIVOS (${fileSummary.length}) ===`);
            if (fileSummary.length > 0) {
                fileSummary.forEach(msg => console.log(msg));
            } else {
                console.warn('❌ NO SE ESTÁN ENVIANDO ARCHIVOS');
            }

            // Verificar estado de documentos
            this.diagnosticarDocumentos();
        }

        // 12. Función de diagnóstico
        diagnosticarDocumentos() {
            console.log('\n=== 🗂️ DIAGNÓSTICO DE DOCUMENTOS ===');

            // Verificar window.loadedDocuments
            if (window.loadedDocuments) {
                console.log('📋 window.loadedDocuments:');
                Object.keys(window.loadedDocuments).forEach(ambito => {
                    const docs = window.loadedDocuments[ambito];
                    console.log(`  ${ambito.toUpperCase()}: ${docs.length} documentos`);
                    docs.forEach((doc, index) => {
                        console.log(`    [${index}] ${doc.nombre_archivo}:`, {
                            hasFile: !!(doc.File || doc.file),
                            fileType: (doc.File || doc.file) ? (doc.File || doc.file).type : 'N/A',
                            fileSize: (doc.File || doc.file) ? (doc.File || doc.file).size : 'N/A',
                            tipoDoc: doc.tipo_documento_id,
                            documentoId: doc.documento_id
                        });
                    });
                });
            } else {
                console.warn('❌ window.loadedDocuments no existe');
            }

            // Verificar window.loadedDamagePhotos
            if (window.loadedDamagePhotos) {
                console.log(`📸 window.loadedDamagePhotos: ${window.loadedDamagePhotos.length} fotos`);
                window.loadedDamagePhotos.forEach((photo, index) => {
                    console.log(`  [${index}] ${photo.nombre_archivo}:`, {
                        hasFile: !!(photo.file || photo.File),
                        fileType: (photo.file || photo.File) ? (photo.file || photo.File).type : 'N/A',
                        fileSize: (photo.file || photo.File) ? (photo.file || photo.File).size : 'N/A'
                    });
                });
            } else {
                console.warn('❌ window.loadedDamagePhotos no existe');
            }

            // Verificar window.loadedValorFiles
            if (window.loadedValorFiles) {
                const valorDocs = Object.values(window.loadedValorFiles).filter(Boolean);
                console.log(`💰 window.loadedValorFiles: ${valorDocs.length} documentos`);
                valorDocs.forEach((doc, index) => {
                    console.log(`  [${index}] ${doc.nombre_archivo}:`, {
                        hasFile: !!(doc.file || doc.File),
                        fileType: (doc.file || doc.File) ? (doc.file || doc.File).type : 'N/A',
                        fileSize: (doc.file || doc.File) ? (doc.file || doc.File).size : 'N/A'
                    });
                });
            } else {
                console.warn('❌ window.loadedValorFiles no existe');
            }

            console.log('=== FIN DIAGNÓSTICO ===\n');
        }
    }

    // ====================================================================================
    // LÓGICA DE CARGA Y GESTIÓN DE DOCUMENTOS GENERALES (CORREGIDA)
    // ====================================================================================

    // Variables globales para la gestión de documentos

    // Funciones auxiliares para iconos y colores
    function getIconByType(mimeType) {
        if (!mimeType) return 'ri-file-line';

        if (mimeType.includes('image')) return 'ri-image-line';
        if (mimeType.includes('pdf')) return 'ri-file-pdf-line';
        if (mimeType.includes('word') || mimeType.includes('document')) return 'ri-file-word-line';
        if (mimeType.includes('excel') || mimeType.includes('spreadsheet')) return 'ri-file-excel-line';
        if (mimeType.includes('powerpoint') || mimeType.includes('presentation')) return 'ri-file-ppt-line';
        if (mimeType.includes('text')) return 'ri-file-text-line';
        if (mimeType.includes('zip') || mimeType.includes('rar')) return 'ri-file-zip-line';

        return 'ri-file-line';
    }

    function getColorByType(mimeType) {
        if (!mimeType) return 'text-secondary';

        if (mimeType.includes('image')) return 'text-success';
        if (mimeType.includes('pdf')) return 'text-danger';
        if (mimeType.includes('word') || mimeType.includes('document')) return 'text-primary';
        if (mimeType.includes('excel') || mimeType.includes('spreadsheet')) return 'text-success';
        if (mimeType.includes('powerpoint') || mimeType.includes('presentation')) return 'text-warning';
        if (mimeType.includes('text')) return 'text-info';
        if (mimeType.includes('zip') || mimeType.includes('rar')) return 'text-dark';

        return 'text-secondary';
    }

    // Función para precargar documentos existentes en la UI
    function loadExistingDocumentsToUI() {
        listaDocumentosDiv.empty(); // Limpiar el contenedor antes de recargar

        if (!window.loadedDocuments) {
            console.warn('window.loadedDocuments no existe al cargar la UI');
            return;
        }

        Object.keys(window.loadedDocuments).forEach(ambitoKey => {
            if (Array.isArray(window.loadedDocuments[ambitoKey])) {
                window.loadedDocuments[ambitoKey].forEach(doc => {
                    const fileMime = doc.tipo_mime || 'application/octet-stream';
                    const docItem = `
                <div class="col-md-4 col-sm-6 mb-3 fade-in-up" data-ui-index="${doc.uiIndex || ''}" data-doc-id="${doc.documento_id || ''}" data-ambito="${doc.ambito_documento ? doc.ambito_documento.toLowerCase() : ambitoKey}">
                    <div class="card border card-animate">
                        <div class="card-body">
                            <div class="d-flex align-items-center">
                                <div class="flex-shrink-0 me-3">
                                    <i class="${getIconByType(fileMime)} fs-2 ${getColorByType(fileMime)}"></i>
                                </div>
                                <div class="flex-grow-1">
                                    <h6 class="mb-1 text-truncate" style="max-width: 150px;">${doc.nombre_archivo || 'Sin nombre'}</h6>
                                    <small class="text-muted">${doc.nombre_tipo_documento || 'Documento'} (${doc.ambito_documento || ambitoKey.toUpperCase()})</small>
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
                            ${doc.observaciones ? `<p class="text-muted mt-2 mb-0 text-wrap"><small>Obs: ${doc.observaciones}</small></p>` : ''}
                        </div>
                    </div>
                </div>
                `;
                    listaDocumentosDiv.append(docItem);
                });
            }
        });

        // Re-inicializa los tooltips de Bootstrap después de añadir todos los elementos
        $('[data-bs-toggle="tooltip"]').tooltip("dispose").tooltip();
    }

    // Cargar documentos existentes al iniciar
    loadExistingDocumentsToUI();

    // Evento para agregar documentos (CORREGIDO)
    $('#btnAgregarDoc').on('click', async function () {
        const tipoDocumentoId = categoriaDocumentoSelect.val();
        const tipoDocumentoText = categoriaDocumentoSelect.find("option:selected").text();
        const selectedOptionElement = categoriaDocumentoSelect.find("option:selected");

        // Lee el ámbito del atributo data-ambito
        let ambitoFijo = selectedOptionElement.data("ambito") ? selectedOptionElement.data("ambito").toLowerCase() : 'caso';

        // Validaciones iniciales
        if (!tipoDocumentoId) {
            categoriaDocumentoSelect.addClass("is-invalid");
            Swal.fire({
                icon: "warning",
                title: "Selección Requerida",
                text: "Por favor, seleccione un tipo de documento de la lista.",
                confirmButtonText: "Aceptar",
                confirmButtonColor: "#f7b84b"
            });
            return;
        } else {
            categoriaDocumentoSelect.removeClass("is-invalid");
        }

        const observaciones = observacionesDocumentoTextarea.val();
        const files = archivosDocumentoInput[0].files;

        if (files.length === 0) {
            Swal.fire({
                icon: "warning",
                title: "Archivos Requeridos",
                text: "Por favor, seleccione al menos un archivo para subir.",
                confirmButtonText: "Aceptar",
                confirmButtonColor: "#f7b84b"
            });
            return;
        }

        // Procesamiento de archivos seleccionados
        for (const file of files) {
            const uiId = crypto.randomUUID();

            // Re-evaluar el ámbito si hay lógica específica por tipoDocumentoId
            let finalAmbito = ambitoFijo;
            if (parseInt(tipoDocumentoId) === 6) { // ID para 'Fotos del Siniestro'
                finalAmbito = "dano";
            } else if (parseInt(tipoDocumentoId) === 13) { // ID para 'Evidencia de Valores Comerciales'
                finalAmbito = "valorcomercial";
            }
            finalAmbito = finalAmbito.toLowerCase();

            // Asegurar que el ámbito existe en loadedDocuments
            if (!window.loadedDocuments.hasOwnProperty(finalAmbito)) {
                console.error(`Error: El ámbito '${finalAmbito}' no es una categoría válida en loadedDocuments.`);
                // Crear el ámbito si no existe
                window.loadedDocuments[finalAmbito] = [];
            }

            // Verificar duplicados
            const yaExiste = window.loadedDocuments[finalAmbito].some(
                doc => (doc.nombre_archivo === file.name || (doc.File && doc.File.name === file.name)) &&
                    doc.tipo_documento_id === parseInt(tipoDocumentoId)
            );

            if (yaExiste) {
                Swal.fire({
                    icon: "info",
                    title: "Archivo Duplicado",
                    text: `El archivo "${file.name}" con este tipo de documento ya ha sido añadido en el ámbito ${finalAmbito}.`,
                    confirmButtonText: "Aceptar",
                    confirmButtonColor: "#f7b84b"
                });
                continue;
            }

            try {
                // CREAR DOCUMENTO CON AMBAS PROPIEDADES File y file
                const newDoc = {
                    tipo_documento_id: parseInt(tipoDocumentoId),
                    nombre_tipo_documento: tipoDocumentoText,
                    nombre_archivo: file.name,
                    observaciones: observaciones,
                    ambito_documento: finalAmbito.toUpperCase(),
                    File: file, // ✅ Propiedad principal
                    file: file, // ✅ Compatibilidad
                    uiIndex: uiId,
                    tipo_mime: file.type,
                    documento_id: null, // Nuevo documento
                    ruta_fisica: '' // Nuevo documento
                };

                window.loadedDocuments[finalAmbito].push(newDoc);
                console.log(`✅ Documento '${file.name}' agregado al ámbito '${finalAmbito}'.`);

                // Añadir a la UI
                const docItem = `
            <div class="col-md-4 col-sm-6 mb-3 fade-in-up" data-ui-index="${uiId}" data-ambito="${finalAmbito}">
                <div class="card border card-animate">
                    <div class="card-body">
                        <div class="d-flex align-items-center">
                            <div class="flex-shrink-0 me-3">
                                <i class="${getIconByType(newDoc.tipo_mime)} fs-2 ${getColorByType(newDoc.tipo_mime)}"></i>
                            </div>
                            <div class="flex-grow-1">
                                <h6 class="mb-1 text-truncate">${newDoc.nombre_archivo}</h6>
                                <small class="text-muted">${newDoc.nombre_tipo_documento} (${newDoc.ambito_documento})</small>
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
                        ${newDoc.observaciones ? `<p class="text-muted mt-2 mb-0 text-wrap"><small>Obs: ${newDoc.observaciones}</small></p>` : ''}
                    </div>
                </div>
            </div>
            `;
                listaDocumentosDiv.append(docItem);

                // Re-inicializar tooltips
                $('[data-bs-toggle="tooltip"]').tooltip('dispose').tooltip();

                Swal.fire({
                    toast: true,
                    position: "top-end",
                    icon: "success",
                    title: "Archivo agregado",
                    showConfirmButton: false,
                    timer: 1500
                });

            } catch (error) {
                console.error("Error al procesar archivo:", error);
                Swal.fire({
                    icon: "error",
                    title: "Error de Archivo",
                    text: `No se pudo procesar el archivo ${file.name}.`,
                    confirmButtonText: "Aceptar"
                });
            }
        }

        // Limpiar campos después de agregar todos los archivos
        archivosDocumentoInput.val('');
        observacionesDocumentoTextarea.val('');
    });

    // Evento: Eliminar Documentos (Delegación de Eventos)
    listaDocumentosDiv.on("click", ".remove-doc-btn", function () {
        const card = $(this).closest(".col-md-4");
        const uiId = card.data("ui-index");
        const documentoId = card.data("doc-id");
        const ambito = card.data("ambito");

        Swal.fire({
            title: "¿Estás seguro?",
            text: "¡El documento será eliminado!",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            cancelButtonColor: "#3085d6",
            confirmButtonText: "Sí, eliminarlo!",
            cancelButtonText: "Cancelar",
        }).then((result) => {
            if (result.isConfirmed) {
                if (documentoId) {
                    // Documento guardado en la BD
                    $.ajax({
                        url: `${API_DOCUMENTOS_BASE_URL}?documentoId=${documentoId}`,
                        type: "POST",
                        headers: {
                            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                        },
                        success: function (response) {
                            card.remove();
                            // Eliminar del array loadedDocuments
                            if (ambito && window.loadedDocuments.hasOwnProperty(ambito)) {
                                window.loadedDocuments[ambito] = window.loadedDocuments[ambito].filter(doc => doc.documento_id !== documentoId);
                                console.log(`Documento con ID ${documentoId} eliminado (DB) del ámbito ${ambito}.`);
                            }
                            Swal.fire("Eliminado!", "El documento ha sido eliminado de la base de datos.", "success");
                        },
                        error: function (xhr, status, error) {
                            console.error("Error al eliminar documento de la BD:", xhr.responseText);
                            const errorMessage = xhr.responseJSON && xhr.responseJSON.message ? xhr.responseJSON.message : "Hubo un problema al eliminar el documento de la base de datos.";
                            Swal.fire("Error!", errorMessage, "error");
                        }
                    });
                } else if (typeof uiId !== 'undefined') {
                    // Documento en memoria
                    if (ambito && window.loadedDocuments.hasOwnProperty(ambito)) {
                        window.loadedDocuments[ambito] = window.loadedDocuments[ambito].filter(doc => doc.uiIndex !== uiId);
                        console.log(`Documento con uiId ${uiId} eliminado (memoria) del ámbito ${ambito}.`);
                    }
                    card.remove();
                    Swal.fire("Eliminado!", "El documento ha sido removido de la lista temporal.", "success");
                } else {
                    Swal.fire("Error!", "No se pudo identificar el documento para eliminar.", "error");
                }
            }
        });
    });

    // Evento: Previsualizar Documentos (Delegación de Eventos)
    listaDocumentosDiv.on("click", ".preview-doc-btn", function () {
        const card = $(this).closest(".col-md-4");
        const uiId = card.data("ui-index");
        const documentoId = card.data("doc-id");
        const ambito = card.data("ambito");

        let doc;
        // Buscar el documento en el ámbito correcto
        if (ambito && window.loadedDocuments.hasOwnProperty(ambito)) {
            if (typeof uiId !== 'undefined') {
                doc = window.loadedDocuments[ambito].find((d) => d.uiIndex === uiId);
            } else if (documentoId) {
                doc = window.loadedDocuments[ambito].find((d) => d.documento_id === documentoId);
            }
        }

        if (!doc) {
            Swal.fire({
                icon: "error",
                title: "Error de Previsualización",
                text: "Documento no encontrado en la memoria o no se pudo identificar.",
                confirmButtonText: "Aceptar"
            });
            return;
        }

        previewContent.empty(); // Limpiar contenido previo del modal

        let previewSource;
        let fileMime = doc.tipo_mime;
        let fileName = doc.nombre_archivo;

        // PRIORIDAD 1: Documentos recién agregados (en memoria, tienen el objeto File)
        if (doc.File || doc.file) {
            const fileObj = doc.File || doc.file;
            fileMime = fileObj.type;
            const reader = new FileReader();
            reader.onload = function (e) {
                previewSource = e.target.result;
                displayPreview(fileMime, previewSource, fileName);
                previewModal.show();
            };
            reader.readAsDataURL(fileObj);
            return;
        }
        // PRIORIDAD 2: Documentos ya guardados en DB
        else if (doc.documento_id && doc.ruta_fisica) {
            previewSource = `${API_CASOS_BASE_URL}?rutaRelativa=${encodeURIComponent(doc.ruta_fisica)}`;
        }
        else {
            Swal.fire({
                icon: "error",
                title: "Error de Previsualización",
                text: "No se encontró contenido o URL para previsualizar este documento.",
                confirmButtonText: "Aceptar"
            });
            return;
        }

        displayPreview(fileMime, previewSource, fileName);
        previewModal.show();
    });

    // Función displayPreview
    function displayPreview(mimeType, source, fileName) {
        previewContent.empty();

        if (mimeType && mimeType.includes("image")) {
            previewContent.append(`<img src="${source}" class="img-fluid" style="max-height: 80vh;" alt="${fileName}">`);
        } else if (mimeType && mimeType.includes("pdf")) {
            previewContent.append(`<iframe src="${source}" width="100%" height="600px" style="border: none;"></iframe>`);
        } else {
            const extension = fileName ? fileName.split(".").pop().toLowerCase() : 'desconocido';
            previewContent.append(`<p class="alert alert-warning">No se puede previsualizar este tipo de archivo: <strong>.${extension}</strong></p><a href="${source}" target="_blank" class="btn btn-primary mt-2">Descargar</a>`);
        }
    }

    // Funciones globales de utilidad para diagnóstico
    window.diagnosticarDocumentos = function () {
        if (window.casoFinancieroManager) {
            window.casoFinancieroManager.diagnosticarDocumentos();
        } else {
            console.warn('CasoFinancieroManager no está inicializado');
        }
    };

    // Función para verificar el estado completo
    window.verificarEstadoCompleto = function () {
        console.log('=== VERIFICACIÓN COMPLETA DEL ESTADO ===');
        console.log('window.loadedDocuments:', window.loadedDocuments);
        console.log('window.loadedDamagePhotos:', window.loadedDamagePhotos);
        console.log('window.loadedValorFiles:', window.loadedValorFiles);

        if (window.casoFinancieroManager) {
            console.log('CasoFinancieroManager inicializado correctamente');
            window.casoFinancieroManager.diagnosticarDocumentos();
        } else {
            console.warn('CasoFinancieroManager no está disponible');
        }
    };

    // Inicialización cuando el DOM esté listo
    $(document).ready(() => {
        window.casoFinancieroManager = new CasoFinancieroManager();
    });

   


});