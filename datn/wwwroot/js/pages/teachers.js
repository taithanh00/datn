// ====== TEACHER MANAGEMENT PAGE ======
// Xử lý tất cả tương tác: tải dữ liệu, render table, quản lý form
const TEACHERS_PAGE_SIZE = 10;
const TEACHERS_TABLE_COLS = 16;

let showInactiveTeachers = false;
let teachersPaginationReady = false;

let currentTeacherId = null;
let isEditMode = false;
let allTeachersData = [];

// DOMContentLoaded - Khởi tạo trang khi HTML đã load xong
document.addEventListener("DOMContentLoaded", function () {
  initializeTeachersPage();
});

// ====== INITIALIZATION ======
async function initializeTeachersPage() {
  setupEventListeners();
  setupFilterListeners();
  await refreshData();
}

async function refreshData() {
  await loadTeachers();
  setupTeachersPaginationAndSearch();
}

function setupTeachersPaginationAndSearch() {
  const table = document.getElementById("teachersTable");
  if (!table) return;

  if (!teachersPaginationReady) {
    teachersPaginationReady = true;
    if (typeof initPagination === "function") {
      initPagination("teachersTable", TEACHERS_PAGE_SIZE);
      wrapTeachersPaginationRefresh(table);
    }
    if (typeof initTableSearch === "function") {
      initTableSearch("searchTeachers", "teachersTable");
    }
  }

  refreshTeachersTablePagination(true);
}

function wrapTeachersPaginationRefresh(table) {
  if (!table._refreshPagination || table._teachersPaginationWrapped) return;
  const originalRefresh = table._refreshPagination;
  table._refreshPagination = function () {
    originalRefresh();
    updateTeachersTableStt(table);
  };
  table._teachersPaginationWrapped = true;
}

function refreshTeachersTablePagination(resetPage) {
  const table = document.getElementById("teachersTable");
  if (!table) return;
  if (resetPage) table._currentPage = 1;
  if (typeof table._refreshPagination === "function") {
    table._refreshPagination();
  } else {
    updateTeachersTableStt(table);
  }
}

function updateTeachersTableStt(table) {
  const tbody = table.querySelector("tbody");
  if (!tbody) return;
  const rows = tbody.querySelectorAll("tr:not(.searching-hidden)");
  
  let sttIndex = 1;
  rows.forEach((row) => {
    if (row.classList.contains("app-loading-row") || row.querySelector("td[colspan]")) return;
    
    const sttCell = row.querySelector("td:first-child");
    if (sttCell) {
      sttCell.textContent = String(sttIndex);
      sttIndex++;
    }
  });
}

// ====== EVENT LISTENERS SETUP ======
function setupEventListeners() {
  // Create Button
  document
    .getElementById("btnCreateTeacher")
    .addEventListener("click", openCreatePanel);

  // Close Panel Button
  document
    .getElementById("closePanelBtn")
    .addEventListener("click", closePanel);

  // Modal Overlay Click
  document.getElementById("modalOverlay").addEventListener("click", closePanel);

  // Form Submit
  document
    .getElementById("editTeacherForm")
    .addEventListener("submit", handleFormSubmit);

  const landingPageToggle = document.getElementById("landingPageToggle");
  if (landingPageToggle) {
    landingPageToggle.addEventListener("click", () => {
      const section = document.getElementById("landingPageSection");
      setLandingPageSectionExpanded(section?.classList.contains("is-collapsed") === true);
    });
  }

  // Status Tabs
  document.querySelectorAll(".status-tab").forEach((tab) => {
    tab.addEventListener("click", function () {
      document
        .querySelectorAll(".status-tab")
        .forEach((t) => t.classList.remove("active"));
      this.classList.add("active");
      showInactiveTeachers = this.getAttribute("data-show-inactive") === "true";
      refreshData();
    });
  });

  // Password real-time validation
  const passwordInput = document.getElementById("password");
  passwordInput.addEventListener("input", function() {
    validatePasswordUI(this.value);
  });
}

function validatePasswordUI(val) {
  const isLength = val.length >= 9;
  const isUpper = /[A-Z]/.test(val);
  const isSpecial = /[!@#$%^&*()_+=\-\[\]{}|;:'",.<>?/\\ ]/.test(val);

  updateRequirementUI("req-length", isLength);
  updateRequirementUI("req-upper", isUpper);
  updateRequirementUI("req-special", isSpecial);

  return isLength && isUpper && isSpecial;
}

function updateRequirementUI(id, isValid) {
  const el = document.getElementById(id);
  if (!el) return;
  const icon = el.querySelector("i");
  if (isValid) {
    el.classList.add("valid");
    icon.className = "fa-solid fa-circle-check";
  } else {
    el.classList.remove("valid");
    icon.className = "fa-solid fa-circle-dot";
  }
}

function setLandingPageSectionExpanded(isExpanded) {
  const section = document.getElementById("landingPageSection");
  const toggle = document.getElementById("landingPageToggle");
  const toggleText = document.getElementById("landingPageToggleText");
  if (!section || !toggle) return;

  section.classList.toggle("is-collapsed", !isExpanded);
  toggle.setAttribute("aria-expanded", isExpanded.toString());
  if (toggleText) toggleText.textContent = isExpanded ? "Thu gọn" : "Mở rộng";
}

// Preview Avatar
function previewAvatar(input) {
  if (input.files && input.files[0]) {
    var reader = new FileReader();
    reader.onload = function (e) {
      document.getElementById("avatarPreview").src = e.target.result;
    };
    reader.readAsDataURL(input.files[0]);
  }
}

// ====== DATA LOADING ======
async function loadTeachers() {
  const tbody = document.getElementById("teachersTableBody");
  if (window.appLoading && tbody) {
    window.appLoading.setTable(tbody, TEACHERS_TABLE_COLS);
  }

  try {
    const t = new Date().getTime();
    const response = await fetch(
      `/Manager/Api/Teachers?showInactive=${showInactiveTeachers}&_t=${t}`,
    );
    const result = await response.json();

    if (!result.success) {
      showTableError("Lỗi tải dữ liệu giáo viên");
      return;
    }

    allTeachersData = result.data;
    populateFilterRoles();
    applyTeacherFilters();
  } catch (error) {
    console.error("Error loading teachers:", error);
    showTableError("Lỗi kết nối máy chủ");
  }
}

// ====== TABLE RENDERING ======
function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function renderTeachersTable(teachers) {
  const tbody = document.getElementById("teachersTableBody");
  tbody.innerHTML = "";

  if (teachers.length === 0) {
    if (window.appLoading) {
      tbody.innerHTML = window.appLoading.tableEmpty(TEACHERS_TABLE_COLS, "Kh\u00f4ng t\u00ecm th\u1ea5y d\u1eef li\u1ec7u");
      refreshTeachersTablePagination(true);
      return;
    }
    tbody.innerHTML =
      `<tr><td colspan="${TEACHERS_TABLE_COLS}" style="text-align: center; padding: 20px;">Không tìm thấy dữ liệu</td></tr>`;
    refreshTeachersTablePagination(true);
    return;
  }

  teachers.forEach((teacher, index) => {
    const statusBadge = teacher.isActive
      ? '<span class="badge badge-success">Hoạt động</span>'
      : '<span class="badge badge-danger">Đã khóa</span>';

    const row = document.createElement("tr");
    const actionBtn = `<button class="btn-table" onclick="openEditPanel(${teacher.id})">Sửa</button>`;
    const roleText = teacher.teacherType === "Lead" ? "Giáo viên phụ trách" : teacher.teacherType;
    const roleBadge = `<span class="badge" style="background:#e3f2fd; color:#1976d2;">${roleText}</span>`;
    const landingBadge = teacher.showOnLanding
      ? '<span class="badge badge-info">Có</span>'
      : '<span class="text-muted">Không</span>';

    row.innerHTML = `
    <td class="sticky-col first-col">${index + 1}</td>
    <td class="sticky-col second-col"><span class="teacher-code">#${teacher.id}</span></td>
    <td class="sticky-col third-col"><img src="${escapeHtml(teacher.avatarPath || "/images/lion_blue.png")}" class="avatar-sm" alt="avatar" onerror="this.src='/images/lion_blue.png'"></td>
    <td class="sticky-col fourth-col"><a href="/Manager/TeacherDetail/${teacher.id}" target="_blank" class="teacher-name-link" title="${escapeHtml(teacher.fullName)}"><strong>${escapeHtml(teacher.fullName)}</strong></a></td>
    <td>${statusBadge}</td>
    <td>${teacher.gender ? "Nam" : "Nữ"}</td>
    <td>${formatDateOnly(teacher.dateOfBirth)}</td>
    <td><span class="username-cell">${escapeHtml(teacher.username || "N/A")}</span></td>
    <td><div class="email-cell" title="${escapeHtml(teacher.email || "N/A")}">${escapeHtml(teacher.email || "N/A")}</div></td>
    <td>${escapeHtml(teacher.phone || "Chưa cập nhật")}</td>
    <td>${roleBadge}</td>
    <td>${teacher.baseSalary ? formatCurrency(teacher.baseSalary) : "N/A"}</td>
    <td>${landingBadge}</td>
    <td><span class="premium-date">${formatPremiumDate(teacher.createdAt)}</span></td>
    <td><span class="premium-date">${formatPremiumDate(teacher.updatedAt)}</span></td>
    <td class="text-end">${actionBtn}</td>
`;
    tbody.appendChild(row);
  });

  refreshTeachersTablePagination(false);
}

// ====== PANEL MANAGEMENT ======
function openCreatePanel() {
  isEditMode = false;
  currentTeacherId = null;
  document.getElementById("panelTitle").textContent = "Thêm giáo viên mới";
  document.getElementById("editTeacherForm").reset();
  document.getElementById("avatarPreview").src = "/images/lion_blue.png";
  document.getElementById("deleteTeacherBtn").style.display = "none";

  // Show account fields for creation
  document.getElementById("usernameGroup").style.display = "block";
  document.getElementById("username").disabled = false;
  document.getElementById("username").style.backgroundColor = "";
  document.getElementById("passwordGroup").style.display = "block";
  document.getElementById("password").required = true;
  document.getElementById("username").required = true;
  document.getElementById("email").required = true;

  document.getElementById("password").value = "";
  validatePasswordUI("");
  setLandingPageSectionExpanded(false);
  clearAlert();
  showPanel();
}

async function openEditPanel(teacherId) {
  isEditMode = true;
  currentTeacherId = teacherId;
  document.getElementById("panelTitle").textContent =
    "Chỉnh sửa thông tin giáo viên";

  document.getElementById("usernameGroup").style.display = "block";
  document.getElementById("username").disabled = true;
  document.getElementById("username").style.backgroundColor = "#e9ecef";
  document.getElementById("passwordGroup").style.display = "none";
  document.getElementById("password").required = false;
  document.getElementById("username").required = false;

  setLandingPageSectionExpanded(false);
  clearAlert();
  showPanel();

  try {
    const response = await fetch(`/Manager/Api/Teacher/${teacherId}`);
    const result = await response.json();

    if (!result.success) {
      showAlert("error", "Lỗi tải dữ liệu giáo viên");
      return;
    }

    const data = result.data;
    document.getElementById("teacherId").value = data.id;
    document.getElementById("firstName").value = data.firstName || "";
    document.getElementById("lastName").value = data.lastName || "";
    document.getElementById("email").value = data.email || "";
    document.getElementById("username").value = data.username || "";
    document.getElementById("phone").value = data.phone || "";
    document.getElementById("dateOfBirth").value = data.dateOfBirth || "";
    document.getElementById("teacherType").value = data.teacherType || "Lead";
    document.getElementById("baseSalary").value = data.baseSalary || "";
    document.getElementById("avatarPreview").src =
      data.avatarPath || "/images/lion_blue.png";
    
    // Set gender radio
    const genderVal = data.gender ? "true" : "false";
    const radio = document.querySelector(`input[name="Gender"][value="${genderVal}"]`);
    if (radio) radio.checked = true;
    
    // Landing Page Fields
    document.getElementById("specialty").value = data.specialty || "";
    document.getElementById("bio").value = data.bio || "";
    document.getElementById("qualifications").value = data.qualifications || "";
    document.getElementById("experience").value = data.experience || "";
    document.getElementById("philosophy").value = data.philosophy || "";
    document.getElementById("showOnLanding").checked = data.showOnLanding || false;

    // Đổi nút theo trạng thái isActive
    const deleteBtn = document.getElementById("deleteTeacherBtn");
    deleteBtn.style.display = "block";

    if (data.isActive) {
      deleteBtn.innerHTML =
        '<i class="fa-solid fa-lock"></i> Vô hiệu hóa giáo viên';
      deleteBtn.onclick = handleDeactivate;
    } else {
      deleteBtn.innerHTML =
        '<i class="fa-solid fa-lock-open"></i> Kích hoạt lại giáo viên';
      deleteBtn.onclick = handleReactivate;
    }
  } catch (error) {
    console.error("Error loading teacher:", error);
    showAlert("error", "Lỗi kết nối máy chủ");
  }
}

async function handleDeactivate() {
  if (!(await window.appConfirm("Bạn có chắc chắn muốn vô hiệu hóa giáo viên này?"))) return;

  try {
    const response = await fetch(`/Manager/Api/Teacher/${currentTeacherId}`, {
      method: "DELETE",
    });
    const result = await response.json();
    if (result.success) {
      showAlert("success", "Đã vô hiệu hóa giáo viên.");
      setTimeout(() => {
        closePanel();
        loadTeachers();
      }, 1500);
    } else {
      showAlert("error", result.message);
    }
  } catch (error) {
    showAlert("error", "Lỗi kết nối máy chủ");
  }
}

async function handleReactivate() {
  if (!(await window.appConfirm("Bạn có chắc chắn muốn kích hoạt lại giáo viên này? \nGiáo viên sẽ có thể đăng nhập lại vào hệ thống."))) return;

  try {
    const response = await fetch(
      `/Manager/Api/Teacher/Reactivate/${currentTeacherId}`,
      {
        method: "POST",
      },
    );
    const result = await response.json();
    if (result.success) {
      showAlert("success", "Đã kích hoạt lại giáo viên.");
      setTimeout(() => {
        closePanel();
        loadTeachers();
      }, 1500);
    } else {
      showAlert("error", result.message);
    }
  } catch (error) {
    showAlert("error", "Lỗi kết nối máy chủ");
  }
}

function showPanel() {
  document.getElementById("slidePanel").classList.add("active");
  document.getElementById("modalOverlay").classList.add("active");
}

function closePanel() {
  document.getElementById("slidePanel").classList.remove("active");
  document.getElementById("modalOverlay").classList.remove("active");
  currentTeacherId = null;
  isEditMode = false;
}

// ====== FORM HANDLING ======
async function handleFormSubmit(e) {
  e.preventDefault();

  const firstName = document.getElementById("firstName").value.trim();
  const lastName = document.getElementById("lastName").value.trim();
  if (!firstName || !lastName) {
    showAlert("error", "Vui lòng nhập đầy đủ họ đệm và tên");
    return;
  }

  const dateOfBirth = document.getElementById("dateOfBirth").value;
  if (window.appValidation && !window.appValidation.isAgeValid(dateOfBirth, 22, true)) {
    showAlert("error", "Giáo viên phải từ 22 tuổi trở lên.");
    return;
  }

  const formData = new FormData();
  formData.append("FirstName", firstName);
  formData.append("LastName", lastName);
  formData.append("Email", document.getElementById("email").value);
  formData.append("Phone", document.getElementById("phone").value);
  formData.append("DateOfBirth", dateOfBirth);
  formData.append("Gender", document.querySelector('input[name="Gender"]:checked')?.value || "true");
  formData.append("TeacherType", document.getElementById("teacherType").value);
  formData.append("BaseSalary", document.getElementById("baseSalary").value);

  // Landing Page Fields
  formData.append("Specialty", document.getElementById("specialty").value);
  formData.append("Bio", document.getElementById("bio").value);
  formData.append("Qualifications", document.getElementById("qualifications").value);
  formData.append("Experience", document.getElementById("experience").value);
  formData.append("Philosophy", document.getElementById("philosophy").value);
  formData.append("ShowOnLanding", document.getElementById("showOnLanding").checked);

  if (!isEditMode) {
    formData.append("Username", document.getElementById("username").value);
    formData.append("Password", document.getElementById("password").value);
  }

  const avatarFile = document.getElementById("avatarFile").files[0];
  if (avatarFile) {
    formData.append("Avatar", avatarFile);
  }

  try {
    let response, url, method;

    if (isEditMode) {
      url = `/Manager/Api/Teacher/${currentTeacherId}`;
      method = "PUT";
    } else {
      url = "/Manager/Api/Teacher";
      method = "POST";
    }

    response = await fetch(url, {
      method: method,
      body: formData,
    });

    const result = await response.json();

    if (!result.success) {
      showAlert("error", result.message || "Lỗi xử lý yêu cầu");
      return;
    }

    showAlert("success", result.message);
    setTimeout(() => {
      closePanel();
      loadTeachers();
    }, 1500);
  } catch (error) {
    console.error("Error submitting form:", error);
    showAlert("error", "Lỗi kết nối máy chủ");
  }
}


// ====== ALERT MANAGEMENT ======
function showAlert(type, message) {
  const alertDiv = document.getElementById("editFormAlert");
  if (alertDiv) {
    alertDiv.style.display = "none";
    alertDiv.textContent = "";
  }
  if (window.showToast) {
    window.showToast(type === "success" ? "Thành công" : "Có lỗi", message, type === "success" ? "success" : "error");
  }
}

function clearAlert() {
  const alertDiv = document.getElementById("editFormAlert");
  alertDiv.style.display = "none";
  alertDiv.textContent = "";
}

// ====== ERROR HANDLING ======
function showTableError(message) {
  const tbody = document.getElementById("teachersTableBody");
  if (window.appLoading) {
    tbody.innerHTML = window.appLoading.tableError(TEACHERS_TABLE_COLS, message);
    refreshTeachersTablePagination(true);
    return;
  }
  tbody.innerHTML = `<tr><td colspan="${TEACHERS_TABLE_COLS}" style="text-align: center; padding: 20px; color: #d32f2f;">${message}</td></tr>`;
  refreshTeachersTablePagination(true);
}

// ====== UTILITY FUNCTIONS ======
function formatCurrency(value) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value);
}

function formatPremiumDate(dateString) {
  if (!dateString) return "N/A";
  if (String(dateString).startsWith("0001-01-01")) return "N/A";
  const date = new Date(dateString);
  if (Number.isNaN(date.getTime())) return "N/A";
  const day = date.getDate().toString().padStart(2, "0");
  const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
  const month = months[date.getMonth()];
  const year = date.getFullYear();
  let hours = date.getHours();
  const ampm = hours >= 12 ? "PM" : "AM";
  hours = hours % 12 || 12;
  const minutes = date.getMinutes().toString().padStart(2, "0");
  return `${day} ${month} ${year} / ${hours}:${minutes}${ampm}`;
}

function formatDateOnly(value) {
  if (!value) return "Chưa cập nhật";
  const date = new Date(`${value}T00:00:00`);
  if (Number.isNaN(date.getTime())) return "Chưa cập nhật";
  const day = date.getDate().toString().padStart(2, "0");
  const month = (date.getMonth() + 1).toString().padStart(2, "0");
  return `${day}/${month}/${date.getFullYear()}`;
}

function setupFilterListeners() {
  const btnToggle = document.getElementById('btnToggleFilter');
  if (btnToggle) {
    btnToggle.addEventListener('click', function() {
      this.classList.toggle('active');
      document.getElementById('filterPanel').classList.toggle('active');
    });
  }
  
  const btnApply = document.getElementById('btnApplyFilter');
  if (btnApply) {
    btnApply.addEventListener('click', () => {
      applyTeacherFilters();
      refreshTeachersTablePagination(true);
    });
  }
  
  const btnReset = document.getElementById('btnResetFilter');
  if (btnReset) {
    btnReset.addEventListener('click', () => {
      document.getElementById('filterTeacherType').value = '';
      document.getElementById('filterGender').value = '';
      applyTeacherFilters();
      refreshTeachersTablePagination(true);
    });
  }
}

function populateFilterRoles() {
  const select = document.getElementById('filterTeacherType');
  if (!select) return;
  const currentVal = select.value;
  select.innerHTML = '<option value="">-- Tất cả --</option>';
  const roles = [...new Set(allTeachersData.map(t => t.teacherType).filter(Boolean))];
  roles.sort().forEach(role => {
    const option = document.createElement('option');
    option.value = role;
    option.textContent = role === 'Lead'
      ? 'Giáo viên phụ trách'
      : role === 'Subject'
        ? 'Giáo viên bộ môn'
        : role;
    select.appendChild(option);
  });
  select.value = currentVal;
}

function applyTeacherFilters() {
  const teacherTypeEl = document.getElementById('filterTeacherType');
  const genderEl = document.getElementById('filterGender');
  if (!teacherTypeEl || !genderEl) return;

  const teacherType = teacherTypeEl.value;
  const gender = genderEl.value;

  let filtered = allTeachersData;

  if (teacherType) {
    filtered = filtered.filter(t => t.teacherType === teacherType);
  }
  if (gender !== '') {
    const genderBool = gender === 'true';
    filtered = filtered.filter(t => t.gender === genderBool);
  }

  renderTeachersTable(filtered);
}

