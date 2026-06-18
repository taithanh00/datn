const filterMonthEl = document.getElementById("filterMonth");
const filterYearEl = document.getElementById("filterYear");
const historyStatusFilterEl = document.getElementById("historyStatusFilter");
const historyKeywordEl = document.getElementById("historyKeyword");
const pendingLeaveDecisions = new Set();
let activeHistoryType = "attendance";
let historySearchTimer = null;

const fmtMoney = (value) => `${new Intl.NumberFormat("vi-VN").format(value || 0)} đ`;
const fmtTime = (value) => {
    if (!value) return "--:--";
    if (typeof value === "string" && /^\d{2}:\d{2}/.test(value)) return value.slice(0, 5);

    return new Date(value).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });
};
const fmtDateTime = (value) => {
    if (!value) return "--";
    if (typeof value === "string" && /^\d{2}\/\d{2}\/\d{4}\s+\d{2}:\d{2}/.test(value)) return value;

    return new Date(value).toLocaleString("vi-VN", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit"
    });
};

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

function getHistoryQuery() {
    const params = new URLSearchParams();
    if (filterMonthEl?.value) params.set("month", filterMonthEl.value);
    if (filterYearEl?.value) params.set("year", filterYearEl.value);
    if (historyStatusFilterEl?.value) params.set("status", historyStatusFilterEl.value);
    if (historyKeywordEl?.value.trim()) params.set("keyword", historyKeywordEl.value.trim());
    return params.toString();
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

function renderApprovalStatusBadge(status) {
    if (status === "Approved") {
        return '<span class="badge badge-success"><i class="fa-solid fa-circle-check"></i> Đã duyệt</span>';
    }

    if (status === "Rejected") {
        return '<span class="badge badge-danger"><i class="fa-solid fa-circle-xmark"></i> Từ chối</span>';
    }

    return `<span class="badge badge-info">${escapeHtml(status || "--")}</span>`;
}

function renderNote(value) {
    const note = value?.trim();
    if (!note) return '<span class="text-muted">--</span>';
    return `<div class="text-muted" style="font-size:0.85rem; max-width:260px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;" title="${escapeHtml(note)}">${escapeHtml(note)}</div>`;
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
                            <button class="btn-table" onclick="leaveDecision(this, ${item.id}, true)">Duyệt</button>
                            <button class="btn-table delete" onclick="leaveDecision(this, ${item.id}, false)">Từ chối</button>
                        </div>
                    </td>
                </tr>
            `;
        }).join("");
    } catch {
        body.innerHTML = `<tr><td colspan="4" class="text-center py-5 text-danger"><i class="fa-solid fa-triangle-exclamation"></i><p>Đã xảy ra lỗi hệ thống.</p></td></tr>`;
    }
}

async function loadAttendanceHistory() {
    const body = document.getElementById("attendanceHistoryBody");
    if (!body) return;

    body.innerHTML = `<tr><td colspan="7" class="text-center py-5 text-muted"><div class="spinner"></div><p class="mt-2">Đang tải lịch sử chấm công...</p></td></tr>`;

    try {
        const res = await fetch(`/LeaveApproval/Api/Attendance/History?${getHistoryQuery()}`);
        const payload = await readJsonResponse(res);

        if (!payload.success) {
            body.innerHTML = `<tr><td colspan="7" class="text-center py-5 text-danger"><i class="fa-solid fa-triangle-exclamation"></i><p class="mt-2">${escapeHtml(payload.message || "Lỗi tải lịch sử chấm công.")}</p></td></tr>`;
            return;
        }

        if (!payload.data.length) {
            body.innerHTML = `<tr><td colspan="7" class="text-center py-5 text-muted"><div class="empty-state"><i class="fa-solid fa-folder-open"></i><p>Chưa có lịch sử duyệt chấm công trong bộ lọc này.</p></div></td></tr>`;
            return;
        }

        body.innerHTML = payload.data.map((item) => {
            const note = item.reviewNote || item.note;
            return `
                <tr>
                    <td><strong>${escapeHtml(item.employeeName)}</strong></td>
                    <td>${escapeHtml(item.date)}</td>
                    <td>
                        <div class="badge badge-info">${fmtTime(item.checkInAt)} - ${fmtTime(item.checkOutAt)}</div>
                    </td>
                    <td>${renderApprovalStatusBadge(item.status)}</td>
                    <td>${escapeHtml(item.reviewerName || "Hệ thống")}</td>
                    <td>${fmtDateTime(item.reviewedAt)}</td>
                    <td>${renderNote(note)}</td>
                </tr>
            `;
        }).join("");
    } catch (error) {
        body.innerHTML = `<tr><td colspan="7" class="text-center py-5 text-danger"><i class="fa-solid fa-triangle-exclamation"></i><p>${escapeHtml(error.message || "Đã xảy ra lỗi hệ thống.")}</p></td></tr>`;
    }
}

async function loadLeaveHistory() {
    const body = document.getElementById("leaveHistoryBody");
    if (!body) return;

    body.innerHTML = `<tr><td colspan="7" class="text-center py-5 text-muted"><div class="spinner"></div><p class="mt-2">Đang tải lịch sử nghỉ phép...</p></td></tr>`;

    try {
        const res = await fetch(`/LeaveApproval/Api/Leave/History?${getHistoryQuery()}`);
        const payload = await readJsonResponse(res);

        if (!payload.success) {
            body.innerHTML = `<tr><td colspan="7" class="text-center py-5 text-danger"><i class="fa-solid fa-triangle-exclamation"></i><p class="mt-2">${escapeHtml(payload.message || "Lỗi tải lịch sử nghỉ phép.")}</p></td></tr>`;
            return;
        }

        if (!payload.data.length) {
            body.innerHTML = `<tr><td colspan="7" class="text-center py-5 text-muted"><div class="empty-state"><i class="fa-solid fa-folder-open"></i><p>Chưa có lịch sử duyệt nghỉ phép trong bộ lọc này.</p></div></td></tr>`;
            return;
        }

        body.innerHTML = payload.data.map((item) => {
            const typeBadge = item.isPaid
                ? '<span class="badge" style="background: rgba(var(--primary-rgb), 0.1); color: var(--primary); font-size: 0.7rem; margin-bottom: 4px;">CÓ LƯƠNG</span>'
                : '<span class="badge" style="background: #f1f5f9; color: #64748b; font-size: 0.7rem; margin-bottom: 4px;">KHÔNG LƯƠNG</span>';
            const reason = item.reason?.trim()
                ? `<div class="text-muted" style="font-size:0.85rem; max-width:240px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;" title="${escapeHtml(item.reason)}">${escapeHtml(item.reason)}</div>`
                : '<div class="text-muted" style="font-size:0.85rem;">--</div>';

            return `
                <tr>
                    <td><strong>${escapeHtml(item.employeeName)}</strong></td>
                    <td>
                        <div style="font-weight:600;">${escapeHtml(item.startDate)}</div>
                        <div class="text-muted" style="font-size:0.8rem;"><i class="fa-solid fa-arrow-right"></i> ${escapeHtml(item.endDate)}</div>
                    </td>
                    <td>
                        ${typeBadge}
                        ${reason}
                    </td>
                    <td>${renderApprovalStatusBadge(item.status)}</td>
                    <td>${escapeHtml(item.reviewerName || "Hệ thống")}</td>
                    <td>${fmtDateTime(item.reviewedAt)}</td>
                    <td>${renderNote(item.reviewNote)}</td>
                </tr>
            `;
        }).join("");
    } catch (error) {
        body.innerHTML = `<tr><td colspan="7" class="text-center py-5 text-danger"><i class="fa-solid fa-triangle-exclamation"></i><p>${escapeHtml(error.message || "Đã xảy ra lỗi hệ thống.")}</p></td></tr>`;
    }
}

async function loadHistory() {
    if (activeHistoryType === "leave") {
        await loadLeaveHistory();
        return;
    }

    await loadAttendanceHistory();
}

async function loadAllData() {
    await Promise.all([loadAttendancePending(), loadLeavePending(), loadHistory()]);
}

function setHistoryType(type) {
    activeHistoryType = type === "leave" ? "leave" : "attendance";

    document.querySelectorAll("[data-history-type]").forEach((button) => {
        button.classList.toggle("active", button.dataset.historyType === activeHistoryType);
    });

    const attendancePanel = document.getElementById("attendanceHistoryPanel");
    const leavePanel = document.getElementById("leaveHistoryPanel");
    if (attendancePanel) attendancePanel.style.display = activeHistoryType === "attendance" ? "" : "none";
    if (leavePanel) leavePanel.style.display = activeHistoryType === "leave" ? "" : "none";

    loadHistory();
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
        if (payload.success) await loadAllData();
    } catch (error) {
        if (window.showToast) window.showToast("Có lỗi", error.message || "Lỗi khi xử lý yêu cầu.", "error");
    }
}

function setLeaveDecisionBusy(button, busy) {
    const row = button?.closest("tr");
    if (!row) return;

    row.querySelectorAll("button").forEach((item) => {
        item.disabled = busy;
        item.style.opacity = busy ? "0.55" : "";
        item.style.pointerEvents = busy ? "none" : "";
    });
}

async function leaveDecision(button, requestId, approve) {
    if (pendingLeaveDecisions.has(requestId)) return;

    pendingLeaveDecisions.add(requestId);
    setLeaveDecisionBusy(button, true);

    try {
        const confirmed = await window.appConfirm(`Bạn có chắc muốn ${approve ? "DUYỆT" : "TỪ CHỐI"} đơn nghỉ phép này?`);
        if (!confirmed) return;

        const url = approve ? "/LeaveApproval/Api/Leave/Approve" : "/LeaveApproval/Api/Leave/Reject";
        const res = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ requestId, reviewNote: "" })
        });
        const payload = await readJsonResponse(res);
        showDecisionToast(payload, "Đã xử lý.");
        if (payload.success || payload.message) await loadAllData();
    } catch (error) {
        if (window.showToast) window.showToast("Có lỗi", error.message || "Lỗi khi xử lý yêu cầu.", "error");
    } finally {
        pendingLeaveDecisions.delete(requestId);
        setLeaveDecisionBusy(button, false);
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
    connection.on("attendanceChanged", () => loadAllData());
    connection.on("leaveRequestChanged", () => loadAllData());
    connection.start().catch(() => console.warn("LeaveApproval realtime unavailable."));
}

document.addEventListener("DOMContentLoaded", () => {
    initFilters();
    filterMonthEl?.addEventListener("change", loadAllData);
    filterYearEl?.addEventListener("change", loadAllData);
    historyStatusFilterEl?.addEventListener("change", loadHistory);
    historyKeywordEl?.addEventListener("input", () => {
        clearTimeout(historySearchTimer);
        historySearchTimer = setTimeout(loadHistory, 250);
    });
    document.querySelectorAll("[data-history-type]").forEach((button) => {
        button.addEventListener("click", () => setHistoryType(button.dataset.historyType));
    });
    loadAllData();
});
