/**
 * Dashboard JavaScript
 * Xử lý các tương tác trong dashboard:
 * - Toggle profile dropdown
 * - Mobile sidebar toggle
 * - Global search
 */

document.addEventListener('DOMContentLoaded', function() {
    // ===== PROFILE DROPDOWN =====
    const profileToggle = document.getElementById('profileToggle');
    const profileMenu = document.getElementById('profileMenu');
    const profileDropdown = document.getElementById('profileDropdown');

    if (profileToggle && profileMenu && profileDropdown) {
        // Toggle dropdown khi click button
        profileToggle.addEventListener('click', function(e) {
            e.stopPropagation();
            profileMenu.classList.toggle('active');
        });

        // Đóng dropdown khi click ra ngoài
        document.addEventListener('click', function(e) {
            if (!profileDropdown.contains(e.target)) {
                profileMenu.classList.remove('active');
            }
        });

        // Đóng dropdown khi click vào item
        document.querySelectorAll('.profile-menu-item').forEach(item => {
            item.addEventListener('click', function() {
                profileMenu.classList.remove('active');
            });
        });
    }

    // ===== MOBILE SIDEBAR TOGGLE =====
    const sidebarToggleMobile = document.getElementById('sidebarToggleMobile');
    const sidebar = document.querySelector('.sidebar');

    if (sidebarToggleMobile && sidebar) {
        sidebarToggleMobile.addEventListener('click', function() {
            sidebar.classList.toggle('mobile-open');
        });

        // Đóng sidebar khi click vào nav item trên mobile
        document.querySelectorAll('.nav-item').forEach(item => {
            item.addEventListener('click', function() {
                if (window.innerWidth <= 768) {
                    sidebar.classList.remove('mobile-open');
                }
            });
        });
    }

    // ===== GLOBAL SEARCH =====
    const globalSearch = document.getElementById('globalSearch');
    if (globalSearch) {
        globalSearch.addEventListener('keyup', function(e) {
            if (e.key === 'Enter') {
                performSearch(this.value);
            }
        });
    }

    // ===== ACTIVE NAV ITEM =====
    updateActiveNavItem();
});

/**
 * Thực hiện tìm kiếm toàn cục
 * @param {string} query - Chuỗi tìm kiếm
 */
function performSearch(query) {
    if (!query.trim()) {
        alert('Vui lòng nhập từ khóa tìm kiếm');
        return;
    }
    // TODO: Implement global search logic
    console.log('Searching for:', query);
}

/**
 * Cập nhật active nav item dựa trên URL hiện tại
 */
function updateActiveNavItem() {
    const currentUrl = window.location.pathname;
    document.querySelectorAll('.nav-item').forEach(item => {
        const href = item.getAttribute('href');
        if (href && currentUrl.includes(href)) {
            // Remove active from all items
            document.querySelectorAll('.nav-item').forEach(i => i.classList.remove('active'));
            // Add active to current item
            item.classList.add('active');
        }
    });
}

/**
 * Đóng notification
 */
function closeNotification(element) {
    element.closest('.notification').remove();
}

/**
 * Xử lý logout
 */
function handleLogout() {
    if (confirm('Bạn có chắc muốn đăng xuất?')) {
        document.querySelector('form[action="/Auth/Logout"]').submit();
    }
}
