let currentClassId = null;
let currentSubjectId = null;
let currentScheduleId = null;
let showInactiveClasses = false;
let showInactiveSubjects = false;
let allClassTeachers = [];
let allClassAssignments = [];
let allScheduleSubjects = [];

const PLAY_SUBJECT_NAME = 'Hoạt động vui chơi';
const PLAY_SUBJECT_CREATE_MESSAGE = 'Vui lòng tạo môn học "Hoạt động vui chơi" trong tab Danh mục môn học trước khi xếp lịch khung 10:00 - 11:00.';

const ACTIVITY_SLOTS = [
    { start: '06:45', end: '07:30', locked: true, label: 'Đón trẻ & Ăn sáng' },
    { start: '07:30', end: '10:00', locked: false },
    { start: '10:00', end: '11:00', locked: false, play: true },
    { start: '11:00', end: '14:00', locked: true, label: 'Ăn & Ngủ trưa' },
    { start: '14:00', end: '16:30', locked: false },
    { start: '16:30', end: '17:00', locked: true, label: 'Trả trẻ' }
];

document.addEventListener('DOMContentLoaded', () => {
    bindClassManagementEvents();
    updateScheduleHeaderSlots();
    initializeClassManagementPage();
});

function updateScheduleHeaderSlots() {
    const headerCells = document.querySelectorAll('.timetable-grid thead th');
    if (headerCells.length < ACTIVITY_SLOTS.length + 1) return;

    const labels = [
        'Đón trẻ & Ăn sáng',
        'Học chính buổi sáng',
        'Vui chơi',
        'Ăn & Ngủ trưa',
        'Học chính buổi chiều',
        'Trả trẻ'
    ];

    ACTIVITY_SLOTS.forEach((slot, index) => {
        headerCells[index + 1].innerHTML = `${labels[index]}<br/><small>${slot.start} - ${slot.end}</small>`;
    });
}

function updateScheduleTodayLabel() {
    const label = document.getElementById('scheduleTodayLabel');
    if (!label) return;

    const today = new Date();
    const day = today.getDate().toString().padStart(2, '0');
    const month = (today.getMonth() + 1).toString().padStart(2, '0');
    const year = today.getFullYear();
    label.textContent = `Ngày hôm nay: ${day}/${month}/${year}`;
}

async function initializeClassManagementPage() {
    updateScheduleTodayLabel();

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

    const saveClassTeacherBtn = document.getElementById('saveClassTeacherBtn');
    if (saveClassTeacherBtn) saveClassTeacherBtn.addEventListener('click', saveClassTeacherAssignment);

    const resetClassTeacherBtn = document.getElementById('resetClassTeacherBtn');
    if (resetClassTeacherBtn) resetClassTeacherBtn.addEventListener('click', resetClassTeacherForm);

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

    ['scheduleStartTime', 'scheduleEndTime'].forEach((id) => {
        const input = document.getElementById(id);
        if (input) {
            input.addEventListener('change', () => {
                applyPlaySubjectLock(
                    document.getElementById('scheduleStartTime')?.value,
                    document.getElementById('scheduleEndTime')?.value
                );
            });
        }
    });

    const scheduleModal = document.getElementById('scheduleModal');
    if (scheduleModal) {
        scheduleModal.addEventListener('click', (e) => {
            if (e.target.id === 'scheduleModal') closeScheduleModal();
        });
    }

    // Status Tabs - Classes
    document.querySelectorAll('.status-tab[data-scope="classes"], .status-tab:not([data-scope])').forEach((tab) => {
        tab.addEventListener("click", function () {
            document
                .querySelectorAll('.status-tab[data-scope="classes"], .status-tab:not([data-scope])')
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
    const tbody = document.getElementById('classesTableBody');
    if (window.appLoading && tbody) {
        window.appLoading.setTable(tbody, 6);
    }
    const result = await fetchJson(`/Manager/Api/Classes/Overview?showInactive=${showInactiveClasses}`);

    if (!result.success) {
        tbody.innerHTML = window.appLoading
            ? window.appLoading.tableError(6, "Kh\u00f4ng t\u1ea3i \u0111\u01b0\u1ee3c d\u1eef li\u1ec7u l\u1edbp h\u1ecdc.")
            : `<tr><td colspan="6">Không tải được dữ liệu lớp học.</td></tr>`;
        return;
    }

    if (result.data.length === 0) {
        tbody.innerHTML = window.appLoading
            ? window.appLoading.tableEmpty(6, "Ch\u01b0a c\u00f3 l\u1edbp h\u1ecdc n\u00e0o.")
            : `<tr><td colspan="6">Chưa có lớp học nào.</td></tr>`;
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
    const tbody = document.getElementById('subjectsTableBody');
    if (window.appLoading && tbody) {
        window.appLoading.setTable(tbody, 5);
    }
    const result = await fetchJson(`/Manager/Api/Subjects?showInactive=${showInactiveSubjects}`);

    if (!result.success) {
        tbody.innerHTML = window.appLoading
            ? window.appLoading.tableError(5, "Kh\u00f4ng t\u1ea3i \u0111\u01b0\u1ee3c d\u1eef li\u1ec7u m\u00f4n h\u1ecdc.")
            : `<tr><td colspan="5">Không tải được dữ liệu môn học.</td></tr>`;
        return;
    }

    if (result.data.length === 0) {
        tbody.innerHTML = window.appLoading
            ? window.appLoading.tableEmpty(5, "Kh\u00f4ng c\u00f3 d\u1eef li\u1ec7u.")
            : `<tr><td colspan="5" class="text-center text-muted" style="padding:24px;">Không có dữ liệu.</td></tr>`;
        return;
    }

    tbody.innerHTML = result.data.map(item => {
        const actionBtns = item.isActive
            ? `<button type="button" class="btn-table" onclick="editSubject(${item.id})">Sửa</button>
               <button type="button" class="btn-table delete" onclick="deleteSubject(${item.id})">Ẩn</button>`
            : `<button type="button" class="btn-table" onclick="reactivateSubject(${item.id})" style="color:var(--primary);">Khôi phục</button>`;

        return `<tr>
            <td>${escapeHtml(item.id)}</td>
            <td>${escapeHtml(item.name)}</td>
            <td class="note-muted">${escapeHtml(item.description || 'Không có mô tả')}</td>
            <td><span class="status-badge ${item.isActive ? 'active' : 'inactive'}">${item.isActive ? 'Đang dùng' : 'Tạm ngưng'}</span></td>
            <td>${actionBtns}</td>
        </tr>`;
    }).join('');
}

async function refreshDropdowns() {
    const [classesResult, subjectsResult] = await Promise.all([
        fetchJson('/Manager/Api/Classes'),
        fetchJson('/Manager/Api/Subjects')
    ]);
    allScheduleSubjects = (subjectsResult.data || []).filter(item => item.isActive);

    fillSelect(document.getElementById('scheduleClassFilter'), classesResult.data || [], 'Chọn lớp');
    fillSelect(
        document.getElementById('scheduleSubjectId'),
        allScheduleSubjects,
        'Chọn môn học',
        'id',
        item => item.name
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
        tbody.innerHTML = window.appLoading
            ? window.appLoading.tableEmpty(7, "Vui l\u00f2ng ch\u1ecdn m\u1ed9t l\u1edbp \u0111\u1ec3 xem th\u1eddi kh\u00f3a bi\u1ec3u.")
            : `<tr><td colspan="7" style="text-align:center; padding: 40px; color: var(--text-muted);">Vui lòng chọn một lớp để xem thời khóa biểu.</td></tr>`;
        return;
    }

    if (window.appLoading) {
        window.appLoading.setTable(tbody, 7);
    }
    const result = await fetchJson(`/Manager/Api/ClassSchedules?classId=${classId}`);
    if (!result.success) {
        tbody.innerHTML = window.appLoading
            ? window.appLoading.tableError(7, "Kh\u00f4ng t\u1ea3i \u0111\u01b0\u1ee3c th\u1eddi kh\u00f3a bi\u1ec3u.")
            : `<tr><td colspan="7">Không tải được thời khóa biểu.</td></tr>`;
        return;
    }

    const schedules = result.data;
    let html = '';
    const days = [
        { val: 1, label: 'Thứ 2' },
        { val: 2, label: 'Thứ 3' },
        { val: 3, label: 'Thứ 4' },
        { val: 4, label: 'Thứ 5' },
        { val: 5, label: 'Thứ 6' },
        { val: 6, label: 'Thứ 7' }
    ];

    days.forEach(day => {
        html += `<tr>`;
        html += `<td class="day-label">${day.label}</td>`;
        
        ACTIVITY_SLOTS.forEach((slot, slotIdx) => {
            // Lấy tất cả các schedule khớp với ngày và khung giờ này
            const matches = schedules.filter(s => s.dayOfWeek === day.val && s.startTime >= slot.start && s.startTime < slot.end);
            
            // Tìm giờ kết thúc muộn nhất để gợi ý cho tiết tiếp theo
            const maxEndTime = matches.reduce((max, s) => s.endTime > max ? s.endTime : max, slot.start);
            
            const lockedAttrs = slot.locked ? ' class="locked-slot"' : ` onclick="openScheduleModal(${day.val}, ${slotIdx}, false, '${maxEndTime}')"`;
            html += `<td${lockedAttrs}>`;
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
            } else if (slot.locked) {
                html += `<div class="locked-slot-content">${escapeHtml(slot.label || 'Nghỉ')}</div>`;
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
    const schoolYear = document.getElementById('schoolYear').value.trim();
    const schoolYearError = validateSchoolYear(schoolYear);
    if (schoolYearError) {
        showAlert('classFormAlert', false, schoolYearError);
        return;
    }

    const payload = {
        name: document.getElementById('className').value.trim(),
        ageFrom: ageFrom,
        ageTo: ageTo,
        maxCapacity: parseNullableInt(document.getElementById('maxCapacity').value) || 25,
        schoolYear: schoolYear
    };

    const isEdit = !!currentClassId;
    const url = isEdit ? `/Manager/Api/Class/${currentClassId}` : '/Manager/Api/Class';
    const method = isEdit ? 'PUT' : 'POST';

    const result = await sendJson(url, method, payload);

    if (result.success) {
        resetClassForm();
        closePanel();
        showAlert('classAlert', true, result.message || 'Lưu lớp học thành công.');
        await Promise.all([loadClassesOverview(), refreshDropdowns()]);
    } else {
        showAlert('classFormAlert', false, result.message || 'Không thể lưu lớp học.');
    }
}

async function saveSubject(event) {
    event.preventDefault();

    const payload = {
        name: document.getElementById('subjectName').value.trim(),
        description: document.getElementById('subjectDescription').value.trim() || null,
        isActive: document.getElementById('subjectIsActive').checked
    };

    const isEdit = !!currentSubjectId;
    const url = isEdit ? `/Manager/Api/Subject/${currentSubjectId}` : '/Manager/Api/Subject';
    const method = isEdit ? 'PUT' : 'POST';

    const result = await sendJson(url, method, payload);

    if (result.success) {
        resetSubjectForm();
        closePanel();
        showAlert('subjectAlert', true, result.message || 'Lưu môn học thành công.');
        await Promise.all([loadSubjects(), refreshDropdowns()]);
    } else {
        showAlert('subjectFormAlert', false, result.message || 'Không thể lưu môn học.');
    }
}

async function saveSchedule(event) {
    event.preventDefault();

    const startTime = document.getElementById('scheduleStartTime').value;
    const endTime = document.getElementById('scheduleEndTime').value;
    const timeError = validateScheduleTimeRange(startTime, endTime);
    if (timeError) {
        showAlert('scheduleFormAlert', false, timeError);
        return;
    }
    const subjectError = validateScheduleSubjectForTime(startTime, endTime);
    if (subjectError) {
        showAlert('scheduleFormAlert', false, subjectError);
        return;
    }

    const payload = {
        classId: parseInt(document.getElementById('scheduleClassId').value, 10),
        subjectId: parseInt(document.getElementById('scheduleSubjectId').value, 10),
        dayOfWeek: parseInt(document.getElementById('scheduleDayOfWeek').value, 10),
        startTime,
        endTime,
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
        showAlert('scheduleAlert', true, result.message || 'Lưu thời khóa biểu thành công.');
        const selectedClassId = parseInt(document.getElementById('scheduleClassFilter').value, 10);
        await loadSchedules(selectedClassId);
    } else {
        showAlert('scheduleFormAlert', false, result.message || 'Không thể lưu thời khóa biểu.');
    }
}

async function editClass(classId) {
    const formAlert = document.getElementById('classFormAlert');
    if (formAlert) formAlert.style.display = 'none';

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
    await loadClassTeacherManagement(classId);
    openPanel();
}

async function editSubject(subjectId) {
    const formAlert = document.getElementById('subjectFormAlert');
    if (formAlert) formAlert.style.display = 'none';

    const result = await fetchJson(`/Manager/Api/Subject/${subjectId}`);
    if (!result.success) {
        showAlert('subjectAlert', false, result.message || 'Không tải được môn học.');
        return;
    }

    currentSubjectId = subjectId;
    document.getElementById('subjectId').value = subjectId;
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
    const formAlert = document.getElementById('scheduleFormAlert');
    if (formAlert) formAlert.style.display = 'none';

    const result = await fetchJson(`/Manager/Api/ClassSchedule/${scheduleId}`);
    if (!result.success) {
        window.notifyError(result.message || 'Không tải được thời khóa biểu.');
        return;
    }

    currentScheduleId = scheduleId;
    const data = result.data;
    
    openScheduleModal(data.dayOfWeek, -1, true); // -1 nghĩa là không set lại giờ từ slot
    
    document.getElementById('scheduleId').value = scheduleId;
    document.getElementById('scheduleClassId').value = data.classId;
    document.getElementById('scheduleSubjectId').value = data.subjectId;
    document.getElementById('scheduleDayOfWeek').value = data.dayOfWeek;
    document.getElementById('scheduleStartTime').value = data.startTime;
    document.getElementById('scheduleEndTime').value = data.endTime;
    document.getElementById('scheduleEffectiveFrom').value = data.effectiveFrom;
    document.getElementById('scheduleEffectiveTo').value = data.effectiveTo || '';
    document.getElementById('scheduleNote').value = data.note || '';
    document.getElementById('scheduleIsActive').value = data.isActive ? 'true' : 'false';
    applyPlaySubjectLock(data.startTime, data.endTime);
    document.getElementById('saveScheduleBtn').textContent = 'Cập nhật phân công';
    document.getElementById('deleteScheduleBtn').style.display = 'block';
    document.getElementById('modalTitle').textContent = 'Chỉnh sửa phân công';
}

function openScheduleModal(day, slotIdx, isEdit = false, suggestedStartTime = null) {
    const classId = parseInt(document.getElementById('scheduleClassFilter').value || '0', 10);
    if (!classId) {
        window.notifyWarning('Vui lòng chọn lớp trước khi xếp lịch.');
        return;
    }

    if (!isEdit && slotIdx >= 0 && ACTIVITY_SLOTS[slotIdx]?.locked) {
        const slot = ACTIVITY_SLOTS[slotIdx];
        showAlert('scheduleAlert', false, `Khung ${slot.start} - ${slot.end} là thời gian ${slot.label.toLowerCase()}, không xếp lịch.`);
        return;
    }

    if (!isEdit) {
        resetScheduleForm();
        document.getElementById('scheduleClassId').value = classId;
        document.getElementById('scheduleDayOfWeek').value = day;
        
        const start = suggestedStartTime || (slotIdx >= 0 ? ACTIVITY_SLOTS[slotIdx].start : '07:30');
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
        applyPlaySubjectLock(start, document.getElementById('scheduleEndTime').value);

        document.getElementById('saveScheduleBtn').textContent = 'Lưu phân công';
        document.getElementById('deleteScheduleBtn').style.display = 'none';
        document.getElementById('modalTitle').textContent = 'Phân công tiết học mới';
    }

    document.getElementById('scheduleModal').classList.add('active');
    document.getElementById('scheduleSlidePanel').classList.add('active');
    document.body.style.overflow = 'hidden';
}

function closeScheduleModal() {
    document.getElementById('scheduleModal').classList.remove('active');
    document.getElementById('scheduleSlidePanel').classList.remove('active');
    document.body.style.overflow = 'auto';
}

async function deleteClass(classId) {
    if (!(await window.appConfirm('Bạn có chắc muốn đóng lớp học này? Tất cả các phân công giáo viên liên quan cũng sẽ tạm ngưng.'))) {
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
    if (!(await window.appConfirm('Bạn có chắc muốn khôi phục lớp học này?'))) {
        return;
    }

    const result = await fetchJson(`/Manager/Api/Class/Reactivate/${classId}`, { method: 'POST' });
    showAlert('classAlert', result.success, result.message || 'Không thể khôi phục lớp học.');
    if (result.success) {
        await Promise.all([loadClassesOverview(), refreshDropdowns()]);
    }
}

async function deleteSubject(subjectId) {
    if (!(await window.appConfirm('Bạn có chắc muốn ẩn môn học này?'))) {
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
    if (!(await window.appConfirm('Bạn có chắc muốn khôi phục môn học này?'))) {
        return;
    }

    const result = await fetchJson(`/Manager/Api/Subject/Reactivate/${subjectId}`, { method: 'POST' });
    showAlert('subjectAlert', result.success, result.message || 'Không thể khôi phục môn học.');
    if (result.success) {
        await Promise.all([loadSubjects(), refreshDropdowns()]);
    }
}

async function deleteSchedule(scheduleId) {
    if (!(await window.appConfirm('Bạn có chắc muốn xóa phân công tiết học này?'))) {
        return;
    }

    const currentClass = parseInt(document.getElementById('scheduleClassFilter').value || '0', 10);
    const result = await fetchJson(`/Manager/Api/ClassSchedule/${scheduleId}`, { method: 'DELETE' });
    if (result.success) {
        closeScheduleModal();
        await loadSchedules(currentClass);
    } else {
        window.notifyError(result.message || 'Không thể xóa thời khóa biểu.');
    }
}

function selectScheduleClass(classId) {
    document.getElementById('scheduleClassFilter').value = classId;
    document.getElementById('scheduleClassId').value = classId;
    loadSchedules(classId);
    window.scrollTo({ top: document.body.scrollHeight, behavior: 'smooth' });
}

async function loadClassTeacherManagement(classId) {
    const section = document.getElementById('classTeachersSection');
    if (!section) return;
    section.style.display = 'block';

    const [teachersResult, assignmentsResult] = await Promise.all([
        fetchJson('/Manager/Api/Teachers'),
        fetchJson('/Manager/Api/Assignments')
    ]);

    allClassTeachers = teachersResult.success ? teachersResult.data : [];
    allClassAssignments = assignmentsResult.success ? assignmentsResult.data : [];
    fillClassTeacherSelect();
    renderClassTeacherAssignments(classId);
    resetClassTeacherForm();
}

function fillClassTeacherSelect() {
    const select = document.getElementById('classTeacherSelect');
    if (!select) return;
    select.innerHTML = '<option value="">-- Chọn giáo viên --</option>';
    allClassTeachers.forEach(t => {
        const option = document.createElement('option');
        option.value = t.id;
        option.textContent = t.fullName;
        select.appendChild(option);
    });
}

function renderClassTeacherAssignments(classId) {
    const list = document.getElementById('classTeacherList');
    if (!list) return;

    const today = new Date().toISOString().split('T')[0];
    const assignments = allClassAssignments.filter(a =>
        a.classId === classId &&
        a.startDate <= today &&
        (!a.endDate || a.endDate >= today)
    );
    if (assignments.length === 0) {
        list.innerHTML = '<div class="text-muted" style="font-size:0.85rem;">Chưa phân công giáo viên cho lớp này.</div>';
        return;
    }

    list.innerHTML = assignments.map(a => `
        <div class="teacher-tag" style="justify-content:space-between; margin-bottom:8px; display:flex;">
            <div>
                <span class="teacher-tag-name">${escapeHtml(a.employeeName)}</span>
                <span class="badge badge-info">${escapeHtml(a.roleInClass || 'Giáo viên phụ trách')}</span>
                <div class="text-muted" style="font-size:0.75rem;">${formatEffectiveRange(a.startDate, a.endDate)}</div>
            </div>
            <div class="d-flex gap-1">
                <button type="button" class="btn-table" onclick="editClassTeacherAssignment(${a.employeeId}, '${a.startDate}')">Sửa</button>
                <button type="button" class="btn-table delete" onclick="deleteClassTeacherAssignment(${a.employeeId}, '${a.startDate}')">Xóa</button>
            </div>
        </div>
    `).join('');
}

function resetClassTeacherForm() {
    const today = new Date().toISOString().split('T')[0];
    const oldEmployee = document.getElementById('classTeacherEditOldEmployeeId');
    const oldStart = document.getElementById('classTeacherEditOldStartDate');
    if (oldEmployee) oldEmployee.value = '';
    if (oldStart) oldStart.value = '';
    if (document.getElementById('classTeacherSelect')) document.getElementById('classTeacherSelect').value = '';
    if (document.getElementById('classTeacherRole')) document.getElementById('classTeacherRole').value = 'Giáo viên phụ trách';
    if (document.getElementById('classTeacherStartDate')) document.getElementById('classTeacherStartDate').value = today;
    if (document.getElementById('classTeacherEndDate')) document.getElementById('classTeacherEndDate').value = '';
}

function editClassTeacherAssignment(employeeId, startDate) {
    const assignment = allClassAssignments.find(a => a.classId === currentClassId && a.employeeId === employeeId && a.startDate === startDate);
    if (!assignment) return;

    document.getElementById('classTeacherEditOldEmployeeId').value = assignment.employeeId;
    document.getElementById('classTeacherEditOldStartDate').value = assignment.startDate;
    document.getElementById('classTeacherSelect').value = assignment.employeeId;
    if (document.getElementById('classTeacherRole')) {
        document.getElementById('classTeacherRole').value = 'Giáo viên phụ trách';
    }
    document.getElementById('classTeacherStartDate').value = assignment.startDate;
    document.getElementById('classTeacherEndDate').value = assignment.endDate || '';
}

async function saveClassTeacherAssignment() {
    if (!currentClassId) return;

    const employeeId = parseInt(document.getElementById('classTeacherSelect').value || '0', 10);
    const startDate = document.getElementById('classTeacherStartDate').value;
    if (!employeeId || !startDate) {
        showAlert('classFormAlert', false, 'Vui lòng chọn giáo viên và ngày bắt đầu.');
        return;
    }

    const oldEmployeeId = parseInt(document.getElementById('classTeacherEditOldEmployeeId').value || '0', 10);
    const oldStartDate = document.getElementById('classTeacherEditOldStartDate').value;
    const isEdit = !!oldEmployeeId && !!oldStartDate;
    const payload = {
        employeeId,
        classId: currentClassId,
        startDate,
        endDate: document.getElementById('classTeacherEndDate').value || null,
        roleInClass: 'Giáo viên phụ trách'
    };

    if (isEdit) {
        payload.oldEmployeeId = oldEmployeeId;
        payload.oldClassId = currentClassId;
        payload.oldStartDate = oldStartDate;
    }

    const result = await sendJson('/Manager/Api/Assignment', isEdit ? 'PUT' : 'POST', payload);
    if (!result.success) {
        showAlert('classFormAlert', false, result.message || 'Không thể lưu phân công giáo viên.');
        return;
    }

    showAlert('classAlert', true, result.message || 'Đã lưu phân công giáo viên.');
    await Promise.all([loadClassTeacherManagement(currentClassId), loadClassesOverview()]);
}

async function deleteClassTeacherAssignment(employeeId, startDate) {
    if (!currentClassId || !(await window.appConfirm('Xóa phân công giáo viên này?'))) return;
    const result = await fetchJson(`/Manager/Api/Assignment?employeeId=${employeeId}&classId=${currentClassId}&startDate=${encodeURIComponent(startDate)}`, { method: 'DELETE' });
    if (!result.success) {
        showAlert('classAlert', false, result.message || 'Không thể xóa phân công giáo viên.');
        return;
    }

    showAlert('classAlert', true, result.message || 'Đã xóa phân công giáo viên.');
    await Promise.all([loadClassTeacherManagement(currentClassId), loadClassesOverview()]);
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
    const teacherSection = document.getElementById('classTeachersSection');
    if (teacherSection) teacherSection.style.display = 'none';
    resetClassTeacherForm();
    const formAlert = document.getElementById('classFormAlert');
    if (formAlert) formAlert.style.display = 'none';
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
    const formAlert = document.getElementById('subjectFormAlert');
    if (formAlert) formAlert.style.display = 'none';
}

function resetScheduleForm(classId = null) {
    currentScheduleId = null;
    const form = document.getElementById('scheduleForm');
    if (form) form.reset();
    
    document.getElementById('scheduleId').value = '';
    document.getElementById('scheduleEffectiveFrom').value = new Date().toISOString().split('T')[0];
    document.getElementById('scheduleStartTime').value = '07:30';
    document.getElementById('scheduleEndTime').value = '08:15';
    const subjectSelect = document.getElementById('scheduleSubjectId');
    if (subjectSelect) subjectSelect.disabled = false;
    document.getElementById('scheduleIsActive').value = 'true';
    document.getElementById('saveScheduleBtn').textContent = 'Lưu thời khóa biểu';
    document.getElementById('saveScheduleBtn').disabled = false;

    const selectedClassId = classId || parseInt(document.getElementById('scheduleClassFilter').value || '0', 10);
    if (selectedClassId) {
        document.getElementById('scheduleClassId').value = selectedClassId;
    }

    const formAlert = document.getElementById('scheduleFormAlert');
    if (formAlert) formAlert.style.display = 'none';
}

function timeToMinutes(value) {
    if (!value || !value.includes(':')) return null;
    const [hours, minutes] = value.split(':').map(Number);
    if (Number.isNaN(hours) || Number.isNaN(minutes)) return null;
    return hours * 60 + minutes;
}

function normalizeSubjectName(value) {
    return String(value || '').trim().replace(/\s+/g, ' ').toLocaleLowerCase('vi-VN');
}

function findPlaySubject() {
    const normalizedPlayName = normalizeSubjectName(PLAY_SUBJECT_NAME);
    return allScheduleSubjects.find(subject => normalizeSubjectName(subject.name) === normalizedPlayName) || null;
}

function isPlayTimeRange(startTime, endTime) {
    const start = timeToMinutes(startTime);
    const end = timeToMinutes(endTime);
    return start !== null && end !== null && start >= timeToMinutes('10:00') && end <= timeToMinutes('11:00');
}

function applyPlaySubjectLock(startTime, endTime) {
    const subjectSelect = document.getElementById('scheduleSubjectId');
    const saveButton = document.getElementById('saveScheduleBtn');
    if (!subjectSelect) return;

    if (!isPlayTimeRange(startTime, endTime)) {
        subjectSelect.disabled = false;
        if (saveButton) saveButton.disabled = false;
        return;
    }

    const playSubject = findPlaySubject();
    if (!playSubject) {
        subjectSelect.disabled = true;
        if (saveButton) saveButton.disabled = true;
        showAlert('scheduleFormAlert', false, PLAY_SUBJECT_CREATE_MESSAGE);
        return;
    }

    subjectSelect.value = String(playSubject.id);
    subjectSelect.disabled = true;
    if (saveButton) saveButton.disabled = false;
}

function validateScheduleSubjectForTime(startTime, endTime) {
    if (!isPlayTimeRange(startTime, endTime)) return null;

    const playSubject = findPlaySubject();
    if (!playSubject) return PLAY_SUBJECT_CREATE_MESSAGE;

    const selectedSubjectId = parseInt(document.getElementById('scheduleSubjectId')?.value || '0', 10);
    if (selectedSubjectId !== playSubject.id) {
        return 'Khung 10:00 - 11:00 chỉ được chọn môn học "Hoạt động vui chơi". Vui lòng tạo/chọn đúng môn trong tab Danh mục môn học.';
    }

    return null;
}

function validateScheduleTimeRange(startTime, endTime) {
    const start = timeToMinutes(startTime);
    const end = timeToMinutes(endTime);
    if (start === null || end === null) return 'Khung giờ không hợp lệ.';
    if (end <= start) return 'Giờ kết thúc phải lớn hơn giờ bắt đầu.';

    const schoolStart = timeToMinutes('06:45');
    const schoolEnd = timeToMinutes('17:00');
    if (start < schoolStart || end > schoolEnd) return 'Chỉ được xếp lịch trong khung 06:45 - 17:00.';

    const lockedSlot = ACTIVITY_SLOTS.find(slot =>
        slot.locked &&
        start < timeToMinutes(slot.end) &&
        end > timeToMinutes(slot.start)
    );
    if (lockedSlot) return `Không được xếp lịch trong khung ${lockedSlot.label} (${lockedSlot.start} - ${lockedSlot.end}).`;

    const matchingSlot = ACTIVITY_SLOTS.some(slot =>
        !slot.locked &&
        start >= timeToMinutes(slot.start) &&
        end <= timeToMinutes(slot.end)
    );
    return matchingSlot ? null : 'Chỉ được xếp lịch trong các khung 07:30-10:00, 10:00-11:00 hoặc 14:00-16:30.';
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
    if (alert) {
        alert.style.display = 'none';
        alert.textContent = '';
    }
    if (window.showToast) {
        window.showToast(success ? 'Thành công' : 'Có lỗi', message, success ? 'success' : 'error');
    }
}

function renderTeacherTags(teachers) {
    if (!teachers || teachers.length === 0) {
        return '<div class="teacher-tags-empty">Chưa phân công</div>';
    }

    return teachers.map(item => {
        const role = escapeHtml(item.roleInClass || 'Giáo viên');
        const isLead = /phụ trách/i.test(item.roleInClass || '');
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
    if (from === 2 && to === 3) return '24 - 36 tháng';
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

function validateSchoolYear(value) {
    if (!value) return 'Niên khóa không được để trống.';
    const match = value.match(/^(\d{4})-(\d{4})$/);
    if (!match) return 'Niên khóa phải có định dạng yyyy-yyyy. Ví dụ: 2025-2026.';

    const startYear = parseInt(match[1], 10);
    const endYear = parseInt(match[2], 10);
    if (endYear !== startYear + 1) {
        return 'Niên khóa phải là hai năm liên tiếp. Ví dụ: 2025-2026.';
    }

    return null;
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

