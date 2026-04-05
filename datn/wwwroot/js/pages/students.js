// ====== STUDENT MANAGEMENT PAGE ======
// Xử lý tất cả tương tác: tải dữ liệu, render table, quản lý form

let currentStudentId = null;
let isEditMode = false;

// DOMContentLoaded - Khởi tạo trang khi HTML đã load xong
document.addEventListener('DOMContentLoaded', function () {
    initializeStudentsPage();
});

// ====== INITIALIZATION ======
async function initializeStudentsPage() {
    setupEventListeners();
    await loadClasses();
    await loadStudents();
}

// ====== EVENT LISTENERS SETUP ======
function setupEventListeners() {
    // Create Button
    document.getElementById('btnCreateStudent').addEventListener('click', openCreatePanel);

    // Close Panel Button
    document.getElementById('closePanelBtn').addEventListener('click', closePanel);

    // Modal Overlay Click
    document.getElementById('modalOverlay').addEventListener('click', closePanel);

    // Form Submit
    document.getElementById('editStudentForm').addEventListener('submit', handleFormSubmit);

    // Delete Button
    document.getElementById('deleteStudentBtn').addEventListener('click', handleDelete);
}

// ====== DATA LOADING ======
async function loadStudents() {
    try {
        const response = await fetch('/Manager/Api/Students');
        const result = await response.json();

        if (!result.success) {
            showTableError('Lỗi tải dữ liệu học sinh');
            return;
        }

        renderStudentsTable(result.data);
    } catch (error) {
        console.error('Error loading students:', error);
        showTableError('Lỗi kết nối máy chủ');
    }
}

async function loadClasses() {
    try {
        const response = await fetch('/Manager/Api/Classes');
        const result = await response.json();

        if (!result.success) return;

        const classSelect = document.getElementById('classId');
        classSelect.innerHTML = '<option value="">-- Chọn lớp --</option>';

        result.data.forEach(cls => {
            const option = document.createElement('option');
            option.value = cls.id;
            option.textContent = cls.name;
            classSelect.appendChild(option);
        });
    } catch (error) {
        console.error('Error loading classes:', error);
    }
}

// ====== TABLE RENDERING ======
function renderStudentsTable(students) {
    const tbody = document.getElementById('studentsTableBody');
    tbody.innerHTML = '';

    if (students.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" style="text-align: center; padding: 20px;">Không có học sinh nào</td></tr>';
        return;
    }

    students.forEach((student, index) => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${index + 1}</td>
            <td><strong>${student.fullName}</strong></td>
            <td>${student.gender}</td>
            <td>${student.dateOfBirth}</td>
            <td><span class="class-badge">${student.className}</span></td>
            <td>${student.enrollDate}</td>
            <td>
                <button type="button" class="btn-edit" onclick="openEditPanel(${student.id})">
                    Thay đổi
                </button>
            </td>
        `;
        tbody.appendChild(row);
    });
}

function showTableError(message) {
    const tbody = document.getElementById('studentsTableBody');
    tbody.innerHTML = `<tr><td colspan="7" style="text-align: center; padding: 20px; color: red;">${message}</td></tr>`;
}

// ====== PANEL MANAGEMENT ======
function openCreatePanel() {
    isEditMode = false;
    currentStudentId = null;

    // Reset form
    document.getElementById('editStudentForm').reset();
    document.getElementById('studentId').value = '';
    document.getElementById('panelTitle').textContent = 'Thêm học sinh mới';
    document.getElementById('deleteStudentBtn').style.display = 'none';

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
            showFormAlert('Lỗi tải dữ liệu học sinh', 'error');
            return;
        }

        const student = result.data;

        // Fill form
        document.getElementById('studentId').value = student.id;
        document.getElementById('firstName').value = student.firstName;
        document.getElementById('lastName').value = student.lastName;
        document.getElementById('gender').value = student.gender !== null ? student.gender : '';
        document.getElementById('dateOfBirth').value = student.dateOfBirth;
        document.getElementById('address').value = student.address;
        document.getElementById('classId').value = student.classId;
        document.getElementById('enrollDate').value = student.enrollDate;

        // Update title and show delete button
        document.getElementById('panelTitle').textContent = `Sửa thông tin - ${student.firstName} ${student.lastName}`;
        document.getElementById('deleteStudentBtn').style.display = 'block';

        // Hide alert
        hideFormAlert();

        // Open panel
        openPanel();
    } catch (error) {
        console.error('Error loading student:', error);
        showFormAlert('Lỗi kết nối máy chủ', 'error');
    }
}

function openPanel() {
    document.getElementById('modalOverlay').classList.add('active');
    document.getElementById('slidePanel').classList.add('active');
    document.body.style.overflow = 'hidden';
}

function closePanel() {
    document.getElementById('modalOverlay').classList.remove('active');
    document.getElementById('slidePanel').classList.remove('active');
    document.body.style.overflow = 'auto';
    isEditMode = false;
    currentStudentId = null;
}

// ====== FORM HANDLING ======
async function handleFormSubmit(e) {
    e.preventDefault();

    const firstName = document.getElementById('firstName').value.trim();
    const lastName = document.getElementById('lastName').value.trim();

    if (!firstName || !lastName) {
        showFormAlert('Vui lòng nhập đầy đủ tên học sinh', 'error');
        return;
    }

    const formData = {
        firstName: firstName,
        lastName: lastName,
        gender: document.getElementById('gender').value,
        dateOfBirth: document.getElementById('dateOfBirth').value,
        address: document.getElementById('address').value,
        classId: parseInt(document.getElementById('classId').value) || 0,
        enrollDate: document.getElementById('enrollDate').value
    };

    try {
        const url = isEditMode 
            ? `/Manager/Api/Student/${currentStudentId}` 
            : '/Manager/Api/Student';

        const method = isEditMode ? 'PUT' : 'POST';

        const response = await fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(formData)
        });

        const result = await response.json();

        if (result.success) {
            showFormAlert(result.message, 'success');
            setTimeout(() => {
                closePanel();
                loadStudents();
            }, 1500);
        } else {
            showFormAlert(result.message || 'Lỗi không xác định', 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showFormAlert('Lỗi kết nối máy chủ', 'error');
    }
}

async function handleDelete() {
    if (!currentStudentId) return;

    if (!confirm('Bạn chắc chắn muốn xóa học sinh này?')) {
        return;
    }

    try {
        const response = await fetch(`/Manager/Api/Student/${currentStudentId}`, {
            method: 'DELETE'
        });

        const result = await response.json();

        if (result.success) {
            showFormAlert(result.message, 'success');
            setTimeout(() => {
                closePanel();
                loadStudents();
            }, 1500);
        } else {
            showFormAlert(result.message || 'Lỗi xóa học sinh', 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showFormAlert('Lỗi kết nối máy chủ', 'error');
    }
}

// ====== ALERT MANAGEMENT ======
function showFormAlert(message, type) {
    const alertContainer = document.getElementById('editFormAlert');
    const className = type === 'success' ? 'alert-success' : 'alert-error';
    
    alertContainer.className = `form-alert ${className}`;
    alertContainer.textContent = message;
    alertContainer.style.display = 'block';
}

function hideFormAlert() {
    const alertContainer = document.getElementById('editFormAlert');
    alertContainer.style.display = 'none';
}
