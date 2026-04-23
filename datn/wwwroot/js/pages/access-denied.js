// Sync theme with localStorage
document.addEventListener('DOMContentLoaded', function() {
    const theme = localStorage.getItem('kindercare-theme');
    if (theme) {
        const bodyElem = document.getElementById('body');
        if (bodyElem) {
            bodyElem.setAttribute('data-theme', theme);
        }
    }
});
