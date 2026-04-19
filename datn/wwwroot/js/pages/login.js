// Swiper init
// Cấu hình Swiper
// loop: cho phép lặp lại vô hạn
// autoplay: tự động chuyển slide sau 3500ms, không dừng khi người dùng tương tác
// pagination: hiển thị pagination và cho phép click vào pagination để chuyển slide
// speed: tốc độ chuyển slide là 600ms
// effect: fade animation for smooth transitions
new Swiper('.swiper', {
    loop: true,
    autoplay: { delay: 3500, disableOnInteraction: false },
    pagination: { el: '.swiper-pagination', clickable: true },
    speed: 600,
    effect: 'fade',
    fadeEffect: { crossFade: true }
});

// Toggle password
// bước 1: bắt sự kiện click vào nút
// bước 2: lấy input và icon
// bước 3: kiểm tra nếu input đang là password thì chuyển sang text và ngược lại
// bước 4: thay đổi icon tương ứng
document.getElementById('togglePasswordBtn').addEventListener('click', function () {
    const input = document.getElementById('passwordField');
    const icon = document.getElementById('eye-icon');
    const isPassword = input.type === 'password';

    input.type = isPassword ? 'text' : 'password';

    icon.innerHTML = isPassword
        ? `<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8
                     a18.45 18.45 0 0 1 5.06-5.94"/>
                   <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8
                     a18.5 18.5 0 0 1-2.16 3.19"/>
                   <line x1="1" y1="1" x2="23" y2="23"/>`
        : `<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                   <circle cx="12" cy="12" r="3"/>`;
});

// Add interaction animations
document.addEventListener('DOMContentLoaded', function () {
    // Form inputs animation on focus
    const inputs = document.querySelectorAll('input[type="text"], input[type="password"]');
    inputs.forEach(input => {
        input.addEventListener('focus', function () {
            this.style.animation = 'slideInForm 0.3s ease-out';
        });
    });

    // Stagger animation for form fields
    const formElements = document.querySelectorAll('.field-group, .remember-row, .btn-login, .btn-forgot-password, .alert');
    formElements.forEach((el, index) => {
        if (!el.style.animation) {
            el.style.animationDelay = `${0.1 * index}s`;
        }
    });

    // Button hover effects
    const buttons = document.querySelectorAll('.btn-login, .btn-forgot-password, .toggle-password');
    buttons.forEach(btn => {
        btn.addEventListener('mouseenter', function () {
            if (btn.classList.contains('btn-login') || btn.classList.contains('btn-forgot-password')) {
                this.style.transform = 'translateY(-2px)';
            }
        });

        btn.addEventListener('mouseleave', function () {
            if (btn.classList.contains('btn-login') || btn.classList.contains('btn-forgot-password')) {
                this.style.transform = 'translateY(0)';
            }
        });
    });

    // Add ripple effect on button click
    function createRipple(event) {
        const button = event.currentTarget;

        if (button.classList.contains('toggle-password')) return;

        const ripple = document.createElement('span');
        const rect = button.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height);
        const x = event.clientX - rect.left - size / 2;
        const y = event.clientY - rect.top - size / 2;

        ripple.style.width = ripple.style.height = size + 'px';
        ripple.style.left = x + 'px';
        ripple.style.top = y + 'px';
        ripple.classList.add('ripple');

        button.appendChild(ripple);

        setTimeout(() => ripple.remove(), 600);
    }

    document.querySelectorAll('.btn-login, .btn-forgot-password').forEach(button => {
        button.addEventListener('click', createRipple);
    });

    // Form submission animation
    const form = document.querySelector('form');
    if (form) {
        form.addEventListener('submit', function (e) {
            const submitButton = form.querySelector('.btn-login');
            submitButton.style.opacity = '0.8';
            submitButton.style.pointerEvents = 'none';
        });
    }
});
