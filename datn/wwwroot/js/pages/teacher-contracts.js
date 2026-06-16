const contractState = {
    isDetail: false,
    teacherId: null,
    teacherName: '',
    teachers: []
};

const contractStatusLabels = {
    Draft: 'Bản nháp',
    Active: 'Đang hiệu lực',
    Expired: 'Hết hạn',
    Terminated: 'Đã chấm dứt',
    Cancelled: 'Đã hủy'
};

document.addEventListener('DOMContentLoaded', initTeacherContracts);

function initTeacherContracts() {
    const detailPanel = document.getElementById('contracts');
    const listPage = document.getElementById('teacherContractsPage');
    if (!detailPanel && !listPage) return;

    contractState.isDetail = !!detailPanel;
    contractState.teacherId = detailPanel?.dataset.teacherId || null;
    contractState.teacherName = detailPanel?.dataset.teacherName || '';

    bindContractEvents();
    loadContractTeachers().then(() => {
        if (contractState.isDetail) {
            lockContractTeacher(contractState.teacherId, contractState.teacherName);
            loadTeacherContracts();
        } else {
            loadContractAlerts();
            loadAllContracts();
        }
    });
}

function bindContractEvents() {
    document.getElementById('openContractFormBtn')?.addEventListener('click', () => openContractForm());
    document.getElementById('btnCreateContractForTeacher')?.addEventListener('click', () => openContractForm());
    document.getElementById('closeContractPanelBtn')?.addEventListener('click', closeContractForm);
    document.getElementById('cancelContractFormBtn')?.addEventListener('click', closeContractForm);
    document.getElementById('contractModalOverlay')?.addEventListener('click', closeContractForm);
    document.getElementById('contractForm')?.addEventListener('submit', saveContractForm);
    document.getElementById('btnApplyContractFilter')?.addEventListener('click', loadAllContracts);
    document.getElementById('btnResetContractFilter')?.addEventListener('click', resetContractFilters);
}

async function loadContractTeachers() {
    if (contractState.isDetail) return;

    try {
        const res = await fetch('/Manager/Api/Teachers');
        const json = await res.json();
        contractState.teachers = json.success ? json.data : [];
        populateTeacherSelects(contractState.teachers);
    } catch (error) {
        console.error('Load teachers failed', error);
    }
}

function populateTeacherSelects(teachers) {
    const filterOptions = '<option value="">Tất cả giáo viên</option>' +
        teachers.map(t => `<option value="${t.id}">${escapeHtml(t.fullName)}</option>`).join('');
    const formOptions = '<option value="">-- Chọn giáo viên --</option>' +
        teachers.map(t => `<option value="${t.id}">${escapeHtml(t.fullName)}</option>`).join('');

    const filter = document.getElementById('contractTeacherFilter');
    if (filter) filter.innerHTML = filterOptions;

    const formSelect = document.getElementById('contractEmployeeId');
    if (formSelect) formSelect.innerHTML = formOptions;
}

function lockContractTeacher(teacherId, teacherName) {
    const group = document.getElementById('contractEmployeeGroup');
    const select = document.getElementById('contractEmployeeId');
    if (!group || !select) return;

    group.style.display = 'none';
    select.innerHTML = `<option value="${teacherId}">${escapeHtml(teacherName)}</option>`;
    select.value = teacherId;
}

async function loadContractAlerts() {
    try {
        const res = await fetch('/Manager/Api/TeacherContractAlerts');
        const json = await res.json();
        if (!json.success) return;

        document.getElementById('contractActiveCount').textContent = json.data.activeContracts;
        document.getElementById('contractExpiringCount').textContent = json.data.expiringSoon;
        document.getElementById('teacherWithoutContractCount').textContent = json.data.teachersWithoutContracts;
    } catch (error) {
        console.error('Load contract alerts failed', error);
    }
}

async function loadAllContracts() {
    const tbody = document.getElementById('contractsTableBody');
    if (!tbody) return;

    if (window.appLoading) {
        tbody.innerHTML = window.appLoading.tableRow(8);
    } else {
        tbody.innerHTML = '<tr><td colspan="8" class="text-muted" style="text-align:center; padding:32px;">Đang tải...</td></tr>';
    }

    const params = new URLSearchParams();
    const status = document.getElementById('contractStatusFilter')?.value;
    const employeeId = document.getElementById('contractTeacherFilter')?.value;
    const expiring = document.getElementById('contractExpiringFilter')?.value;
    if (status) params.append('status', status);
    if (employeeId) params.append('employeeId', employeeId);
    if (expiring) params.append('expiringWithinDays', expiring);

    try {
        const res = await fetch('/Manager/Api/TeacherContracts?' + params.toString());
        const json = await res.json();
        if (!json.success) throw new Error(json.message || 'Không tải được danh sách hợp đồng.');
        renderContractsTable(json.data, tbody, true);
    } catch (error) {
        if (window.appLoading) {
            tbody.innerHTML = window.appLoading.tableError(8, error.message);
            return;
        }
        tbody.innerHTML = `<tr><td colspan="8" class="text-danger" style="text-align:center; padding:32px;">${escapeHtml(error.message)}</td></tr>`;
    }
}

async function loadTeacherContracts() {
    const tbody = document.getElementById('teacherContractsBody');
    if (!tbody || !contractState.teacherId) return;

    if (window.appLoading) {
        tbody.innerHTML = window.appLoading.tableRow(7);
    } else {
        tbody.innerHTML = '<tr><td colspan="7" class="text-muted" style="text-align:center; padding:32px;">Đang tải...</td></tr>';
    }

    try {
        const res = await fetch(`/Manager/Api/Teacher/${contractState.teacherId}/Contracts`);
        const json = await res.json();
        if (!json.success) throw new Error(json.message || 'Không tải được hợp đồng giáo viên.');
        renderTeacherContractSummary(json.data);
        renderContractsTable(json.data, tbody, false);
    } catch (error) {
        if (window.appLoading) {
            tbody.innerHTML = window.appLoading.tableError(7, error.message);
            return;
        }
        tbody.innerHTML = `<tr><td colspan="7" class="text-danger" style="text-align:center; padding:32px;">${escapeHtml(error.message)}</td></tr>`;
    }
}

function renderTeacherContractSummary(contracts) {
    const target = document.getElementById('teacherContractSummary');
    if (!target) return;

    const active = contracts.find(c => c.status === 'Active');
    const expired = contracts.some(c => c.status === 'Expired');
    if (active) {
        target.innerHTML = `
            <div class="contract-summary-grid">
                <div class="contract-summary-card">
                    <div class="contract-summary-label">Hợp đồng hiện tại</div>
                    <div class="contract-summary-value">${escapeHtml(active.contractNumber)}</div>
                </div>
                <div class="contract-summary-card ${active.isExpiringSoon ? 'warning' : ''}">
                    <div class="contract-summary-label">Ngày hết hạn</div>
                    <div class="contract-summary-value">${formatDate(active.expiryDate) || 'Không thời hạn'}</div>
                </div>
                <div class="contract-summary-card">
                    <div class="contract-summary-label">Lương thỏa thuận</div>
                    <div class="contract-summary-value">${formatMoney(active.agreedSalary)}</div>
                </div>
            </div>`;
        return;
    }

    target.innerHTML = `
        <div class="contract-summary-grid">
            <div class="contract-summary-card ${expired ? 'danger' : 'warning'}">
                <div class="contract-summary-label">${expired ? 'Không có hợp đồng hiệu lực' : 'Chưa có hợp đồng'}</div>
                <div class="contract-summary-value">${contracts.length}</div>
            </div>
        </div>`;
}

function renderContractsTable(contracts, tbody, showTeacher) {
    const colspan = showTeacher ? 8 : 7;
    if (!contracts || contracts.length === 0) {
        if (window.appLoading) {
            tbody.innerHTML = window.appLoading.tableEmpty(colspan, 'Không có hợp đồng.');
            return;
        }
        tbody.innerHTML = `<tr><td colspan="${colspan}" class="text-muted" style="text-align:center; padding:32px;">Không có hợp đồng.</td></tr>`;
        return;
    }

    tbody.innerHTML = contracts.map(c => `
        <tr>
            <td><strong>${escapeHtml(c.contractNumber)}</strong>${c.hasFile ? ' <i class="fa-solid fa-paperclip text-muted"></i>' : ''}</td>
            ${showTeacher ? `<td>${escapeHtml(c.employeeName || '')}</td>` : ''}
            <td>${escapeHtml(c.contractType || '')}</td>
            <td>${formatDate(c.effectiveDate)}</td>
            <td>${formatDate(c.expiryDate) || 'Không thời hạn'} ${c.isExpiringSoon ? '<span class="contract-badge warning">Sắp hết hạn</span>' : ''}</td>
            <td>${formatMoney(c.agreedSalary)}</td>
            <td>${statusBadge(c.status)}</td>
            <td>
                <div class="contract-actions">
                    ${c.status === 'Draft' ? `<button class="contract-icon-btn" title="Sửa" onclick="editTeacherContract(${c.id})"><i class="fa-solid fa-pen"></i></button>` : ''}
                    ${c.status === 'Draft' ? `<button class="contract-icon-btn" title="Kích hoạt" onclick="activateTeacherContract(${c.id})"><i class="fa-solid fa-circle-play"></i></button>` : ''}
                    ${c.status === 'Active' ? `<button class="contract-icon-btn" title="Chấm dứt" onclick="terminateTeacherContract(${c.id})"><i class="fa-solid fa-ban"></i></button>` : ''}
                    ${c.status === 'Draft' ? `<button class="contract-icon-btn" title="Hủy" onclick="cancelTeacherContract(${c.id})"><i class="fa-solid fa-xmark"></i></button>` : ''}
                    ${c.hasFile ? `<button class="contract-icon-btn" title="Tải file" onclick="downloadTeacherContractFile(${c.id})"><i class="fa-solid fa-download"></i></button>` : ''}
                    ${c.status !== 'Active' ? `<button class="contract-icon-btn" title="Xóa" onclick="deleteTeacherContract(${c.id})"><i class="fa-solid fa-trash"></i></button>` : ''}
                </div>
            </td>
        </tr>
    `).join('');
}

function openContractForm(contract = null) {
    clearContractForm();
    const title = document.getElementById('contractPanelTitle');
    if (title) title.textContent = contract ? 'Sửa hợp đồng' : 'Thêm hợp đồng';

    if (contract) fillContractForm(contract);
    if (contractState.isDetail) {
        document.getElementById('contractEmployeeId').value = contractState.teacherId;
    }

    document.getElementById('contractModalOverlay')?.classList.add('active');
    document.getElementById('contractSlidePanel')?.classList.add('active');
    document.body.style.overflow = 'hidden';
}

function closeContractForm() {
    document.getElementById('contractModalOverlay')?.classList.remove('active');
    document.getElementById('contractSlidePanel')?.classList.remove('active');
    document.body.style.overflow = 'auto';
}

function clearContractForm() {
    document.getElementById('contractForm')?.reset();
    document.getElementById('contractId').value = '';
    document.getElementById('contractStatus').value = 'Draft';
    showContractAlert('', '');
}

function fillContractForm(contract) {
    document.getElementById('contractId').value = contract.id;
    document.getElementById('contractEmployeeId').value = contract.employeeId;
    document.getElementById('contractNumber').value = contract.contractNumber || '';
    document.getElementById('contractType').value = contract.contractType || '';
    document.getElementById('signedDate').value = contract.signedDate || '';
    document.getElementById('effectiveDate').value = contract.effectiveDate || '';
    document.getElementById('expiryDate').value = contract.expiryDate || '';
    document.getElementById('agreedSalary').value = contract.agreedSalary || '';
    document.getElementById('workPosition').value = contract.workPosition || '';
    document.getElementById('workLocation').value = contract.workLocation || '';
    document.getElementById('workingHours').value = contract.workingHours || '';
    document.getElementById('contractStatus').value = contract.status === 'Active' ? 'Active' : 'Draft';
    document.getElementById('contractNote').value = contract.note || '';
}

async function saveContractForm(event) {
    event.preventDefault();
    const employeeId = document.getElementById('contractEmployeeId').value;
    const contractId = document.getElementById('contractId').value;
    if (!employeeId) {
        showContractAlert('Vui lòng chọn giáo viên.', 'error');
        return;
    }

    const formData = new FormData();
    formData.append('ContractNumber', document.getElementById('contractNumber').value);
    formData.append('ContractType', document.getElementById('contractType').value);
    formData.append('SignedDate', document.getElementById('signedDate').value);
    formData.append('EffectiveDate', document.getElementById('effectiveDate').value);
    formData.append('ExpiryDate', document.getElementById('expiryDate').value);
    formData.append('AgreedSalary', document.getElementById('agreedSalary').value);
    formData.append('WorkPosition', document.getElementById('workPosition').value);
    formData.append('WorkLocation', document.getElementById('workLocation').value);
    formData.append('WorkingHours', document.getElementById('workingHours').value);
    formData.append('Status', document.getElementById('contractStatus').value);
    formData.append('Note', document.getElementById('contractNote').value);

    const file = document.getElementById('contractFile').files[0];
    if (file) formData.append('File', file);

    const url = contractId ? `/Manager/Api/TeacherContract/${contractId}` : `/Manager/Api/Teacher/${employeeId}/Contracts`;
    const method = contractId ? 'PUT' : 'POST';

    try {
        const res = await fetch(url, { method, body: formData });
        const json = await res.json();
        if (!json.success) throw new Error(json.message || 'Không lưu được hợp đồng.');
        showContractAlert(json.message || 'Đã lưu hợp đồng.', 'success');
        setTimeout(() => {
            closeContractForm();
            refreshContractViews();
        }, 350);
    } catch (error) {
        showContractAlert(error.message, 'error');
    }
}

async function editTeacherContract(id) {
    const contract = await getTeacherContract(id);
    if (contract) openContractForm(contract);
}

async function activateTeacherContract(id) {
    if (!(await window.appConfirm('Kích hoạt hợp đồng này? Hợp đồng đang hiệu lực cũ của giáo viên sẽ được chấm dứt tự động.'))) return;
    await postContractAction(`/Manager/Api/TeacherContract/${id}/Activate`);
}

async function terminateTeacherContract(id) {
    const reason = prompt('Nhập lý do chấm dứt hợp đồng:');
    if (!reason) return;
    const today = new Date().toISOString().slice(0, 10);
    const terminationDate = prompt('Ngày chấm dứt (yyyy-MM-dd):', today);
    if (!terminationDate) return;
    await postContractAction(`/Manager/Api/TeacherContract/${id}/Terminate`, { terminationDate, terminationReason: reason });
}

async function cancelTeacherContract(id) {
    if (!(await window.appConfirm('Hủy hợp đồng này?'))) return;
    await postContractAction(`/Manager/Api/TeacherContract/${id}/Cancel`);
}

async function deleteTeacherContract(id) {
    if (!(await window.appConfirm('Xóa hợp đồng này? Thao tác này không thể hoàn tác.'))) return;
    try {
        const res = await fetch(`/Manager/Api/TeacherContract/${id}`, { method: 'DELETE' });
        const json = await res.json();
        if (!json.success) throw new Error(json.message || 'Không xóa được hợp đồng.');
        refreshContractViews();
    } catch (error) {
        window.notifyError(error.message);
    }
}

function downloadTeacherContractFile(id) {
    window.location.href = `/Manager/Api/TeacherContract/${id}/File`;
}

async function getTeacherContract(id) {
    try {
        const res = await fetch(`/Manager/Api/TeacherContract/${id}`);
        const json = await res.json();
        if (!json.success) throw new Error(json.message || 'Không lấy được hợp đồng.');
        return json.data;
    } catch (error) {
        window.notifyError(error.message);
        return null;
    }
}

async function postContractAction(url, body = null) {
    try {
        const options = { method: 'POST' };
        if (body) {
            options.headers = { 'Content-Type': 'application/json' };
            options.body = JSON.stringify(body);
        }
        const res = await fetch(url, options);
        const json = await res.json();
        if (!json.success) throw new Error(json.message || 'Không thực hiện được thao tác.');
        refreshContractViews();
    } catch (error) {
        window.notifyError(error.message);
    }
}

function refreshContractViews() {
    if (contractState.isDetail) {
        loadTeacherContracts();
    } else {
        loadContractAlerts();
        loadAllContracts();
    }
}

function resetContractFilters() {
    document.getElementById('contractStatusFilter').value = '';
    document.getElementById('contractTeacherFilter').value = '';
    document.getElementById('contractExpiringFilter').value = '';
    loadAllContracts();
}

function showContractAlert(message, type) {
    const alertBox = document.getElementById('contractFormAlert');
    if (alertBox) {
        alertBox.textContent = '';
        alertBox.className = 'contract-alert';
    }
    if (message && window.showToast) {
        window.showToast(type === 'success' ? 'Thành công' : 'Có lỗi', message, type === 'success' ? 'success' : 'error');
    }
}

function statusBadge(status) {
    const cls = (status || '').toLowerCase();
    const label = contractStatusLabels[status] || status || '';
    return `<span class="contract-badge ${cls}">${escapeHtml(label)}</span>`;
}

function formatDate(value) {
    if (!value) return '';
    const parts = value.split('-');
    return parts.length === 3 ? `${parts[2]}/${parts[1]}/${parts[0]}` : value;
}

function formatMoney(value) {
    if (value === null || value === undefined || value === '') return '0 đ';
    return new Intl.NumberFormat('vi-VN').format(value) + ' đ';
}

function escapeHtml(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

window.editTeacherContract = editTeacherContract;
window.activateTeacherContract = activateTeacherContract;
window.terminateTeacherContract = terminateTeacherContract;
window.cancelTeacherContract = cancelTeacherContract;
window.deleteTeacherContract = deleteTeacherContract;
window.downloadTeacherContractFile = downloadTeacherContractFile;
