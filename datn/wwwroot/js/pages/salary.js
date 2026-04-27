const monthEl = document.getElementById("salaryMonth");
const yearEl = document.getElementById("salaryYear");
const statusBar = document.getElementById("salaryStatusBar");
const tableBody = document.getElementById("salaryTableBody");
const btnRecalculate = document.getElementById("btnRecalculateSalary");
const btnLock = document.getElementById("btnLockSalary");
let isLocked = false;
let currentPeriodId = 0;

function setStatus(message, type = 'info') {
    if(!statusBar) return;
    let icon = 'fa-circle-info'; let color = 'var(--text-muted)'; let border = 'var(--border)'; let bg = 'var(--bg-card)';
    if (type === 'error') { icon = 'fa-triangle-exclamation'; color = 'var(--danger)'; border = 'var(--danger)'; bg = 'rgba(248,113,113,0.1)'; }
    if (type === 'info') { icon = 'fa-circle-check'; color = 'var(--success)'; border = 'var(--success)'; bg = 'rgba(52,211,153,0.1)'; }
    if (type === 'locked') { icon = 'fa-lock'; color = 'var(--warning)'; border = 'var(--warning)'; bg = 'rgba(251,191,36,0.1)'; }

    statusBar.style.background = bg;
    statusBar.style.color = color;
    statusBar.style.borderLeftColor = border;
    statusBar.innerHTML = `<i class="fa-solid ${icon}"></i> <span>${message}</span>`;
}

const fmtMoney = (v) => new Intl.NumberFormat("vi-VN").format(v || 0) + " đ";

function initFilters() {
    if(!monthEl || !yearEl) return;
    const now = new Date();
    const currentMonth = now.getMonth() + 1;
    const currentYear = now.getFullYear();

    monthEl.innerHTML = Array.from({ length: 12 }, (_, idx) => {
        const m = idx + 1;
        return `<option value="${m}" ${m === currentMonth ? "selected" : ""}>Tháng ${m}</option>`;
    }).join("");

    const years = [];
    for (let y = currentYear - 1; y <= currentYear + 1; y++) {
        years.push(`<option value="${y}" ${y === currentYear ? "selected" : ""}>Năm ${y}</option>`);
    }
    yearEl.innerHTML = years.join("");
}

async function loadSalarySummary() {
    if(!monthEl || !yearEl || !tableBody) return;
    const month = monthEl.value;
    const year = yearEl.value;
    tableBody.innerHTML = `<tr><td colspan='6' class='text-center py-5'><div class='spinner'></div></td></tr>`;

    try {
        const res = await fetch(`/TeacherSalary/Api/Summary?month=${month}&year=${year}`);
        const payload = await res.json();

        if (!payload.success) {
            setStatus(payload.message || "Lỗi tải dữ liệu.", "error");
            tableBody.innerHTML = `<tr><td colspan='6' class='text-center py-5 text-danger'>Lỗi tải dữ liệu.</td></tr>`;
            return;
        }

        isLocked = payload.isLocked === true;
        currentPeriodId = payload.periodId;
        if(btnRecalculate) btnRecalculate.disabled = isLocked;
        if(btnLock) btnLock.disabled = isLocked;

        if (!payload.data.length) {
            tableBody.innerHTML = `<tr><td colspan='6' class='text-center py-5 text-muted'><div class="empty-state"><i class="fa-solid fa-folder-open"></i><p>Chưa có dữ liệu lương cho kỳ này.</p></div></td></tr>`;
            setStatus(`Kỳ lương ${month}/${year} chưa có dữ liệu. Bấm "Tính lại" để khởi tạo.`, "open");
            return;
        }

        tableBody.innerHTML = payload.data.map(x => `
            <tr>
                <td><div class="text-muted">#${x.employeeId}</div></td>
                <td><strong>${x.employeeName}</strong></td>
                <td><span class="badge badge-info">${fmtMoney(x.baseSalary)}</span></td>
                <td style="text-align: center;"><span class="badge" style="background: var(--bg-body); border: 1px solid var(--border); color: var(--text-main);">${x.workingDays} công</span></td>
                <td style="text-align: right;"><span style="color: var(--success); font-weight: 700; font-size: 1.1rem;">${fmtMoney(x.salaryAmount)}</span></td>
                <td style="text-align: right;">
                    <a href="/TeacherSalary/SalarySlip/${x.employeeId}/${payload.periodId}" class="btn btn-outline" style="padding: 4px 10px; font-size: 0.85rem;">
                        <i class="fa-solid fa-file-invoice"></i> Chi tiết
                    </a>
                </td>
            </tr>
        `).join("");

        if (isLocked) {
            const lockTime = payload.lockedAtUtc ? new Date(payload.lockedAtUtc).toLocaleString("vi-VN") : "";
            setStatus(`Bảng lương tháng ${month}/${year} đã chốt vào lúc ${lockTime}.`, "locked");
        } else {
            setStatus(`Đã tải bảng lương tháng ${month}/${year}. Bạn có thể chỉnh sửa và tính lại nếu cần.`, "info");
        }
    } catch (e) {
        setStatus("Đã xảy ra lỗi hệ thống.", "error");
        tableBody.innerHTML = `<tr><td colspan='6' class='text-center py-5 text-danger'>Lỗi kết nối.</td></tr>`;
    }
}

async function recalculateSalary() {
    if (isLocked || !monthEl || !yearEl) return;
    
    const month = parseInt(monthEl.value, 10);
    const year = parseInt(yearEl.value, 10);
    
    setStatus("Đang tính toán lại bảng lương...", "open");

    try {
        const res = await fetch("/TeacherSalary/Api/Recalculate", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ month, year })
        });
        const payload = await res.json();
        if (payload.success) {
            if(window.showToast) window.showToast('Thành công', 'Đã tính lại lương', 'success');
            await loadSalarySummary();
        } else {
            setStatus(payload.message, "error");
        }
    } catch (e) {
        setStatus("Lỗi kết nối máy chủ.", "error");
    }
}

async function lockSalary() {
    if(!monthEl || !yearEl) return;
    const month = parseInt(monthEl.value, 10);
    const year = parseInt(yearEl.value, 10);
    
    if (!confirm(`Xác nhận CHỐT bảng lương tháng ${month}/${year}?\nSau khi chốt, mọi thay đổi về chấm công sẽ không thể cập nhật vào bảng lương này nữa.`)) {
        return;
    }

    try {
        const res = await fetch("/TeacherSalary/Api/Lock", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ month, year })
        });
        const payload = await res.json();
        if (payload.success) {
            if(window.showToast) window.showToast('Thành công', 'Đã chốt bảng lương', 'success');
            await loadSalarySummary();
        } else {
            setStatus(payload.message, "error");
        }
    } catch (e) {
        setStatus("Lỗi kết nối máy chủ.", "error");
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const loadBtn = document.getElementById("btnLoadSalary");
    const recalculateBtn = document.getElementById("btnRecalculateSalary");
    const lockBtn = document.getElementById("btnLockSalary");

    if(loadBtn) loadBtn.addEventListener("click", loadSalarySummary);
    if(recalculateBtn) recalculateBtn.addEventListener("click", recalculateSalary);
    if(lockBtn) lockBtn.addEventListener("click", lockSalary);

    initFilters();
    loadSalarySummary();
});
