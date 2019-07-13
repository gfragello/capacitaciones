$("#fotoCapacitado").change(function (e) {
    /*
    loadImage(
        e.target.files[0],
        function (img) {
    */
            alert("Cargando foto2!");

            //se define una variable de tipo FormData, porque los parámetros de entrada se van a pasar por post
            var formData = new FormData();

            formData.append('capacitadoId', $("#CapacitadoID").val());
            formData.append('foto', $('input[type=file]')[0].files[0]);
            
            alert("Mostrando capacitado id");
            alert($("#CapacitadoID").val());

            $.ajax({
                url: '/Capacitados/CargarFotoCapacitado',
                type: "POST",
                dataType: "JSON",
                data: formData,
                contentType: false,
                processData: false,
                success: function (resultadoOK) {
                    if (resultadoOK) {
                        alert("Funcionó!")
                        /*
                        //buscarCapacitados();
                        actualizarDisponibilidadJornada();
                        //$("#spanCantidadCuposDisponibles").text("Se agregó otro capacitado");
                        */
                    }
                    else {
                        alert("ERROR - NO Funcionó!")
                    }
                }
            });

            //document.body.appendChild(img); //se agrgega en el body del documento para visualizar,
        /*},
        { orientation: 6 } // Options
    );*/
});