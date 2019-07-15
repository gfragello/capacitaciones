$("#fotoCapacitado").change(function (e) {

    //se muestra la imagen que indica que se está procesando
    $("#imgCapacitadoFoto").attr("src", "/images/cargando.gif");

    //se define una variable de tipo FormData, porque los parámetros de entrada se van a pasar por post
    var formData = new FormData();

    formData.append('capacitadoId', $("#CapacitadoID").val());
    formData.append('foto', $('input[type=file]')[0].files[0]);
            
    $.ajax({
        url: '/Capacitados/CargarFotoCapacitado',
        type: "POST",
        dataType: "JSON",
        data: formData,
        contentType: false,
        processData: false,
        success: function (resultadoOK) {
            if (resultadoOK) {
                //se recarga la página para que se muestre la foto recién cargada
                //cambiar este código para solo recargar el elemtno foto para que funcione en la versión que se muestra en una ventana modal
                location.reload(true);
            }
            else {
                alert("Error cargando la foto. Vuelva a intentarlo.")
            }
        }
    });
});

$("#imgRotarIzquierda").click(function () {
    rotarFoto("i");
});

$("#imgRotarDerecha").click(function () {
    rotarFoto("d");
});

function rotarFoto(direccion)
{
    //se muestra la imagen que indica que se está procesando
    $("#imgCapacitadoFoto").attr("src", "/images/cargando.gif");

    var capacitadoId = $("#CapacitadoID").val();

    $.ajax({
        url: '/Capacitados/RotarFotoCapacitado',
        type: "GET",
        dataType: "JSON",
        data: { capacitadoId: capacitadoId,
                direccion: direccion},
        success: function (resultadoOK)
        {
            if (resultadoOK)
            {
                var srcCapacitadoFoto = $("#srcCapacitadoFotoOriginal").val() + "&" + new Date().getTime();
                $("#imgCapacitadoFoto").attr("src", srcCapacitadoFoto);
            }
        }
    });
}