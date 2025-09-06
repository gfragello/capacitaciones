// Inicializa el autocompletado para empresas con soporte de tooltips y botón de limpiar
function initEmpresaAutocomplete({
    inputId,
    hiddenId,
    sugerenciasId,
    urlAutocomplete,
    placeholder = "Buscar empresa...",
    mensajeNoEncontrada = "No se encontraron empresas",
    showClearIcon = true,
    minLength = 3,
    inputMinWidth = 350,
    inputMaxWidth = 500,
    enableTooltips = true,
    valorInicial = null,
    onClear = null,
    onSelect = null
}) {
    const $input = $("#" + inputId);
    const $hidden = $("#" + hiddenId);
    const $sugerencias = $("#" + sugerenciasId);

    // Establecer placeholder y estilos de ancho
    $input.attr("placeholder", placeholder);
    $input.css({
        minWidth: inputMinWidth + "px",
        maxWidth: inputMaxWidth + "px",
        width: "100%",
        paddingRight: "2.2rem", // espacio para el icono "x"
        boxSizing: "border-box"
    });
    
    // Si hay valor inicial, establecerlo
    if (valorInicial && valorInicial.id && valorInicial.text) {
        $input.val(valorInicial.text);
        $hidden.val(valorInicial.id);
    }

    // Icono para limpiar input
    let $clearIcon = null;
    if (showClearIcon) {
        if ($("#clear-" + inputId).length === 0) {
            $clearIcon = $(`
                <span id="clear-${inputId}" title="Borrar filtro de empresa"
                    class="empresa-clear-icon">
                    <i class="fa fa-times text-secondary"></i>
                </span>`);
            // El input debe estar en un contenedor position:relative y con max-width
            $input.parent().css({ position: "relative", maxWidth: inputMaxWidth + "px" }).append($clearIcon);

            $clearIcon.on("click", function () {
                $input.val('');
                $hidden.val('');
                $sugerencias.hide().empty();
                $input.focus();
                
                // Si hay función onClear definida, ejecutarla
                if (typeof onClear === 'function') {
                    onClear();
                }
            });
        }
    }

    // Autocompletado AJAX
    $input.on("input", function () {
        const valor = $(this).val();
        $hidden.val(""); // Borra el id oculto si el usuario escribe
        if (valor.length >= minLength) {
            $.ajax({
                url: urlAutocomplete,
                data: { term: valor },
                success: function (data) {
                    $sugerencias.empty();
                    if (data.length > 0) {
                        data.forEach(function (item) {
                            const $a = $("<a></a>")
                                .addClass("list-group-item list-group-item-action sugerencia-empresa-item")
                                .text(item.label)
                                .attr("href", "#")
                                .attr("title", item.label)
                                .css({
                                    whiteSpace: "nowrap",
                                    overflow: "hidden",
                                    textOverflow: "ellipsis"
                                })
                                .on("mousedown", function (e) {
                                    $input.val(item.value);
                                    $hidden.val(item.id);
                                    $sugerencias.hide();
                                    e.preventDefault();
                                    
                                    // Si hay función onSelect definida, ejecutarla
                                    if (typeof onSelect === 'function') {
                                        onSelect(item);
                                    }
                                });

                            if (enableTooltips) {
                                $a.attr("data-bs-toggle", "tooltip")
                                    .attr("data-bs-placement", "right");
                            }
                            $sugerencias.append($a);
                        });
                        $sugerencias.show();

                        // Inicializar tooltips si corresponde (Bootstrap 5)
                        if (enableTooltips && typeof bootstrap !== "undefined") {
                            $sugerencias.find('[data-bs-toggle="tooltip"]').each(function () {
                                if (this._tooltip) this._tooltip.dispose();
                                this._tooltip = new bootstrap.Tooltip(this, { html: false });
                            });
                        }
                    } else {
                        $sugerencias.append(
                            $("<div></div>")
                                .addClass("list-group-item disabled")
                                .text(mensajeNoEncontrada)
                        ).show();
                    }
                }
            });
        } else {
            $sugerencias.hide();
        }
    });

    // Ocultar sugerencias si se hace click fuera
    $(document).on("click", function (e) {
        if (!$(e.target).closest($input).length && !$(e.target).closest($sugerencias).length) {
            $sugerencias.hide();
        }
    });

    // Limpiar ambos campos si el usuario borra el input
    $input.on("change keyup", function () {
        if (!$(this).val()) {
            $hidden.val("");
        }
    });

    // Inicializar tooltips al cargar (por si acaso)
    if (enableTooltips && typeof bootstrap !== "undefined") {
        $('[data-bs-toggle="tooltip"]').each(function () {
            new bootstrap.Tooltip(this, { html: false });
        });
    }
}