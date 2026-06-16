const filterMonthEl = document.getElementById("filterMonth");
const filterYearEl = document.getElementById("filterYear");

const fmtMoney = (value) => `${new Intl.NumberFormat("vi-VN").format(value || 0)} đ`;
const fmtTime = (value) => value
    ? new Date(value).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })
    : "--:--";

function escapeHtml(value) {
    return String(value ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function getFilterQuery() {
    if (!filterMonthEl || !filterYearEl) return "";
    return `month=${filterMonthEl.value}&year=${filterYearEl.value}`;
}

async function readJsonResponse(response) {
    const text = await response.text();
    let payload = {};

    try {
        payload = text ? JSON.parse(text) : {};
    } catch {
        throw new Error("Phản hồi từ máy chủ không hợp lệ.");
    }

    if (!response.ok) {
        throw new Error(payload.message || `Máy chủ trả về lỗi ${response.status}.`);
    }

    return payload;
}

function showDecisionToast(payload, fallbackSuccessMessage) {
    if (!window.showToast) return;

    if (payload.success) {
        window.showToast("Thành công", payload.message || fallbackSuccessMessage, "success");
        return;
    }

    window.showToast("Có lỗi", payload.message || "Không thể xử lý yêu cầu.", "error");
}

async function loadAttendancePending() {
    const body = document.getElementById("pendingAttendanceBody");
    if (!body) return;

    try {
        const res = await fetch(`/LeaveApproval/Api/PendingAttendance?${getFilterQuery()}`);
        const payload = await res.json();

        if (!payload.success) {
            body.innerHTML = `<tr><td colspan="7" class="text-center py-5 text-danger"><i class="fa-solid fa-triangle-exclamation"></i><p class="mt-2">Lỗi tải dữ liệu chấm công.</p></td></tr>`;
            return;
        }

        if (!payload.data.length) {
            body.innerHTML = `<tr><td colspan="7" class="text-center py-5 text-muted"><div class="empty-state"><i class="fa-solid fa-calendar-check"></i><p>Không có bản ghi chấm công nào cần duyệt trong tháng này.</p></div></td></tr>`;
            return;
        }

        body.innerHTML = payload.data.map((item) => `
            <tr>
                <td><strong>${escapeHtml(item.employeeName)}</strong></td>
                <td>${escapeHtml(item.date)}</td>
                <td><div class="badge badge-info">${fmtTime(item.checkInAt)}</div></td>
                <td><div class="badge badge-info">${fmtTime(item.checkOutAt)}</div></td>
                <td>
                    <span class="badge ${item.isLate ? "badge-warning" : "badge-success"}">
                        ${item.isLate ? "Đi trễ" : "Đúng giờ"}
                    </span>
                </td>
                <td style="color: var(--danger); font-weight: 600;">${item.penaltyAmount > 0 ? fmtMoney(item.penaltyAmount) : "--"}</td>
                <td style="text-align: right;">
                    <div class="d-flex justify-end gap-1">
                        <button class="btn-table" onclick="attendanceDecision(${item.employeeId}, '${escapeHtml(item.rawDate)}', true)">Duyệt</button>
                        <button class="btn-table delete" onclick="attendanceDecision(${item.employeeId}, '${escapeHtml(item.rawDate)}', false)">Từ chối</button>
                    </div>
                </td>
            </tr>
        `).join("");
    } catch {
        body.innerHTML = `<tr><td colspan="7" class="text-center py-5 text-danger"><i class="fa-solid fa-triangle-exclamation"></i><p>Đã xảy ra lỗi hệ thống.</p></td></tr>`;
    }
}

async function loadLeavePending() {
    const body = document.getElementById("pendingLeaveBody");
    if (!body) return;

    try {
        const res = await fetch(`/LeaveApproval/Api/PendingLeaveRequests?${getFilterQuery()}`);
        const payload = await res.json();

        if (!payload.success) {
            body.innerHTML = `<tr><td colspan="4" class="text-center py-5 text-danger"><i class="fa-solid fa-triangle-exclamation"></i><p>Lỗi tải đơn nghỉ phép.</p></td></tr>`;
            return;
        }

        if (!payload.data.length) {
            body.innerHTML = `<tr><td colspan="4" class="text-center py-5 text-muted"><div class="empty-state"><i class="fa-solid fa-mug-hot"></i><p>Không có đơn nghỉ phép nào đang chờ duyệt.</p></div></td></tr>`;
            return;
        }

        body.innerHTML = payload.data.map((item) => {
            const typeBadge = item.isPaid
                ? '<span class="badge" style="background: rgba(var(--primary-rgb), 0.1); color: var(--primary); font-size: 0.7rem; margin-bottom: 4px;">CÓ LƯƠNG</span>'
                : '<span class="badge" style="background: #f1f5f9; color: #64748b; font-size: 0.7rem; margin-bottom: 4px;">KHÔNG LƯƠNG</span>';
            const reason = item.reason ? escapeHtml(item.reason) : "<em>Không có lý do</em>";

            return `
                <tr>
                    <td><strong>${escapeHtml(item.employeeName)}</strong></td>
                    <td>
                        <div style="font-weight:600;">${escapeHtml(item.startDate)}</div>
                        <div class="text-muted" style="font-size:0.8rem;"><i class="fa-solid fa-arrow-right"></i> ${escapeHtml(item.endDate)}</div>
                    </td>
                    <td>
                        ${typeBadge}
                        <div class="text-muted" style="font-size: 0.9rem; max-width: 250px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;" title="${escapeHtml(item.reason || "")}">
                            ${reason}
                        </div>
                    </td>
                    <td style="text-align: right;">
                        <div class="d-flex justify-end gap-1">
                            <button class="btn-table" onclick="leaveDecision(${item.id}, true)">Duyệt</button>
                            <button class="btn-table delete" onclick="leaveDecision(${item.id}, false)">Từ chối</button>
                        </div>
                    </td>
                </tr>
            `;
        }).join("");
    } catch {
        body.innerHTML = `<tr><td colspan="4" class="text-center py-5 text-danger"><i class="fa-solid fa-triangle-exclamation"></i><p>Đã xảy ra lỗi hệ thống.</p></td></tr>`;
    }
}

async function attendanceDecision(employeeId, date, approve) {
    if (!(await window.appConfirm(`Bạn có chắc muốn ${approve ? "DUYỆT" : "TỪ CHỐI"} bản ghi chấm công này?`))) return;

    const url = approve ? "/LeaveApproval/Api/Attendance/Approve" : "/LeaveApproval/Api/Attendance/Reject";
    try {
        const res = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ employeeId, date, reviewNote: "" })
        });
        const payload = await readJsonResponse(res);
        showDecisionToast(payload, "Đã xử lý.");
        if (payload.success) await loadAllPending();
    } catch (error) {
        if (window.showToast) window.showToast("Có lỗi", error.message || "Lỗi khi xử lý yêu cầu.", "error");
    }
}

async function leaveDecision(requestId, approve) {
    if (!(await window.appConfirm(`Bạn có chắc muốn ${approve ? "DUYỆT" : "TỪ CHỐI"} đơn nghỉ phép này?`))) return;

    const url = approve ? "/LeaveApproval/Api/Leave/Approve" : "/LeaveApproval/Api/Leave/Reject";
    try {
        const res = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ requestId, reviewNote: "" })
        });
        const payload = await readJsonResponse(res);
        showDecisionToast(payload, "Đã xử lý.");
        if (payload.success) await loadAllPending();
    } catch (error) {
        if (window.showToast) window.showToast("Có lỗi", error.message || "Lỗi khi xử lý yêu cầu.", "error");
    }
}

async function loadAllPending() {
    await Promise.all([loadAttendancePending(), loadLeavePending()]);
}

function initFilters() {
    if (!filterMonthEl || !filterYearEl) return;

    const now = new Date();
    const currentMonth = now.getMonth() + 1;
    const currentYear = now.getFullYear();

    filterMonthEl.innerHTML = Array.from({ length: 12 }, (_, index) => {
        const month = index + 1;
        return `<option value="${month}" ${month === currentMonth ? "selected" : ""}>Tháng ${month}</option>`;
    }).join("");

    const years = [];
    for (let year = currentYear - 1; year <= currentYear + 1; year += 1) {
        years.push(`<option value="${year}" ${year === currentYear ? "selected" : ""}>Năm ${year}</option>`);
    }
    filterYearEl.innerHTML = years.join("");
}

if (typeof signalR !== "undefined") {
    const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/realtime").build();
    connection.on("attendanceChanged", () => loadAllPending());
    connection.on("leaveRequestChanged", () => loadAllPending());
    connection.start().catch(() => console.warn("LeaveApproval realtime unavailable."));
}

document.addEventListener("DOMContentLoaded", () => {
    initFilters();
    filterMonthEl?.addEventListener("change", loadAllPending);
    filterYearEl?.addEventListener("change", loadAllPending);
    loadAllPending();
});
