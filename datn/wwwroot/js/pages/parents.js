let isEditMode = false;
let currentParentId = null;
let currentParentGender = true; // Default to Male (Nam)
let selectedStudentForLink = null;
let pendingStudentLinks = [];
let linkModalOpenedFromPanel = false;
let showInactiveParents = false;
let allParentsData = [];

// DOMContentLoaded - Khởi tạo trang khi HTML đã load xong
document.addEventListener("DOMContentLoaded", function () {
  initializeParentsPage();
});

// ====== INITIALIZATION ======
async function initializeParentsPage() {
  setupEventListeners();
  setupFilterListeners();
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
  if (studentSearch) {
    studentSearch.addEventListener("input", function () {
      clearTimeout(timeout);
      const q = this.value;
      if (q.length < 2) {
        document.getElementById("studentSearchResults").innerHTML = "";
        return;
      }
      timeout = setTimeout(() => searchStudentsForLink(q), 300);
    });
  }

  // Link modal overlay click
  // Link panel overlay click (nested slide panel) - handled via inline onclick in HTML
  // Nested link panel back/close buttons
  const linkBack = document.getElementById("linkBackBtn");
  if (linkBack) linkBack.addEventListener("click", closeLinkStudentPanel);
  const closeLinkBtn = document.getElementById("closeLinkPanelBtn");
  if (closeLinkBtn) closeLinkBtn.addEventListener("click", closeLinkStudentPanel);
  const linkPanelCloseBtn = document.getElementById("linkPanelCloseBtn");
  if (linkPanelCloseBtn) linkPanelCloseBtn.addEventListener("click", closeLinkStudentPanel);

  // Password real-time validation
  const passwordInput = document.getElementById("password");
  passwordInput.addEventListener("input", function() {
    validatePasswordUI(this.value);
  });

  // Status Tabs
  document.querySelectorAll(".status-tab").forEach((tab) => {
    tab.addEventListener("click", function () {
      document
        .querySelectorAll(".status-tab")
        .forEach((t) => t.classList.remove("active"));
      this.classList.add("active");
      showInactiveParents = this.getAttribute("data-show-inactive") === "true";
      refreshData();
    });
  });
}

function validatePasswordUI(val) {
  const isLength = val.length >= 9;
  const isUpper = /[A-Z]/.test(val);
  const isSpecial = /[!@#$%^&*()_+=\-\[\]{}|;:'",.<>?/\\ ]/.test(val);

  updateRequirementUI("req-length", isLength);
  updateRequirementUI("req-upper", isUpper);
  updateRequirementUI("req-special", isSpecial);

  // Return overall validity for potential use in saveParent
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

// ====== DATA LOADING ======
async function loadParents() {
  const tbody = document.getElementById("parentsTableBody");
  if (window.appLoading && tbody) {
    window.appLoading.setTable(tbody, 17);
  }

  try {
    const t = new Date().getTime();
    const response = await fetch(`/Manager/Api/Parents?showInactive=${showInactiveParents}&_t=${t}`);
    const result = await response.json();

    if (!result.success) {
      showTableError("Lỗi tải dữ liệu phụ huynh");
      return;
    }

    allParentsData = result.data;
    applyParentFilters();
  } catch (error) {
    console.error("Error loading parents:", error);
    showTableError("Lỗi kết nối máy chủ");
  }
}

function setupFilterListeners() {
  const filterToggle = document.getElementById('btnToggleFilter');
  if (filterToggle) {
    filterToggle.addEventListener('click', function() {
      this.classList.toggle('active');
      document.getElementById('filterPanel').classList.toggle('active');
    });
  }

  const btnApplyFilter = document.getElementById('btnApplyFilter');
  if (btnApplyFilter) {
    btnApplyFilter.addEventListener('click', () => applyParentFilters());
  }

  const btnResetFilter = document.getElementById('btnResetFilter');
  if (btnResetFilter) {
    btnResetFilter.addEventListener('click', () => {
      document.getElementById('filterGender').value = '';
      document.getElementById('filterHasChildren').value = '';
      applyParentFilters();
    });
  }
}

function applyParentFilters() {
  const gender = document.getElementById('filterGender').value;
  const hasChildren = document.getElementById('filterHasChildren').value;

  let filtered = allParentsData;

  if (gender !== '') {
    const genderBool = gender === 'true';
    filtered = filtered.filter(p => p.gender === genderBool);
  }
  if (hasChildren === 'yes') {
    filtered = filtered.filter(p => p.children && p.children.length > 0);
  } else if (hasChildren === 'no') {
    filtered = filtered.filter(p => !p.children || p.children.length === 0);
  }

  renderParentsTable(filtered);
}

function openLinkStudentPanel() {
  const genderInput = document.querySelector('input[name="Gender"]:checked');
  if (genderInput) {
  currentParentGender = genderInput.value === "true";
  }

  selectedStudentForLink = null;
  linkModalOpenedFromPanel = true;
  document.getElementById("studentSearchInput").value = "";
  document.getElementById("studentSearchResults").innerHTML = "";
  document.getElementById("linkDetailSection").style.display = "none";
  
  const confirmBtn = document.getElementById("linkPanelConfirmBtn");
  if (confirmBtn) {
  confirmBtn.disabled = true;
  confirmBtn.innerHTML = '<i class="fa-solid fa-link"></i> Xác nhận liên kết';
  }

  // Tự động chọn và ràng buộc mối quan hệ dựa trên giới tính
  const relInputs = document.querySelectorAll('input[name="relationship"]');
  relInputs.forEach(input => {
    if (currentParentGender) { // Nam -> Bố
      if (input.value === "Bố") {
        input.checked = true;
        input.disabled = false;
      } else {
        input.disabled = true;
      }
    } else { // Nữ -> Mẹ
      if (input.value === "Mẹ") {
        input.checked = true;
        input.disabled = false;
      } else {
        input.disabled = true;
      }
    }
      
    // Cập nhật giao diện (mờ đi các option bị disable)
    const content = input.nextElementSibling;
    if (input.disabled) {
      content.style.opacity = "0.4";
      content.style.cursor = "not-allowed";
    } else {
      content.style.opacity = "1";
      content.style.cursor = "pointer";
    }
  });

  // Open nested link slide panel on top of existing parent slide panel
  const linkOverlay = document.getElementById("linkPanelOverlay");
  const linkPanel = document.getElementById("linkSlidePanel");
  if (linkOverlay) linkOverlay.classList.add("active");
  if (linkPanel) linkPanel.classList.add("active");
}
function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function renderParentsTable(parents) {
  const tbody = document.getElementById("parentsTableBody");
  tbody.innerHTML = "";

  if (!parents || parents.length === 0) {
    if (window.appLoading) {
      tbody.innerHTML = window.appLoading.tableEmpty(17, "Kh\u00f4ng t\u00ecm th\u1ea5y d\u1eef li\u1ec7u");
      return;
    }
    tbody.innerHTML = '<tr><td colspan="17" class="text-center">Không tìm thấy dữ liệu</td></tr>';
    return;
  }

  parents.forEach((p, index) => {
    const row = document.createElement("tr");
    const children = Array.isArray(p.children) ? p.children : [];
    const childrenNames = children.length > 0
      ? children
          .map((c) => {
            const relationship = c.relationship || "Phụ huynh";
            return `<span class="child-badge" title="${escapeHtml(relationship)}">${escapeHtml(c.fullName)} <small>${escapeHtml(relationship)}</small></span>`;
          })
          .join("")
      : '<span class="text-muted" style="font-size:0.8rem; font-style:italic;">Chưa gán con</span>';
    const classNames = [
      ...new Set(children.map((c) => c.className).filter((name) => name && name !== "N/A")),
    ];
    const childClasses = classNames.length > 0
      ? classNames.map((name) => `<span class="class-badge">${escapeHtml(name)}</span>`).join("")
      : '<span class="text-muted">N/A</span>';
    const statusBadge = p.isActive
      ? '<span class="badge badge-success">Hoạt động</span>'
      : '<span class="badge badge-danger">Đã khóa</span>';

    row.innerHTML = `
            <td class="sticky-col first-col">${index + 1}</td>
            <td class="sticky-col second-col"><span class="parent-code">#${p.id}</span></td>
            <td class="sticky-col third-col">
                <img src="${escapeHtml(p.avatarPath || "/images/lion_orange.png")}" class="avatar-sm" alt="avatar" onerror="this.src='/images/lion_orange.png'">
            </td>
            <td class="sticky-col fourth-col">
                <a href="/Manager/ParentDetail/${p.id}" target="_blank" class="parent-name-link" title="${escapeHtml(p.fullName)}">${escapeHtml(p.fullName)}</a>
            </td>
            <td>${statusBadge}</td>
            <td>${p.gender ? "Nam" : "Nữ"}</td>
            <td>${formatDateOnly(p.dateOfBirth)}</td>
            <td><span class="username-cell">${escapeHtml(p.username || "N/A")}</span></td>
            <td><div class="email-cell" title="${escapeHtml(p.email || "N/A")}">${escapeHtml(p.email || "N/A")}</div></td>
            <td>${escapeHtml(p.phone || "N/A")}</td>
            <td><div class="address-cell" title="${escapeHtml(p.address || "N/A")}">${escapeHtml(p.address || "N/A")}</div></td>
            <td><span class="children-count">${p.childrenCount ?? children.length}</span></td>
            <td><div class="children-cell">${childrenNames}</div></td>
            <td><div class="child-class-cell">${childClasses}</div></td>
            <td><span class="premium-date">${formatPremiumDate(p.createdAt)}</span></td>
            <td><span class="premium-date">${formatPremiumDate(p.updatedAt)}</span></td>
            <td class="text-end">
                <button class="btn-table" onclick="openEditPanel(${p.id})">Sửa</button>
            </td>

        `;
    tbody.appendChild(row);
  });
}

function showTableError(message) {
  const tbody = document.getElementById("parentsTableBody");
  if (window.appLoading) {
    tbody.innerHTML = window.appLoading.tableError(17, message);
    return;
  }
  tbody.innerHTML = `<tr><td colspan="17" style="text-align: center; padding: 20px; color: red;">${message}</td></tr>`;
}

// ====== DATE FORMATTING (sync với students.js) ======
function formatPremiumDate(dateString) {
  if (!dateString) return "N/A";
  if (String(dateString).startsWith("0001-01-01")) return "N/A";
  const date = new Date(dateString);
  if (Number.isNaN(date.getTime())) return "N/A";
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

function formatDateOnly(value) {
  if (!value) return "Chưa cập nhật";
  const date = new Date(`${value}T00:00:00`);
  if (Number.isNaN(date.getTime())) return "Chưa cập nhật";
  const day = date.getDate().toString().padStart(2, "0");
  const month = (date.getMonth() + 1).toString().padStart(2, "0");
  return `${day}/${month}/${date.getFullYear()}`;
}

// ====== PANEL MANAGEMENT ======
function openCreatePanel() {
  isEditMode = false;
  currentParentId = null;
  currentParentGender = true;

  document.getElementById("parentForm").reset();
  document.getElementById("parentId").value = "";
  document.getElementById("username").readOnly = false;
  document.getElementById("username").style.backgroundColor = "";
  document.getElementById("avatarPreview").src = "/images/lion_orange.png";
  document.getElementById("panelTitle").textContent = "Thêm phụ huynh mới";
  const passwordInput = document.getElementById("password");
  passwordInput.required = true;
  passwordInput.value = ""; // Clear password field for new parent
  validatePasswordUI(""); // Reset validation hints
  document.getElementById("passwordFieldGroup").style.display = "block"; // Show on create so manager can set initial password
  document.getElementById("linkedStudentsSection").style.display = "block";
  const hint = document.getElementById("linkedStudentsHint");
  if (hint) hint.style.display = "block";
  pendingStudentLinks = [];
  renderLinkedStudents([]);
  document.getElementById("deleteParentBtn").style.display = "none";

  hideFormAlert();
  openPanel();
}

let currentParentIsActive = true;

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
    document.getElementById("username").readOnly = true;
    document.getElementById("username").style.backgroundColor = "var(--bg-input-disabled, #f1f3f5)";
    document.getElementById("email").value = p.email;
    document.getElementById("lastName").value = p.lastName;
    document.getElementById("firstName").value = p.firstName;
    document.getElementById("phone").value = p.phone || "";
    document.getElementById("dateOfBirth").value = p.dateOfBirth || "";
    document.getElementById("address").value = p.address || "";
    
    // Gender
    currentParentGender = p.gender;
    const genderInput = document.querySelector(`input[name="Gender"][value="${p.gender}"]`);
    if (genderInput) genderInput.checked = true;

    document.getElementById("avatarPreview").src =
      p.avatarPath || "/images/lion_orange.png";

    document.getElementById("password").required = false;
    document.getElementById("passwordFieldGroup").style.display = "none"; // Hide on edit to prevent accidental changes
    document.getElementById("password").value = "******";

    document.getElementById("panelTitle").textContent =
      `Sửa thông tin - ${p.firstName} ${p.lastName}`;
    
    currentParentIsActive = p.isActive;
    const actionBtn = document.getElementById("deleteParentBtn");
    actionBtn.style.display = "block";
    if (p.isActive) {
        actionBtn.className = "btn-delete";
        actionBtn.innerHTML = '<i class="fa-solid fa-lock"></i> Vô hiệu hóa tài khoản';
    } else {
        actionBtn.className = "btn-activate";
        actionBtn.innerHTML = '<i class="fa-solid fa-unlock"></i> Kích hoạt tài khoản';
    }

    // Show linked children
    pendingStudentLinks = [];
    document.getElementById("linkedStudentsSection").style.display = "block";
    const hint = document.getElementById("linkedStudentsHint");
    if (hint) hint.style.display = "none";
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
  document.body.classList.add("panel-open");
}

function closePanel() {
  document.getElementById("modalOverlay").classList.remove("active");
  document.getElementById("slidePanel").classList.remove("active");
  document.body.style.overflow = "auto";
  document.body.classList.remove("panel-open");
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

  const dateOfBirth = document.getElementById("dateOfBirth").value;
  if (window.appValidation && !window.appValidation.isAgeValid(dateOfBirth, 16, false)) {
    showFormAlert("Phụ huynh phải trên 16 tuổi.", "error");
    return;
  }

  const formData = new FormData(form);
  const id = document.getElementById("parentId").value;
  const url = isEditMode ? `/Manager/Api/Parent/${id}` : "/Manager/Api/Parent";
  const method = isEditMode ? "PUT" : "POST";

  if (!isEditMode && pendingStudentLinks.length > 0) {
    pendingStudentLinks.forEach((link, index) => {
      formData.append(`StudentLinks[${index}].StudentId`, String(link.id));
      formData.append(`StudentLinks[${index}].Relationship`, link.relationship);
    });
  }

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

async function toggleCurrentParentStatus() {
  if (!currentParentId) return;
  const name = document
    .getElementById("panelTitle")
    .textContent.replace("Sửa thông tin - ", "");
  
  if (currentParentIsActive) {
    if (!confirm(`Bạn có chắc chắn muốn vô hiệu hóa phụ huynh "${name}"? Tài khoản này sẽ không thể đăng nhập vào hệ thống nữa.`)) return;
    
    try {
      const response = await fetch(`/Manager/Api/Parent/${currentParentId}`, { method: "DELETE" });
      const result = await response.json();
      if (result.success) {
        showFormAlert(result.message || "Vô hiệu hóa thành công", "success");
        setTimeout(() => { closePanel(); refreshData(); }, 1500);
      } else {
        showFormAlert(result.message || "Lỗi vô hiệu hóa phụ huynh", "error");
      }
    } catch (error) {
      showFormAlert("Lỗi kết nối máy chủ", "error");
    }
  } else {
    if (!confirm(`Bạn có chắc chắn muốn kích hoạt lại tài khoản cho phụ huynh "${name}"?`)) return;

    try {
      const response = await fetch(`/Manager/Api/Parent/Reactivate/${currentParentId}`, { method: "POST" });
      const result = await response.json();
      if (result.success) {
        showFormAlert(result.message || "Kích hoạt thành công", "success");
        setTimeout(() => { closePanel(); refreshData(); }, 1500);
      } else {
        showFormAlert(result.message || "Lỗi kích hoạt phụ huynh", "error");
      }
    } catch (error) {
      showFormAlert("Lỗi kết nối máy chủ", "error");
    }
  }
}

// ====== LINKED STUDENTS ======
function renderLinkedStudents(children) {
  const container = document.getElementById("linkedStudentsList");
  if (!children || children.length === 0) {
    container.innerHTML =
      '<p class="text-muted small text-center" style="padding: 12px 0;">Chưa có học sinh nào được liên kết.</p>';
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
// (replaced by nested slide panel: openLinkStudentPanel)

function closeLinkStudentPanel() {
  const linkOverlay = document.getElementById("linkPanelOverlay");
  const linkPanel = document.getElementById("linkSlidePanel");
  if (linkOverlay) linkOverlay.classList.remove("active");
  if (linkPanel) linkPanel.classList.remove("active");
  selectedStudentForLink = null;
  linkModalOpenedFromPanel = false;
}

async function searchStudentsForLink(q) {
  try {
    const response = await fetch(
      `/Manager/Api/Students/Search?q=${encodeURIComponent(q)}`,
    );
    const result = await response.json();
    if (result.success) {
      const list = document.getElementById("studentSearchResults");

      // Bug 2: lọc ra những học sinh đã được gán cho phụ huynh này
      const assignedIds = new Set(pendingStudentLinks.map(l => l.id));
      // Trong edit mode, lấy id từ danh sách linked students đang hiển thị
      if (isEditMode) {
        document.querySelectorAll("#linkedStudentsList .linked-student-item").forEach(item => {
          const btn = item.querySelector("[onclick^='unlinkStudent']");
          if (btn) {
            const match = btn.getAttribute("onclick").match(/unlinkStudent\((\d+)\)/);
            if (match) assignedIds.add(parseInt(match[1], 10));
          }
        });
      }

      const filtered = result.data.filter(s => !assignedIds.has(s.id));

      if (filtered.length === 0) {
        list.innerHTML =
          '<div style="padding: 12px; text-align: center; color: var(--text-muted); font-size: 0.85rem;">Không tìm thấy học sinh nào</div>';
        return;
      }
      list.innerHTML = filtered
        .map(
          (s) => `
                <a href="javascript:void(0)" class="list-group-item list-group-item-action"
                   data-id="${s.id}"
                   data-name="${s.fullName}"
                   style="padding: 12px 16px; display: flex; justify-content: space-between; align-items: center; cursor: pointer; border-bottom: 1px solid var(--border);">
                    <div style="display: flex; flex-direction: column;">
                        <span style="font-weight: 700; font-size: 0.95rem; color: var(--text-main);">${s.fullName}</span>
                        <span style="font-size: 0.75rem; color: var(--text-muted);">ID: ${s.id}</span>
                    </div>
                    <i class="fa-solid fa-chevron-right text-muted" style="font-size: 0.8rem;"></i>
                </a>
            `,
        )
        .join("");

      list.querySelectorAll("a").forEach((el) => {
        el.addEventListener("click", () => {
          selectStudentForLink(el.dataset.id, el.dataset.name);
        });
      });
    }
  } catch (error) {
    console.error("Error searching students:", error);
  }
}

function selectStudentForLink(id, name) {
  selectedStudentForLink = id;
  document.getElementById("selectedStudentName").textContent = name;
  document.getElementById("selectedStudentId").textContent = ` (ID: ${id})`;
  document.getElementById("linkDetailSection").style.display = "block";
  const confirmBtn = document.getElementById("linkPanelConfirmBtn");
  if (confirmBtn) confirmBtn.disabled = false;
}

async function confirmLinkStudent() {
  const confirmBtn = document.getElementById("linkPanelConfirmBtn");
  if (confirmBtn.disabled && confirmBtn.innerHTML.includes("fa-spin")) return; // Chặn click khi đang xử lý

  // Lấy giá trị mối quan hệ (hỗ trợ cả radio cards mới và select cũ để tránh lỗi cache)
  let relationship = "";
  const radioChecked = document.querySelector('input[name="relationship"]:checked');
  const selectEl = document.getElementById("linkRelationship");
  
  if (radioChecked) {
    relationship = radioChecked.value;
  } else if (selectEl) {
    relationship = selectEl.value;
  } else {
      alert("Không tìm thấy dữ liệu mối quan hệ. Vui lòng nhấn Ctrl + F5 để tải lại trang.");
      return;
  }

  const parentId = Number.parseInt(currentParentId, 10);
  const studentId = Number.parseInt(selectedStudentForLink, 10);

  console.log("parentId parsed:", parentId);
  console.log("studentId parsed:", studentId);

  if (!Number.isFinite(studentId) || studentId <= 0) {
    alert("Vui lòng chọn học sinh hợp lệ trước khi liên kết.");
    return;
  }

  if (!isEditMode) {
    if (pendingStudentLinks.some((l) => l.id === studentId)) {
      alert("Học sinh này đã được thêm vào danh sách.");
      return;
    }
    const studentName =
      document.getElementById("selectedStudentName").textContent || "";
    pendingStudentLinks.push({
      id: studentId,
      fullName: studentName,
      relationship,
    });
    closeLinkStudentPanel();
    renderLinkedStudents(pendingStudentLinks);
    return;
  }

  if (!Number.isFinite(parentId) || parentId <= 0) {
    alert("Vui lòng mở phụ huynh cần liên kết trước.");
    return;
  }

  if (confirmBtn) {
    confirmBtn.disabled = true;
    confirmBtn.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> Đang xử lý...';
  }

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
      const parentIdToRefresh = currentParentId;
      // close both panels after successful link
      closeLinkStudentPanel();
      closePanel();
      await openEditPanel(parentIdToRefresh);
      refreshData();
      if (typeof showToast === "function") showToast("Liên kết thành công", "success");
    } else {
      alert(result.message || "Lỗi khi liên kết");
      if (confirmBtn) {
        confirmBtn.disabled = false;
        confirmBtn.innerHTML = '<i class="fa-solid fa-link"></i> Xác nhận liên kết';
      }
    }
  } catch (error) {
    console.error("Error linking student:", error);
    alert("Lỗi kết nối hoặc lỗi server khi liên kết");
    if (confirmBtn) {
      confirmBtn.disabled = false;
      confirmBtn.innerHTML = '<i class="fa-solid fa-link"></i> Xác nhận liên kết';
    }
  }
}

async function unlinkStudent(studentId) {
  if (!confirm("Bạn có chắc chắn muốn hủy liên kết với học sinh này?")) return;

  if (!isEditMode) {
    pendingStudentLinks = pendingStudentLinks.filter((l) => l.id !== studentId);
    renderLinkedStudents(pendingStudentLinks);
    return;
  }

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
  if (alertContainer) {
    alertContainer.style.display = "none";
    alertContainer.textContent = "";
  }
  if (window.showToast) {
    window.showToast(type === "success" ? "Thành công" : "Có lỗi", message, type === "success" ? "success" : "error");
  }
}

function hideFormAlert() {
  const alertContainer = document.getElementById("formAlert");
  alertContainer.style.display = "none";
}
