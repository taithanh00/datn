const monthEl = document.getElementById("salaryMonth");
const yearEl = document.getElementById("salaryYear");
const statusEl = document.getElementById("salaryStatus");
const statusBar = document.getElementById("salaryStatusBar");
const tableBody = document.getElementById("salaryTableBody");
const btnRecalculate = document.getElementById("btnRecalculateSalary");
const btnLock = document.getElementById("btnLockSalary");
const btnMarkPaidAll = document.getElementById("btnMarkPaidAllSalary");
const tableContainer = document.getElementById("salaryTableContainer");
const detailPanel = document.getElementById("salaryDetailPanel");
const detailOverlay = document.getElementById("salaryPanelOverlay");
const detailBody = document.getElementById("salaryDetailBody");

let isLocked = false;
let salaryRows = [];

const fmtMoney = (value) => new Intl.NumberFormat("vi-VN").format(value || 0) + " đ";

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function setStatus(message, type = "info") {
  if (!statusBar) return;
  const styles = {
    error: ["fa-triangle-exclamation", "var(--danger)", "rgba(248,113,113,0.1)"],
    info: ["fa-circle-check", "var(--success)", "rgba(52,211,153,0.1)"],
    locked: ["fa-lock", "var(--warning)", "rgba(251,191,36,0.1)"],
    neutral: ["fa-circle-info", "var(--text-muted)", "var(--bg-card)"],
  };
  const [icon, color, bg] = styles[type] || styles.neutral;
  statusBar.style.background = bg;
  statusBar.style.color = color;
  statusBar.style.borderLeftColor = color;
  statusBar.innerHTML = `<i class="fa-solid ${icon}"></i> <span>${escapeHtml(message)}</span>`;
}

function showPayrollWarning(title, message) {
  if (window.showToast) window.showToast(title, message, "warning");
  setStatus(message, "error");
}

function getSalaryRow(employeeId) {
  return salaryRows.find((item) => Number(item.employeeId) === Number(employeeId));
}

function disabledActionAttr(disabled) {
  return disabled ? 'disabled aria-disabled="true" style="opacity:.45; pointer-events:none;"' : "";
}

function initFilters() {
  if (!monthEl || !yearEl) return;
  const now = new Date();
  const currentMonth = now.getMonth() + 1;
  const currentYear = now.getFullYear();

  monthEl.innerHTML = Array.from({ length: 12 }, (_, index) => {
    const month = index + 1;
    return `<option value="${month}" ${month === currentMonth ? "selected" : ""}>Tháng ${month}</option>`;
  }).join("");

  yearEl.innerHTML = Array.from({ length: 3 }, (_, index) => {
    const year = currentYear - 1 + index;
    return `<option value="${year}" ${year === currentYear ? "selected" : ""}>Năm ${year}</option>`;
  }).join("");
}

async function loadSalarySummary() {
  if (!monthEl || !yearEl || !tableBody) return;
  const month = monthEl.value;
  const year = yearEl.value;
  const status = statusEl?.value || "";
  const query = new URLSearchParams({ month, year });
  if (status) query.append("status", status);

  tableBody.innerHTML = window.appLoading
    ? window.appLoading.tableRow(9)
    : `<tr><td colspan="9" class="text-center py-5"><div class="spinner"></div></td></tr>`;

  try {
    const response = await fetch(`/TeacherSalary/Api/Summary?${query.toString()}`);
    const payload = await response.json();

    if (!payload.success) {
      if (window.appLoading) {
        setStatus(payload.message || "Không thể tải dữ liệu.", "error");
        tableBody.innerHTML = window.appLoading.tableError(9, "Không thể tải dữ liệu lương.");
        return;
      }
      setStatus(payload.message || "Lỗi tải dữ liệu.", "error");
      tableBody.innerHTML = `<tr><td colspan="9" class="text-center py-5 text-danger">Lỗi tải dữ liệu.</td></tr>`;
      return;
    }

    isLocked = payload.isLocked === true;
    salaryRows = payload.data || [];
    updateSummaryCards(payload.summary || {});
    renderSalaryRows(salaryRows, payload.periodId);
    if (tableContainer) tableContainer.scrollLeft = 0;

    if (btnRecalculate) btnRecalculate.disabled = isLocked;
    if (btnLock) btnLock.disabled = isLocked || salaryRows.length === 0;
    if (btnMarkPaidAll) btnMarkPaidAll.disabled = salaryRows.length === 0;

    if (salaryRows.length === 0) {
      setStatus(`Kỳ lương ${month}/${year} chưa có dữ liệu. Bấm "Tính lại kỳ lương" để khởi tạo.`, "neutral");
    } else if (isLocked) {
      setStatus(`Kỳ lương ${month}/${year} đã chốt. Không thể tính lại kỳ này.`, "locked");
    } else {
      setStatus(`Đã tải bảng lương ${month}/${year}. Có thể tính lại, chốt từng giáo viên hoặc chốt toàn bộ.`, "info");
    }
  } catch (error) {
    console.error(error);
    if (window.appLoading) {
      setStatus("Lỗi kết nối máy chủ.", "error");
      tableBody.innerHTML = window.appLoading.tableError(9, "Lỗi kết nối máy chủ.");
      return;
    }
    setStatus("Lỗi kết nối máy chủ.", "error");
    tableBody.innerHTML = `<tr><td colspan="9" class="text-center py-5 text-danger">Lỗi kết nối.</td></tr>`;
  }
}

function updateSummaryCards(summary) {
  document.getElementById("statTotalTeachers").textContent = summary.totalTeachers || 0;
  document.getElementById("statCalculated").textContent = summary.calculated || 0;
  document.getElementById("statLocked").textContent = summary.locked || 0;
  document.getElementById("statPaid").textContent = summary.paid || 0;
  document.getElementById("statTotalAmount").textContent = fmtMoney(summary.totalAmount || 0);
}

function renderSalaryRows(rows, periodId) {
  if (!rows.length) {
    if (window.appLoading) {
      tableBody.innerHTML = window.appLoading.tableEmpty(9, "Chưa có dữ liệu lương cho kỳ này.");
      return;
    }
    tableBody.innerHTML = `<tr><td colspan="9" class="text-center py-5 text-muted"><div class="empty-state"><i class="fa-solid fa-folder-open"></i><p>Chưa có dữ liệu lương cho kỳ này.</p></div></td></tr>`;
    return;
  }

  tableBody.innerHTML = rows.map((row) => {
    const lockedRow = row.status === "Locked" || row.status === "Paid" || isLocked;
    const paidRow = row.status === "Paid";
    const immutableAttr = disabledActionAttr(lockedRow);
    const paidAttr = disabledActionAttr(paidRow);
    return `
      <tr>
        <td>
          <strong>${escapeHtml(row.employeeName)}</strong>
          <div class="text-muted">#${row.employeeId}</div>
        </td>
        <td class="payroll-money"><span class="badge badge-info">${fmtMoney(row.baseSalary)}</span></td>
        <td class="payroll-days">${row.standardWorkingDays || 0} ngày</td>
        <td class="payroll-days"><strong>${row.workingDays || 0}</strong> công</td>
        <td class="payroll-money" style="color: var(--danger);">${fmtMoney(row.penaltyAmount)}</td>
        <td class="payroll-money" style="color: var(--success);">${fmtMoney(row.coverageBonusAmount)}</td>
        <td class="payroll-money" style="text-align:right;"><strong style="color: var(--primary); font-size:1.05rem;">${fmtMoney(row.salaryAmount)}</strong></td>
        <td class="payroll-status">${renderStatusBadge(row.status)}</td>
        <td class="payroll-actions-cell" style="text-align:right;">
          <div class="payroll-actions-row">
            <button class="btn-table" onclick="openSalaryDetail(${row.employeeId})">Chi tiết</button>
            <a href="/TeacherSalary/SalarySlip/${row.employeeId}/${periodId}" class="btn-table text-decoration-none">Phiếu</a>
            <button class="btn-table" ${immutableAttr} onclick="recalculateEmployee(${row.employeeId})">Tính lại</button>
            <button class="btn-table" ${immutableAttr} onclick="lockEmployee(${row.employeeId})">Chốt</button>
            <button class="btn-table" ${paidAttr} onclick="markPaid(${row.employeeId})">Thanh toán</button>
          </div>
        </td>
      </tr>
    `;
  }).join("");
}

function renderStatusBadge(status) {
  switch (status) {
    case "Paid":
      return `<span class="badge badge-success"><i class="fa-solid fa-circle-check"></i> Đã thanh toán</span>`;
    case "Locked":
      return `<span class="badge badge-info"><i class="fa-solid fa-lock"></i> Đã chốt</span>`;
    case "Calculated":
      return `<span class="badge badge-warning"><i class="fa-solid fa-calculator"></i> Đã tính</span>`;
    case "Cancelled":
      return `<span class="badge badge-danger">Đã hủy</span>`;
    default:
      return `<span class="badge">Nháp</span>`;
  }
}

async function recalculateSalary() {
  if (isLocked) return;
  const month = Number.parseInt(monthEl.value, 10);
  const year = Number.parseInt(yearEl.value, 10);
  setStatus("Đang tính lại toàn bộ kỳ lương...", "neutral");
  await postPayroll("/TeacherSalary/Api/Recalculate", { month, year });
}

async function recalculateEmployee(employeeId) {
  const row = getSalaryRow(employeeId);
  if (isLocked || row?.status === "Locked" || row?.status === "Paid") {
    showPayrollWarning("Không thể tính lại", "Dòng lương đã chốt hoặc đã thanh toán, không thể tính lại.");
    return;
  }

  const month = Number.parseInt(monthEl.value, 10);
  const year = Number.parseInt(yearEl.value, 10);
  await postPayroll("/TeacherSalary/Api/RecalculateEmployee", { employeeId, month, year });
}

async function lockEmployee(employeeId) {
  const row = getSalaryRow(employeeId);
  if (row?.status === "Paid") {
    showPayrollWarning("Không thể chốt", "Dòng lương đã thanh toán, không thể chốt lại.");
    return;
  }
  if (isLocked || row?.status === "Locked") {
    showPayrollWarning("Không thể chốt", "Dòng lương này đã được chốt.");
    return;
  }

  if (!(await window.appConfirm("Chốt lương giáo viên này? Sau khi chốt sẽ không thể tính lại dòng lương này."))) return;
  const month = Number.parseInt(monthEl.value, 10);
  const year = Number.parseInt(yearEl.value, 10);
  await postPayroll("/TeacherSalary/Api/LockSalary", { employeeId, month, year });
}

async function lockSalary() {
  if (!monthEl || !yearEl) return;
  const month = Number.parseInt(monthEl.value, 10);
  const year = Number.parseInt(yearEl.value, 10);
  if (!(await window.appConfirm(`Chốt toàn bộ bảng lương tháng ${month}/${year}?`))) return;
  await postPayroll("/TeacherSalary/Api/Lock", { month, year });
}

async function markPaid(employeeId) {
  const row = getSalaryRow(employeeId);
  if (row?.status === "Paid") {
    showPayrollWarning("Đã thanh toán", "Dòng lương này đã được ghi nhận thanh toán.");
    return;
  }

  if (!row || row.status !== "Locked") {
    const message = "Vui lòng chốt lương giáo viên trước khi ghi nhận đã trả.";
    showPayrollWarning("Chưa thể ghi nhận", message);
    return;
  }

  const method = prompt("Phương thức thanh toán", "Chuyển khoản");
  if (method === null) return;
  const note = prompt("Ghi chú thanh toán", "") || "";
  const month = Number.parseInt(monthEl.value, 10);
  const year = Number.parseInt(yearEl.value, 10);
  await postPayroll("/TeacherSalary/Api/MarkPaid", { employeeId, month, year, paymentMethod: method, note });
}

async function markPaidAll() {
  const lockedCount = salaryRows.filter((row) => row.status === "Locked").length;
  if (lockedCount === 0) {
    showPayrollWarning("Chưa thể ghi nhận", "Không có dòng lương đã chốt nào để ghi nhận thanh toán.");
    return;
  }

  const confirmed = await window.appConfirm(
    "Ghi nhận đã trả cho toàn bộ lương đã chốt trong tháng này? Phương thức thanh toán mặc định là Chuyển khoản."
  );
  if (!confirmed) return;

  const month = Number.parseInt(monthEl.value, 10);
  const year = Number.parseInt(yearEl.value, 10);
  await postPayroll("/TeacherSalary/Api/MarkPaidAll", { month, year });
}

async function postPayroll(url, body) {
  try {
    const response = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    const payload = await response.json();
    if (payload.success) {
      if (window.showToast) window.showToast("Thành công", payload.message || "Đã cập nhật.", "success");
      setStatus(payload.message || "Đã cập nhật.", "info");
      await loadSalarySummary();
    } else {
      setStatus(payload.message || "Không thể xử lý yêu cầu.", "error");
    }
  } catch (error) {
    console.error(error);
    setStatus("Lỗi kết nối máy chủ.", "error");
  }
}

function openSalaryDetail(employeeId) {
  const row = salaryRows.find((item) => item.employeeId === employeeId);
  if (!row || !detailPanel || !detailOverlay || !detailBody) return;

  const standardDays = row.standardWorkingDays || 0;
  const dailyRate = standardDays ? row.baseSalary / standardDays : 0;
  const gross = (row.workingDays || 0) * dailyRate;

  detailBody.innerHTML = `
    <div class="salary-breakdown">
      <h4>${escapeHtml(row.employeeName)}</h4>
      <div class="salary-line"><span>Lương cơ bản</span><strong>${fmtMoney(row.baseSalary)}</strong></div>
      <div class="salary-line"><span>Ngày công chuẩn</span><strong>${standardDays}</strong></div>
      <div class="salary-line"><span>Công thực tế đã duyệt</span><strong>${row.workingDays || 0}</strong></div>
      <div class="salary-line"><span>Đơn giá/ngày</span><strong>${fmtMoney(dailyRate)}</strong></div>
      <div class="salary-line"><span>Lương theo công</span><strong>${fmtMoney(gross)}</strong></div>
      <div class="salary-line"><span>Phạt</span><strong style="color:var(--danger)">- ${fmtMoney(row.penaltyAmount)}</strong></div>
      <div class="salary-line"><span>Phụ cấp dạy thay</span><strong style="color:var(--success)">+ ${fmtMoney(row.coverageBonusAmount)}</strong></div>
      <div class="salary-line"><span>Trạng thái</span>${renderStatusBadge(row.status)}</div>
      <div class="salary-line"><span>Phương thức thanh toán</span><strong>${escapeHtml(row.paymentMethod || "Chưa ghi nhận")}</strong></div>
      <div class="salary-total"><span>Thực nhận</span><strong>${fmtMoney(row.salaryAmount)}</strong></div>
    </div>
  `;

  detailOverlay.classList.add("active");
  detailPanel.classList.add("active");
}

function closeSalaryDetail() {
  detailOverlay?.classList.remove("active");
  detailPanel?.classList.remove("active");
}

document.addEventListener("DOMContentLoaded", () => {
  initFilters();
  document.getElementById("btnLoadSalary")?.addEventListener("click", loadSalarySummary);
  btnRecalculate?.addEventListener("click", recalculateSalary);
  btnLock?.addEventListener("click", lockSalary);
  btnMarkPaidAll?.addEventListener("click", markPaidAll);
  statusEl?.addEventListener("change", loadSalarySummary);
  document.getElementById("closeSalaryPanelBtn")?.addEventListener("click", closeSalaryDetail);
  detailOverlay?.addEventListener("click", closeSalaryDetail);
  loadSalarySummary();
});
