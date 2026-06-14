let showInactiveHolidays = false;

document.addEventListener('DOMContentLoaded', () => {
    const holidayDateInput = document.getElementById("holidayDate");
    if (holidayDateInput) {
        holidayDateInput.min = getTodayString();
    }

    loadHolidays();

    document.getElementById("btnCreateHoliday").addEventListener("click", createHoliday);

    // Status Tabs
    document.querySelectorAll(".status-tab").forEach((tab) => {
        tab.addEventListener("click", function () {
            document
                .querySelectorAll(".status-tab")
                .forEach((t) => t.classList.remove("active"));
            this.classList.add("active");
            showInactiveHolidays = this.getAttribute("data-show-inactive") === "true";
            loadHolidays();
        });
    });
});

async function loadHolidays() {
    const body = document.getElementById("holidayTableBody");
    if (window.appLoading && body) {
        window.appLoading.setTable(body, 4);
    }
    try {
        const res = await fetch(`/HolidayManagement/Api/List?showInactive=${showInactiveHolidays}`);
        const payload = await res.json();
        
        if (!payload.data.length) {
            if (window.appLoading) {
                body.innerHTML = window.appLoading.tableEmpty(4, "Ch\u01b0a c\u00f3 ng\u00e0y l\u1ec5 n\u00e0o \u0111\u01b0\u1ee3c thi\u1ebft l\u1eadp.");
                return;
            }
            body.innerHTML = "<tr><td colspan='4' class='text-center py-5 text-muted'>Chưa có ngày lễ nào được thiết lập.</td></tr>";
            return;
        }

        const today = new Date().toISOString().split('T')[0];

        body.innerHTML = payload.data.map(x => {
            const isPast = x.date < today;
            const statusClass = x.isActive ? (isPast ? "badge-secondary" : "badge-success") : "badge-danger";
            const statusText = x.isActive ? (isPast ? "Đã qua" : "Sắp tới") : "Đã ẩn/Xóa";

            const actionBtn = x.isActive 
                ? `<button class="btn-table delete" onclick="deleteHoliday(${x.id})">Xóa</button>`
                : `<button class="btn-table" onclick="reactivateHoliday(${x.id})" style="color: var(--primary);">Khôi phục</button>`;

            return `
                <tr>
                    <td style="font-weight: 600;">${new Date(x.date).toLocaleDateString('vi-VN')}</td>
                    <td>
                        <div style="font-weight: 600;">${x.name}</div>
                        <div class="text-muted" style="font-size: 0.85rem;">${x.description || "--"}</div>
                    </td>
                    <td><span class="badge ${statusClass}">${statusText}</span></td>
                    <td style="text-align: right;">
                        ${actionBtn}
                    </td>
                </tr>
            `;
        }).join("");
    } catch(e) {
        if (window.appLoading) {
            body.innerHTML = window.appLoading.tableError(4, "L\u1ed7i k\u1ebft n\u1ed1i.");
            return;
        }
        body.innerHTML = "<tr><td colspan='4' class='text-center text-danger py-4'>Lỗi kết nối.</td></tr>";
    }
}

async function createHoliday() {
    const btn = document.getElementById("btnCreateHoliday");
    const name = document.getElementById("holidayName").value;
    const date = document.getElementById("holidayDate").value;
    const description = document.getElementById("holidayDesc").value;

    if (!name || !date) {
        setAlert("Vui lòng nhập tên và chọn ngày lễ.", true);
        return;
    }

    if (date < getTodayString()) {
        setAlert("Ch\u1ec9 \u0111\u01b0\u1ee3c t\u1ea1o ng\u00e0y l\u1ec5 t\u1eeb h\u00f4m nay tr\u1edf \u0111i.", true);
        return;
    }

    btn.disabled = true;
    btn.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> Đang lưu...';

    try {
        const res = await fetch("/HolidayManagement/Api/Create", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ name, date, description })
        });
        const payload = await res.json();
        
        if (payload.success) {
            setAlert(payload.message, false);
            document.getElementById("holidayName").value = '';
            document.getElementById("holidayDate").value = '';
            document.getElementById("holidayDesc").value = '';
            await loadHolidays();
        } else {
            setAlert(payload.message, true);
        }
    } catch (e) {
        setAlert("Lỗi hệ thống.", true);
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="fa-solid fa-calendar-check"></i> Thiết lập ngày lễ';
    }
}

async function deleteHoliday(id) {
    if (!confirm("Bạn có chắc muốn ẩn ngày lễ này? Các bản ghi chấm công tự động được tạo cho ngày này cũng sẽ bị thu hồi.")) return;

    try {
        const res = await fetch(`/HolidayManagement/Api/Delete/${id}`, { method: "DELETE" });
        const payload = await res.json();
        if (payload.success) {
            if (window.showToast) window.showToast('Đã ẩn', payload.message, 'info');
            await loadHolidays();
        } else {
            alert(payload.message);
        }
    } catch (e) { alert("Lỗi kết nối."); }
}

async function reactivateHoliday(id) {
    if (!confirm("Bạn có chắc muốn khôi phục ngày lễ này?")) return;

    try {
        const res = await fetch(`/HolidayManagement/Api/Reactivate/${id}`, { method: "POST" });
        const payload = await res.json();
        if (payload.success) {
            await loadHolidays();
        } else {
            alert(payload.message);
        }
    } catch (e) { alert("Lỗi kết nối."); }
}

function getTodayString() {
    const today = new Date();
    today.setMinutes(today.getMinutes() - today.getTimezoneOffset());
    return today.toISOString().split('T')[0];
}

function setAlert(msg, isError) {
    const alert = document.getElementById("holidayAlert");
    if (alert) {
        alert.style.display = "none";
        alert.textContent = "";
    }
    if (window.showToast) window.showToast(isError ? "Có lỗi" : "Thành công", msg, isError ? "error" : "success");
}
