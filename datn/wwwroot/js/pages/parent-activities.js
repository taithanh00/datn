document.addEventListener('DOMContentLoaded', function() {
    if (document.getElementById('activitiesTable')) {
        if (typeof initPagination === 'function') {
            initPagination('activitiesTable', 10);
        }
    }
});
