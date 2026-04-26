// ===== KinderCare App.js =====
(function () {
    'use strict';

    function initTheme() {
        const saved = localStorage.getItem('kindercare-theme');
        if (saved) {
            document.documentElement.setAttribute('data-theme', saved);
        } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
            document.documentElement.setAttribute('data-theme', 'dark');
        }
    }

    // === Sidebar Active State ===
    function initSidebarActive() {
        const path = window.location.pathname.toLowerCase();
        document.querySelectorAll('.nav-item').forEach(item => {
            item.classList.remove('active');
            const href = item.getAttribute('href');
            if (!href) return;
            const hrefLower = href.toLowerCase();
            if (path === hrefLower || (hrefLower !== '/' && path.startsWith(hrefLower))) {
                item.classList.add('active');
            }
        });
    }



    // === Page Transitions ===
    function initPageTransitions() {
        document.querySelectorAll('a.nav-item, a[data-transition]').forEach(link => {
            link.addEventListener('click', function (e) {
                const href = this.getAttribute('href');
                if (!href || href === '#' || href.startsWith('javascript')) return;
                if (this.getAttribute('target') === '_blank') return;

                e.preventDefault();
                const wrapper = document.querySelector('.page-wrapper');
                if (wrapper) {
                    wrapper.classList.add('page-leaving');
                    setTimeout(() => { window.location.href = href; }, 200);
                } else {
                    window.location.href = href;
                }
            });
        });
    }

    // === Pagination ===
    window.initPagination = function (tableId, pageSize) {
        pageSize = pageSize || 10;
        const table = document.getElementById(tableId);
        if (!table) return;

        const tbody = table.querySelector('tbody');
        if (!tbody) return;

        function render() {
            // Only count rows that are NOT explicitly hidden by search (exclude .searching-hidden)
            const allRows = Array.from(tbody.querySelectorAll('tr:not(.searching-hidden)'));
            const totalPages = Math.ceil(allRows.length / pageSize) || 1;
            
            // Adjust current page if it's out of bounds
            if (table._currentPage > totalPages) table._currentPage = totalPages;
            if (table._currentPage < 1) table._currentPage = 1;
            
            const currentPage = table._currentPage || 1;

            // Hide all rows first
            tbody.querySelectorAll('tr').forEach(row => row.style.display = 'none');
            
            // Show only rows for current page that are NOT hidden by search
            allRows.forEach((row, i) => {
                if (i >= (currentPage - 1) * pageSize && i < currentPage * pageSize) {
                    row.style.display = '';
                }
            });

            // Render buttons
            let pagEl = table.parentElement.querySelector('.pagination');
            if (!pagEl) {
                pagEl = document.createElement('div');
                pagEl.className = 'pagination';
                table.parentElement.appendChild(pagEl);
            }

            if (totalPages <= 1) {
                pagEl.innerHTML = '';
                return;
            }

            let html = '';
            html += `<button class="page-btn" ${currentPage === 1 ? 'disabled' : ''} data-page="${currentPage - 1}"><i class="fa-solid fa-chevron-left"></i></button>`;

            for (let p = 1; p <= totalPages; p++) {
                if (totalPages > 7) {
                    if (p === 1 || p === totalPages || (p >= currentPage - 1 && p <= currentPage + 1)) {
                        html += `<button class="page-btn ${p === currentPage ? 'active' : ''}" data-page="${p}">${p}</button>`;
                    } else if (p === currentPage - 2 || p === currentPage + 2) {
                        html += `<span class="page-info">...</span>`;
                    }
                } else {
                    html += `<button class="page-btn ${p === currentPage ? 'active' : ''}" data-page="${p}">${p}</button>`;
                }
            }

            html += `<button class="page-btn" ${currentPage === totalPages ? 'disabled' : ''} data-page="${currentPage + 1}"><i class="fa-solid fa-chevron-right"></i></button>`;
            pagEl.innerHTML = html;

            pagEl.querySelectorAll('.page-btn').forEach(btn => {
                btn.addEventListener('click', () => {
                    const pg = parseInt(btn.getAttribute('data-page'));
                    if (pg >= 1 && pg <= totalPages) {
                        table._currentPage = pg;
                        render();
                    }
                });
            });
        }

        table._currentPage = 1;
        table._pageSize = pageSize;
        table._refreshPagination = render;
        render();
    };

    // === Search Filter ===
    window.initTableSearch = function (inputId, tableId) {
        const input = document.getElementById(inputId);
        const table = document.getElementById(tableId);
        if (!input || !table) return;

        input.addEventListener('input', function () {
            const query = this.value.toLowerCase();
            const rows = table.querySelectorAll('tbody tr');
            
            rows.forEach(row => {
                const text = row.textContent.toLowerCase();
                if (text.includes(query)) {
                    row.classList.remove('searching-hidden');
                } else {
                    row.classList.add('searching-hidden');
                }
            });

            // Reset to first page when searching
            table._currentPage = 1;
            
            if (typeof table._refreshPagination === 'function') {
                table._refreshPagination();
            } else {
                // Fallback if no pagination
                rows.forEach(row => {
                    row.style.display = row.classList.contains('searching-hidden') ? 'none' : '';
                });
            }
        });
    };

    // === Modal helpers ===
    window.openModal = function (id) {
        const modal = document.getElementById(id);
        if (modal) modal.classList.add('show');
    };
    window.closeModal = function (id) {
        const modal = document.getElementById(id);
        if (modal) modal.classList.remove('show');
    };

    // === Init All ===
    document.addEventListener('DOMContentLoaded', function () {
        initTheme();
        initSidebarActive();
        initPageTransitions();

        // Close modals on overlay click
        document.querySelectorAll('.modal-overlay').forEach(overlay => {
            overlay.addEventListener('click', function (e) {
                if (e.target === this) this.classList.remove('show');
            });
        });
    });
})();
