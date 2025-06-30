// Función debounce: retrasa la ejecución de una función hasta que hayan pasado 'delay' ms desde el último llamado
function debounce(fn, delay) {
    let timer = null;
    return function () {
        clearTimeout(timer);
        timer = setTimeout(fn, delay);
    };
}

// Función para habilitar o deshabilitar formularios de forma dinámica
function toggleFormState(formId, isVisible) {
    const form = document.getElementById(formId);
    if (form) {
        if (isVisible) {
            form.classList.remove("d-none");
            form.querySelectorAll("input, select, textarea").forEach(el => {
                el.disabled = false;
            });
        } else {
            form.classList.add("d-none");
            form.querySelectorAll("input, select, textarea").forEach(el => {
                el.disabled = true;
            });
        }
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // Renombramos la variable para reflejar que se valida sobre el monto de la fianza
    let porcentajeMaximoFianza = 0;
    let hotInstance;

    // ---------- MOSTRAR FORMULARIOS DE PRENDA Y ASIGNAR TIPO A LA TABLA ----------
    function mostrarFormularioPrenda() {
        // Ocultar y deshabilitar todos los formularios de prenda
        toggleFormState("formPrendaComercial", false);
        toggleFormState("formPrendaIndustrial", false);
        toggleFormState("formPrendaHipotecaria", false);
        toggleFormState("formPrendaCartaAval", false);

        const prendaSeleccionada = document.querySelector('input[name="prenda"]:checked');
        if (prendaSeleccionada) {
            switch (prendaSeleccionada.value) {
                case "Prenda Comercial":
                    toggleFormState("formPrendaComercial", true);
                    break;
                case "Prenda Industrial":
                    toggleFormState("formPrendaIndustrial", true);
                    break;
                case "Prenda Hipotecaria":
                    toggleFormState("formPrendaHipotecaria", true);
                    break;
                case "Prenda Carta Aval":
                    toggleFormState("formPrendaCartaAval", true);
                    break;
                default:
                    break;
            }
            // Asigna el tipo de prenda a todas las filas de Handsontable
            if (hotInstance) {
                let selectedType = prendaSeleccionada.value;
                let rowCount = hotInstance.countRows();
                for (let i = 0; i < rowCount; i++) {
                    hotInstance.setDataAtRowProp(i, 'PrenTipo', selectedType);
                }
            }
        }
    }
    document.querySelectorAll('input[name="prenda"]').forEach(radio => {
        radio.addEventListener("change", mostrarFormularioPrenda);
    });

    // ---------- FUNCIONES PARA MANEJO DE EMPRESA Y FIANZA ----------
    function cargarDatosEmpresa(empresaId) {
        const selectedId = parseInt(empresaId, 10);
        let cupo = 0;
        if (selectedId) {
            const emp = empresasData.find(e => e.EmpId === selectedId);
            if (emp && emp.Historial && emp.Historial.CupoRestante !== null) {
                cupo = emp.Historial.CupoRestante;
            }
            // Sección FCC/BUA
            document.getElementById("solicitante").value = emp.EmpNombre || "";
            document.getElementById("direccionSolicitante").value = emp.EmpUbicacion || "";
            document.getElementById("rucSolicitante").value = emp.EmpRUC || "";
            document.getElementById("emailSolicitante").value = emp.EmpEmail || "";
            document.getElementById("telefonoSolicitante").value = emp.EmpTelefono || "";
            // Sección GA
            document.getElementById("solicitanteGA").value = emp.EmpNombre || "";
            document.getElementById("direccionSolicitanteGA").value = emp.EmpUbicacion || "";
            document.getElementById("rucSolicitanteGA").value = emp.EmpRUC || "";
            document.getElementById("emailSolicitanteGA").value = emp.EmpEmail || "";
            document.getElementById("telefonoSolicitanteGA").value = emp.EmpTelefono || "";
            // Habilitar campos de monto de fianza
            document.getElementById("montoFianza").disabled = false;
            document.getElementById("montoFianzaGA").disabled = false;
            const cupoInput = document.getElementById("cupoDisponible");
            cupoInput.value = parseFloat(cupo).toLocaleString('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            cupoInput.dataset.valorNumerico = cupo;
        } else {
            limpiarCamposEmpresa();
        }
        // Se invoca la validación de fianza (ya no se utiliza para contrato)
        validarMontoFianza();
    }
    function cargarTipoFianza(tipoSeleccionado) {
        tipoSeleccionado = parseInt(tipoSeleccionado, 10);
        // Ocultar secciones y deshabilitar controles asociados
        toggleFormState("datosContrato", false);
        toggleFormState("datosAduanera", false);
        porcentajeMaximoFianza = 0;
        if (tipoSeleccionado === 1) {
            porcentajeMaximoFianza = 15;
            toggleFormState("datosContrato", true);
        } else if (tipoSeleccionado === 2) {
            porcentajeMaximoFianza = 100;
            toggleFormState("datosAduanera", true);
        } else if (tipoSeleccionado === 3) {
            porcentajeMaximoFianza = 50;
            toggleFormState("datosContrato", true);
        }
        // Se limpian los campos para evitar inconsistencias
        document.getElementById("montocontrato").value = "";
        document.getElementById("montoFianza").value = "";
        document.getElementById("montoFianzaGA").value = "";
        validarMontoFianza();
    }
    const tipoFianzaSelect = document.getElementById("tipoFianza");
    if (tipoFianzaSelect) {
        tipoFianzaSelect.addEventListener("change", function () {
            cargarTipoFianza(this.value);
        });
    }
    if (typeof empresasData !== 'undefined') {
        const empresaSelect = document.getElementById("empresaSelect");
        if (empresaSelect) {
            empresaSelect.addEventListener("change", function () {
                cargarDatosEmpresa(this.value);
            });
        }
    }
    function limpiarCamposEmpresa() {
        const campos = ["solicitante", "direccionSolicitante", "rucSolicitante", "emailSolicitante", "telefonoSolicitante"];
        const camposGA = ["solicitanteGA", "direccionSolicitanteGA", "rucSolicitanteGA", "emailSolicitanteGA", "telefonoSolicitanteGA"];
        campos.forEach(id => document.getElementById(id).value = "");
        camposGA.forEach(id => document.getElementById(id).value = "");
        document.getElementById("montoFianza").disabled = true;
        document.getElementById("montoFianzaGA").disabled = true;
        document.getElementById("cupoDisponible").value = "";
        document.getElementById("cupoDisponible").dataset.valorNumerico = 0;
    }
    function mostrarError(errorElem, inputElem, mensaje) {
        errorElem.textContent = mensaje;
        errorElem.classList.remove("d-none");
        inputElem.classList.add("is-invalid");
    }
    function ocultarError(errorElem, inputElem) {
        errorElem.textContent = "";
        errorElem.classList.add("d-none");
        inputElem.classList.remove("is-invalid");
    }

    // Validación únicamente sobre el monto de la fianza
    function validarMontoFianza() {
        let montoFianzaElem, errorMontoFianza;
        if (!document.getElementById("datosAduanera").classList.contains("d-none")) {
            montoFianzaElem = document.getElementById("montoFianzaGA");
            errorMontoFianza = document.getElementById("errorMontoFianzaGA") || document.getElementById("errorMontoFianza");
        } else {
            montoFianzaElem = document.getElementById("montoFianza");
            errorMontoFianza = document.getElementById("errorMontoFianza");
        }
        const cupoDisponibleElem = document.getElementById("cupoDisponible");
        const cupoDisponible = parseFloat(cupoDisponibleElem.dataset.valorNumerico) || 0;
        const montoFianza = parseFloat(montoFianzaElem.value) || 0;

        if (porcentajeMaximoFianza === 0 || cupoDisponible === 0) {
            mostrarError(errorMontoFianza, montoFianzaElem, "Debe seleccionar una empresa y un tipo de fianza.");
            return;
        }

        // Se calcula el máximo permitido basándose en el porcentaje para la fianza
        const maxPermitido = (cupoDisponible * porcentajeMaximoFianza) / 100;
        if (montoFianza > maxPermitido) {
            mostrarError(errorMontoFianza, montoFianzaElem, `El monto de la fianza supera el ${porcentajeMaximoFianza}% del cupo disponible ($${maxPermitido.toFixed(2)}).`);
        } else {
            ocultarError(errorMontoFianza, montoFianzaElem);
        }

        // Validación opcional para que el monto de la fianza no supere el cupo completo
        if (montoFianza > cupoDisponible) {
            mostrarError(errorMontoFianza, montoFianzaElem, `El monto de la fianza no puede superar el cupo disponible ($${cupoDisponible.toFixed(2)}).`);
        }

        // Mostrar u ocultar documentos secundarios según el monto de la fianza
        const documentosSecundarios = document.getElementById("documentosSecundarios");
        if (montoFianza > 416000) {
            documentosSecundarios.classList.remove("d-none");
        } else {
            documentosSecundarios.classList.add("d-none");
        }
    }

    function calcularPlazoEnDias() {
        let inicio, fin, plazoElem;
        if (!document.getElementById("datosAduanera").classList.contains("d-none")) {
            inicio = document.getElementById("inicioVigenciaGA").value;
            fin = document.getElementById("finVigenciaGA").value;
            plazoElem = document.getElementById("plazoGarantiaDiasGA");
        } else {
            inicio = document.getElementById("inicioVigencia").value;
            fin = document.getElementById("finVigencia").value;
            plazoElem = document.getElementById("plazoGarantiaDias");
        }
        if (!inicio || !fin) {
            plazoElem.value = "";
            return;
        }
        const inicioDate = new Date(inicio);
        const finDate = new Date(fin);
        const diferenciaMs = finDate - inicioDate;
        const diferenciaDias = Math.ceil(diferenciaMs / (1000 * 60 * 60 * 24));
        if (diferenciaDias < 0) {
            plazoElem.value = "";
            alert("La fecha de fin debe ser posterior a la de inicio.");
            return;
        }
        plazoElem.value = diferenciaDias;
    }

    // Asignar eventos para calcular el plazo
    const inicioVigenciaElem = document.getElementById("inicioVigencia");
    const finVigenciaElem = document.getElementById("finVigencia");
    const inicioVigenciaGAElem = document.getElementById("inicioVigenciaGA");
    const finVigenciaGAElem = document.getElementById("finVigenciaGA");
    if (inicioVigenciaElem) { inicioVigenciaElem.addEventListener("change", calcularPlazoEnDias); }
    if (finVigenciaElem) { finVigenciaElem.addEventListener("change", calcularPlazoEnDias); }
    if (inicioVigenciaGAElem) { inicioVigenciaGAElem.addEventListener("change", calcularPlazoEnDias); }
    if (finVigenciaGAElem) { finVigenciaGAElem.addEventListener("change", calcularPlazoEnDias); }

    // Se asignan eventos de validación únicamente a los campos de monto de fianza
    const montoFianzaElem = document.getElementById("montoFianza");
    const montoFianzaGAElem = document.getElementById("montoFianzaGA");
    if (montoFianzaElem) { montoFianzaElem.addEventListener("input", validarMontoFianza); }
    if (montoFianzaGAElem) { montoFianzaGAElem.addEventListener("input", validarMontoFianza); }

    if (typeof empresasData !== 'undefined') {
        const empresaSelect = document.getElementById("empresaSelect");
        if (empresaSelect && empresaSelect.value) { cargarDatosEmpresa(empresaSelect.value); }
    }
    if (tipoFianzaSelect && tipoFianzaSelect.value) { cargarTipoFianza(tipoFianzaSelect.value); }

    // ---------- INICIALIZACIÓN DE HANDSONTABLE PARA PRENDA COMERCIAL ----------
    var container = document.getElementById('prendasExcel');
    if (container) {
        hotInstance = new Handsontable(container, {
            data: [],
            dataSchema: {
                PrenFechaCreacion: new Date(),
                PrenTipo: "",
                PrenBien: "",
                PrenDescripcion: "",
                PrenValor: null,
                PrenUbicacion: "",
                PrenCustodio: "",
                PrenFechaConstatacion: null,
                PrenResponsableConstatacion: "",
                PrenArchivo: null,
                PrenNumeroItem: null,
                PrenValorTotal: null
            },
            minSpareRows: 1,
            columns: [
                { data: 'PrenTipo', type: 'text' },
                { data: 'PrenNumeroItem', type: 'numeric' },
                { data: 'PrenBien', type: 'text' },
                { data: 'PrenDescripcion', type: 'text' },
                { data: 'PrenValor', type: 'numeric', numericFormat: { pattern: '0,0.00', culture: 'en-US' } },
                { data: 'PrenUbicacion', type: 'text' },
                { data: 'PrenCustodio', type: 'text' },
                { data: 'PrenFechaConstatacion', type: 'date', dateFormat: 'YYYY-MM-DD' },
                { data: 'PrenResponsableConstatacion', type: 'text' }
            ],
            colHeaders: [
                'Tipo',
                'Número Item',
                'Bien',
                'Contenido/Descripción',
                'Valor',
                'Ubicación',
                'Custodio',
                'Fecha Constatación',
                'Responsable Constatación'
            ],
            rowHeaders: true,
            stretchH: 'all',
            licenseKey: 'non-commercial-and-evaluation',
            afterChange: debounce(function (changes, source) {
                if (source === 'loadData' || !changes) return;
                let total = 0;
                let data = this.getSourceData();
                for (let i = 0; i < data.length; i++) {
                    let cellValue = data[i].PrenValor;
                    if (cellValue != null) {
                        let numVal = parseFloat(cellValue);
                        if (!isNaN(numVal)) {
                            total += numVal;
                        }
                    }
                    if (!data[i].PrenFechaCreacion) {
                        data[i].PrenFechaCreacion = new Date();
                    }
                }
                let totalInput = document.getElementById("totalPrendaValor");
                if (totalInput) {
                    totalInput.value = total.toFixed(2);
                }
                for (let i = 0; i < data.length; i++) {
                    this.setDataAtRowProp(i, 'PrenValorTotal', total);
                }
            }, 300)
        });
        document.getElementById("formPrendaComercial").addEventListener('transitionend', function () {
            hotInstance.render();
        });
    }

    // ---------- VALIDACIÓN UNIFICADA DEL FORMULARIO ----------
    const mainForm = document.getElementById('solicitudFianzaForm');
    if (mainForm) {
        mainForm.addEventListener('submit', function (event) {
            // Deshabilitar controles ocultos para que no sean validados
            mainForm.querySelectorAll('.d-none input, .d-none select, .d-none textarea')
                .forEach(control => control.disabled = true);

            // Validación personalizada: recorrer solo los campos visibles con atributo required
            let missingFields = [];
            mainForm.querySelectorAll('[required]').forEach(function (field) {
                if (field.offsetParent === null) return;
                if (!field.checkValidity()) {
                    let fieldName = "";
                    if (field.id) {
                        const label = mainForm.querySelector(`label[for="${field.id}"]`);
                        fieldName = label ? label.textContent.trim() : "";
                    }
                    if (!fieldName && field.placeholder) {
                        fieldName = field.placeholder.trim();
                    }
                    missingFields.push(fieldName);
                }
            });

            if (missingFields.length > 0) {
                event.preventDefault();
                const missingFieldsMessage = 'Por favor complete los siguientes campos requeridos:\n\n' + missingFields.join(';\n');
                Swal.fire({
                    title: '¡Error!',
                    text: missingFieldsMessage,
                    icon: 'error',
                    confirmButtonText: 'OK'
                }).then(() => {
                    const firstInvalid = mainForm.querySelector('[required]:invalid');
                    if (firstInvalid) {
                        firstInvalid.focus();
                    }
                });
                return;
            }

            // Si Handsontable está activo, filtrar filas vacías y asignar al input oculto
            if (typeof hotInstance !== 'undefined') {
                let allData = hotInstance.getSourceData();
                let validData = allData.filter(function (row) {
                    return (row.PrenTipo && row.PrenTipo.trim() !== "") ||
                        (row.PrenBien && row.PrenBien.trim() !== "") ||
                        (row.PrenDescripcion && row.PrenDescripcion.trim() !== "");
                });
                document.getElementById('PrendasJson').value = JSON.stringify(validData);
            }
        });
    }
});
