// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Initialize Select2 for all select elements
$(document).ready(function() {
    // Initialize Select2 for existing select elements
    initializeSelect2();
    
    // Re-initialize Select2 when modals are shown (for dynamic content)
    $('.modal').on('shown.bs.modal', function () {
        initializeSelect2();
    });
});

function initializeSelect2() {
    // Initialize Select2 for all select elements that don't already have it
    $('select:not(.select2-hidden-accessible)').each(function() {
        var $select = $(this);
        var placeholder = $select.find('option:first').text() || 'Seçiniz...';
        
        // Configure Select2 options
        var options = {
            theme: 'bootstrap-5',
            width: '100%',
            placeholder: placeholder,
            allowClear: !$select.prop('required'),
            language: {
                noResults: function() {
                    return 'Sonuç bulunamadı';
                },
                searching: function() {
                    return 'Aranıyor...';
                },
                loadingMore: function() {
                    return 'Daha fazla yükleniyor...';
                },
                inputTooShort: function(args) {
                    return 'En az ' + args.minimum + ' karakter giriniz';
                },
                inputTooLong: function(args) {
                    return 'En fazla ' + args.maximum + ' karakter girebilirsiniz';
                },
                maximumSelected: function(args) {
                    return 'En fazla ' + args.maximum + ' seçim yapabilirsiniz';
                }
            }
        };
        
        // Special handling for dropdowns in modals
        if ($select.closest('.modal').length > 0) {
            options.dropdownParent = $select.closest('.modal');
        }
        
        // Initialize Select2
        $select.select2(options);
    });
}

// Function to refresh Select2 for dynamically added content
function refreshSelect2(container) {
    if (container) {
        $(container).find('select:not(.select2-hidden-accessible)').each(function() {
            var $select = $(this);
            var placeholder = $select.find('option:first').text() || 'Seçiniz...';
            
            var options = {
                theme: 'bootstrap-5',
                width: '100%',
                placeholder: placeholder,
                allowClear: !$select.prop('required'),
                language: {
                    noResults: function() {
                        return 'Sonuç bulunamadı';
                    },
                    searching: function() {
                        return 'Aranıyor...';
                    }
                }
            };
            
            if ($select.closest('.modal').length > 0) {
                options.dropdownParent = $select.closest('.modal');
            }
            
            $select.select2(options);
        });
    } else {
        initializeSelect2();
    }
}
