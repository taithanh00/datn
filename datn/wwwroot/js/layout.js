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
    window.showToast = function(title, message, type) {
        type = type || 'info';
        const icons = { info:'fa-circle-info', success:'fa-circle-check', warning:'fa-triangle-exclamation', error:'fa-circle-xmark' };
        const colors = { info:'var(--primary)', success:'var(--success)', warning:'var(--warning)', error:'var(--danger)' };
        const c = document.getElementById('toastContainer');
        if(!c) return;
        const t = document.createElement('div');
        t.style.cssText = 'background:var(--bg-card); border:1px solid var(--border); border-left:4px solid '+colors[type]+'; padding:14px 18px; border-radius:10px; box-shadow:0 8px 24px rgba(0,0,0,0.15); display:flex; gap:12px; align-items:flex-start; animation:slideInRight 0.3s ease; min-width:300px;';
        t.innerHTML = '<i class="fa-solid '+icons[type]+'" style="color:'+colors[type]+'; margin-top:2px;"></i><div><div style="font-weight:600; font-size:0.85rem;">'+title+'</div><div style="font-size:0.8rem; color:var(--text-muted); margin-top:2px;">'+message+'</div></div>';
        c.appendChild(t);
        setTimeout(() => { t.style.opacity = '0'; t.style.transition = 'opacity 0.3s'; setTimeout(() => t.remove(), 300); }, 5000);
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
});
