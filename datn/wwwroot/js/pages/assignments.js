let allAssignments = [];

document.addEventListener('DOMContentLoaded', function() {
    loadAssignments();
    loadDropdowns();
});

async function loadAssignments() {
    try {
        const response = await fetch('/Manager/Api/Assignments');
        const result = await response.json();
        if (result.success) {
            allAssignments = result.data;
            updateStats();
            renderAssignments(allAssignments);
        }
    } catch (e) { console.error(e); }
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
        tbody.innerHTML = '<tr><td colspan="5" class="text-muted" style="text-align:center; padding:40px;">Không có dữ liệu</td></tr>';
        return;
    }
    tbody.innerHTML = data.map(item => {
        const initial = item.employeeName.charAt(0).toUpperCase();
        const isPrimary = item.roleInClass?.toLowerCase().includes('chủ nhiệm');
        return `<tr>
            <td><div class="d-flex align-center gap-1"><div class="avatar" style="background:${isPrimary?'var(--primary)':'#64748B'}">${initial}</div><div><div style="font-weight:600;">${item.employeeName}</div></div></div></td>
            <td><strong>${item.className}</strong></td>
            <td><span class="badge ${isPrimary?'badge-info':'badge-success'}">${item.roleInClass||'Giáo viên'}</span></td>
            <td>${formatDate(item.startDate)} ${item.endDate ? '- '+formatDate(item.endDate) : '<span class="text-success">Hiện tại</span>'}</td>
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
    const modalTitle = document.getElementById('modalTitle');
    const isEdit = document.getElementById('isEdit');

    if(form) form.reset(); 
    if(startDate) startDate.value = new Date().toISOString().split('T')[0]; 
    if(modalTitle) modalTitle.textContent = 'Thêm Phân công mới';
    if(isEdit) isEdit.value = 'false';

    // Mở lại các trường nếu trước đó bị khóa
    document.getElementById('employeeSelect').disabled = false;
    document.getElementById('classSelect').disabled = false;
    document.getElementById('startDate').disabled = false;
}

function editAssignment(empId, clsId, start) {
    const assignment = allAssignments.find(a => a.employeeId == empId && a.classId == clsId && a.startDate == start);
    if(!assignment) return;

    document.getElementById('employeeSelect').value = assignment.employeeId;
    document.getElementById('classSelect').value = assignment.classId;
    document.getElementById('startDate').value = assignment.startDate;
    document.getElementById('endDate').value = assignment.endDate || '';
    document.getElementById('roleInClass').value = assignment.roleInClass || '';

    // Khóa các trường khóa chính
    document.getElementById('employeeSelect').disabled = true;
    document.getElementById('classSelect').disabled = true;
    document.getElementById('startDate').disabled = true;

    document.getElementById('modalTitle').textContent = 'Chỉnh sửa Phân công';
    document.getElementById('isEdit').value = 'true';

    if(typeof openModal === 'function') openModal('assignmentModal');
}

async function saveAssignment() {
    const isEdit = document.getElementById('isEdit').value === 'true';
    const data = {
        employeeId: parseInt(document.getElementById('employeeSelect')?.value),
        classId: parseInt(document.getElementById('classSelect')?.value),
        startDate: document.getElementById('startDate')?.value,
        endDate: document.getElementById('endDate')?.value || null,
        roleInClass: document.getElementById('roleInClass')?.value
    };
    if (!data.employeeId || !data.classId || !data.startDate) { alert('Vui lòng điền đầy đủ'); return; }
    
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
            if(typeof closeModal === 'function') closeModal('assignmentModal'); 
            loadAssignments(); 
            if(window.showToast) window.showToast('Thành công', result.message, 'success'); 
        }
        else alert('Lỗi: ' + result.message);
    } catch(e) { console.error(e); }
}

async function deleteAssignment(empId, clsId, start) {
    if (!confirm('Xóa phân công này?')) return;
    try {
        const r = await fetch(`/Manager/Api/Assignment?employeeId=${empId}&classId=${clsId}&startDate=${start}`, { method:'DELETE' });
        const result = await r.json();
        if (result.success) { 
            loadAssignments(); 
            if(window.showToast) window.showToast('Đã xóa', result.message, 'info'); 
        }
        else alert('Lỗi: ' + result.message);
    } catch(e) { console.error(e); }
}

function formatDate(d) { if(!d)return''; return new Date(d).toLocaleDateString('vi-VN'); }

// Expose functions to global scope for inline onclick handlers
window.prepareCreate = prepareCreate;
window.saveAssignment = saveAssignment;
window.editAssignment = editAssignment;
window.deleteAssignment = deleteAssignment;
window.applyFilters = applyFilters;
