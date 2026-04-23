// ===== KinderCare App.js =====
(function () {
    'use strict';

    // === Dark Mode ===
    const THEME_KEY = 'kindercare-theme';

    function initTheme() {
        const saved = localStorage.getItem(THEME_KEY);
        if (saved) {
            document.documentElement.setAttribute('data-theme', saved);
        } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
            document.documentElement.setAttribute('data-theme', 'dark');
        }
    }

    function toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme');
        const next = current === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', next);
        localStorage.setItem(THEME_KEY, next);
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

    // === Mobile Sidebar ===
    function initMobileSidebar() {
        const hamburger = document.querySelector('.hamburger');
        const sidebar = document.querySelector('.sidebar');
        const overlay = document.querySelector('.sidebar-overlay');

        if (hamburger) {
            hamburger.addEventListener('click', () => {
                sidebar.classList.toggle('open');
                overlay.classList.toggle('show');
            });
        }
        if (overlay) {
            overlay.addEventListener('click', () => {
                sidebar.classList.remove('open');
                overlay.classList.remove('show');
            });
        }
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

        const rows = Array.from(tbody.querySelectorAll('tr'));
        const totalPages = Math.ceil(rows.length / pageSize);
        let currentPage = 1;

        // Create pagination container
        let pagEl = table.parentElement.querySelector('.pagination');
        if (!pagEl) {
            pagEl = document.createElement('div');
            pagEl.className = 'pagination';
            table.parentElement.appendChild(pagEl);
        }

        function render() {
            rows.forEach((row, i) => {
                row.style.display = (i >= (currentPage - 1) * pageSize && i < currentPage * pageSize) ? '' : 'none';
            });

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
                        currentPage = pg;
                        render();
                    }
                });
            });
        }

        if (totalPages > 1) render();
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
                row.style.display = text.includes(query) ? '' : 'none';
            });
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
        initMobileSidebar();
        initPageTransitions();

        // Theme toggle button
        const toggleBtn = document.getElementById('themeToggle');
        if (toggleBtn) toggleBtn.addEventListener('click', toggleTheme);

        // Close modals on overlay click
        document.querySelectorAll('.modal-overlay').forEach(overlay => {
            overlay.addEventListener('click', function (e) {
                if (e.target === this) this.classList.remove('show');
            });
        });
    });
})();
