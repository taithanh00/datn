// ====== STUDENT MANAGEMENT PAGE ======
// Xử lý tất cả tương tác: tải dữ liệu, render table, quản lý form

let currentStudentId = null;
let isEditMode = false;

// DOMContentLoaded - Khởi tạo trang khi HTML đã load xong
document.addEventListener("DOMContentLoaded", function () {
  initializeStudentsPage();
});

// ====== INITIALIZATION ======
async function initializeStudentsPage() {
  setupEventListeners();
  await loadClasses();
  await refreshData();
}

async function refreshData() {
  await loadStudents();

  // Khởi tạo pagination và search sau khi dữ liệu đã được load và render
  if (typeof initPagination === "function") {
    initPagination("studentsTable", 7);
  }
  if (typeof initTableSearch === "function") {
    initTableSearch("searchStudents", "studentsTable");
  }
}

// ====== EVENT LISTENERS SETUP ======
function setupEventListeners() {
  // Create Button
  document
    .getElementById("btnCreateStudent")
    .addEventListener("click", openCreatePanel);

  // Close Panel Button
  document
    .getElementById("closePanelBtn")
    .addEventListener("click", closePanel);

  // Modal Overlay Click
  document.getElementById("modalOverlay").addEventListener("click", closePanel);

  // Form Submit
  document
    .getElementById("editStudentForm")
    .addEventListener("submit", handleFormSubmit);

  // Delete Button
  document
    .getElementById("deleteStudentBtn")
    .addEventListener("click", handleDelete);

  // Duplicate Modal Buttons
  document
    .getElementById("forceCreateBtn")
    .addEventListener("click", () => submitForm(true));
    
  document.getElementById("viewExistingBtn").addEventListener("click", () => {
    const id = document.getElementById("duplicateModal").dataset.existingId;
    window.open(`/Manager/StudentDetail/${id}`, "_blank");
    closeDuplicateModal();
  });

  document
    .getElementById("cancelDuplicateBtn")
    .addEventListener("click", closeDuplicateModal);
}

// Preview Avatar
// Helper to format date: 24 Apr 2026 / 10:00AM
function formatPremiumDate(dateString) {
  if (!dateString) return "N/A";
  const date = new Date(dateString);
  const day = date.getDate().toString().padStart(2, "0");
  const months = [
    "Jan", "Feb", "Mar", "Apr", "May", "Jun",
    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
  ];
  const month = months[date.getMonth()];
  const year = date.getFullYear();

  let hours = date.getHours();
  const ampm = hours >= 12 ? "PM" : "AM";
  hours = hours % 12;
  hours = hours ? hours : 12;
  const minutes = date.getMinutes().toString().padStart(2, "0");

  return `${day} ${month} ${year} / ${hours}:${minutes}${ampm}`;
}

function previewAvatar(input) {
  if (input.files && input.files[0]) {
    const reader = new FileReader();
    reader.onload = function (e) {
      document.getElementById("avatarPreview").src = e.target.result;
    };
    reader.readAsDataURL(input.files[0]);
  }
}

// ====== DATA LOADING ======
async function loadStudents() {
  try {
    const response = await fetch("/Manager/Api/Students");
    const result = await response.json();

    if (!result.success) {
      showTableError("Lỗi tải dữ liệu học sinh");
      return;
    }

    renderStudentsTable(result.data);
  } catch (error) {
    console.error("Error loading students:", error);
    showTableError("Lỗi kết nối máy chủ");
  }
}

async function loadClasses() {
  try {
    const response = await fetch("/Manager/Api/Classes");
    const result = await response.json();

    if (!result.success) return;

    const classSelect = document.getElementById("classId");
    classSelect.innerHTML = '<option value="">-- Chọn lớp --</option>';

    result.data.forEach((cls) => {
      const option = document.createElement("option");
      option.value = cls.id;
      option.textContent = cls.name;
      classSelect.appendChild(option);
    });
  } catch (error) {
    console.error("Error loading classes:", error);
  }
}

// ====== TABLE RENDERING ======
function renderStudentsTable(students) {
  const tbody = document.getElementById("studentsTableBody");
  tbody.innerHTML = "";

  if (students.length === 0) {
    tbody.innerHTML =
      '<tr><td colspan="14" class="text-center">Không tìm thấy dữ liệu</td></tr>';
    return;
  }

  students.forEach((s, index) => {
    const statusClass = s.status === 0 ? "badge-active" : "badge-inactive";
    const noInfo =
      '<span class="text-muted" style="font-size:0.8rem;font-style:italic;">Chưa có</span>';

    const row = document.createElement("tr");
    row.innerHTML = `
    <td class="sticky-col first-col">${index + 1}</td>
    <td class="sticky-col second-col"><span class="student-code">${s.studentCode}</span></td>
    <td class="sticky-col third-col">
        <img src="${s.avatarPath}" class="student-avatar" alt="avatar" onerror="this.src='/images/lion_orange.png'">
    </td>
    <td class="sticky-col fourth-col">
        <a href="/Manager/StudentDetail/${s.id}" target="_blank" class="student-name-link" title="${s.fullName}">${s.fullName}</a>
    </td>
    <td><span class="badge ${statusClass}">${s.statusText}</span></td>
    <td><span class="badge ${s.gender === "Nam" ? "badge-primary" : "badge-secondary"}">${s.gender}</span></td>
    <td>${s.dateOfBirth}</td>
    <td><div class="address-cell" title="${s.address}">${s.address}</div></td>
    <td><span class="badge badge-outline">${s.className}</span></td>
    <td>${s.fatherName || noInfo}</td>
    <td>${s.motherName || noInfo}</td>
    <td>${s.enrollDate}</td>
    <td><span class="premium-date">${formatPremiumDate(s.createdAt)}</span></td>
    <td class="text-end">
      <div class="table-actions">
          <button class="btn-action btn-action-edit" onclick="openEditPanel(${s.id})">
              <i class="fa-solid fa-pen-to-square"></i> Chỉnh sửa
          </button>
      </div>
    </td>
`;
    tbody.appendChild(row);
  });
}

function showTableError(message) {
  const tbody = document.getElementById("studentsTableBody");
  tbody.innerHTML = `<tr><td colspan="14" style="text-align: center; padding: 20px; color: red;">${message}</td></tr>`;
}

// ====== PANEL MANAGEMENT ======
function openCreatePanel() {
  isEditMode = false;
  currentStudentId = null;

  // Reset form
  document.getElementById("editStudentForm").reset();
  document.getElementById("studentId").value = "";
  document.getElementById("avatarPreview").src = "/images/lion_orange.png";
  document.getElementById("panelTitle").textContent = "Thêm học sinh mới";
  document.getElementById("deleteStudentBtn").style.display = "none";

  // Hide alert
  hideFormAlert();

  // Open panel
  openPanel();
}

async function openEditPanel(studentId) {
  isEditMode = true;
  currentStudentId = studentId;

  try {
    const response = await fetch(`/Manager/Api/Student/${studentId}`);
    const result = await response.json();

    if (!result.success) {
      showFormAlert("Lỗi tải dữ liệu học sinh", "error");
      return;
    }

    const student = result.data;

    // Fill form
    document.getElementById("studentId").value = student.id;
    document.getElementById("firstName").value = student.firstName;
    document.getElementById("lastName").value = student.lastName;
    document.getElementById("gender").value =
      student.gender !== null ? student.gender : "";
    document.getElementById("dateOfBirth").value = student.dateOfBirth;
    document.getElementById("address").value = student.address;
    document.getElementById("classId").value = student.classId;
    document.getElementById("enrollDate").value = student.enrollDate;
    document.getElementById("status").value = student.status;

    // Avatar preview
    document.getElementById("avatarPreview").src =
      student.avatarPath || "/images/lion_orange.png";

    // Update title and show delete button
    document.getElementById("panelTitle").textContent =
      `Sửa thông tin - ${student.firstName} ${student.lastName}`;
    document.getElementById("deleteStudentBtn").style.display = "block";

    // Hide alert
    hideFormAlert();

    // Open panel
    openPanel();
  } catch (error) {
    console.error("Error loading student:", error);
    showFormAlert("Lỗi kết nối máy chủ", "error");
  }
}

function openPanel() {
  document.getElementById("modalOverlay").classList.add("active");
  document.getElementById("slidePanel").classList.add("active");
  document.body.style.overflow = "hidden";
}

function closePanel() {
  document.getElementById("modalOverlay").classList.remove("active");
  document.getElementById("slidePanel").classList.remove("active");
  document.body.style.overflow = "auto";
  isEditMode = false;
  currentStudentId = null;
}

// ====== FORM HANDLING ======
async function handleFormSubmit(e) {
  e.preventDefault();
  submitForm(false);
}

async function submitForm(forceCreate = false) {
  const firstName = document.getElementById("firstName").value.trim();
  const lastName = document.getElementById("lastName").value.trim();

  if (!firstName || !lastName) {
    showFormAlert("Vui lòng nhập đầy đủ tên học sinh", "error");
    return;
  }

  const formData = new FormData();
  formData.append("firstName", firstName);
  formData.append("lastName", lastName);
  formData.append("gender", document.getElementById("gender").value);
  formData.append("dateOfBirth", document.getElementById("dateOfBirth").value);
  formData.append("address", document.getElementById("address").value);
  formData.append("status", document.getElementById("status").value);
  formData.append(
    "classId",
    parseInt(document.getElementById("classId").value) || 0,
  );
  formData.append("enrollDate", document.getElementById("enrollDate").value);
  formData.append("forceCreate", forceCreate);

  const avatarFile = document.getElementById("avatarFile").files[0];
  if (avatarFile) {
    formData.append("avatar", avatarFile);
  }

  try {
    const url = isEditMode
      ? `/Manager/Api/Student/${currentStudentId}`
      : "/Manager/Api/Student";

    const method = isEditMode ? "PUT" : "POST";

    const response = await fetch(url, {
      method: method,
      body: formData,
    });

    if (response.status === 409) {
      const result = await response.json();
      showDuplicateModal(result.message, result.existingStudentId);
      return;
    }

    const result = await response.json();

    if (result.success) {
      showFormAlert(result.message, "success");
      setTimeout(() => {
        closePanel();
        refreshData();
      }, 1500);
    } else {
      showFormAlert(result.message || "Lỗi không xác định", "error");
    }
  } catch (error) {
    console.error("Error:", error);
    showFormAlert("Lỗi kết nối máy chủ", "error");
  }
}

async function handleDelete(id) {
  const studentId = id || currentStudentId;
  if (!studentId) return;

  if (
    !confirm(
      'Bạn có chắc chắn muốn chuyển trạng thái học sinh này sang "Đã thôi học"?',
    )
  ) {
    return;
  }

  try {
    const response = await fetch(`/Manager/Api/Student/${studentId}`, {
      method: "DELETE",
    });

    const result = await response.json();

    if (result.success) {
      showFormAlert(result.message, "success");
      setTimeout(() => {
        closePanel();
        refreshData();
      }, 1500);
    } else {
      showFormAlert(result.message || "Lỗi xóa học sinh", "error");
    }
  } catch (error) {
    console.error("Error:", error);
    showFormAlert("Lỗi kết nối máy chủ", "error");
  }
}

// ====== ALERT MANAGEMENT ======
function showFormAlert(message, type) {
  const alertContainer = document.getElementById("editFormAlert");
  const className = type === "success" ? "alert-success" : "alert-error";

  alertContainer.className = `form-alert ${className}`;
  alertContainer.textContent = message;
  alertContainer.style.display = "block";
}

function hideFormAlert() {
  const alertContainer = document.getElementById("editFormAlert");
  alertContainer.style.display = "none";
}

// ====== DUPLICATE MODAL ======
function showDuplicateModal(message, existingId) {
  document.getElementById("duplicateMessage").textContent = message;
  document.getElementById("duplicateModal").dataset.existingId = existingId;
  document.getElementById("duplicateModalOverlay").classList.add("active");
  document.getElementById("duplicateModal").classList.add("active");
}

function closeDuplicateModal() {
  document.getElementById("duplicateModalOverlay").classList.remove("active");
  document.getElementById("duplicateModal").classList.remove("active");
}