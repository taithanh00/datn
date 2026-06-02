document.addEventListener('DOMContentLoaded', function() {
    const header = document.getElementById('mainHeader');
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');
    
    // Header Scroll Effect
    window.addEventListener('scroll', function() {
        if (window.scrollY > 50) {
            header.classList.add('scrolled');
        } else {
            header.classList.remove('scrolled');
        }
    });

    // Mobile Menu Toggle
    const mobileNav = document.getElementById('mobileNav');
    if (mobileMenuBtn && mobileNav) {
        mobileMenuBtn.addEventListener('click', function() {
            const isActive = mobileNav.classList.toggle('active');
            mobileMenuBtn.innerHTML = isActive
                ? '<i class="fa-solid fa-xmark"></i>'
                : '<i class="fa-solid fa-bars"></i>';
        });

        mobileNav.querySelectorAll('a').forEach((link) => {
            link.addEventListener('click', function () {
                mobileNav.classList.remove('active');
                mobileMenuBtn.innerHTML = '<i class="fa-solid fa-bars"></i>';
            });
        });

        document.addEventListener('click', function (event) {
            if (
                mobileNav.classList.contains('active') &&
                !mobileNav.contains(event.target) &&
                !mobileMenuBtn.contains(event.target)
            ) {
                mobileNav.classList.remove('active');
                mobileMenuBtn.innerHTML = '<i class="fa-solid fa-bars"></i>';
            }
        });
    }

    // Consultation Form Submission
    const consultForm = document.getElementById('consultForm');
    if (consultForm) {
        consultForm.addEventListener('submit', function(e) {
            e.preventDefault();
            const btn = consultForm.querySelector('button[type="submit"]');
            const originalText = btn.innerHTML;
            
            btn.disabled = true;
            btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Đang gửi...';
            
            // Giả lập gửi form
            setTimeout(() => {
                alert('Cảm ơn bạn! Chúng tôi đã nhận được thông tin và sẽ liên hệ sớm nhất.');
                btn.disabled = false;
                btn.innerHTML = originalText;
                consultForm.reset();
            }, 1500);
        });
    }

    const consultSection = document.getElementById('consultationFormSection');
    const consultToggle = document.getElementById('consultToggle');
    if (consultSection && consultToggle) {
        consultToggle.addEventListener('click', function() {
            const isCollapsed = consultSection.classList.toggle('collapsed');
            consultToggle.setAttribute('aria-expanded', String(!isCollapsed));
            const label = consultToggle.querySelector('.toggle-label');
            const icon = consultToggle.querySelector('i');
            if (label) {
                label.textContent = isCollapsed ? 'Mở rộng' : 'Thu gọn';
            }
            if (icon) {
                icon.classList.toggle('fa-chevron-up', !isCollapsed);
                icon.classList.toggle('fa-chevron-down', isCollapsed);
            }
        });
    }

    // Lazy Loading Images (Native support check)
    if ('loading' in HTMLImageElement.prototype) {
        const images = document.querySelectorAll('img[loading="lazy"]');
        images.forEach(img => {
            img.src = img.dataset.src;
        });
    } else {
        // Fallback for older browsers if needed
    }
});
