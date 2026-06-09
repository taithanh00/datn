document.addEventListener('DOMContentLoaded', function() {
    // === Theme Management ===
    const THEME_KEY = 'kindercare-theme';
    const themeToggle = document.getElementById('themeToggle');

    function toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme');
        const next = current === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', next);
        localStorage.setItem(THEME_KEY, next);
    }

    if (themeToggle) {
        themeToggle.addEventListener('click', toggleTheme);
    }

    // Sidebar Mobile Toggle
    const hamburgerBtn = document.getElementById('hamburgerBtn');
    const sidebar = document.querySelector('.sidebar');
    const overlay = document.querySelector('.sidebar-overlay');
    
    if (hamburgerBtn && sidebar) {
        hamburgerBtn.addEventListener('click', function () {
            sidebar.classList.toggle('open');
            if(overlay) overlay.classList.toggle('show');
        });
    }

    if (overlay) {
        overlay.addEventListener('click', function() {
            sidebar.classList.remove('open');
            overlay.classList.remove('show');
        });
    }

    // Dropdown Menu Toggles
    function initMenuToggle(toggleId, subMenuId) {
        const toggleBtn = document.getElementById(toggleId);
        if (!toggleBtn) return;

        toggleBtn.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();

            const subMenu = document.getElementById(subMenuId);
            const chevron = this.querySelector('.nav-chevron');
            const isOpen = subMenu?.style.display !== 'none';

            if (subMenu) {
                subMenu.style.display = isOpen ? 'none' : 'block';
            }

            this.setAttribute('aria-expanded', (!isOpen).toString());

            if (chevron) {
                chevron.style.transform = isOpen ? 'rotate(0deg)' : 'rotate(90deg)';
            }
        });
    }

    initMenuToggle('teacherMenuToggle', 'teacherSubMenu');
    initMenuToggle('financeMenuToggle', 'financeSubMenu');

    // === Notification System ===
    const toastTypes = ['success', 'error', 'warning', 'info'];
    const nativeAlert = window.alert?.bind(window);

    function normalizeToastArgs(title, message, type) {
        if (toastTypes.includes(String(message || '').toLowerCase()) && type === undefined) {
            return {
                title: defaultToastTitle(message),
                message: title || '',
                type: String(message).toLowerCase()
            };
        }

        return {
            title: title || defaultToastTitle(type),
            message: message || '',
            type: toastTypes.includes(String(type || '').toLowerCase()) ? String(type).toLowerCase() : 'info'
        };
    }

    function defaultToastTitle(type) {
        switch (String(type || '').toLowerCase()) {
            case 'success': return 'Thành công';
            case 'error': return 'Có lỗi';
            case 'warning': return 'Cảnh báo';
            default: return 'Thông tin';
        }
    }

    function setText(el, text) {
        el.textContent = text == null ? '' : String(text);
    }

    window.showToast = function(title, message, type) {
        const args = normalizeToastArgs(title, message, type);
        const icons = { info:'fa-circle-info', success:'fa-circle-check', warning:'fa-triangle-exclamation', error:'fa-circle-xmark' };
        const colors = { info:'var(--primary)', success:'var(--success)', warning:'var(--warning)', error:'var(--danger)' };
        const c = document.getElementById('toastContainer');
        if (!c) {
            if (nativeAlert) nativeAlert(args.message || args.title);
            return;
        }

        const t = document.createElement('div');
        t.style.cssText = 'background:var(--bg-card); border:1px solid var(--border); border-left:4px solid '+colors[args.type]+'; padding:14px 18px; border-radius:10px; box-shadow:0 8px 24px rgba(0,0,0,0.15); display:flex; gap:12px; align-items:flex-start; animation:slideInRight 0.3s ease; min-width:300px; max-width:380px;';

        const icon = document.createElement('i');
        icon.className = 'fa-solid ' + icons[args.type];
        icon.style.cssText = 'color:'+colors[args.type]+'; margin-top:2px;';

        const content = document.createElement('div');
        const titleEl = document.createElement('div');
        titleEl.style.cssText = 'font-weight:600; font-size:0.85rem;';
        setText(titleEl, args.title);
        const messageEl = document.createElement('div');
        messageEl.style.cssText = 'font-size:0.8rem; color:var(--text-muted); margin-top:2px;';
        setText(messageEl, args.message);

        content.appendChild(titleEl);
        if (args.message) content.appendChild(messageEl);
        t.appendChild(icon);
        t.appendChild(content);
        c.appendChild(t);
        setTimeout(() => {
            t.style.opacity = '0';
            t.style.transform = 'translateX(12px)';
            t.style.transition = 'opacity 0.3s, transform 0.3s';
            setTimeout(() => t.remove(), 300);
        }, 5000);
    };

    window.notifySuccess = (message, title = 'Thành công') => window.showToast(title, message, 'success');
    window.notifyError = (message, title = 'Có lỗi') => window.showToast(title, message, 'error');
    window.notifyWarning = (message, title = 'Cảnh báo') => window.showToast(title, message, 'warning');
    window.notifyInfo = (message, title = 'Thông tin') => window.showToast(title, message, 'info');
    window.alert = (message) => window.showToast('Thông báo', message, 'warning');

    if (window.Swal && typeof window.Swal.fire === 'function') {
        const originalSwalFire = window.Swal.fire.bind(window.Swal);
        window.Swal.fire = function(arg1, arg2, arg3) {
            const simpleIcon = typeof arg1 === 'string' ? arg3 : arg1?.icon;
            const isToastable = toastTypes.includes(String(simpleIcon || '').toLowerCase())
                && !(typeof arg1 === 'object' && (arg1.showConfirmButton || arg1.allowOutsideClick === false || arg1.didOpen));

            if (isToastable) {
                const title = typeof arg1 === 'string' ? arg1 : arg1.title;
                const message = typeof arg1 === 'string' ? arg2 : arg1.text;
                window.showToast(title || defaultToastTitle(simpleIcon), message || '', simpleIcon);
                return Promise.resolve({ isConfirmed: true, isDismissed: false, isDenied: false });
            }

            return originalSwalFire(arg1, arg2, arg3);
        };
    }

    async function loadLatestNotifications() {
        try {
            const res = await fetch('/Notification/Api/Latest');
            const result = await res.json();
            if (result.success) updateNotificationUI(result.data);
        } catch(e) {}
    }

    function updateNotificationUI(data) {
        const list = document.getElementById('notificationList');
        const badge = document.getElementById('notificationCount');
        if (!list || !badge) return;
        const unread = data.filter(n => !n.isRead).length;
        badge.style.display = unread > 0 ? 'flex' : 'none';
        if (unread > 0) badge.textContent = unread > 9 ? '9+' : unread;
        if (data.length === 0) { list.innerHTML = '<div class="empty-state" style="padding:32px 16px;"><i class="fa-solid fa-bell-slash"></i><p>Không có thông báo</p></div>'; return; }
        list.innerHTML = data.map(n => `<a href="${n.url||'#'}" class="notification-item ${n.isRead?'':'unread'}" onclick="markAsRead(${n.id})" style="display:flex; gap:12px; padding:12px; border-radius:8px; text-decoration:none; color:var(--text-main); ${n.isRead?'':'background:var(--primary-soft);'}"><div style="flex:1;"><div style="font-size:0.85rem; font-weight:600;">${n.title}</div><div style="font-size:0.8rem; color:var(--text-muted);">${n.message}</div><div style="font-size:0.72rem; color:var(--text-muted); margin-top:4px;">${new Date(n.createdAt).toLocaleString()}</div></div></a>`).join('');
    }

    window.markAsRead = async function(id) { try { await fetch('/Notification/Api/MarkRead/'+id, {method:'POST'}); } catch(e){} }

    // Notification dropdown toggle
    document.getElementById('notificationToggle')?.addEventListener('click', function(e) {
        e.stopPropagation();
        const menu = document.getElementById('notificationMenu');
        if(menu) {
            menu.classList.toggle('active');
            if (menu.classList.contains('active')) loadLatestNotifications();
        }
    });

    document.addEventListener('click', () => document.getElementById('notificationMenu')?.classList.remove('active'));
    document.getElementById('notificationMenu')?.addEventListener('click', e => e.stopPropagation());

    // SignalR Initialization
    if (typeof signalR !== 'undefined') {
        const notificationConn = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/realtime")
            .withAutomaticReconnect()
            .build();

        notificationConn.on("ReceiveNotification", (n) => { 
            if(window.showToast) window.showToast(n.title, n.message, n.type); 
            loadLatestNotifications(); 
        });

        notificationConn.on("attendanceChanged", () => { 
            if(typeof refreshManagerCounts === 'function') refreshManagerCounts(); 
        });

        notificationConn.on("leaveRequestChanged", () => { 
            if(typeof refreshManagerCounts === 'function') refreshManagerCounts(); 
        });

        notificationConn.start()
            .then(() => { loadLatestNotifications(); })
            .catch(err => console.error("SignalR err:", err));
    }

    // === Sidebar Scroll Persistence ===
    const sidebarNav = document.querySelector('.sidebar-nav');
    const SCROLL_KEY = 'senhong_sidebar_scroll';

    if (sidebarNav) {
        // Restore scroll position
        const savedScroll = sessionStorage.getItem(SCROLL_KEY);
        if (savedScroll) {
            sidebarNav.scrollTop = parseInt(savedScroll, 10);
        }

        // Save scroll position when clicking a link
        sidebarNav.addEventListener('click', (e) => {
            const link = e.target.closest('a');
            if (link && link.href && !link.href.includes('#')) {
                sessionStorage.setItem(SCROLL_KEY, sidebarNav.scrollTop);
            }
        });

        // Backup: Save scroll position on beforeunload
        window.addEventListener('beforeunload', () => {
            sessionStorage.setItem(SCROLL_KEY, sidebarNav.scrollTop);
        });
    }
});
