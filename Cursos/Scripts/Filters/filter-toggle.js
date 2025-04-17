// Funcionalidad para toggle de filtros
$(function () {
    // Variable para recordar el estado
    var filtersVisible = true;

    // Funcionalidad para toggle de filtros usando jQuery directamente
    $('.filter-toggle').on('click', function (e) {
        e.preventDefault();

        // Toggle simple con jQuery
        $('#filterContent').slideToggle(300, function () {
            // Callback después de completar la animación
            filtersVisible = !filtersVisible;

            if (filtersVisible) {
                $('.filter-toggle').text('Ocultar Filtros');
            } else {
                $('.filter-toggle').text('Mostrar Filtros');
            }
        });
    });
});