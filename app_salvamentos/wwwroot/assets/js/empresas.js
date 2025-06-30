// Inicializa AutoNumeric en todos los inputs visibles con la clase "autonumeric"
document.addEventListener('DOMContentLoaded', function () {
    console.log("DOM cargado. Inicializando AutoNumeric para campos visibles...");
    // Usa el selector para los inputs visibles (asegúrate de que sean de tipo "text")
    let selector = ".autonumeric";
    console.log("Selector para AutoNumeric:", selector);
    // Inicializamos AutoNumeric en los inputs visibles
    let instances = AutoNumeric.multiple(selector, {
        digitGroupSeparator: '.',    // Separador de miles
        decimalCharacter: ',',       // Separador decimal
        decimalPlaces: 2,            // Número de decimales
        modifyValueOnWheel: false    // Evita modificar el valor con la rueda del mouse
    });
    console.log("Instancias AutoNumeric (visibles):", instances);

    // Asigna el evento "input" a cada campo visible para actualizar el campo oculto
    const visibleInputs = document.querySelectorAll(selector);
    console.log("Campos visibles encontrados:", visibleInputs);
    visibleInputs.forEach(input => {
        input.addEventListener("input", function () {
            // Usa la instancia de AutoNumeric para obtener el valor numérico sin formato
            let instance = AutoNumeric.getAutoNumericElement(input);
            let numericValue = instance ? instance.getNumber() : 0;
            console.log("Campo modificado:", input.id, "Valor numérico:", numericValue);
            // Determina el id del campo oculto (quitamos la parte "Visible" del id)
            let hiddenId = input.id.replace("Visible", "");
            let hiddenField = document.getElementById(hiddenId);
            if (hiddenField) {
                hiddenField.value = numericValue;
                console.log("Campo oculto", hiddenId, "actualizado con:", hiddenField.value);
            }
            calcularCupo(); // Recalcula al modificar el campo
        });
    });
    // Ejecuta el cálculo inicial al cargar la página
    calcularCupo();
});

// Función helper para obtener el valor numérico desde el campo oculto
function getNumberValue(id) {
    let element = document.getElementById(id);
    if (!element) {
        console.log("Elemento con id", id, "no encontrado.");
        return 0;
    }
    // El campo oculto ya tiene el valor limpio
    let value = parseFloat(element.value) || 0;
    console.log("Valor obtenido para", id, ":", value);
    return value;
}

function calcularCupo() {
    console.log("Ejecutando calcularCupo...");
    // Obtener valores usando los campos ocultos
    const activoCorriente = getNumberValue("EmpfActivoCorriente");
    const activoFijo = getNumberValue("EmpfActivoFijo");
    const activoDiferido = getNumberValue("EmpfActivoDiferido");
    const otrosActivos = getNumberValue("EmpfOtrosActivos");
    const pasivoCorriente = getNumberValue("EmpfPasivoCorriente");
    const pasivoLargoPlazo = getNumberValue("EmpfPasivoLargoPlazo");
    const pasivoDiferido = getNumberValue("EmpfPasivoDiferido");
    const capital = getNumberValue("EmpfCapital");
    const reserva = getNumberValue("EmpfReserva");
    const otrasCuentasPatrimonio = getNumberValue("EmpfOtrasCuentasPatrimonio");
    const utilidadesAcumuladas = getNumberValue("EmpfUtilidadesAcumuladas");
    const utilidadEjercicio = getNumberValue("EmpfUtilidadEjercicio");
    const perdida = getNumberValue("EmpfPerdida");
    const otrasPerdidas = getNumberValue("EmpfOtrasPerdidas");
    const ventas = getNumberValue("EmpfVentas");
    const utilidad = getNumberValue("EmpfUtilidad");

    // Cálculo de totales.
    const totalActivo = activoCorriente + activoFijo + activoDiferido + otrosActivos;
    document.getElementById("EmpfTotalActivos").value = totalActivo.toFixed(2);
    console.log("Total Activo:", totalActivo);

    const totalPasivo = pasivoCorriente + pasivoLargoPlazo + pasivoDiferido;
    document.getElementById("EmpfTotalPasivo").value = totalPasivo.toFixed(2);
    console.log("Total Pasivo:", totalPasivo);

    const patrimonioNeto = capital + reserva + otrasCuentasPatrimonio + utilidadesAcumuladas + utilidadEjercicio - perdida - otrasPerdidas;
    document.getElementById("EmpfPatrimonioNeto").value = patrimonioNeto.toFixed(2);
    console.log("Patrimonio Neto:", patrimonioNeto);

    const patrimonioPasivo = patrimonioNeto + totalPasivo;
    document.getElementById("EmpfPasivoPatrimonio").value = patrimonioPasivo.toFixed(2);
    console.log("Patrimonio + Pasivo:", patrimonioPasivo);

    // Calcular base de cupo y cupo final.
    const baseCupo = (activoCorriente * 0.1) + (activoFijo * 0.05) + (ventas * 0.03) + (utilidad * 0.01) + (patrimonioNeto * 0.2);
    const cupoFinal = baseCupo * 0.10;
    console.log("Base Cupo:", baseCupo, "Cupo Final calculado:", cupoFinal);
    document.getElementById("EmpfCupoAsignado").value = cupoFinal.toFixed(2);

    // Cálculo de ratios financieros con control de división por cero.
    const liquidez = pasivoCorriente !== 0 ? activoCorriente / pasivoCorriente : 0;
    validarCampo("AnfLiquidez", liquidez, (val) => val >= 1);

    const solvencia = totalPasivo !== 0 ? totalActivo / totalPasivo : 0;
    validarCampo("AnfSolvencia", solvencia, (val) => val >= 1);

    const capCobertura = totalPasivo !== 0 ? activoFijo / totalPasivo : 0;
    validarCampo("AnfCapCobertura", capCobertura, (val) => val >= 1);

    const endeudamiento = totalActivo !== 0 ? totalPasivo / totalActivo : 0;
    validarCampo("AnfEndeudamiento", endeudamiento, (val) => val < 1);

    const capitalPropio = totalActivo !== 0 ? patrimonioNeto / totalActivo : 0;
    validarCampo("AnfCapitalPropio", capitalPropio, (val) => val >= 1);

    // Calcular ROA y ROE (en porcentaje)
    const roa = totalActivo !== 0 ? (utilidad / totalActivo) * 100 : 0;
    document.getElementById("AnfROA").value = roa.toFixed(2);

    const roe = patrimonioNeto !== 0 ? (utilidad / patrimonioNeto) * 100 : 0;
    document.getElementById("AnfROE").value = roe.toFixed(2);
}

function validarCampo(inputId, valor, condicion) {
    const input = document.getElementById(inputId);
    if (!input) return;
    input.value = valor.toFixed(2);
    if (condicion(valor)) {
        input.classList.add("valido");
        input.classList.remove("invalido");
    } else {
        input.classList.add("invalido");
        input.classList.remove("valido");
    }
}
