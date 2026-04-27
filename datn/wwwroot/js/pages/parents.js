// ====== PARENT MANAGEMENT PAGE (Synced with Students) ======

let isEditMode = false;
let currentParentId = null;
let selectedStudentForLink = null;

// DOMContentLoaded - Khởi tạo trang khi HTML đã load xong
document.addEventListener("DOMContentLoaded", function () {
  initializeParentsPage();
});

// ====== INITIALIZATION ======
async function initializeParentsPage() {
  setupEventListeners();
  await refreshData();
}

async function refreshData() {
  await loadParents();

  // Khởi tạo pagination và search sau khi dữ liệu đã được load (sync với students.js)
  if (typeof initPagination === "function") {
    initPagination("parentsTable", 7);
  }
  if (typeof initTableSearch === "function") {
    initTableSearch("searchParents", "parentsTable");
  }
}

// ====== EVENT LISTENERS SETUP ======
function setupEventListeners() {
  // Create Button
  document
    .getElementById("btnCreateParent")
    .addEventListener("click", openCreatePanel);

  // Close Panel Button
  document
    .getElementById("closePanelBtn")
    .addEventListener("click", closePanel);

  // Modal Overlay Click
  document.getElementById("modalOverlay").addEventListener("click", closePanel);

  // Student search for linking
  let timeout = null;
  const studentSearch = document.getElementById("studentSearchInput");
  studentSearch.addEventListener("input", function () {
    clearTimeout(timeout);
    const q = this.value;
    if (q.length < 2) {
      document.getElementById("studentSearchResults").innerHTML = "";
      return;
    }
    timeout = setTimeout(() => searchStudentsForLink(q), 300);
  });

  // Link modal overlay click
  document
    .getElementById("linkModalOverlay")
    .addEventListener("click", closeLinkStudentModal);
}

// ====== DATA LOADING ======
async function loadParents() {
  try {
    const response = await fetch("/Manager/Api/Parents");
    const result = await response.json();

    if (!result.success) {
      showTableError("Lỗi tải dữ liệu phụ huynh");
      return;
    }

    renderParentsTable(result.data);
  } catch (error) {
    console.error("Error loading parents:", error);
    showTableError("Lỗi kết nối máy chủ");
  }
}

// ====== TABLE RENDERING ======
function renderParentsTable(parents) {
  const tbody = document.getElementById("parentsTableBody");
  tbody.innerHTML = "";

  if (parents.length === 0) {
    tbody.innerHTML =
      '<tr><td colspan="9" class="text-center">Không tìm thấy dữ liệu</td></tr>';
    return;
  }

  parents.forEach((p, index) => {
    const row = document.createElement("tr");
    row.innerHTML = `
            <td class="sticky-col first-col">${index + 1}</td>
            <td class="sticky-col second-col">
                <img src="${p.avatarPath}" class="avatar-sm" alt="avatar" onerror="this.src='/images/lion_orange.png'">
            </td>
            <td class="sticky-col third-col">
                <a href="/Manager/ParentDetail/${p.id}" target="_blank" class="parent-name-link" title="${p.fullName}">${p.fullName}</a>
            </td>
            <td>${p.email || "N/A"}</td>
            <td><i class="fa-solid fa-phone me-2 text-muted" style="font-size:0.75rem;"></i>${p.phone || "N/A"}</td>
            <td><div class="address-cell" title="${p.address}">${p.address || "N/A"}</div></td>
            <td>
                ${
                  p.children && p.children.length > 0
                    ? p.children
                        .map(
                          (c) =>
                            `<span class="child-badge" title="${c.relationship}">${c.fullName}</span>`,
                        )
                        .join("")
                    : '<span class="text-muted" style="font-size:0.8rem; font-style:italic;">Chưa gán con</span>'
                }
            </td>
            <td><span class="premium-date">${formatPremiumDate(p.createdAt)}</span></td>
            <td class="text-end">
              <div class="table-actions">
                  <button class="btn-action btn-action-edit" onclick="openEditPanel(${p.id})">
                      <i class="fa-solid fa-pen-to-square"></i> Chỉnh sửa
                  </button>
              </div>
            </td>

        `;
    tbody.appendChild(row);
  });
}

function showTableError(message) {
  const tbody = document.getElementById("parentsTableBody");
  tbody.innerHTML = `<tr><td colspan="9" style="text-align: center; padding: 20px; color: red;">${message}</td></tr>`;
}

// ====== DATE FORMATTING (sync với students.js) ======
function formatPremiumDate(dateString) {
  if (!dateString) return "N/A";
  const date = new Date(dateString);
  const day = date.getDate().toString().padStart(2, "0");
  const months = [
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
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

// ====== PANEL MANAGEMENT ======
function openCreatePanel() {
  isEditMode = false;
  currentParentId = null;

  document.getElementById("parentForm").reset();
  document.getElementById("parentId").value = "";
  document.getElementById("avatarPreview").src = "/images/lion_orange.png";
  document.getElementById("panelTitle").textContent = "Thêm phụ huynh mới";
  document.getElementById("passwordFieldGroup").style.display = "none"; // Hide on create
  document.getElementById("linkedStudentsSection").style.display = "none";
  document.getElementById("deleteParentBtn").style.display = "none";

  hideFormAlert();
  openPanel();
}

async function openEditPanel(id) {
  isEditMode = true;
  currentParentId = id;
  hideFormAlert();

  try {
    const response = await fetch(`/Manager/Api/Parent/${id}`);
    const result = await response.json();

    if (!result.success) {
      showFormAlert("Lỗi tải dữ liệu phụ huynh", "error");
      return;
    }

    const p = result.data;
    document.getElementById("parentId").value = p.id;
    document.getElementById("username").value = p.username;
    document.getElementById("email").value = p.email;
    document.getElementById("lastName").value = p.lastName;
    document.getElementById("firstName").value = p.firstName;
    document.getElementById("phone").value = p.phone || "";
    document.getElementById("address").value = p.address || "";
    document.getElementById("avatarPreview").src =
      p.avatarPath || "/images/lion_orange.png";

    document.getElementById("passwordFieldGroup").style.display = "block"; // Show on edit
    document.getElementById("password").value = "******";
    document.getElementById("passwordNote").textContent =
      "Để trống nếu không thay đổi mật khẩu";

    document.getElementById("panelTitle").textContent =
      `Sửa thông tin - ${p.firstName} ${p.lastName}`;
    document.getElementById("deleteParentBtn").style.display = "block";

    // Show linked children
    document.getElementById("linkedStudentsSection").style.display = "block";
    renderLinkedStudents(p.children);

    openPanel();
  } catch (error) {
    console.error("Error loading parent:", error);
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
  currentParentId = null;
}

// ====== AVATAR PREVIEW ======
function previewImage(input) {
  if (input.files && input.files[0]) {
    const reader = new FileReader();
    reader.onload = (e) =>
      (document.getElementById("avatarPreview").src = e.target.result);
    reader.readAsDataURL(input.files[0]);
  }
}

// ====== FORM HANDLING ======
async function saveParent() {
  const form = document.getElementById("parentForm");
  if (!form.checkValidity()) {
    form.reportValidity();
    return;
  }

  const formData = new FormData(form);
  const id = document.getElementById("parentId").value;
  const url = isEditMode ? `/Manager/Api/Parent/${id}` : "/Manager/Api/Parent";
  const method = isEditMode ? "PUT" : "POST";

  const saveBtn = document.getElementById("saveParentBtn");
  saveBtn.disabled = true;
  saveBtn.innerHTML =
    '<i class="fa-solid fa-circle-notch fa-spin"></i> Đang lưu...';

  try {
    const response = await fetch(url, { method, body: formData });
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
  } finally {
    saveBtn.disabled = false;
    saveBtn.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Lưu thông tin';
  }
}

async function deleteCurrentParent() {
  if (!currentParentId) return;
  const name = document
    .getElementById("panelTitle")
    .textContent.replace("Sửa thông tin - ", "");
  await deleteParent(currentParentId, name);
}

async function deleteParent(id, name) {
  if (
    !confirm(
      `Bạn có chắc chắn muốn xóa phụ huynh "${name}"? Thao tác này cũng sẽ xóa tài khoản đăng nhập của họ.`,
    )
  ) {
    return;
  }

  try {
    const response = await fetch(`/Manager/Api/Parent/${id}`, {
      method: "DELETE",
    });
    const result = await response.json();

    if (result.success) {
      showFormAlert(result.message || "Xóa thành công", "success");
      setTimeout(() => {
        closePanel();
        refreshData();
      }, 1500);
    } else {
      showFormAlert(result.message || "Lỗi xóa phụ huynh", "error");
    }
  } catch (error) {
    console.error("Error:", error);
    showFormAlert("Lỗi kết nối máy chủ", "error");
  }
}

// ====== LINKED STUDENTS ======
function renderLinkedStudents(children) {
  const container = document.getElementById("linkedStudentsList");
  if (!children || children.length === 0) {
    container.innerHTML =
      '<p class="text-muted small text-center" style="padding: 12px 0;">Phụ huynh này chưa được liên kết với học sinh nào.</p>';
    return;
  }

  container.innerHTML = children
    .map(
      (c) => `
        <div class="linked-student-item">
            <div class="student-info">
                <span class="student-name">${c.fullName}</span>
                <span class="student-relation">${c.relationship}</span>
            </div>
            <button type="button" class="btn-icon btn-delete" onclick="unlinkStudent(${c.id})" title="Hủy liên kết">
                <i class="fa-solid fa-unlink"></i>
            </button>
        </div>
    `,
    )
    .join("");
}

// ====== LINK STUDENT MODAL ======
function showLinkStudentModal() {
  if (!currentParentId) {
    alert("Vui lòng mở phụ huynh cần liên kết trước.");
    return;
  }

  selectedStudentForLink = null;
  document.getElementById("studentSearchInput").value = "";
  document.getElementById("studentSearchResults").innerHTML = "";
  document.getElementById("linkDetailSection").style.display = "none";
  document.getElementById("confirmLinkBtn").disabled = true;

  // Ẩn slide panel nhưng KHÔNG reset currentParentId
  document.getElementById("slidePanel").classList.remove("active");
  document.getElementById("modalOverlay").classList.remove("active");

  // Mở modal link
  document.getElementById("linkModalOverlay").classList.add("active");
  document.getElementById("linkStudentModal").classList.add("active");
}

function closeLinkStudentModal() {
  document.getElementById("linkModalOverlay").classList.remove("active");
  document.getElementById("linkStudentModal").classList.remove("active");
  selectedStudentForLink = null;
  // Không mở lại slide panel
}

async function searchStudentsForLink(q) {
  try {
    const response = await fetch(
      `/Manager/Api/Students/Search?q=${encodeURIComponent(q)}`,
    );
    const result = await response.json();
    if (result.success) {
      const list = document.getElementById("studentSearchResults");
      if (result.data.length === 0) {
        list.innerHTML =
          '<div style="padding: 12px; text-align: center; color: var(--text-muted); font-size: 0.85rem;">Không tìm thấy học sinh nào</div>';
        return;
      }
      list.innerHTML = result.data
        .map(
          (s) => `
                <a href="javascript:void(0)" class="list-group-item list-group-item-action"
                   data-id="${s.id}"
                   data-name="${s.fullName}"
                   data-code="${s.code}"
                   style="padding: 10px 12px; display: flex; justify-content: space-between; align-items: center; cursor: pointer; border-bottom: 1px solid var(--border);">
                    <span style="font-weight: 600; font-size: 0.9rem;">${s.fullName}</span>
                    <span class="child-badge">${s.code}</span>
                </a>
            `,
        )
        .join("");

      list.querySelectorAll("a").forEach((el) => {
        el.addEventListener("click", () => {
          selectStudentForLink(el.dataset.id, el.dataset.name, el.dataset.code);
        });
      });
    }
  } catch (error) {
    console.error("Error searching students:", error);
  }
}

function selectStudentForLink(id, name, code) {
  selectedStudentForLink = id;
  document.getElementById("selectedStudentName").textContent = name;
  document.getElementById("selectedStudentCode").textContent = ` (${code})`;
  document.getElementById("linkDetailSection").style.display = "block";
  document.getElementById("confirmLinkBtn").disabled = false;
}

async function confirmLinkStudent() {
  console.log("currentParentId:", currentParentId);
  console.log("selectedStudentForLink:", selectedStudentForLink);

  const relationship = document.getElementById("linkRelationship").value;
  const confirmBtn = document.getElementById("confirmLinkBtn");

  const parentId = Number.parseInt(currentParentId, 10);
  const studentId = Number.parseInt(selectedStudentForLink, 10);

  console.log("parentId parsed:", parentId);
  console.log("studentId parsed:", studentId);

  if (
    !Number.isFinite(parentId) ||
    parentId <= 0 ||
    !Number.isFinite(studentId) ||
    studentId <= 0
  ) {
    alert("Vui lòng chọn phụ huynh và học sinh hợp lệ trước khi liên kết.");
    return;
  }

  confirmBtn.disabled = true;
  confirmBtn.innerHTML =
    '<i class="fa-solid fa-circle-notch fa-spin"></i> Đang xử lý...';

  const formData = new URLSearchParams();
  formData.append("parentId", parentId);
  formData.append("studentId", studentId);
  formData.append("relationship", relationship);

  try {
    const response = await fetch("/Manager/Api/Parent/LinkStudent", {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: formData.toString(),
    });

    const result = await response.json();
    if (result.success) {
      closeLinkStudentModal();
      currentParentId = null;
      refreshData();
      if (typeof showToast === "function")
        showToast("Liên kết thành công", "success");
    } else {
      alert(result.message || "Lỗi khi liên kết");
      confirmBtn.disabled = false;
      confirmBtn.innerHTML =
        '<i class="fa-solid fa-link"></i> Xác nhận liên kết';
    }
  } catch (error) {
    console.error("Error linking student:", error);
    alert("Lỗi kết nối hoặc lỗi server khi liên kết");
    confirmBtn.disabled = false;
    confirmBtn.innerHTML = '<i class="fa-solid fa-link"></i> Xác nhận liên kết';
  }
}

async function unlinkStudent(studentId) {
  if (!confirm("Bạn có chắc chắn muốn hủy liên kết với học sinh này?")) return;

  try {
    const response = await fetch(
      `/Manager/Api/Parent/UnlinkStudent?parentId=${currentParentId}&studentId=${studentId}`,
      { method: "DELETE" },
    );
    const result = await response.json();
    if (result.success) {
      openEditPanel(currentParentId);
      refreshData();
    } else {
      showFormAlert(result.message, "error");
    }
  } catch (error) {
    console.error("Error unlinking student:", error);
    showFormAlert("Lỗi khi hủy liên kết", "error");
  }
}

// ====== ALERT MANAGEMENT (sync với students.js) ======
function showFormAlert(message, type) {
  const alertContainer = document.getElementById("formAlert");
  const className = type === "success" ? "alert-success" : "alert-error";

  alertContainer.className = `form-alert ${className}`;
  alertContainer.textContent = message;
  alertContainer.style.display = "block";
}

function hideFormAlert() {
  const alertContainer = document.getElementById("formAlert");
  alertContainer.style.display = "none";
}
