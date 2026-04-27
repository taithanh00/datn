// ====== TEACHER MANAGEMENT PAGE ======
// Xử lý tất cả tương tác: tải dữ liệu, render table, quản lý form

let currentTeacherId = null;
let isEditMode = false;

// DOMContentLoaded - Khởi tạo trang khi HTML đã load xong
document.addEventListener("DOMContentLoaded", function () {
  initializeTeachersPage();
});

// ====== INITIALIZATION ======
async function initializeTeachersPage() {
  setupEventListeners();
  await loadTeachers();

  // Khởi tạo pagination và search sau khi dữ liệu đã được load và render
  if (typeof initPagination === "function") {
    initPagination("teachersTable", 15);
  }
  if (typeof initTableSearch === "function") {
    initTableSearch("searchTeachers", "teachersTable");
  }
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

  // Delete Button
  document
    .getElementById("deleteTeacherBtn")
    .addEventListener("click", handleDelete);
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
    const response = await fetch("/Manager/Api/Teachers");
    const result = await response.json();

    if (!result.success) {
      showTableError("Lỗi tải dữ liệu giáo viên");
      return;
    }

    renderTeachersTable(result.data);
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
      '<tr><td colspan="6" style="text-align: center; padding: 20px;">Không có giáo viên nào</td></tr>';
    return;
  }

  teachers.forEach((teacher, index) => {
    const statusBadge = teacher.isActive
      ? '<span class="badge bg-success" style="font-size: 11px; padding: 4px 8px; border-radius: 4px;">Đang hoạt động</span>'
      : '<span class="badge bg-danger" style="font-size: 11px; padding: 4px 8px; border-radius: 4px;">Đã vô hiệu hóa</span>';

    const row = document.createElement("tr");
    const actionBtn = teacher.isActive
      ? `<button class="btn-action btn-action-edit" onclick="openEditPanel(${teacher.id})">
           <i class="fa-solid fa-pen-to-square"></i> Chỉnh sửa
       </button>`
      : `<button class="btn-action btn-action-danger" onclick="openEditPanel(${teacher.id})">
           <i class="fa-solid fa-lock-open"></i> Đã vô hiệu hóa
       </button>`;
    row.innerHTML = `
    <td>${index + 1}</td>
    <td> <img src="${teacher.avatarPath}" class="rounded-circle" style="width:36px; height:36px; object-fit:cover; border-radius:50%;"> </td>
    <td> <a href="/Manager/TeacherDetail/${teacher.id}" target="_blank" class="teacher-name-link" title="${teacher.fullName}"><strong>${teacher.fullName}</strong></a> </td>
    <td>${teacher.phone || "Chưa cập nhật"}</td>
    <td>${teacher.position || "Chưa cập nhật"}</td>
    <td>${statusBadge}</td>
    <td class="text-end">${actionBtn}</td>
`;
    tbody.appendChild(row);
  });
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
  document.getElementById("passwordGroup").style.display = "block";
  document.getElementById("username").required = true;
  document.getElementById("email").required = true;

  clearAlert();
  showPanel();
}

async function openEditPanel(teacherId) {
  isEditMode = true;
  currentTeacherId = teacherId;
  document.getElementById("panelTitle").textContent =
    "Chỉnh sửa thông tin giáo viên";

  document.getElementById("usernameGroup").style.display = "none";
  document.getElementById("passwordGroup").style.display = "none";
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
    document.getElementById("fullName").value = data.fullName || "";
    document.getElementById("email").value = data.email || "";
    document.getElementById("phone").value = data.phone || "";
    document.getElementById("position").value = data.position || "";
    document.getElementById("baseSalary").value = data.baseSalary || "";
    document.getElementById("avatarPreview").src =
      data.avatarPath || "/images/lion_blue.png";

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
  if (!confirm("Bạn có chắc chắn muốn kích hoạt lại giáo viên này?")) return;

  try {
    const response = await fetch(
      `/Manager/Api/Teacher/${currentTeacherId}/Reactivate`,
      {
        method: "PUT",
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

  const fullName = document.getElementById("fullName").value.trim();
  if (!fullName) {
    showAlert("error", "Vui lòng nhập họ và tên");
    return;
  }

  const formData = new FormData();
  formData.append("FullName", fullName);
  formData.append("Email", document.getElementById("email").value);
  formData.append("Phone", document.getElementById("phone").value);
  formData.append("Position", document.getElementById("position").value);
  formData.append("BaseSalary", document.getElementById("baseSalary").value);

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

// ====== DELETE (DEACTIVATE) HANDLING ======
async function handleDelete() {
  if (
    !confirm(
      "Bạn có chắc chắn muốn vô hiệu hóa giáo viên này? \nGiáo viên sẽ bị đăng xuất và không thể truy cập hệ thống nữa.",
    )
  ) {
    return;
  }

  try {
    const response = await fetch(`/Manager/Api/Teacher/${currentTeacherId}`, {
      method: "DELETE",
    });

    const result = await response.json();

    if (!result.success) {
      showAlert("error", result.message || "Lỗi vô hiệu hóa giáo viên");
      return;
    }

    showAlert("success", "Vô hiệu hóa giáo viên thành công");
    setTimeout(() => {
      closePanel();
      loadTeachers();
    }, 1500);
  } catch (error) {
    console.error("Error deactivating teacher:", error);
    showAlert("error", "Lỗi kết nối máy chủ");
  }
}

// ====== ALERT MANAGEMENT ======
function showAlert(type, message) {
  const alertDiv = document.getElementById("editFormAlert");
  alertDiv.className = `form-alert ${type}`;
  alertDiv.textContent = message;
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
  tbody.innerHTML = `<tr><td colspan="6" style="text-align: center; padding: 20px; color: #d32f2f;">${message}</td></tr>`;
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
