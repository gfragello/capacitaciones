$(document).on("blur", '.txtDocumentacionAdicionalDatos', function (event) {
    var registroCapacitacionId = $(this).data("registrocapacitacionid")
    actualizarDocumentacionAdicionalDatos(registroCapacitacionId, $(this).val(), $("#txtDocumentacionAdicionalDatosAnteriores_" + registroCapacitacionId.toString()).val());
});

function actualizarDocumentacionAdicionalDatos(registroCapacitacionId, datos, datosAnteriores) {
    
    if (datos != datosAnteriores) {
        console.log("Actualizando datos...");
        $.ajax({
            url: '/RegistrosCapacitacion/ActualizarDocumentacionAdicionalDatos',
            type: "GET",
            dataType: "JSON",
            data:
            {
                registroCapacitacionId: registroCapacitacionId,
                documentacionAdicionalDatos: datos
            },
            success: function (datosActualizados) {
                if (datosActualizados) {
                    if (datos != "") { //sino está vacío se pone el fondo en VERDE
                        $("#txtDocumentacionAdicionalDatos_" + registroCapacitacionId.toString()).css("background-color", "#DCF9AF");
                    }
                    else { //si está vacío se pone el fondo en ROJO
                        $("#txtDocumentacionAdicionalDatos_" + registroCapacitacionId.toString()).css("background-color", "#F9C1AF");
                    }
                    $("#txtDocumentacionAdicionalDatosAnteriores_" + registroCapacitacionId.toString()).val(datos);
                    actualizarLabelsDocumentacionAdicional();
                }
                else {
                    alert("No se pudo actualizar los datos adicionales. Verifique si el valor es correcto.");
                    //$("#txtNota_" + registroCapacitacionId.toString()).val(notaValorAnterior);
                    $("#txtDocumentacionAdicionalDatos_" + registroCapacitacionId.toString()).focus();
                }
            }
        });
    }
    else {
        console.log("No se actualizarán los datos");
    }
}

function actualizarLabelsDocumentacionAdicional()
{
    var jornadaId = $("#JornadaID").val();
    console.log("jornadaId: " + $("#JornadaID").val());

    $.ajax({
        url: '/Jornadas/ObtenerDatosDocumentacionAdicional',
        type: "GET",
        dataType: "JSON",
        data: { JornadaId: jornadaId},
        success: function (datosDocumentacionAdicional)
        {
            $("#tdDocumentacionAdicional").html(datosDocumentacionAdicional.LabelDocumentacionAdicional);

            if (datosDocumentacionAdicional.DocumentacionAdicionalCompleta)
            {
                /*
                if ($("#documento").val() != "")
                    buscarCapacitados();

                //alert("Aún quedan cupos disponibles");
                $("#dvBotonAgregarCapacitado").show();
                $("#demo").show();
                */
            }
            else
            {
                /*
                //se ocultan las opciones para agregar nuevos incriptos
                $("#dvBotonAgregarCapacitado").hide();
                $("#demo").hide();

                //se vacía el contenido del DIV que incluye los resultados de búsqueda y el cuadro de texto de búsqueda por documento
                $("#documento").val("");
                $("#dvSeleccionarCapacitados").html("");
                */
            }
        }
    });
}