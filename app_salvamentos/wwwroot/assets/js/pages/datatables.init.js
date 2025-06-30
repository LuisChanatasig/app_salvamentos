function initializeTables() {
    // Tabla básica
    let example = new DataTable('#example', {
        language: {
            url: '//cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json'
        }
    });

    // Scroll vertical
    let scrollVertical = new DataTable('#scroll-vertical', {
        scrollY: "210px",
        scrollCollapse: true,
        paging: false,
        language: {
            url: '//cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json'
        }
    });

    // Scroll horizontal
    let scrollHorizontal = new DataTable('#scroll-horizontal', {
        scrollX: true,
        language: {
            url: '//cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json'
        }
    });

    // Paginación alternativa
    let alternativePagination = new DataTable('#alternative-pagination', {
        pagingType: "full_numbers",
        language: {
            url: '//cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json'
        }
    });

    // Fixed header
    let fixedHeader = new DataTable('#fixed-header', {
        fixedHeader: true,
        language: {
            url: '//cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json'
        }
    });

    // Modal data tables
    let modelDataTables = new DataTable('#model-datatables', {
        responsive: {
            details: {
                display: $.fn.dataTable.Responsive.display.modal({
                    header: function (row) {
                        var data = row.data();
                        return 'Details for ' + data[0] + ' ' + data[1];
                    }
                }),
                renderer: $.fn.dataTable.Responsive.renderer.tableAll({
                    tableClass: 'table'
                })
            }
        },
        language: {
            url: '//cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json'
        }
    });

    // Botones
    let buttonsDataTables = new DataTable('#buttons-datatables', {
        dom: 'Bfrtip',
        buttons: [
            'copy', 'csv', 'excel', 'print', 'pdf'
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json'
        }
    });

    // Ajax
    let ajaxDataTables = new DataTable('#ajax-datatables', {
        ajax: '/assets/json/datatable.json',
        language: {
            url: '//cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json'
        }
    });

    // Ejemplo de añadir filas dinámicamente
    var t = $('#add-rows').DataTable({
        language: {
            url: '//cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json'
        }
    });
    var counter = 1;

    $('#addRow').on('click', function () {
        t.row.add([
            counter + '.1',
            counter + '.2',
            counter + '.3',
            counter + '.4',
            counter + '.5',
            counter + '.6',
            counter + '.7',
            counter + '.8',
            counter + '.9',
            counter + '.10',
            counter + '.11',
            counter + '.12'
        ]).draw(false);

        counter++;
    });

    // Agregamos una fila al cargar
    $('#addRow').trigger('click');
}

document.addEventListener('DOMContentLoaded', function () {
    initializeTables();
});
