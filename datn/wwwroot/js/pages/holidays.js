document.addEventListener('DOMContentLoaded', () => {
    loadHolidays();

    document.getElementById("btnCreateHoliday").addEventListener("click", createHoliday);
});

async function loadHolidays() {
    const body = document.getElementById("holidayTableBody");
    try {
        const res = await fetch("/HolidayManagement/Api/List");
        const payload = await res.json();
        
        if (!payload.data.length) {
            body.innerHTML = "<tr><td colspan='4' class='text-center py-5 text-muted'>Chưa có ngày lễ nào được thiết lập.</td></tr>";
            return;
        }

        const today = new Date().toISOString().split('T')[0];

        body.innerHTML = payload.data.map(x => {
            const isPast = x.date < today;
            const statusClass = isPast ? "badge-secondary" : "badge-success";
            const statusText = isPast ? "Đã qua" : "Sắp tới";

            return `
                <tr>
                    <td style="font-weight: 600;">${new Date(x.date).toLocaleDateString('vi-VN')}</td>
                    <td>
                        <div style="font-weight: 600;">${x.name}</div>
                        <div class="text-muted" style="font-size: 0.85rem;">${x.description || "--"}</div>
                    </td>
                    <td><span class="badge ${statusClass}">${statusText}</span></td>
                    <td style="text-align: right;">
                        <button class="btn btn-outline" style="border-color: var(--danger); color: var(--danger); padding: 4px 10px; font-size: 0.8rem;" onclick="deleteHoliday(${x.id})">
                            <i class="fa-solid fa-trash"></i> Xóa
                        </button>
                    </td>
                </tr>
            `;
        }).join("");
    } catch(e) {
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
            if (window.showToast) window.showToast('Thành công', payload.message, 'success');
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
    if (!confirm("Bạn có chắc muốn xóa ngày lễ này? Hệ thống sẽ thu hồi các bản ghi chấm công tự động của giáo viên vào ngày đó.")) return;

    try {
        const res = await fetch(`/HolidayManagement/Api/Delete/${id}`, { method: "DELETE" });
        const payload = await res.json();
        if (payload.success) {
            if (window.showToast) window.showToast('Đã xóa', payload.message, 'info');
            await loadHolidays();
        } else {
            alert(payload.message);
        }
    } catch (e) { alert("Lỗi kết nối."); }
}

function setAlert(msg, isError) {
    const alert = document.getElementById("holidayAlert");
    alert.className = `page-alert ${isError ? 'error' : 'success'} mt-3`;
    alert.innerHTML = `<i class="fa-solid ${isError ? 'fa-circle-xmark' : 'fa-circle-check'}"></i> ${msg}`;
    alert.style.display = "block";
    setTimeout(() => { alert.style.display = "none"; }, 5000);
}
