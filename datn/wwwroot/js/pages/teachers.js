// ====== TEACHER MANAGEMENT PAGE ======
// Xử lý tất cả tương tác: tải dữ liệu, render table, quản lý form
const TEACHERS_PAGE_SIZE = 10;
const TEACHERS_TABLE_COLS = 7;

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
  rows.forEach((row, index) => {
    const sttCell = row.querySelector("td:first-child");
    if (sttCell) sttCell.textContent = String(index + 1);
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
    populateFilterPositions();
    applyTeacherFilters();
  } catch (error) {
    console.error("Error loading teachers:", error);
    showTableError("Lỗi kết nối máy chủ");
  }
}

// ====== TABLE RENDERING ======
function renderTeachersTable(teachers) {
  const tbody = document.getElementById("teachersTableBody");
  tbody.innerHTML = "";

  if (teachers.length === 0) {
    tbody.innerHTML =
      `<tr><td colspan="${TEACHERS_TABLE_COLS}" style="text-align: center; padding: 20px;">Không có giáo viên nào</td></tr>`;
    refreshTeachersTablePagination(true);
    return;
  }

  teachers.forEach((teacher, index) => {
    const statusBadge = teacher.isActive
      ? '<span class="badge badge-success">Hoạt động</span>'
      : '<span class="badge badge-danger">Đã khóa</span>';

    const row = document.createElement("tr");
    const actionBtn = `<button class="btn-table" onclick="openEditPanel(${teacher.id})">Sửa</button>`;
    const roleBadge = teacher.teacherType === 0 || teacher.teacherType === 'Lead' 
      ? '<span class="badge" style="background:#e3f2fd; color:#1976d2;">GV Chủ nhiệm</span>' 
      : '<span class="badge" style="background:#f3e5f5; color:#7b1fa2;">GV Bộ môn</span>';

    row.innerHTML = `
    <td>${index + 1}</td>
    <td> <img src="${teacher.avatarPath}" class="rounded-circle" style="width:36px; height:36px; object-fit:cover; border-radius:50%;"> </td>
    <td> <a href="/Manager/TeacherDetail/${teacher.id}" target="_blank" class="teacher-name-link" title="${teacher.fullName}"><strong>${teacher.fullName}</strong></a> </td>
    <td>${teacher.phone || "Chưa cập nhật"}</td>
    <td>${roleBadge}</td>
    <td>${statusBadge}</td>
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
    document.getElementById("position").value = data.position || "";
    document.getElementById("teacherType").value = (data.teacherType === 0 || data.teacherType === 'Lead') ? "Lead" : "Subject";
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
  if (!confirm("Bạn có chắc chắn muốn vô hiệu hóa giáo viên này?")) return;

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
  if (!confirm("Bạn có chắc chắn muốn kích hoạt lại giáo viên này? \nGiáo viên sẽ có thể đăng nhập lại vào hệ thống.")) return;

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

  const formData = new FormData();
  formData.append("FirstName", firstName);
  formData.append("LastName", lastName);
  formData.append("Email", document.getElementById("email").value);
  formData.append("Phone", document.getElementById("phone").value);
  formData.append("Position", document.getElementById("position").value);
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
  const icon = type === "success" ? "fa-circle-check" : "fa-circle-exclamation";
  
  alertDiv.className = `form-alert ${type}`;
  alertDiv.innerHTML = `<i class="fa-solid ${icon}" style="margin-right: 8px;"></i>${message}`;
  alertDiv.style.display = "block";
}

function clearAlert() {
  const alertDiv = document.getElementById("editFormAlert");
  alertDiv.style.display = "none";
  alertDiv.textContent = "";
}

// ====== ERROR HANDLING ======
function showTableError(message) {
  const tbody = document.getElementById("teachersTableBody");
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
      document.getElementById('filterPosition').value = '';
      document.getElementById('filterGender').value = '';
      applyTeacherFilters();
      refreshTeachersTablePagination(true);
    });
  }
}

function populateFilterPositions() {
  const select = document.getElementById('filterPosition');
  if (!select) return;
  const currentVal = select.value;
  select.innerHTML = '<option value="">-- Tất cả --</option>';
  const positions = [...new Set(allTeachersData.map(t => t.position).filter(Boolean))];
  positions.sort().forEach(pos => {
    select.innerHTML += `<option value="${pos}">${pos}</option>`;
  });
  select.value = currentVal;
}

function applyTeacherFilters() {
  const positionEl = document.getElementById('filterPosition');
  const genderEl = document.getElementById('filterGender');
  if (!positionEl || !genderEl) return;

  const position = positionEl.value;
  const gender = genderEl.value;

  let filtered = allTeachersData;

  if (position) {
    filtered = filtered.filter(t => t.position === position);
  }
  if (gender !== '') {
    const genderBool = gender === 'true';
    filtered = filtered.filter(t => t.gender === genderBool);
  }

  renderTeachersTable(filtered);
}
