// ====== TEACHER MANAGEMENT PAGE ======
// Xử lý tất cả tương tác: tải dữ liệu, render table, quản lý form

let currentTeacherId = null;
let isEditMode = false;

// DOMContentLoaded - Khởi tạo trang khi HTML đã load xong
document.addEventListener('DOMContentLoaded', function () {
    initializeTeachersPage();
});

// ====== INITIALIZATION ======
async function initializeTeachersPage() {
    setupEventListeners();
    await loadTeachers();
}

// ====== EVENT LISTENERS SETUP ======
function setupEventListeners() {
    // Create Button
    document.getElementById('btnCreateTeacher').addEventListener('click', openCreatePanel);

    // Close Panel Button
    document.getElementById('closePanelBtn').addEventListener('click', closePanel);

    // Modal Overlay Click
    document.getElementById('modalOverlay').addEventListener('click', closePanel);

    // Form Submit
    document.getElementById('editTeacherForm').addEventListener('submit', handleFormSubmit);

    // Delete Button
    document.getElementById('deleteTeacherBtn').addEventListener('click', handleDelete);
}

// Preview Avatar
function previewAvatar(input) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) {
            document.getElementById('avatarPreview').src = e.target.result;
        };
        reader.readAsDataURL(input.files[0]);
    }
}

// ====== DATA LOADING ======
async function loadTeachers() {
    try {
        const response = await fetch('/Manager/Api/Teachers');
        const result = await response.json();

        if (!result.success) {
            showTableError('Lỗi tải dữ liệu giáo viên');
            return;
        }

        renderTeachersTable(result.data);
    } catch (error) {
        console.error('Error loading teachers:', error);
        showTableError('Lỗi kết nối máy chủ');
    }
}

// ====== TABLE RENDERING ======
function renderTeachersTable(teachers) {
    const tbody = document.getElementById('teachersTableBody');
    tbody.innerHTML = '';

    if (teachers.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 20px;">Không có giáo viên nào</td></tr>';
        return;
    }

    teachers.forEach((teacher, index) => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${index + 1}</td>
            <td>
                <img src="${teacher.avatarPath}" alt="Avatar" class="rounded-circle" style="width: 40px; height: 40px; object-fit: cover;" />
            </td>
            <td><strong>${teacher.fullName}</strong></td>
            <td>${teacher.phone || 'Chưa cập nhật'}</td>
            <td>${teacher.position || 'Chưa cập nhật'}</td>
            <td>
                <button type="button" class="btn-edit" onclick="openEditPanel(${teacher.id})">
                    Thay đổi
                </button>
            </td>
        `;
        tbody.appendChild(row);
    });
}

// ====== PANEL MANAGEMENT ======
function openCreatePanel() {
    isEditMode = false;
    currentTeacherId = null;
    document.getElementById('panelTitle').textContent = 'Thêm giáo viên mới';
    document.getElementById('editTeacherForm').reset();
    document.getElementById('avatarPreview').src = '/images/lion_blue.png';
    document.getElementById('deleteStudentBtn') ? document.getElementById('deleteStudentBtn').style.display = 'none' : null;
    document.getElementById('deleteTeacherBtn').style.display = 'none';
    clearAlert();
    showPanel();
}

async function openEditPanel(teacherId) {
    isEditMode = true;
    currentTeacherId = teacherId;
    document.getElementById('panelTitle').textContent = 'Chỉnh sửa thông tin giáo viên';
    document.getElementById('deleteTeacherBtn').style.display = 'block';
    clearAlert();
    showPanel();

    try {
        const response = await fetch(`/Manager/Api/Teacher/${teacherId}`);
        const result = await response.json();

        if (!result.success) {
            showAlert('error', 'Lỗi tải dữ liệu giáo viên');
            return;
        }

        const data = result.data;
        document.getElementById('teacherId').value = data.id;
        document.getElementById('fullName').value = data.fullName || '';
        document.getElementById('phone').value = data.phone || '';
        document.getElementById('position').value = data.position || '';
        document.getElementById('baseSalary').value = data.baseSalary || '';
        
        // Avatar preview
        document.getElementById('avatarPreview').src = data.avatarPath || '/images/lion_blue.png';
    } catch (error) {
        console.error('Error loading teacher:', error);
        showAlert('error', 'Lỗi kết nối máy chủ');
    }
}

function showPanel() {
    document.getElementById('slidePanel').classList.add('active');
    document.getElementById('modalOverlay').classList.add('active');
}

function closePanel() {
    document.getElementById('slidePanel').classList.remove('active');
    document.getElementById('modalOverlay').classList.remove('active');
    currentTeacherId = null;
    isEditMode = false;
}

// ====== FORM HANDLING ======
async function handleFormSubmit(e) {
    e.preventDefault();

    const fullName = document.getElementById('fullName').value.trim();
    if (!fullName) {
        showAlert('error', 'Vui lòng nhập họ và tên');
        return;
    }

    const formData = new FormData();
    formData.append('FullName', fullName);
    formData.append('Phone', document.getElementById('phone').value);
    formData.append('Position', document.getElementById('position').value);
    formData.append('BaseSalary', parseFloat(document.getElementById('baseSalary').value) || '');

    const avatarFile = document.getElementById('avatarFile').files[0];
    if (avatarFile) {
        formData.append('Avatar', avatarFile);
    }

    try {
        let response, url, method;

        if (isEditMode) {
            url = `/Manager/Api/Teacher/${currentTeacherId}`;
            method = 'PUT';
        } else {
            url = '/Manager/Api/Teacher';
            method = 'POST';
        }

        response = await fetch(url, {
            method: method,
            body: formData
        });

        const result = await response.json();

        if (!result.success) {
            showAlert('error', result.message || 'Lỗi xử lý yêu cầu');
            return;
        }

        showAlert('success', result.message);
        setTimeout(() => {
            closePanel();
            loadTeachers();
        }, 1500);
    } catch (error) {
        console.error('Error submitting form:', error);
        showAlert('error', 'Lỗi kết nối máy chủ');
    }
}

// ====== DELETE HANDLING ======
async function handleDelete() {
    if (!confirm('Bạn có chắc chắn muốn xóa giáo viên này?')) {
        return;
    }

    try {
        const response = await fetch(`/Manager/Api/Teacher/${currentTeacherId}`, {
            method: 'DELETE'
        });

        const result = await response.json();

        if (!result.success) {
            showAlert('error', result.message || 'Lỗi xóa giáo viên');
            return;
        }

        showAlert('success', 'Xóa giáo viên thành công');
        setTimeout(() => {
            closePanel();
            loadTeachers();
        }, 1500);
    } catch (error) {
        console.error('Error deleting teacher:', error);
        showAlert('error', 'Lỗi kết nối máy chủ');
    }
}

// ====== ALERT MANAGEMENT ======
function showAlert(type, message) {
    const alertDiv = document.getElementById('editFormAlert');
    alertDiv.className = `form-alert ${type}`;
    alertDiv.textContent = message;
    alertDiv.style.display = 'block';
}

function clearAlert() {
    const alertDiv = document.getElementById('editFormAlert');
    alertDiv.style.display = 'none';
    alertDiv.textContent = '';
}

// ====== ERROR HANDLING ======
function showTableError(message) {
    const tbody = document.getElementById('teachersTableBody');
    tbody.innerHTML = `<tr><td colspan="6" style="text-align: center; padding: 20px; color: #d32f2f;">${message}</td></tr>`;
}

// ====== UTILITY FUNCTIONS ======
function formatCurrency(value) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(value);
}
