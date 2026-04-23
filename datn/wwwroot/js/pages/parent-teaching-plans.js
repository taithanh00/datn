document.addEventListener('DOMContentLoaded', function() {
    if (document.getElementById('teachingPlansTable')) {
        if (typeof initPagination === 'function') {
            initPagination('teachingPlansTable', 10);
        }
    }
});

function showDetail(title, content) {
    const titleElem = document.getElementById('pModalTitle');
    const contentElem = document.getElementById('pModalContent');
    const overlayElem = document.getElementById('pModalOverlay');

    if(titleElem) titleElem.textContent = title;
    if(contentElem) contentElem.textContent = content || 'Chưa có nội dung chi tiết.';
    if(overlayElem) overlayElem.classList.add('active');
}

function closePModal() {
    const overlayElem = document.getElementById('pModalOverlay');
    if(overlayElem) overlayElem.classList.remove('active');
}

// Expose to global scope for onclick handlers
window.showDetail = showDetail;
window.closePModal = closePModal;
