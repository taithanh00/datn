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

    // Mobile Menu Toggle (Basic for now)
    if (mobileMenuBtn) {
        mobileMenuBtn.addEventListener('click', function() {
            alert('Chức năng Menu Mobile sẽ được hoàn thiện trong bước tiếp theo!');
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
