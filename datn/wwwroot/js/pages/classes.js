let currentClassId = null;
let currentSubjectId = null;
let currentScheduleId = null;

document.addEventListener('DOMContentLoaded', () => {
    bindClassManagementEvents();
    initializeClassManagementPage();
});

async function initializeClassManagementPage() {
    document.getElementById('scheduleEffectiveFrom').value = new Date().toISOString().split('T')[0];
    await Promise.all([
        loadClassesOverview(),
        loadSubjects(),
        refreshDropdowns()
    ]);
    
    // Khởi tạo pagination sau khi dữ liệu đã load
    if (typeof initPagination === 'function') {
        initPagination('classesTable', 10);
        initPagination('subjectsTable', 10);
        initPagination('scheduleTable', 10);
    }
}

function bindClassManagementEvents() {
    document.getElementById('classForm').addEventListener('submit', saveClass);
    document.getElementById('subjectForm').addEventListener('submit', saveSubject);
    document.getElementById('scheduleForm').addEventListener('submit', saveSchedule);

    document.getElementById('clearClassBtn').addEventListener('click', resetClassForm);
    document.getElementById('clearSubjectBtn').addEventListener('click', resetSubjectForm);
    document.getElementById('clearScheduleBtn').addEventListener('click', resetScheduleForm);
    document.getElementById('resetClassFormBtn').addEventListener('click', resetClassForm);
    document.getElementById('resetSubjectFormBtn').addEventListener('click', resetSubjectForm);

    document.getElementById('scheduleClassFilter').addEventListener('change', (event) => {
        const classId = parseInt(event.target.value || '0', 10);
        document.getElementById('scheduleClassId').value = classId || '';
        loadSchedules(classId);
    });

    document.getElementById('scheduleClassId').addEventListener('change', (event) => {
        const classId = parseInt(event.target.value || '0', 10);
        document.getElementById('scheduleClassFilter').value = classId || '';
    });
}

async function loadClassesOverview() {
    const result = await fetchJson('/Manager/Api/Classes/Overview');
    const tbody = document.getElementById('classesTableBody');

    if (!result.success) {
        tbody.innerHTML = `<tr><td colspan="6">Không tải được dữ liệu lớp học.</td></tr>`;
        return;
    }

    if (result.data.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6">Chưa có lớp học nào.</td></tr>`;
        return;
    }

    tbody.innerHTML = result.data.map(item => `
        <tr>
            <td><strong>${escapeHtml(item.name || '')}</strong></td>
            <td>${formatAgeRange(item.ageFrom, item.ageTo)}</td>
            <td>${escapeHtml(item.schoolYear || 'Chưa cập nhật')}</td>
            <td>${item.studentCount}</td>
            <td class="teacher-tags">${renderTeacherTags(item.teachers)}</td>
            <td>
                <button type="button" class="btn-table" onclick="editClass(${item.id})">Sửa</button>
                <button type="button" class="btn-table" onclick="selectScheduleClass(${item.id})">Lịch</button>
                <button type="button" class="btn-table delete" onclick="deleteClass(${item.id})">Xóa</button>
            </td>
        </tr>
    `).join('');
}

async function loadSubjects() {
    const result = await fetchJson('/Manager/Api/Subjects');
    const tbody = document.getElementById('subjectsTableBody');

    if (!result.success) {
        tbody.innerHTML = `<tr><td colspan="5">Không tải được dữ liệu môn học.</td></tr>`;
        return;
    }

    if (result.data.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5">Chưa có môn học nào.</td></tr>`;
        return;
    }

    tbody.innerHTML = result.data.map(item => `
        <tr>
            <td><strong>${escapeHtml(item.code)}</strong></td>
            <td>${escapeHtml(item.name)}</td>
            <td class="note-muted">${escapeHtml(item.description || 'Không có mô tả')}</td>
            <td><span class="status-badge ${item.isActive ? 'active' : 'inactive'}">${item.isActive ? 'Đang dùng' : 'Tạm ngưng'}</span></td>
            <td>
                <button type="button" class="btn-table" onclick="editSubject(${item.id})">Sửa</button>
                <button type="button" class="btn-table delete" onclick="deleteSubject(${item.id})">Xóa</button>
            </td>
        </tr>
    `).join('');
}

async function refreshDropdowns() {
    const [classesResult, teachersResult, subjectsResult] = await Promise.all([
        fetchJson('/Manager/Api/Classes'),
        fetchJson('/Manager/Api/Teachers'),
        fetchJson('/Manager/Api/Subjects')
    ]);

    fillSelect(document.getElementById('scheduleClassFilter'), classesResult.data || [], 'Chọn lớp');
    fillSelect(document.getElementById('scheduleClassId'), classesResult.data || [], 'Chọn lớp');
    fillSelect(document.getElementById('scheduleTeacherId'), teachersResult.data || [], 'Chọn giáo viên', 'id', item => item.fullName);
    fillSelect(
        document.getElementById('scheduleSubjectId'),
        (subjectsResult.data || []).filter(item => item.isActive),
        'Chọn môn học',
        'id',
        item => `${item.code} - ${item.name}`
    );

    if (!document.getElementById('scheduleClassFilter').value && classesResult.data && classesResult.data.length > 0) {
        const defaultClassId = classesResult.data[0].id;
        document.getElementById('scheduleClassFilter').value = defaultClassId;
        document.getElementById('scheduleClassId').value = defaultClassId;
        await loadSchedules(defaultClassId);
    } else if (document.getElementById('scheduleClassFilter').value) {
        await loadSchedules(parseInt(document.getElementById('scheduleClassFilter').value, 10));
    }
}

async function loadSchedules(classId) {
    const tbody = document.getElementById('scheduleTableBody');
    if (!classId) {
        tbody.innerHTML = `<tr><td colspan="7">Chọn một lớp để xem thời khóa biểu.</td></tr>`;
        return;
    }

    const result = await fetchJson(`/Manager/Api/ClassSchedules?classId=${classId}`);
    if (!result.success) {
        tbody.innerHTML = `<tr><td colspan="7">Không tải được thời khóa biểu.</td></tr>`;
        return;
    }

    if (result.data.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7">Lớp này chưa có thời khóa biểu.</td></tr>`;
        return;
    }

    tbody.innerHTML = result.data.map(item => `
        <tr>
            <td>${escapeHtml(item.dayLabel)}</td>
            <td><strong>${item.startTime} - ${item.endTime}</strong></td>
            <td>${escapeHtml(item.subjectName)}</td>
            <td>${escapeHtml(item.teacherName)}</td>
            <td>${formatEffectiveRange(item.effectiveFrom, item.effectiveTo)}</td>
            <td class="note-muted">${escapeHtml(item.note || 'Không có')}</td>
            <td>
                <button type="button" class="btn-table" onclick="editSchedule(${item.id})">Sửa</button>
                <button type="button" class="btn-table delete" onclick="deleteSchedule(${item.id})">Xóa</button>
            </td>
        </tr>
    `).join('');
}

async function saveClass(event) {
    event.preventDefault();

    const payload = {
        name: document.getElementById('className').value.trim(),
        ageFrom: parseNullableInt(document.getElementById('ageFrom').value),
        ageTo: parseNullableInt(document.getElementById('ageTo').value),
        schoolYear: document.getElementById('schoolYear').value.trim() || null
    };

    const isEdit = !!currentClassId;
    const url = isEdit ? `/Manager/Api/Class/${currentClassId}` : '/Manager/Api/Class';
    const method = isEdit ? 'PUT' : 'POST';

    const result = await sendJson(url, method, payload);
    showAlert('classAlert', result.success, result.message || 'Không thể lưu lớp học.');

    if (result.success) {
        resetClassForm();
        await Promise.all([loadClassesOverview(), refreshDropdowns()]);
    }
}

async function saveSubject(event) {
    event.preventDefault();

    const payload = {
        code: document.getElementById('subjectCode').value.trim(),
        name: document.getElementById('subjectName').value.trim(),
        description: document.getElementById('subjectDescription').value.trim() || null,
        isActive: document.getElementById('subjectIsActive').checked
    };

    const isEdit = !!currentSubjectId;
    const url = isEdit ? `/Manager/Api/Subject/${currentSubjectId}` : '/Manager/Api/Subject';
    const method = isEdit ? 'PUT' : 'POST';

    const result = await sendJson(url, method, payload);
    showAlert('subjectAlert', result.success, result.message || 'Không thể lưu môn học.');

    if (result.success) {
        resetSubjectForm();
        await Promise.all([loadSubjects(), refreshDropdowns()]);
    }
}

async function saveSchedule(event) {
    event.preventDefault();

    const payload = {
        classId: parseInt(document.getElementById('scheduleClassId').value, 10),
        subjectId: parseInt(document.getElementById('scheduleSubjectId').value, 10),
        employeeId: parseInt(document.getElementById('scheduleTeacherId').value, 10),
        dayOfWeek: parseInt(document.getElementById('scheduleDayOfWeek').value, 10),
        startTime: document.getElementById('scheduleStartTime').value,
        endTime: document.getElementById('scheduleEndTime').value,
        effectiveFrom: document.getElementById('scheduleEffectiveFrom').value,
        effectiveTo: document.getElementById('scheduleEffectiveTo').value || null,
        note: document.getElementById('scheduleNote').value.trim() || null,
        isActive: document.getElementById('scheduleIsActive').value === 'true'
    };

    const isEdit = !!currentScheduleId;
    const url = isEdit ? `/Manager/Api/ClassSchedule/${currentScheduleId}` : '/Manager/Api/ClassSchedule';
    const method = isEdit ? 'PUT' : 'POST';

    const result = await sendJson(url, method, payload);
    showAlert('scheduleAlert', result.success, result.message || 'Không thể lưu thời khóa biểu.');

    if (result.success) {
        const selectedClassId = parseInt(document.getElementById('scheduleClassId').value, 10);
        resetScheduleForm(selectedClassId);
        document.getElementById('scheduleClassFilter').value = selectedClassId;
        await loadSchedules(selectedClassId);
    }
}

async function editClass(classId) {
    const result = await fetchJson(`/Manager/Api/Class/${classId}`);
    if (!result.success) {
        showAlert('classAlert', false, result.message || 'Không tải được lớp học.');
        return;
    }

    currentClassId = classId;
    document.getElementById('classId').value = classId;
    document.getElementById('className').value = result.data.name || '';
    document.getElementById('schoolYear').value = result.data.schoolYear || '';
    document.getElementById('ageFrom').value = result.data.ageFrom || '';
    document.getElementById('ageTo').value = result.data.ageTo || '';
    document.getElementById('saveClassBtn').textContent = 'Cập nhật lớp học';
}

async function editSubject(subjectId) {
    const result = await fetchJson(`/Manager/Api/Subject/${subjectId}`);
    if (!result.success) {
        showAlert('subjectAlert', false, result.message || 'Không tải được môn học.');
        return;
    }

    currentSubjectId = subjectId;
    document.getElementById('subjectId').value = subjectId;
    document.getElementById('subjectCode').value = result.data.code || '';
    document.getElementById('subjectName').value = result.data.name || '';
    document.getElementById('subjectDescription').value = result.data.description || '';
    document.getElementById('subjectIsActive').checked = !!result.data.isActive;
    document.getElementById('saveSubjectBtn').textContent = 'Cập nhật môn học';
}

async function editSchedule(scheduleId) {
    const result = await fetchJson(`/Manager/Api/ClassSchedule/${scheduleId}`);
    if (!result.success) {
        showAlert('scheduleAlert', false, result.message || 'Không tải được thời khóa biểu.');
        return;
    }

    currentScheduleId = scheduleId;
    const data = result.data;
    document.getElementById('scheduleId').value = scheduleId;
    document.getElementById('scheduleClassId').value = data.classId;
    document.getElementById('scheduleClassFilter').value = data.classId;
    document.getElementById('scheduleSubjectId').value = data.subjectId;
    document.getElementById('scheduleTeacherId').value = data.employeeId;
    document.getElementById('scheduleDayOfWeek').value = data.dayOfWeek;
    document.getElementById('scheduleStartTime').value = data.startTime;
    document.getElementById('scheduleEndTime').value = data.endTime;
    document.getElementById('scheduleEffectiveFrom').value = data.effectiveFrom;
    document.getElementById('scheduleEffectiveTo').value = data.effectiveTo || '';
    document.getElementById('scheduleNote').value = data.note || '';
    document.getElementById('scheduleIsActive').value = data.isActive ? 'true' : 'false';
    document.getElementById('saveScheduleBtn').textContent = 'Cập nhật thời khóa biểu';

    await loadSchedules(data.classId);
}

async function deleteClass(classId) {
    if (!confirm('Bạn có chắc muốn xóa lớp học này?')) {
        return;
    }

    const result = await fetchJson(`/Manager/Api/Class/${classId}`, { method: 'DELETE' });
    showAlert('classAlert', result.success, result.message || 'Không thể xóa lớp học.');
    if (result.success) {
        resetClassForm();
        await Promise.all([loadClassesOverview(), refreshDropdowns()]);
    }
}

async function deleteSubject(subjectId) {
    if (!confirm('Bạn có chắc muốn xóa môn học này?')) {
        return;
    }

    const result = await fetchJson(`/Manager/Api/Subject/${subjectId}`, { method: 'DELETE' });
    showAlert('subjectAlert', result.success, result.message || 'Không thể xóa môn học.');
    if (result.success) {
        resetSubjectForm();
        await Promise.all([loadSubjects(), refreshDropdowns()]);
    }
}

async function deleteSchedule(scheduleId) {
    if (!confirm('Bạn có chắc muốn xóa thời khóa biểu này?')) {
        return;
    }

    const currentClass = parseInt(document.getElementById('scheduleClassFilter').value || '0', 10);
    const result = await fetchJson(`/Manager/Api/ClassSchedule/${scheduleId}`, { method: 'DELETE' });
    showAlert('scheduleAlert', result.success, result.message || 'Không thể xóa thời khóa biểu.');
    if (result.success) {
        resetScheduleForm(currentClass);
        await loadSchedules(currentClass);
    }
}

function selectScheduleClass(classId) {
    document.getElementById('scheduleClassFilter').value = classId;
    document.getElementById('scheduleClassId').value = classId;
    loadSchedules(classId);
    window.scrollTo({ top: document.body.scrollHeight, behavior: 'smooth' });
}

function resetClassForm() {
    currentClassId = null;
    document.getElementById('classForm').reset();
    document.getElementById('classId').value = '';
    document.getElementById('saveClassBtn').textContent = 'Lưu lớp học';
}

function resetSubjectForm() {
    currentSubjectId = null;
    document.getElementById('subjectForm').reset();
    document.getElementById('subjectId').value = '';
    document.getElementById('subjectIsActive').checked = true;
    document.getElementById('saveSubjectBtn').textContent = 'Lưu môn học';
}

function resetScheduleForm(classId = null) {
    currentScheduleId = null;
    document.getElementById('scheduleForm').reset();
    document.getElementById('scheduleId').value = '';
    document.getElementById('scheduleEffectiveFrom').value = new Date().toISOString().split('T')[0];
    document.getElementById('scheduleStartTime').value = '07:00';
    document.getElementById('scheduleEndTime').value = '08:00';
    document.getElementById('scheduleIsActive').value = 'true';
    document.getElementById('saveScheduleBtn').textContent = 'Lưu thời khóa biểu';

    const selectedClassId = classId || parseInt(document.getElementById('scheduleClassFilter').value || '0', 10);
    if (selectedClassId) {
        document.getElementById('scheduleClassId').value = selectedClassId;
    }
}

function fillSelect(selectElement, data, placeholder, valueKey = 'id', textResolver = item => item.name) {
    selectElement.innerHTML = `<option value="">${placeholder}</option>`;
    data.forEach(item => {
        const option = document.createElement('option');
        option.value = item[valueKey];
        option.textContent = textResolver(item);
        selectElement.appendChild(option);
    });
}

function showAlert(elementId, success, message) {
    const alert = document.getElementById(elementId);
    alert.className = `page-alert ${success ? 'success' : 'error'}`;
    alert.textContent = message;
    alert.style.display = 'block';
}

function renderTeacherTags(teachers) {
    if (!teachers || teachers.length === 0) {
        return 'Chưa phân công';
    }
    return teachers.map(item => `${escapeHtml(item.teacherName)} (${escapeHtml(item.roleInClass)})`).join('<br />');
}

function formatAgeRange(from, to) {
    if (!from && !to) return 'Chưa cập nhật';
    if (from && to) return `${from} - ${to} tuổi`;
    if (from) return `Từ ${from} tuổi`;
    return `Đến ${to} tuổi`;
}

function formatEffectiveRange(from, to) {
    if (!from) return 'Không xác định';
    return to ? `${formatDate(from)} - ${formatDate(to)}` : `Từ ${formatDate(from)}`;
}

function formatDate(value) {
    if (!value) return '';
    const parts = value.split('-');
    return `${parts[2]}/${parts[1]}/${parts[0]}`;
}

function parseNullableInt(value) {
    const parsed = parseInt(value, 10);
    return Number.isNaN(parsed) ? null : parsed;
}

async function sendJson(url, method, payload) {
    return fetchJson(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });
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
