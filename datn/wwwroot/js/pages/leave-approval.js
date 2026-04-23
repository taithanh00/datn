const managerAlert = document.getElementById("managerAlert");
const filterMonthEl = document.getElementById("filterMonth");
const filterYearEl = document.getElementById("filterYear");

function setAlert(message, type = 'info') {
    if(!managerAlert) return;
    let icon = 'fa-circle-info'; let color = 'var(--text-muted)'; let border = 'var(--border)'; let bg = 'var(--bg-card)';
    if (type === 'error') { icon = 'fa-circle-exclamation'; color = 'var(--danger)'; border = 'var(--danger)'; bg = 'rgba(248,113,113,0.1)'; }
    if (type === 'success') { icon = 'fa-circle-check'; color = 'var(--success)'; border = 'var(--success)'; bg = 'rgba(52,211,153,0.1)'; }
    if (type === 'connected') { icon = 'fa-link'; color = 'var(--primary)'; border = 'var(--primary)'; bg = 'var(--primary-soft)'; }
    if (type === 'connecting') { icon = 'fa-circle-notch fa-spin'; }

    managerAlert.style.background = bg;
    managerAlert.style.color = color;
    managerAlert.style.borderLeftColor = border;
    managerAlert.innerHTML = `<i class="fa-solid ${icon}"></i> <span>${message}</span>`;
}

const fmtMoney = (v) => new Intl.NumberFormat("vi-VN").format(v || 0) + " đ";
const fmtTime = (v) => v ? new Date(v).toLocaleTimeString("vi-VN", { hour: '2-digit', minute: '2-digit' }) : "--:--";

function getFilterQuery() {
    if(!filterMonthEl || !filterYearEl) return "";
    const month = filterMonthEl.value;
    const year = filterYearEl.value;
    return `month=${month}&year=${year}`;
}

async function loadAttendancePending() {
    const body = document.getElementById("pendingAttendanceBody");
    if(!body) return;
    try {
        const res = await fetch(`/LeaveApproval/Api/PendingAttendance?${getFilterQuery()}`);
        const payload = await res.json();
        
        if (!payload.success) {
            body.innerHTML = `<tr><td colspan='7' class='text-center py-5 text-danger'><i class='fa-solid fa-triangle-exclamation'></i><p class="mt-2">Lỗi tải dữ liệu chấm công.</p></td></tr>`;
            return;
        }
        if (!payload.data.length) {
            body.innerHTML = `<tr><td colspan='7' class='text-center py-5 text-muted'><div class="empty-state"><i class='fa-solid fa-calendar-check'></i><p>Không có bản ghi chấm công nào cần duyệt trong tháng này.</p></div></td></tr>`;
            return;
        }
        body.innerHTML = payload.data.map(x => `
            <tr>
                <td><strong>${x.employeeName}</strong></td>
                <td>${x.date}</td>
                <td><div class="badge badge-info">${fmtTime(x.checkInAt)}</div></td>
                <td><div class="badge badge-info">${fmtTime(x.checkOutAt)}</div></td>
                <td>
                    <span class="badge ${x.isLate ? 'badge-warning' : 'badge-success'}">
                        ${x.isLate ? 'Đi trễ' : 'Đúng giờ'}
                    </span>
                </td>
                <td style="color: var(--danger); font-weight: 600;">${x.penaltyAmount > 0 ? fmtMoney(x.penaltyAmount) : '--'}</td>
                <td style="text-align: right;">
                    <div class="d-flex justify-end gap-1">
                        <button class="btn btn-outline" style="border-color: var(--success); color: var(--success); padding: 4px 10px; font-size: 0.8rem;" onclick="attendanceDecision(${x.employeeId}, '${x.rawDate}', true)">
                            <i class="fa-solid fa-check"></i> Duyệt
                        </button>
                        <button class="btn btn-outline" style="border-color: var(--danger); color: var(--danger); padding: 4px 10px; font-size: 0.8rem;" onclick="attendanceDecision(${x.employeeId}, '${x.rawDate}', false)">
                            <i class="fa-solid fa-xmark"></i> Từ chối
                        </button>
                    </div>
                </td>
            </tr>
        `).join("");
    } catch (e) {
        body.innerHTML = `<tr><td colspan='7' class='text-center py-5 text-danger'><i class='fa-solid fa-triangle-exclamation'></i><p>Đã xảy ra lỗi hệ thống.</p></td></tr>`;
    }
}

async function loadLeavePending() {
    const body = document.getElementById("pendingLeaveBody");
    if(!body) return;
    try {
        const res = await fetch(`/LeaveApproval/Api/PendingLeaveRequests?${getFilterQuery()}`);
        const payload = await res.json();
        
        if (!payload.success) {
            body.innerHTML = `<tr><td colspan='5' class='text-center py-5 text-danger'><i class='fa-solid fa-triangle-exclamation'></i><p>Lỗi tải đơn nghỉ phép.</p></td></tr>`;
            return;
        }
        if (!payload.data.length) {
            body.innerHTML = `<tr><td colspan='5' class='text-center py-5 text-muted'><div class="empty-state"><i class='fa-solid fa-mug-hot'></i><p>Không có đơn nghỉ phép nào đang chờ duyệt.</p></div></td></tr>`;
            return;
        }
        body.innerHTML = payload.data.map(x => `
            <tr>
                <td><strong>${x.employeeName}</strong></td>
                <td>${x.startDate}</td>
                <td>${x.endDate}</td>
                <td class="text-muted" style="max-width: 250px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;" title="${x.reason || ''}">
                    ${x.reason || "<em>Không có lý do</em>"}
                </td>
                <td style="text-align: right;">
                    <div class="d-flex justify-end gap-1">
                        <button class="btn btn-outline" style="border-color: var(--success); color: var(--success); padding: 4px 10px; font-size: 0.8rem;" onclick="leaveDecision(${x.id}, true)">
                            <i class="fa-solid fa-check"></i> Duyệt
                        </button>
                        <button class="btn btn-outline" style="border-color: var(--danger); color: var(--danger); padding: 4px 10px; font-size: 0.8rem;" onclick="leaveDecision(${x.id}, false)">
                            <i class="fa-solid fa-xmark"></i> Từ chối
                        </button>
                    </div>
                </td>
            </tr>
        `).join("");
    } catch (e) {
        body.innerHTML = `<tr><td colspan='5' class='text-center py-5 text-danger'><i class='fa-solid fa-triangle-exclamation'></i><p>Đã xảy ra lỗi hệ thống.</p></td></tr>`;
    }
}

async function attendanceDecision(employeeId, date, approve) {
    if (!confirm(`Bạn có chắc muốn ${approve ? 'DUYỆT' : 'TỪ CHỐI'} bản ghi chấm công này?`)) return;
    
    const url = approve ? "/LeaveApproval/Api/Attendance/Approve" : "/LeaveApproval/Api/Attendance/Reject";
    try {
        const res = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ employeeId, date, reviewNote: "" })
        });
        const payload = await res.json();
        setAlert(payload.message || "Đã xử lý thành công.", payload.success ? "success" : "error");
        if (payload.success && window.showToast) window.showToast('Thành công', payload.message || "Đã xử lý.", 'success');
        await loadAllPending();
    } catch (e) {
        setAlert("Lỗi khi xử lý yêu cầu.", "error");
    }
}

async function leaveDecision(requestId, approve) {
    if (!confirm(`Bạn có chắc muốn ${approve ? 'DUYỆT' : 'TỪ CHỐI'} đơn nghỉ phép này?`)) return;

    const url = approve ? "/LeaveApproval/Api/Leave/Approve" : "/LeaveApproval/Api/Leave/Reject";
    try {
        const res = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ requestId, reviewNote: "" })
        });
        const payload = await res.json();
        setAlert(payload.message || "Đã xử lý thành công.", payload.success ? "success" : "error");
        if (payload.success && window.showToast) window.showToast('Thành công', payload.message || "Đã xử lý.", 'success');
        await loadAllPending();
    } catch (e) {
        setAlert("Lỗi khi xử lý yêu cầu.", "error");
    }
}

async function loadAllPending() {
    await Promise.all([loadAttendancePending(), loadLeavePending()]);
}

function initFilters() {
    if(!filterMonthEl || !filterYearEl) return;
    const now = new Date();
    const currentMonth = now.getMonth() + 1;
    const currentYear = now.getFullYear();

    filterMonthEl.innerHTML = Array.from({ length: 12 }, (_, idx) => {
        const m = idx + 1;
        return `<option value="${m}" ${m === currentMonth ? "selected" : ""}>Tháng ${m}</option>`;
    }).join("");

    const years = [];
    for (let y = currentYear - 1; y <= currentYear + 1; y++) {
        years.push(`<option value="${y}" ${y === currentYear ? "selected" : ""}>Năm ${y}</option>`);
    }
    filterYearEl.innerHTML = years.join("");
}

// SignalR initialization
if (typeof signalR !== 'undefined') {
    const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/realtime").build();
    connection.on("attendanceChanged", () => loadAllPending());
    connection.on("leaveRequestChanged", () => loadAllPending());
    
    connection.start()
        .then(() => setAlert("Đã kết nối realtime. Dữ liệu sẽ tự động cập nhật.", "connected"))
        .catch(() => setAlert("Không kết nối được realtime. Bạn có thể cần tải lại trang.", "error"));
}

document.addEventListener('DOMContentLoaded', () => {
    const filterBtn = document.getElementById("btnApplyFilter");
    if(filterBtn) {
        filterBtn.addEventListener("click", () => {
            setAlert("Đang lọc dữ liệu...", "connecting");
            loadAllPending();
        });
    }

    initFilters();
    loadAllPending();
});
