document.addEventListener('DOMContentLoaded', () => {
    initializeWorkSchedulePage();
});

async function initializeWorkSchedulePage() {
    const initialClassId = parseInt(document.getElementById('initialClassId').value || '0', 10);
    const [scheduleResult, classesResult] = await Promise.all([
        fetchJson('/Employee/Api/TodaySchedule'),
        fetchJson('/Employee/Api/MyClasses')
    ]);

    renderTodaySchedule(scheduleResult);
    populateManagedClasses(classesResult, initialClassId);

    const select = document.getElementById('managedClassSelect');
    select.addEventListener('change', () => {
        const classId = parseInt(select.value || '0', 10);
        loadManagedStudents(classId);
    });

    const defaultClassId = initialClassId
        || parseFirstScheduledClassId(scheduleResult)
        || parseFirstManagedClassId(classesResult);

    if (defaultClassId) {
        select.value = defaultClassId;
        await loadManagedStudents(defaultClassId);
    } else {
        renderEmptyStudentsState('Bạn chưa được phân công quản lý lớp nào.');
    }
}

function renderTodaySchedule(result) {
    const container = document.getElementById('todayScheduleContainer');
    const label = document.getElementById('todayLabel');

    if (!result.success) {
        label.textContent = 'Không tải được lịch dạy hôm nay.';
        container.innerHTML = `<div class="empty-state">Không lấy được dữ liệu từ máy chủ.</div>`;
        return;
    }

    label.textContent = `Ngày ${result.date}`;

    if (!result.data || result.data.length === 0) {
        container.innerHTML = `<div class="empty-state">Hôm nay bạn chưa có lịch dạy nào.</div>`;
        return;
    }

    container.innerHTML = result.data.map(item => `
        <article class="schedule-item">
            <div class="time">${item.startTime} - ${item.endTime}</div>
            <div class="class-name">${escapeHtml(item.className)}</div>
            <div class="subject-name">${escapeHtml(item.subjectName)}</div>
            <div class="schedule-note">${escapeHtml(item.note || 'Không có ghi chú')}</div>
            <div class="schedule-actions">
                <a class="btn-table" href="/Employee/Attendance?classId=${item.classId}">Điểm danh</a>
                <button class="btn-table" type="button" onclick="focusManagedClass(${item.classId})">Xem học sinh</button>
            </div>
        </article>
    `).join('');
}

function populateManagedClasses(result, selectedClassId) {
    const select = document.getElementById('managedClassSelect');
    select.innerHTML = '<option value="">Chọn lớp học</option>';

    if (!result.success || !result.data) {
        return;
    }

    result.data.forEach(item => {
        const option = document.createElement('option');
        option.value = item.classId;
        option.textContent = `${item.className} (${item.role})`;
        if (selectedClassId && item.classId === selectedClassId) {
            option.selected = true;
        }
        select.appendChild(option);
    });
}

async function loadManagedStudents(classId) {
    if (!classId) {
        renderEmptyStudentsState('Chọn một lớp để xem học sinh.');
        return;
    }

    const result = await fetchJson(`/Employee/Api/ManagedStudents/${classId}`);
    if (!result.success) {
        showAlert(false, result.message || 'Không tải được danh sách học sinh.');
        renderEmptyStudentsState('Không tải được danh sách học sinh.');
        return;
    }

    showAlert(true, `Đang hiển thị ${result.data.length} học sinh của lớp được chọn.`);
    renderStudentsTable(result.data);
}

function renderStudentsTable(students) {
    const tbody = document.getElementById('managedStudentsTableBody');

    if (!students || students.length === 0) {
        tbody.innerHTML = `<tr><td colspan="8">Lớp này chưa có học sinh.</td></tr>`;
        return;
    }

    tbody.innerHTML = students.map(student => `
        <tr>
            <td><img class="student-avatar" src="${student.avatarPath}" alt="Avatar" /></td>
            <td><strong>${escapeHtml(student.fullName)}</strong></td>
            <td>${escapeHtml(student.gender)}</td>
            <td>${escapeHtml(student.dateOfBirth)}</td>
            <td>${escapeHtml(student.address)}</td>
            <td>${escapeHtml(student.enrollDate)}</td>
            <td>${renderAttendanceStatus(student.attendanceStatus)}</td>
            <td><a class="btn-table" href="/Employee/Attendance?classId=${document.getElementById('managedClassSelect').value}">Điểm danh lớp</a></td>
        </tr>
    `).join('');
}

function renderAttendanceStatus(status) {
    if (!status) return 'Chưa điểm danh';
    if (status === 'Present') return 'Có mặt';
    if (status === 'Excused') return 'Có phép';
    if (status === 'Absent') return 'Vắng mặt';
    return escapeHtml(status);
}

function renderEmptyStudentsState(message) {
    document.getElementById('managedStudentsTableBody').innerHTML = `<tr><td colspan="8">${message}</td></tr>`;
}

function focusManagedClass(classId) {
    const select = document.getElementById('managedClassSelect');
    select.value = classId;
    loadManagedStudents(classId);
    select.scrollIntoView({ behavior: 'smooth', block: 'center' });
}

function parseFirstScheduledClassId(result) {
    if (!result.success || !result.data || result.data.length === 0) {
        return 0;
    }
    return result.data[0].classId;
}

function parseFirstManagedClassId(result) {
    if (!result.success || !result.data || result.data.length === 0) {
        return 0;
    }
    return result.data[0].classId;
}

function showAlert(success, message) {
    const alert = document.getElementById('studentsAlert');
    alert.className = `page-alert ${success ? 'success' : 'error'}`;
    alert.textContent = message;
    alert.style.display = 'block';
}

async function fetchJson(url, options) {
    const response = await fetch(url, options);
    return response.json();
}

function escapeHtml(value) {
    return (value ?? '')
        .toString()
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');
}
