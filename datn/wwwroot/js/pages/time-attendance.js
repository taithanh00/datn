const btnCheckIn = document.getElementById("btnCheckIn");
const btnCheckOut = document.getElementById("btnCheckOut");
const alertBox = document.getElementById("attendanceAlert");

function money(v) {
    return new Intl.NumberFormat("vi-VN").format(v || 0) + " đ";
}

function setAlert(message, isError = false, isWarning = false) {
    if(!alertBox) return;
    alertBox.innerHTML = message;
    if (isError) {
        alertBox.className = "page-alert error mt-4 w-100 text-center";
    } else if (isWarning) {
        alertBox.className = "page-alert mt-4 w-100 text-center";
        alertBox.style.background = "rgba(251,191,36,0.15)";
        alertBox.style.color = "var(--warning)";
        alertBox.style.borderColor = "var(--warning)";
    } else {
        alertBox.className = "page-alert success mt-4 w-100 text-center";
    }
}

function syncUi(data) {
    const timeEl = document.getElementById("serverTime");
    const statusText = document.getElementById("statusText");
    const checkInText = document.getElementById("checkInText");
    const checkOutText = document.getElementById("checkOutText");
    const isLateSpan = document.getElementById("isLateText");
    const penaltyText = document.getElementById("penaltyText");

    if(timeEl) timeEl.textContent = data.serverTimeVnt || "--";
    
    if(statusText) {
        let statusBadge = `<span class="badge badge-info">${data.status || "--"}</span>`;
        if (data.status === 'Present') statusBadge = '<span class="badge badge-success">Đã chấm công</span>';
        if (data.status === 'Late') statusBadge = '<span class="badge badge-warning">Đi trễ</span>';
        statusText.innerHTML = statusBadge;
    }
    
    if(checkInText) checkInText.textContent = data.checkInAt || "--";
    if(checkOutText) checkOutText.textContent = data.checkOutAt || "--";
    
    if(isLateSpan) {
        isLateSpan.textContent = data.isLate ? "Có" : "Không";
        isLateSpan.style.color = data.isLate ? "var(--warning)" : "var(--success)";
    }
    
    if(penaltyText) penaltyText.textContent = money(data.penaltyAmount);

    if(btnCheckIn) btnCheckIn.disabled = !data.canCheckIn;
    if(btnCheckOut) btnCheckOut.disabled = !data.canCheckOut;

    if (!data.isAllowedNow) {
        setAlert('<i class="fa-solid fa-lock"></i> Ngoài giờ làm việc. Nút chấm công đang bị khóa.', false, true);
    } else {
        setAlert('<i class="fa-solid fa-unlock"></i> Bạn đang trong khung giờ làm việc.');
    }
}

async function loadToday() {
    try {
        const res = await fetch("/TimeAttendance/Api/Today");
        const payload = await res.json();
        if (!payload.success) {
            setAlert('<i class="fa-solid fa-triangle-exclamation"></i> ' + (payload.message || "Không lấy được trạng thái chấm công."), true);
            return;
        }
        syncUi(payload.data);
    } catch (e) {
        setAlert('<i class="fa-solid fa-link-slash"></i> Mất kết nối máy chủ.', true);
    }
}

async function postAction(url) {
    if(btnCheckIn) btnCheckIn.disabled = true;
    if(btnCheckOut) btnCheckOut.disabled = true;
    try {
        const res = await fetch(url, { method: "POST" });
        const payload = await res.json();
        
        if (payload.success) {
            if (window.showToast) window.showToast('Thành công', payload.message || "Đã ghi nhận.", 'success');
            setAlert('<i class="fa-solid fa-check"></i> ' + (payload.message || "Đã xử lý."));
        } else {
            setAlert('<i class="fa-solid fa-triangle-exclamation"></i> ' + (payload.message || "Lỗi xử lý."), true);
        }
        await loadToday();
    } catch (e) {
        setAlert('<i class="fa-solid fa-link-slash"></i> Lỗi kết nối.', true);
        await loadToday();
    }
}

document.addEventListener('DOMContentLoaded', () => {
    if(btnCheckIn) btnCheckIn.addEventListener("click", () => postAction("/TimeAttendance/Api/CheckIn"));
    if(btnCheckOut) btnCheckOut.addEventListener("click", () => postAction("/TimeAttendance/Api/CheckOut"));

    if (typeof signalR !== 'undefined') {
        const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/realtime").build();
        connection.on("attendanceChanged", () => loadToday());
        connection.start().catch(() => {});
    }

    loadToday();
    
    // Cập nhật đồng hồ local hiển thị cho đẹp trong lúc đợi realtime (nếu có)
    setInterval(() => {
        const timeEl = document.getElementById("serverTime");
        if (timeEl && timeEl.textContent !== "--:--:--" && timeEl.textContent !== "--") {
            const now = new Date();
            timeEl.textContent = now.getHours().toString().padStart(2, '0') + ':' + 
                                 now.getMinutes().toString().padStart(2, '0') + ':' + 
                                 now.getSeconds().toString().padStart(2, '0');
        }
    }, 1000);
});
