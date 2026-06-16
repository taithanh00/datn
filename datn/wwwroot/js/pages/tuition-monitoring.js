let tuitionSearchTimer;

document.addEventListener("DOMContentLoaded", async () => {
    bindTuitionMonitoringEvents();
    await loadClassFilters();
    await loadMonitoring();
});

function bindTuitionMonitoringEvents() {
    const searchElem = document.getElementById("tuitionSearch");
    const toggleFilterBtn = document.getElementById("btnToggleFilter");
    const filterPanel = document.getElementById("filterPanel");
    const applyBtn = document.getElementById("btnApplyFilter");
    const resetBtn = document.getElementById("btnResetFilter");
    const monthElem = document.getElementById("monMonth");
    const yearElem = document.getElementById("monYear");
    const statusElem = document.getElementById("monStatus");
    const classElem = document.getElementById("monClass");

    searchElem?.addEventListener("input", () => {
        clearTimeout(tuitionSearchTimer);
        tuitionSearchTimer = setTimeout(loadMonitoring, 300);
    });

    toggleFilterBtn?.addEventListener("click", () => {
        filterPanel?.classList.toggle("active");
        toggleFilterBtn.classList.toggle("active");
    });

    applyBtn?.addEventListener("click", loadMonitoring);
    monthElem?.addEventListener("change", loadMonitoring);
    yearElem?.addEventListener("change", loadMonitoring);
    statusElem?.addEventListener("change", loadMonitoring);
    classElem?.addEventListener("change", loadMonitoring);

    resetBtn?.addEventListener("click", async () => {
        if (searchElem) searchElem.value = "";
        if (statusElem) statusElem.value = "";
        if (classElem) classElem.value = "";
        await loadMonitoring();
    });
}

async function loadClassFilters() {
    const classElem = document.getElementById("monClass");
    if (!classElem) return;

    try {
        const res = await fetch("/Manager/Api/Classes");
        const result = await res.json();
        if (!result.success) return;

        const options = result.data
            .map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`)
            .join("");
        classElem.innerHTML = `<option value="">-- Tất cả --</option>${options}`;
    } catch (error) {
        console.error("Load class filters failed", error);
    }
}

async function loadMonitoring() {
    const tbody = document.getElementById("monitoringTable");
    const resultCountElem = document.getElementById("tuitionResultCount");
    if (!tbody) return;

    const query = buildMonitoringQuery();

    if (window.appLoading) {
        tbody.innerHTML = window.appLoading.tableRow(5);
    } else {
    tbody.innerHTML = `
        <tr>
            <td colspan="5" class="text-center py-5 text-muted">
                <div class="spinner-border text-primary" role="status"></div>
                <div class="mt-2">Đang tải dữ liệu...</div>
            </td>
        </tr>`;
    }
    if (resultCountElem) resultCountElem.textContent = "Đang tải dữ liệu...";

    try {
        const res = await fetch(`/Tuition/Api/Monitoring?${query.toString()}`);
        const data = await res.json();
        if (!data.success) return;

        const rows = data.data || [];
        if (resultCountElem) {
            resultCountElem.textContent = `${rows.length} học sinh phù hợp với bộ lọc`;
        }

        if (rows.length === 0) {
            if (window.appLoading) {
                tbody.innerHTML = window.appLoading.tableEmpty(5, "Kh\u00f4ng c\u00f3 h\u00f3a \u0111\u01a1n n\u00e0o ph\u00f9 h\u1ee3p v\u1edbi b\u1ed9 l\u1ecdc hi\u1ec7n t\u1ea1i.");
                return;
            }
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="text-center py-5 text-muted">
                        <div class="empty-state">
                            <i class="fa-solid fa-file-invoice"></i>
                            <p>Không có hóa đơn nào phù hợp với bộ lọc hiện tại.</p>
                        </div>
                    </td>
                </tr>`;
            return;
        }

        tbody.innerHTML = rows.map(t => `
            <tr>
                <td>
                    <div style="font-weight: 600;">${escapeHtml(t.studentName)}</div>
                    <div class="text-muted" style="font-size: 0.85rem;">ID: #${t.studentId}</div>
                </td>
                <td>
                    <div class="badge badge-info">${escapeHtml(t.className)}</div>
                </td>
                <td>
                    <div style="font-weight: 700; color: var(--text-main);">${formatMoney(t.total)}</div>
                    ${t.extraFee > 0 ? `<div style="font-size: 0.8rem; color: var(--success);"><i class="fa-solid fa-plus"></i> Phụ phí: ${formatMoney(t.extraFee)}</div>` : ""}
                </td>
                <td style="text-align: center;">
                    ${t.isPaid
                        ? '<span class="badge badge-success"><i class="fa-solid fa-check-circle"></i> Đã nộp</span>'
                        : '<span class="badge badge-warning"><i class="fa-solid fa-clock"></i> Chờ nộp</span>'}
                </td>
                <td style="text-align: right;">
                    ${!t.isPaid ? `
                        <button class="btn-table" onclick="confirmPaid(${t.id})">Xác nhận nộp</button>` : `
                        <button class="btn-table" disabled style="opacity:0.5; cursor:default; color:var(--success);">Đã xác nhận</button>
                    `}
                </td>
            </tr>
        `).join("");

        if (window.initPagination) initPagination("monitoringTableEl", 15);
    } catch (error) {
        console.error("Load monitoring failed", error);
        if (window.appLoading) {
            if (resultCountElem) resultCountElem.textContent = "Kh\u00f4ng th\u1ec3 t\u1ea3i d\u1eef li\u1ec7u";
            tbody.innerHTML = window.appLoading.tableError(5, "Kh\u00f4ng th\u1ec3 t\u1ea3i d\u1eef li\u1ec7u h\u1ecdc ph\u00ed. Vui l\u00f2ng th\u1eed l\u1ea1i.");
            return;
        }
        if (resultCountElem) resultCountElem.textContent = "Không thể tải dữ liệu";
        tbody.innerHTML = `
            <tr>
                <td colspan="5" class="text-center py-5 text-danger">
                    Không thể tải dữ liệu học phí. Vui lòng thử lại.
                </td>
            </tr>`;
    }
}

function buildMonitoringQuery() {
    const month = document.getElementById("monMonth")?.value;
    const year = document.getElementById("monYear")?.value;
    const isPaid = document.getElementById("monStatus")?.value;
    const classId = document.getElementById("monClass")?.value;
    const search = document.getElementById("tuitionSearch")?.value?.trim();

    const query = new URLSearchParams();
    if (month) query.set("month", month);
    if (year) query.set("year", year);
    if (isPaid) query.set("isPaid", isPaid);
    if (classId) query.set("classId", classId);
    if (search) query.set("search", search);
    return query;
}

async function confirmPaid(id) {
    if (!(await window.appConfirm("Bạn chắc chắn rằng học sinh này đã nộp học phí?"))) return;

    try {
        const res = await fetch(`/Tuition/Api/ConfirmPaid/${id}`, { method: "POST" });
        const result = await res.json();
        if (result.success) {
            await loadMonitoring();
            if (window.showToast) window.showToast("Thành công", result.message, "success");
        }
    } catch (error) {
        console.error("Confirm paid failed", error);
    }
}

function formatMoney(value) {
    return `${new Intl.NumberFormat("vi-VN").format(value || 0)} đ`;
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

window.loadMonitoring = loadMonitoring;
window.confirmPaid = confirmPaid;
