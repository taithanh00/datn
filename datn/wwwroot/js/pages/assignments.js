let allAssignments = [];
let assignmentPanelOverlay = null;
let assignmentSlidePanel = null;
let assignmentPanelTitle = null;

function showAssignmentAlert(message) {
    const alertEl = document.getElementById('assignmentFormAlert');
    if (alertEl) {
        alertEl.style.display = 'none';
        alertEl.textContent = '';
    }
    if (window.showToast) window.showToast('Có lỗi', message, 'error');
}

function clearAssignmentAlert() {
    const alertEl = document.getElementById('assignmentFormAlert');
    if (alertEl) {
        alertEl.style.display = 'none';
        alertEl.textContent = '';
    }
}

function parseAssignmentDate(value) {
    if (!value) return null;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date;
}

function dateRangesOverlap(startA, endA, startB, endB) {
    if (!startA || !startB) return false;
    const aStart = startA.getTime();
    const aEnd = endA ? endA.getTime() : Infinity;
    const bStart = startB.getTime();
    const bEnd = endB ? endB.getTime() : Infinity;
    return aStart <= bEnd && bStart <= aEnd;
}

function validateAssignmentPayload(data, isEdit) {
    if (!data.employeeId || !data.classId || !data.startDate) {
        return 'Vui lòng điền đầy đủ thông tin';
    }

    const currentStart = parseAssignmentDate(data.startDate);
    const currentEnd = parseAssignmentDate(data.endDate);
    const overlappingAssignments = allAssignments.filter(a => {
        if (a.classId !== data.classId) return false;
        if (isEdit && data.oldEmployeeId === a.employeeId && data.oldClassId === a.classId && data.oldStartDate === a.startDate) {
            return false;
        }
        return dateRangesOverlap(currentStart, currentEnd, parseAssignmentDate(a.startDate), parseAssignmentDate(a.endDate));
    });

    if (overlappingAssignments.length >= 2) {
        return 'Một lớp chỉ được phân công tối đa 2 giáo viên phụ trách trong cùng thời gian.';
    }

    const teacherBusy = allAssignments.filter(a => {
        if (a.employeeId !== data.employeeId) return false;
        if (isEdit && data.oldEmployeeId === a.employeeId && data.oldClassId === a.classId && data.oldStartDate === a.startDate) {
            return false;
        }
        return dateRangesOverlap(currentStart, currentEnd, parseAssignmentDate(a.startDate), parseAssignmentDate(a.endDate));
    });

    if (teacherBusy.length > 0) {
        return 'Giáo viên này đã được phân công cho lớp khác trong cùng thời gian.';
    }

    return null;
}

function openAssignmentPanel(title = 'Thêm Phân công mới') {
    if (assignmentSlidePanel) assignmentSlidePanel.classList.add('active');
    if (assignmentPanelOverlay) assignmentPanelOverlay.classList.add('active');
    if (assignmentPanelTitle) assignmentPanelTitle.textContent = title;
}

function closeAssignmentPanel() {
    if (assignmentSlidePanel) assignmentSlidePanel.classList.remove('active');
    if (assignmentPanelOverlay) assignmentPanelOverlay.classList.remove('active');
}

document.addEventListener('DOMContentLoaded', function() {
    assignmentPanelOverlay = document.getElementById('assignmentPanelOverlay');
    assignmentSlidePanel = document.getElementById('assignmentSlidePanel');
    assignmentPanelTitle = document.getElementById('assignmentPanelTitle');
    if (assignmentPanelOverlay) assignmentPanelOverlay.addEventListener('click', closeAssignmentPanel);

    loadAssignments();
    loadDropdowns();
});

async function loadAssignments() {
    const tbody = document.getElementById('assignmentTableBody');
    if (window.appLoading && tbody) {
        window.appLoading.setTable(tbody, 6);
    }
    try {
        const response = await fetch('/Manager/Api/Assignments');
        const result = await response.json();
        if (result.success) {
            allAssignments = result.data;
            updateStats();
            renderAssignments(allAssignments);
        } else if (window.appLoading && tbody) {
            tbody.innerHTML = window.appLoading.tableError(6, result.message || "Kh\u00f4ng th\u1ec3 t\u1ea3i d\u1eef li\u1ec7u.");
        }
    } catch (e) {
        console.error(e);
        if (window.appLoading && tbody) {
            tbody.innerHTML = window.appLoading.tableError(6, "L\u1ed7i k\u1ebft n\u1ed1i m\u00e1y ch\u1ee7.");
        }
    }
}

function updateStats() {
    const totalElem = document.getElementById('statTotalAssignments');
    const classesElem = document.getElementById('statClassesAssigned');
    const currentElem = document.getElementById('statCurrentAssignments');

    if(totalElem) totalElem.textContent = allAssignments.length;
    
    if(classesElem) {
        const uniqueClasses = new Set(allAssignments.map(a => a.classId)).size;
        classesElem.textContent = uniqueClasses;
    }
    
    if(currentElem) {
        const now = new Date().toISOString().split('T')[0];
        const active = allAssignments.filter(a => a.startDate <= now && (!a.endDate || a.endDate >= now)).length;
        currentElem.textContent = active;
    }
}

function renderAssignments(data) {
    const tbody = document.getElementById('assignmentTableBody');
    if(!tbody) return;

    if (data.length === 0) {
        if (window.appLoading) {
            tbody.innerHTML = window.appLoading.tableEmpty(6, "Kh\u00f4ng c\u00f3 d\u1eef li\u1ec7u.");
            return;
        }
        tbody.innerHTML = '<tr><td colspan="6" class="text-muted" style="text-align:center; padding:40px;">Không có dữ liệu</td></tr>';
        return;
    }
    tbody.innerHTML = data.map(item => {
        const initial = item.employeeName.charAt(0).toUpperCase();
        const avatar = item.avatarPath || '/images/lion_blue.png';
        const timeText = item.endDate
            ? `${formatDate(item.startDate)} - ${formatDate(item.endDate)}`
            : formatDate(item.startDate);
        const statusText = item.endDate
            ? '<span class="text-muted">--</span>'
            : '<span class="badge badge-success">Hiện tại</span>';
        return `<tr>
            <td><div class="d-flex align-center gap-1"><img src="${avatar}" alt="Avatar" class="avatar" style="width:36px; height:36px; object-fit:cover; border-radius:50%; border:1px solid var(--border);" onerror="this.src='/images/lion_blue.png'"><div><div style="font-weight:600;">${item.employeeName}</div></div></div></td>
            <td><strong>${item.className}</strong></td>
            <td><span class="badge badge-info">${item.roleInClass || 'Giáo viên phụ trách'}</span></td>
            <td>${timeText}</td>
            <td>${statusText}</td>
            <td style="text-align:right;">
                <button class="btn-table" onclick="editAssignment(${item.employeeId},${item.classId},'${item.startDate}')">Sửa</button>
                <button class="btn-table delete" onclick="deleteAssignment(${item.employeeId},${item.classId},'${item.startDate}')">Xóa</button>
            </td>
        </tr>`;
    }).join('');
    
    if (typeof initPagination === 'function') {
        initPagination('assignmentTable', 10);
    }
}

function applyFilters() {
    const classId = document.getElementById('filterClass')?.value;
    const name = document.getElementById('searchTeacher')?.value.toLowerCase();
    const filtered = allAssignments.filter(a => {
        return (!classId || a.classId == classId) && (!name || a.employeeName.toLowerCase().includes(name));
    });
    renderAssignments(filtered);
}

async function loadDropdowns() {
    try {
        const [resT, resC] = await Promise.all([fetch('/Manager/Api/Teachers'), fetch('/Manager/Api/Classes')]);
        const dataT = await resT.json();
        const dataC = await resC.json();
        
        const employeeSelect = document.getElementById('employeeSelect');
        const classSelect = document.getElementById('classSelect');
        const filterClass = document.getElementById('filterClass');

        if (dataT.success && employeeSelect) {
            employeeSelect.innerHTML = '<option value="">-- Chọn giáo viên --</option>' + dataT.data.map(i => `<option value="${i.id}">${i.fullName}</option>`).join('');
        }
        if (dataC.success) {
            const opts = dataC.data.map(i => `<option value="${i.id}">${i.name}</option>`).join('');
            if(classSelect) classSelect.innerHTML = '<option value="">-- Chọn lớp --</option>' + opts;
            if(filterClass) filterClass.innerHTML += opts;
        }
    } catch (e) { console.error(e); }
}

function prepareCreate() { 
    const form = document.getElementById('assignmentForm');
    const startDate = document.getElementById('startDate');
    const isEdit = document.getElementById('isEdit');

    if(form) form.reset(); 
    if(startDate) startDate.value = new Date().toISOString().split('T')[0]; 
    if(isEdit) isEdit.value = 'false';

    document.getElementById('oldEmployeeId').value = '';
    document.getElementById('oldClassId').value = '';
    document.getElementById('oldStartDate').value = '';

    clearAssignmentAlert();

    // Mở lại các trường nếu trước đó bị khóa
    document.getElementById('employeeSelect').disabled = false;
    document.getElementById('classSelect').disabled = false;
    document.getElementById('startDate').disabled = false;
    openAssignmentPanel('Thêm Phân công mới');
}

function editAssignment(empId, clsId, start) {
    const assignment = allAssignments.find(a => a.employeeId == empId && a.classId == clsId && a.startDate == start);
    if(!assignment) return;

    document.getElementById('employeeSelect').value = assignment.employeeId;
    document.getElementById('classSelect').value = assignment.classId;
    document.getElementById('startDate').value = assignment.startDate;
    document.getElementById('endDate').value = assignment.endDate || '';
    if (document.getElementById('roleInClass')) {
        document.getElementById('roleInClass').value = 'Giáo viên phụ trách';
    }

    document.getElementById('oldEmployeeId').value = assignment.employeeId;
    document.getElementById('oldClassId').value = assignment.classId;
    document.getElementById('oldStartDate').value = assignment.startDate;

    clearAssignmentAlert();

    // Không khóa trường để có thể đổi lớp
    document.getElementById('employeeSelect').disabled = false;
    document.getElementById('classSelect').disabled = false;
    document.getElementById('startDate').disabled = false;

    document.getElementById('assignmentPanelTitle').textContent = 'Chỉnh sửa Phân công';
    document.getElementById('isEdit').value = 'true';

    openAssignmentPanel('Chỉnh sửa Phân công');
}

async function saveAssignment() {
    const isEdit = document.getElementById('isEdit').value === 'true';
    const data = {
        employeeId: parseInt(document.getElementById('employeeSelect')?.value),
        classId: parseInt(document.getElementById('classSelect')?.value),
        startDate: document.getElementById('startDate')?.value,
        endDate: document.getElementById('endDate')?.value || null,
        roleInClass: 'Giáo viên phụ trách'
    };
    
    if (isEdit) {
        data.oldEmployeeId = parseInt(document.getElementById('oldEmployeeId')?.value) || null;
        data.oldClassId = parseInt(document.getElementById('oldClassId')?.value) || null;
        data.oldStartDate = document.getElementById('oldStartDate')?.value || null;
    }

    const validationError = validateAssignmentPayload(data, isEdit);
    if (validationError) {
        showAssignmentAlert(validationError);
        return;
    }
    
    const url = '/Manager/Api/Assignment';
    const method = isEdit ? 'PUT' : 'POST';

    try {
        const r = await fetch(url, { 
            method: method, 
            headers: { 'Content-Type': 'application/json' }, 
            body: JSON.stringify(data) 
        });
        const result = await r.json();
        if (result.success) { 
            closeAssignmentPanel(); 
            loadAssignments(); 
            if(window.showToast) window.showToast('Thành công', result.message, 'success'); 
        }
        else {
            showAssignmentAlert('Lỗi: ' + result.message);
        }
    } catch(e) { console.error(e); }
}

async function deleteAssignment(empId, clsId, start) {
    if (!(await window.appConfirm('Xóa phân công này?'))) return;
    try {
        const r = await fetch(`/Manager/Api/Assignment?employeeId=${empId}&classId=${clsId}&startDate=${encodeURIComponent(start)}`, { method:'DELETE' });
        const result = await r.json();
        if (result.success) { 
            loadAssignments(); 
            if(window.showToast) window.showToast('Đã xóa', result.message, 'info'); 
        }
        else if (window.showToast) window.showToast('Có lỗi', result.message, 'error');
    } catch(e) { console.error(e); }
}

function formatDate(d) { if(!d)return''; return new Date(d).toLocaleDateString('vi-VN'); }

// Expose functions to global scope for inline onclick handlers
window.prepareCreate = prepareCreate;
window.saveAssignment = saveAssignment;
window.editAssignment = editAssignment;
window.deleteAssignment = deleteAssignment;
window.applyFilters = applyFilters;

