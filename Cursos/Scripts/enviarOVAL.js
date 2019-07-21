var tipoDeEnvioOVAL = ""; //RI - Registro Individual; RL - Todos los registros listos para envío; RJ - Todos los registros de una jornada
var registroCapacitacionId = "";
var jornadaId = "";

$(".btnEnvioRegistroOVAL").click(function () {

    registroCapacitacionId = $(this).data("registrocapacitacionid");
    tipoDeEnvioOVAL = $(this).data("tipodeenviooval");

    iniciarEnvioDatosOVAL();

});

function iniciarEnvioDatosOVAL() {
    if (confirm("¿Enviar los datos al sistema OVAL?")) {
        //al abrir la ventana modal, se dispara la ejecución del envío de datos
        $('#modalMensaje').modal('show');
    }
}

//al abrir la ventana modal, se dispara la ejecución del envío de datos
$("#modalMensaje").on('shown.bs.modal', function () {
    
    switch (tipoDeEnvioOVAL) {

        case "RI":
            
            $.ajax({
                url: '/RegistrosCapacitacion/EnviarDatosRegistroOVAL',
                type: "GET",
                dataType: "JSON",
                data: { registroCapacitacionId: registroCapacitacionId },
                success: function (resultadoEnviarDatosOVAL) {
                    mostrarResultadoEnviarDatosOVAL(resultadoEnviarDatosOVAL);
                }
            });

            break;

        case "RJ":

            $.ajax({
                url: '/Jornadas/EnviarDatosOVAL',
                type: "GET",
                dataType: "JSON",
                data: { jornadaId: jornadaId },
                success: function (resultadoEnviarDatosOVAL) {

                    var mensaje = "";

                    if (resultadoEnviarDatosOVAL.totalAceptados > 0)
                        mensaje += "<div class='alert alert-success'><strong>Se recibieron correctamente " + resultadoEnviarDatosOVAL.totalAceptados.toString() + " registros.</strong></div>"

                    //mensaje += "<p>Se recibieron correctamente " + resultadoEnviarDatosOVAL.totalAceptados.toString() + " registros.</p>"

                    if (resultadoEnviarDatosOVAL.totalRechazados > 0)
                        mensaje += "<div class='alert alert-warning'><strong>Se rechazaron " + resultadoEnviarDatosOVAL.totalRechazados.toString() + " registros.</strong></div>"

                    //mensaje += "<p>Se rechazaron " + resultadoEnviarDatosOVAL.totalRechazados.toString() + " registros.</p>";

                    $("#bodyMensaje").html(mensaje);
                }
            });

            break;

        default:
            alert("No se pudo realizar el envío");
    }

});

function mostrarResultadoEnviarDatosOVAL(resultadoEnviarDatosOVAL)
{
    var mensaje = "";

    if (resultadoEnviarDatosOVAL.totalAceptados > 0)
        mensaje += "<div class='alert alert-success'><strong>Se recibieron correctamente " + resultadoEnviarDatosOVAL.totalAceptados.toString() + " registros.</strong></div>";

    if (resultadoEnviarDatosOVAL.totalRechazados > 0)
        mensaje += "<div class='alert alert-warning'><strong>Se rechazaron " + resultadoEnviarDatosOVAL.totalRechazados.toString() + " registros.</strong></div>";

    $("#bodyMensaje").html(mensaje);
}