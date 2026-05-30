let currentClassId = null;
let currentSubjectId = null;
let currentScheduleId = null;
let showInactiveClasses = false;
let showInactiveSubjects = false;

const ACTIVITY_SLOTS = [
    { start: '07:00', end: '08:30' },
    { start: '08:30', end: '10:00' },
    { start: '10:00', end: '11:00' },
    { start: '11:00', end: '14:00' },
    { start: '14:00', end: '15:30' },
    { start: '15:30', end: '17:00' }
];

document.addEventListener('DOMContentLoaded', () => {
    bindClassManagementEvents();
    initializeClassManagementPage();
});

async function initializeClassManagementPage() {
    const scheduleEffectiveFrom = document.getElementById('scheduleEffectiveFrom');
    if (scheduleEffectiveFrom) {
        scheduleEffectiveFrom.value = new Date().toISOString().split('T')[0];
    }

    const loaders = [];
    if (document.getElementById('classesTableBody')) loaders.push(loadClassesOverview());
    if (document.getElementById('subjectsTableBody')) loaders.push(loadSubjects());
    if (document.getElementById('scheduleClassFilter')) loaders.push(refreshDropdowns());

    if (loaders.length > 0) {
        await Promise.all(loaders);
    }
    
    // Khởi tạo pagination sau khi dữ liệu đã load
    if (typeof initPagination === 'function') {
        if (document.getElementById('classesTable')) initPagination('classesTable', 10);
        if (document.getElementById('subjectsTable')) initPagination('subjectsTable', 10);
        if (document.getElementById('scheduleTable')) initPagination('scheduleTable', 10);
    }
}

function bindClassManagementEvents() {
    const classForm = document.getElementById('classForm');
    if (classForm) classForm.addEventListener('submit', saveClass);

    const subjectForm = document.getElementById('subjectForm');
    if (subjectForm) subjectForm.addEventListener('submit', saveSubject);

    const scheduleForm = document.getElementById('scheduleForm');
    if (scheduleForm) scheduleForm.addEventListener('submit', saveSchedule);

    const clearClassBtn = document.getElementById('clearClassBtn');
    if (clearClassBtn) clearClassBtn.addEventListener('click', resetClassForm);

    const clearSubjectBtn = document.getElementById('clearSubjectBtn');
    if (clearSubjectBtn) clearSubjectBtn.addEventListener('click', resetSubjectForm);

    const resetClassFormBtn = document.getElementById('resetClassFormBtn');
    if (resetClassFormBtn) {
        resetClassFormBtn.addEventListener('click', () => {
            resetClassForm();
            openPanel();
        });
    }

    const resetSubjectFormBtn = document.getElementById('resetSubjectFormBtn');
    if (resetSubjectFormBtn) {
        resetSubjectFormBtn.addEventListener('click', () => {
            resetSubjectForm();
            openPanel();
        });
    }

    const closePanelBtn = document.getElementById('closePanelBtn');
    if (closePanelBtn) closePanelBtn.addEventListener('click', closePanel);

    const modalOverlay = document.getElementById('modalOverlay');
    if (modalOverlay) modalOverlay.addEventListener('click', closePanel);

    const deleteScheduleBtn = document.getElementById('deleteScheduleBtn');
    if (deleteScheduleBtn) deleteScheduleBtn.addEventListener('click', () => {
        const id = document.getElementById('scheduleId').value;
        if (id) deleteSchedule(parseInt(id, 10));
    });

    const scheduleClassFilter = document.getElementById('scheduleClassFilter');
    if (scheduleClassFilter) {
        scheduleClassFilter.addEventListener('change', (event) => {
            const classId = parseInt(event.target.value || '0', 10);
            const scheduleClassIdInput = document.getElementById('scheduleClassId');
            if (scheduleClassIdInput) scheduleClassIdInput.value = classId || '';
            loadSchedules(classId);
        });
    }

    const scheduleClassIdInput = document.getElementById('scheduleClassId');
    if (scheduleClassIdInput) {
        scheduleClassIdInput.addEventListener('change', (event) => {
            const classId = parseInt(event.target.value || '0', 10);
            const filter = document.getElementById('scheduleClassFilter');
            if (filter) filter.value = classId || '';
        });
    }

    const scheduleModal = document.getElementById('scheduleModal');
    if (scheduleModal) {
        scheduleModal.addEventListener('click', (e) => {
            if (e.target.id === 'scheduleModal') closeScheduleModal();
        });
    }

    // Status Tabs - Classes (no scope or scope=classes)
    document.querySelectorAll(".status-tab:not([data-scope])").forEach((tab) => {
        tab.addEventListener("click", function () {
            document
                .querySelectorAll(".status-tab:not([data-scope])")
                .forEach((t) => t.classList.remove("active"));
            this.classList.add("active");
            showInactiveClasses = this.getAttribute("data-show-inactive") === "true";
            loadClassesOverview();
        });
    });

    // Status Tabs - Subjects (scope=subjects)
    document.querySelectorAll('.status-tab[data-scope="subjects"]').forEach((tab) => {
        tab.addEventListener("click", function () {
            document
                .querySelectorAll('.status-tab[data-scope="subjects"]')
                .forEach((t) => t.classList.remove("active"));
            this.classList.add("active");
            showInactiveSubjects = this.getAttribute("data-show-inactive") === "true";
            loadSubjects();
        });
    });
}

async function loadClassesOverview() {
    const result = await fetchJson(`/Manager/Api/Classes/Overview?showInactive=${showInactiveClasses}`);
    const tbody = document.getElementById('classesTableBody');

    if (!result.success) {
        tbody.innerHTML = `<tr><td colspan="6">Không tải được dữ liệu lớp học.</td></tr>`;
        return;
    }

    if (result.data.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6">Chưa có lớp học nào.</td></tr>`;
        return;
    }

    tbody.innerHTML = result.data.map(item => {
        const actionBtns = item.isActive 
            ? `
                <button type="button" class="btn-table" onclick="editClass(${item.id})">Sửa</button>
                <button type="button" class="btn-table delete" onclick="deleteClass(${item.id})">Đóng</button>
            `
            : `
                <button type="button" class="btn-table" onclick="reactivateClass(${item.id})" style="color: var(--primary);">Khôi phục</button>
            `;

        return `
        <tr>
            <td><strong>${escapeHtml(item.name || '')}</strong></td>
            <td>${formatAgeRange(item.ageFrom, item.ageTo)}</td>
            <td>${escapeHtml(item.schoolYear || 'Chưa cập nhật')}</td>
            <td>
                <span class="capacity-badge ${item.studentCount >= item.maxCapacity ? 'full' : ''}">
                    ${item.studentCount} / ${item.maxCapacity}
                </span>
            </td>
            <td class="teacher-tags">${renderTeacherTags(item.teachers)}</td>
            <td>
                ${actionBtns}
            </td>
        </tr>
    `}).join('');
}

async function loadSubjects() {
    const result = await fetchJson(`/Manager/Api/Subjects?showInactive=${showInactiveSubjects}`);
    const tbody = document.getElementById('subjectsTableBody');

    if (!result.success) {
        tbody.innerHTML = `<tr><td colspan="5">Không tải được dữ liệu môn học.</td></tr>`;
        return;
    }

    if (result.data.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted" style="padding:24px;">Không có dữ liệu.</td></tr>`;
        return;
    }

    tbody.innerHTML = result.data.map(item => {
        const actionBtns = item.isActive
            ? `<button type="button" class="btn-table" onclick="editSubject(${item.id})">Sửa</button>
               <button type="button" class="btn-table delete" onclick="deleteSubject(${item.id})">Ẩn</button>`
            : `<button type="button" class="btn-table" onclick="reactivateSubject(${item.id})" style="color:var(--primary);">Khôi phục</button>`;

        return `<tr>
            <td><strong>${escapeHtml(item.code)}</strong></td>
            <td>${escapeHtml(item.name)}</td>
            <td class="note-muted">${escapeHtml(item.description || 'Không có mô tả')}</td>
            <td><span class="status-badge ${item.isActive ? 'active' : 'inactive'}">${item.isActive ? 'Đang dùng' : 'Tạm ngưng'}</span></td>
            <td>${actionBtns}</td>
        </tr>`;
    }).join('');
}

async function refreshDropdowns() {
    const [classesResult, teachersResult, subjectsResult] = await Promise.all([
        fetchJson('/Manager/Api/Classes'),
        fetchJson('/Manager/Api/Teachers'),
        fetchJson('/Manager/Api/Subjects')
    ]);

    fillSelect(document.getElementById('scheduleClassFilter'), classesResult.data || [], 'Chọn lớp');
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
    const tbody = document.getElementById('scheduleGridBody');
    if (!classId) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; padding: 40px; color: var(--text-muted);">Vui lòng chọn một lớp để xem thời khóa biểu.</td></tr>`;
        return;
    }

    const result = await fetchJson(`/Manager/Api/ClassSchedules?classId=${classId}`);
    if (!result.success) {
        tbody.innerHTML = `<tr><td colspan="7">Không tải được thời khóa biểu.</td></tr>`;
        return;
    }

    const schedules = result.data;
    let html = '';
    const days = [
        { val: 1, label: 'Thứ 2' },
        { val: 2, label: 'Thứ 3' },
        { val: 3, label: 'Thứ 4' },
        { val: 4, label: 'Thứ 5' },
        { val: 5, label: 'Thứ 6' }
    ];

    days.forEach(day => {
        html += `<tr>`;
        html += `<td class="day-label">${day.label}</td>`;
        
        ACTIVITY_SLOTS.forEach((slot, slotIdx) => {
            // Lấy tất cả các schedule khớp với ngày và khung giờ này
            const matches = schedules.filter(s => s.dayOfWeek === day.val && s.startTime >= slot.start && s.startTime < slot.end);
            
            // Tìm giờ kết thúc muộn nhất để gợi ý cho tiết tiếp theo
            const maxEndTime = matches.reduce((max, s) => s.endTime > max ? s.endTime : max, slot.start);
            
            html += `<td onclick="openScheduleModal(${day.val}, ${slotIdx}, false, '${maxEndTime}')">`;
            html += `<div class="slot-cell">`;
            
            if (matches.length > 0) {
                matches.forEach(match => {
                    html += `
                        <div class="assignment-block" onclick="editSchedule(${match.id}); event.stopPropagation();">
                            <div class="time">${match.startTime} - ${match.endTime}</div>
                            <div class="subject" title="${escapeHtml(match.subjectName)}">${escapeHtml(match.subjectName)}</div>
                            <div class="teacher">${escapeHtml(match.teacherName)}</div>
                        </div>`;
                });
            } else {
                html += `<div class="add-icon"><i class="fa-solid fa-plus"></i></div>`;
            }
            
            html += `</div></td>`;
        });
        
        html += `</tr>`;
    });

    tbody.innerHTML = html;
}

async function saveClass(event) {
    event.preventDefault();

    const ageRange = document.getElementById('ageRange').value;
    const [ageFrom, ageTo] = ageRange ? ageRange.split('-').map(Number) : [null, null];

    const payload = {
        name: document.getElementById('className').value.trim(),
        ageFrom: ageFrom,
        ageTo: ageTo,
        maxCapacity: parseNullableInt(document.getElementById('maxCapacity').value) || 25,
        schoolYear: document.getElementById('schoolYear').value.trim() || null
    };

    const isEdit = !!currentClassId;
    const url = isEdit ? `/Manager/Api/Class/${currentClassId}` : '/Manager/Api/Class';
    const method = isEdit ? 'PUT' : 'POST';

    const result = await sendJson(url, method, payload);
    showAlert('classAlert', result.success, result.message || 'Không thể lưu lớp học.');

    if (result.success) {
        resetClassForm();
        closePanel();
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
        closePanel();
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
    if (result.success) {
        closeScheduleModal();
        const selectedClassId = parseInt(document.getElementById('scheduleClassFilter').value, 10);
        await loadSchedules(selectedClassId);
    } else {
        alert(result.message || 'Không thể lưu thời khóa biểu.');
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
    
    if (result.data.ageFrom && result.data.ageTo) {
        document.getElementById('ageRange').value = `${result.data.ageFrom}-${result.data.ageTo}`;
    } else {
        document.getElementById('ageRange').value = '';
    }

    document.getElementById('maxCapacity').value = result.data.maxCapacity || 25;
    
    const panelTitle = document.getElementById('panelTitle');
    if (panelTitle) panelTitle.textContent = 'Chỉnh sửa lớp học';
    document.getElementById('saveClassBtn').textContent = 'Cập nhật lớp học';
    openPanel();
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
    const isActiveCheckbox = document.getElementById('subjectIsActive');
    if (isActiveCheckbox) isActiveCheckbox.checked = !!result.data.isActive;
    
    const panelTitle = document.getElementById('panelTitle');
    if (panelTitle) panelTitle.textContent = 'Chỉnh sửa môn học';
    document.getElementById('saveSubjectBtn').textContent = 'Cập nhật môn học';
    openPanel();
}

async function editSchedule(scheduleId) {
    const result = await fetchJson(`/Manager/Api/ClassSchedule/${scheduleId}`);
    if (!result.success) {
        alert(result.message || 'Không tải được thời khóa biểu.');
        return;
    }

    currentScheduleId = scheduleId;
    const data = result.data;
    
    openScheduleModal(data.dayOfWeek, -1, true); // -1 nghĩa là không set lại giờ từ slot
    
    document.getElementById('scheduleId').value = scheduleId;
    document.getElementById('scheduleClassId').value = data.classId;
    document.getElementById('scheduleSubjectId').value = data.subjectId;
    document.getElementById('scheduleTeacherId').value = data.employeeId;
    document.getElementById('scheduleDayOfWeek').value = data.dayOfWeek;
    document.getElementById('scheduleStartTime').value = data.startTime;
    document.getElementById('scheduleEndTime').value = data.endTime;
    document.getElementById('scheduleEffectiveFrom').value = data.effectiveFrom;
    document.getElementById('scheduleEffectiveTo').value = data.effectiveTo || '';
    document.getElementById('scheduleNote').value = data.note || '';
    document.getElementById('scheduleIsActive').value = data.isActive ? 'true' : 'false';
    document.getElementById('saveScheduleBtn').textContent = 'Cập nhật phân công';
    document.getElementById('deleteScheduleBtn').style.display = 'block';
    document.getElementById('modalTitle').textContent = 'Chỉnh sửa phân công';
}

function openScheduleModal(day, slotIdx, isEdit = false, suggestedStartTime = null) {
    const classId = parseInt(document.getElementById('scheduleClassFilter').value || '0', 10);
    if (!classId) {
        alert('Vui lòng chọn lớp trước khi xếp lịch.');
        return;
    }

    if (!isEdit) {
        resetScheduleForm();
        document.getElementById('scheduleClassId').value = classId;
        document.getElementById('scheduleDayOfWeek').value = day;
        
        const start = suggestedStartTime || (slotIdx >= 0 ? ACTIVITY_SLOTS[slotIdx].start : '07:00');
        document.getElementById('scheduleStartTime').value = start;
        
        // Tự động tính giờ kết thúc (mặc định 45 phút sau)
        const [h, m] = start.split(':').map(Number);
        const endMin = h * 60 + m + 45;
        const endH = Math.floor(endMin / 60).toString().padStart(2, '0');
        const endM = (endMin % 60).toString().padStart(2, '0');
        const endStr = `${endH}:${endM}`;
        
        // Nếu giờ kết thúc vượt quá giới hạn slot, thì dùng giờ kết thúc của slot
        const slotEnd = slotIdx >= 0 ? ACTIVITY_SLOTS[slotIdx].end : '17:00';
        document.getElementById('scheduleEndTime').value = endStr > slotEnd ? slotEnd : endStr;

        document.getElementById('saveScheduleBtn').textContent = 'Lưu phân công';
        document.getElementById('deleteScheduleBtn').style.display = 'none';
        document.getElementById('modalTitle').textContent = 'Phân công tiết học mới';
    }

    document.getElementById('scheduleModal').style.display = 'flex';
}

function closeScheduleModal() {
    document.getElementById('scheduleModal').style.display = 'none';
}

async function deleteClass(classId) {
    if (!confirm('Bạn có chắc muốn đóng lớp học này? Tất cả các phân công giáo viên liên quan cũng sẽ tạm ngưng.')) {
        return;
    }

    const result = await fetchJson(`/Manager/Api/Class/${classId}`, { method: 'DELETE' });
    showAlert('classAlert', result.success, result.message || 'Không thể xóa lớp học.');
    if (result.success) {
        resetClassForm();
        await Promise.all([loadClassesOverview(), refreshDropdowns()]);
    }
}

async function reactivateClass(classId) {
    if (!confirm('Bạn có chắc muốn khôi phục lớp học này?')) {
        return;
    }

    const result = await fetchJson(`/Manager/Api/Class/Reactivate/${classId}`, { method: 'POST' });
    showAlert('classAlert', result.success, result.message || 'Không thể khôi phục lớp học.');
    if (result.success) {
        await Promise.all([loadClassesOverview(), refreshDropdowns()]);
    }
}

async function deleteSubject(subjectId) {
    if (!confirm('Bạn có chắc muốn ẩn môn học này?')) {
        return;
    }

    const result = await fetchJson(`/Manager/Api/Subject/${subjectId}`, { method: 'DELETE' });
    showAlert('subjectAlert', result.success, result.message || 'Không thể xóa môn học.');
    if (result.success) {
        resetSubjectForm();
        await Promise.all([loadSubjects(), refreshDropdowns()]);
    }
}

async function reactivateSubject(subjectId) {
    if (!confirm('Bạn có chắc muốn khôi phục môn học này?')) {
        return;
    }

    const result = await fetchJson(`/Manager/Api/Subject/Reactivate/${subjectId}`, { method: 'POST' });
    showAlert('subjectAlert', result.success, result.message || 'Không thể khôi phục môn học.');
    if (result.success) {
        await Promise.all([loadSubjects(), refreshDropdowns()]);
    }
}

async function deleteSchedule(scheduleId) {
    if (!confirm('Bạn có chắc muốn xóa phân công tiết học này?')) {
        return;
    }

    const currentClass = parseInt(document.getElementById('scheduleClassFilter').value || '0', 10);
    const result = await fetchJson(`/Manager/Api/ClassSchedule/${scheduleId}`, { method: 'DELETE' });
    if (result.success) {
        closeScheduleModal();
        await loadSchedules(currentClass);
    } else {
        alert(result.message || 'Không thể xóa thời khóa biểu.');
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
    const form = document.getElementById('classForm');
    if (form) {
        form.reset();
        document.getElementById('classId').value = '';
        document.getElementById('saveClassBtn').textContent = 'Lưu lớp học';
        const panelTitle = document.getElementById('panelTitle');
        if (panelTitle) panelTitle.textContent = 'Thêm lớp học mới';
    }
}

function resetSubjectForm() {
    currentSubjectId = null;
    const form = document.getElementById('subjectForm');
    if (form) {
        form.reset();
        document.getElementById('subjectId').value = '';
        const isActiveCheckbox = document.getElementById('subjectIsActive');
        if (isActiveCheckbox) isActiveCheckbox.checked = true;
        document.getElementById('saveSubjectBtn').textContent = 'Lưu môn học';
        const panelTitle = document.getElementById('panelTitle');
        if (panelTitle) panelTitle.textContent = 'Thêm môn học mới';
    }
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
        return '<div class="teacher-tags-empty">Chưa phân công</div>';
    }

    return teachers.map(item => {
        const role = escapeHtml(item.roleInClass || 'Giáo viên');
        const isLead = /chủ nhiệm/i.test(item.roleInClass || '');
        const badgeClass = isLead ? 'badge-success' : 'badge-info';

        return `
            <div class="teacher-tag">
                <span class="teacher-tag-name">${escapeHtml(item.teacherName)}</span>
                <span class="badge ${badgeClass}">${role}</span>
            </div>`;
    }).join('');
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

async function fetchJson(url, options = {}) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    options.headers = options.headers || {};
    if (token) {
        options.headers['RequestVerificationToken'] = token;
    }

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

function openPanel() {
    const overlay = document.getElementById("modalOverlay");
    const panel = document.getElementById("slidePanel");
    if (overlay && panel) {
        overlay.classList.add("active");
        panel.classList.add("active");
        document.body.style.overflow = "hidden";
    }
}

function closePanel() {
    const overlay = document.getElementById("modalOverlay");
    const panel = document.getElementById("slidePanel");
    if (overlay && panel) {
        overlay.classList.remove("active");
        panel.classList.remove("active");
        document.body.style.overflow = "auto";
    }
}
