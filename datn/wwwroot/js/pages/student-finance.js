// ====== STUDENT FINANCE MANAGEMENT ======

let studentFeeConfigs = [];
let allFeeItems = [];

document.addEventListener('DOMContentLoaded', () => {
    const studentId = document.getElementById('studentIdHidden')?.value;
    if (studentId) {
        loadStudentFinance(studentId);
        loadAllFeeItems();
    }
});

async function loadStudentFinance(studentId) {
    const tbody = document.getElementById('studentFinanceBody');
    try {
        const response = await fetch(`/Tuition/Api/StudentFinance/${studentId}`);
        const result = await response.json();

        if (result.success) {
            studentFeeConfigs = result.data;
            renderFinanceTable();
        } else {
            tbody.innerHTML = `<tr><td colspan="5" class="text-center text-danger">${result.message}</td></tr>`;
        }
    } catch (error) {
        console.error('Error loading finance:', error);
    }
}

async function loadAllFeeItems() {
    try {
        const response = await fetch('/Tuition/Api/FeeItems');
        const result = await response.json();
        if (result.success) {
            allFeeItems = result.data.filter(i => i.isActive);
            fillFeeItemSelect();
        }
    } catch (error) {
        console.error('Error loading fee items:', error);
    }
}

function fillFeeItemSelect() {
    const select = document.getElementById('feeItemIdSelect');
    if (!select) return;
    
    // Chỉ hiển thị những khoản thu chưa được đăng ký (trừ khi đang sửa)
    const registeredIds = studentFeeConfigs.map(c => c.feeItemId);
    const availableItems = allFeeItems.filter(i => !registeredIds.includes(i.id));
    
    select.innerHTML = '<option value="">-- Chọn khoản thu --</option>';
    availableItems.forEach(item => {
        const option = document.createElement('option');
        option.value = item.id;
        option.textContent = `${item.name} (${formatCurrency(item.defaultAmount)})`;
        select.appendChild(option);
    });
}

function renderFinanceTable() {
    const tbody = document.getElementById('studentFinanceBody');
    if (studentFeeConfigs.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">Học sinh này chưa đăng ký khoản thu riêng nào.</td></tr>';
        return;
    }

    tbody.innerHTML = studentFeeConfigs.map(c => `
        <tr>
            <td>
                <div class="fw-bold">${escapeHtml(c.feeName)}</div>
                ${c.note ? `<div class="text-muted small">${escapeHtml(c.note)}</div>` : ''}
            </td>
            <td>
                ${c.customAmount ? `<span class="text-decoration-line-through text-muted small">${formatCurrency(c.defaultAmount)}</span><br>` : ''}
                <span>${formatCurrency(c.customAmount || c.defaultAmount)}</span>
            </td>
            <td>
                ${c.discountPercentage > 0 ? `<span class="badge badge-info">-${c.discountPercentage}%</span> ` : ''}
                ${c.discountAmount > 0 ? `<span class="badge badge-warning">-${formatCurrency(c.discountAmount)}</span>` : ''}
                ${c.discountPercentage === 0 && c.discountAmount === 0 ? '--' : ''}
            </td>
            <td><span class="fw-bold text-success">${formatCurrency(c.finalAmount)}</span></td>
            <td class="text-end">
                <button class="btn-table btn-table-edit" onclick="editFinanceConfig(${c.id})">
                    <i class="fa-solid fa-pen-to-square"></i>
                </button>
                <button class="btn-table btn-table-delete" onclick="deleteFinanceConfig(${c.id})">
                    <i class="fa-solid fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
}

function showAddFeeConfigModal() {
    document.getElementById('financeModalTitle').textContent = 'Đăng ký khoản thu mới';
    document.getElementById('studentFeeForm').reset();
    document.getElementById('configId').value = '';
    document.getElementById('feeItemSelectGroup').style.display = 'block';
    fillFeeItemSelect();
    document.getElementById('financeModalOverlay').classList.add('show');
}

function closeFinanceModal() {
    document.getElementById('financeModalOverlay').classList.remove('show');
}

function onFeeItemChanged() {
    const select = document.getElementById('feeItemIdSelect');
    const itemId = parseInt(select.value);
    const item = allFeeItems.find(i => i.id === itemId);
    if (item) {
        document.getElementById('configAmount').placeholder = `Mặc định: ${formatCurrency(item.defaultAmount)}`;
    }
}

async function editFinanceConfig(id) {
    const config = studentFeeConfigs.find(c => c.id === id);
    if (!config) return;

    document.getElementById('financeModalTitle').textContent = 'Chỉnh sửa cấu hình phí';
    document.getElementById('configId').value = config.id;
    document.getElementById('feeItemSelectGroup').style.display = 'none'; // Không cho đổi loại phí khi đang sửa
    
    document.getElementById('configAmount').value = config.customAmount || '';
    document.getElementById('configDiscountAmount').value = config.discountAmount;
    document.getElementById('configDiscountPercentage').value = config.discountPercentage;
    document.getElementById('configNote').value = config.note || '';
    
    document.getElementById('financeModalOverlay').classList.add('show');
}

async function saveStudentFeeConfig() {
    const studentId = parseInt(document.getElementById('studentIdHidden').value);
    const configId = document.getElementById('configId').value;
    const feeItemId = parseInt(document.getElementById('feeItemIdSelect').value);

    const payload = {
        id: configId ? parseInt(configId) : 0,
        studentId: studentId,
        feeItemId: configId ? studentFeeConfigs.find(c => c.id === parseInt(configId)).feeItemId : feeItemId,
        customAmount: document.getElementById('configAmount').value ? parseFloat(document.getElementById('configAmount').value) : null,
        discountAmount: parseFloat(document.getElementById('configDiscountAmount').value) || 0,
        discountPercentage: parseFloat(document.getElementById('configDiscountPercentage').value) || 0,
        note: document.getElementById('configNote').value.trim()
    };

    if (!configId && !payload.feeItemId) {
        window.notifyWarning('Vui lòng chọn khoản thu.');
        return;
    }

    try {
        const response = await fetch('/Tuition/Api/StudentFinance', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const result = await response.json();

        if (result.success) {
            closeFinanceModal();
            loadStudentFinance(studentId);
        } else {
            window.notifyError(result.message);
        }
    } catch (error) {
        console.error('Error saving config:', error);
    }
}

async function deleteFinanceConfig(id) {
    if (!(await window.appConfirm('Bạn có chắc chắn muốn hủy đăng ký khoản thu này cho học sinh?'))) return;
    const studentId = document.getElementById('studentIdHidden').value;

    try {
        const response = await fetch(`/Tuition/Api/StudentFinance/${id}`, { method: 'DELETE' });
        const result = await response.json();
        if (result.success) {
            loadStudentFinance(studentId);
        } else {
            window.notifyError(result.message);
        }
    } catch (error) {
        console.error('Error deleting config:', error);
    }
}

// Helpers
function formatCurrency(amount) {
    if (amount === null || amount === undefined) return '';
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
