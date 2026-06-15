let showInactiveHolidays = false;

document.addEventListener('DOMContentLoaded', () => {
    const holidayDateInput = document.getElementById("holidayDate");
    if (holidayDateInput) {
        holidayDateInput.min = getTodayString();
    }

    loadHolidays();

    document.getElementById("btnCreateHoliday").addEventListener("click", createHoliday);

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
                body.innerHTML = window.appLoading.tableEmpty(4, "Chưa có ngày lễ nào được thiết lập.");
                return;
            }
            body.innerHTML = "<tr><td colspan='4' class='text-center py-5 text-muted'>Chưa có ngày lễ nào được thiết lập.</td></tr>";
            return;
        }

        const today = getTodayString();

        body.innerHTML = payload.data.map(x => {
            const isPast = x.date < today;
            const isToday = x.date === today;
            const statusClass = x.isActive
                ? (isPast ? "badge-secondary" : isToday ? "badge-info" : "badge-success")
                : "badge-danger";
            const statusText = x.isActive
                ? (isPast ? "Đã qua" : isToday ? "Hôm nay" : "Sắp tới")
                : "Đã ẩn/Xóa";

            const actionBtn = x.isActive
                ? `<button class="btn-table delete" onclick="deleteHoliday(${x.id})">Xóa</button>`
                : `<button class="btn-table" onclick="reactivateHoliday(${x.id})" style="color: var(--primary);">Khôi phục</button>`;

            return `
                <tr>
                    <td style="font-weight: 600;">${formatHolidayDate(x.date)}</td>
                    <td>
                        <div style="font-weight: 600;">${escapeHtml(x.name)}</div>
                        <div class="text-muted" style="font-size: 0.85rem;">${escapeHtml(x.description || "--")}</div>
                    </td>
                    <td><span class="badge ${statusClass}">${statusText}</span></td>
                    <td style="text-align: right;">
                        ${actionBtn}
                    </td>
                </tr>
            `;
        }).join("");
    } catch (e) {
        if (window.appLoading) {
            body.innerHTML = window.appLoading.tableError(4, "Lỗi kết nối.");
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
        setAlert("Chỉ được tạo ngày lễ từ hôm nay trở đi.", true);
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
    } catch (e) {
        alert("Lỗi kết nối.");
    }
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
    } catch (e) {
        alert("Lỗi kết nối.");
    }
}

function getTodayString() {
    const today = new Date();
    today.setMinutes(today.getMinutes() - today.getTimezoneOffset());
    return today.toISOString().split('T')[0];
}

function formatHolidayDate(value) {
    if (!value || !value.includes("-")) return value || "";
    const [year, month, day] = value.split("-");
    return `${Number(day)}/${Number(month)}/${year}`;
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function setAlert(msg, isError) {
    const alert = document.getElementById("holidayAlert");
    if (alert) {
        alert.style.display = "none";
        alert.textContent = "";
    }
    if (window.showToast) window.showToast(isError ? "Có lỗi" : "Thành công", msg, isError ? "error" : "success");
}
