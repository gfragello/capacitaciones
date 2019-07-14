$("#fotoCapacitado").change(function (e) {
    /*
    loadImage(
        e.target.files[0],
        function (img) {
    */
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

            //document.body.appendChild(img); //se agrgega en el body del documento para visualizar,
        /*},
        { orientation: 6 } // Options
    );*/
});

$("#imgRotarIzquierda").click(function () {
    rotarFoto("i");
});

$("#imgRotarDerecha").click(function () {
    rotarFoto("d");
});

function rotarFoto(direccion)
{
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
                //se recarga la página para que se muestre la foto en su nueva orientación
                //cambiar este código para solo recargar el elemtno foto para que funcione en la versión que se muestra en una ventana modal
                //location.reload(true);

                var srcCapacitadoFoto = $("#imgCapacitadoFoto").attr('src') + "&" + new Date().getTime();
                //srcCapacitadoFoto += "?timestamp=" + new Date().getTime();

                alert(srcCapacitadoFoto);

                $("#imgCapacitadoFoto").attr("src", "");
                $("#imgCapacitadoFoto").attr("src", srcCapacitadoFoto);
            }
        }
    });
}