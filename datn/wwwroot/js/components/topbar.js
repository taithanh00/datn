    document.addEventListener('DOMContentLoaded', function() {
    const profileToggle = document.getElementById('profileToggle');
    const profileMenu = document.getElementById('profileMenu');
    const profileDropdown = document.getElementById('profileDropdown');

    // Toggle dropdown khi click vào button
    profileToggle.addEventListener('click', function(e) {
        e.stopPropagation();
        profileMenu.classList.toggle('active');
        profileDropdown.classList.toggle('active');
    });

    // Đóng dropdown khi click ra ngoài
    document.addEventListener('click', function(e) {
        if (!profileDropdown.contains(e.target)) {
            profileMenu.classList.remove('active');
            profileDropdown.classList.remove('active');
        }
    });

    // Đóng dropdown khi click vào một item
    document.querySelectorAll('.profile-menu-item').forEach(item => {
        item.addEventListener('click', function() {
            profileMenu.classList.remove('active');
            profileDropdown.classList.remove('active');
        });
    });
});