// ====== FEE ITEMS MANAGEMENT ======

document.addEventListener('DOMContentLoaded', () => {
    loadFeeItems();
    document.getElementById('feeModalOverlay')?.addEventListener('click', closeFeeModal);
});

async function loadFeeItems() {
    const tbody = document.getElementById('feeItemsTableBody');
    if (window.appLoading && tbody) {
        window.appLoading.setTable(tbody, 6);
    }
    try {
        const response = await fetch('/Tuition/Api/FeeItems');
        const result = await response.json();

        if (!result.success) {
            if (window.appLoading) {
                tbody.innerHTML = window.appLoading.tableError(6, result.message || "Kh\u00f4ng th\u1ec3 t\u1ea3i d\u1eef li\u1ec7u.");
                return;
            }
            tbody.innerHTML = `<tr><td colspan="6" class="text-center text-danger">Lỗi: ${result.message}</td></tr>`;
            return;
        }

        if (result.data.length === 0) {
            if (window.appLoading) {
                tbody.innerHTML = window.appLoading.tableEmpty(6, "Ch\u01b0a c\u00f3 kho\u1ea3n thu n\u00e0o.");
                return;
            }
            tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted">Chưa có khoản thu nào.</td></tr>`;
            return;
        }

        tbody.innerHTML = result.data.map(item => `
            <tr>
                <td>
                    <div class="fw-bold">${escapeHtml(item.name)}</div>
                    <div class="text-muted small">${escapeHtml(item.description || '')}</div>
                </td>
                <td>
                    ${(item.ageFrom || item.ageTo) ? `<span class="badge badge-outline">${formatAgeRange(item.ageFrom, item.ageTo)}</span>` : '<span class="text-muted">--</span>'}
                </td>
                <td><span class="fw-bold text-primary">${formatCurrency(item.defaultAmount)}</span></td>
                <td>
                    <span class="badge ${item.isRequired ? 'badge-danger' : 'badge-info'}">
                        ${item.isRequired ? 'Bắt buộc' : 'Tùy chọn'}
                    </span>
                </td>
                <td>
                    <span class="status-badge ${item.isActive ? 'active' : 'inactive'}">
                        ${item.isActive ? 'Hoạt động' : 'Ngưng'}
                    </span>
                </td>
                <td class="text-end">
                    <button class="btn-table" onclick="editFeeItem(${item.id})">Sửa</button>
                    <button class="btn-table delete" onclick="deleteFeeItem(${item.id})">Xóa</button>
                </td>
            </tr>
        `).join('');

    } catch (error) {
        console.error('Error loading fee items:', error);
        if (window.appLoading) {
            tbody.innerHTML = window.appLoading.tableError(6, "Lỗi kết nối máy chủ.");
            return;
        }
        tbody.innerHTML = `<tr><td colspan="6" class="text-center text-danger">Lỗi kết nối máy chủ.</td></tr>`;
    }
}

function showCreateFeeModal() {
    document.getElementById('modalTitle').textContent = 'Thêm khoản thu mới';
    document.getElementById('feeItemForm').reset();
    document.getElementById('feeItemId').value = '';
    document.getElementById('feeAgeRange').value = '';
    document.getElementById('feeIsActive').checked = true;
    openFeePanel();
}

function closeFeeModal() {
    document.getElementById('feeModalOverlay').classList.remove('active');
    document.getElementById('feeSlidePanel').classList.remove('active');
    document.body.style.overflow = 'auto';
}

async function editFeeItem(id) {
    try {
        const response = await fetch(`/Tuition/Api/FeeItem/${id}`);
        const result = await response.json();

        if (result.success) {
            const item = result.data;
            document.getElementById('modalTitle').textContent = 'Chỉnh sửa khoản thu';
            document.getElementById('feeItemId').value = item.id;
            document.getElementById('feeName').value = item.name;
            
            if (item.ageFrom && item.ageTo) {
                document.getElementById('feeAgeRange').value = `${item.ageFrom}-${item.ageTo}`;
            } else {
                document.getElementById('feeAgeRange').value = '';
            }

            document.getElementById('feeAmount').value = item.defaultAmount;
            document.getElementById('feeDescription').value = item.description || '';
            document.getElementById('feeIsRequired').checked = item.isRequired;
            document.getElementById('feeIsActive').checked = item.isActive;
            openFeePanel();
        } else {
            window.notifyError(result.message);
        }
    } catch (error) {
        console.error('Error fetching fee item:', error);
    }
}

function openFeePanel() {
    document.getElementById('feeModalOverlay').classList.add('active');
    document.getElementById('feeSlidePanel').classList.add('active');
    document.body.style.overflow = 'hidden';
}

async function saveFeeItem() {
    const id = document.getElementById('feeItemId').value;
    const ageRange = document.getElementById('feeAgeRange').value;
    const [ageFrom, ageTo] = ageRange ? ageRange.split('-').map(Number) : [null, null];

    const payload = {
        name: document.getElementById('feeName').value.trim(),
        ageFrom: ageFrom,
        ageTo: ageTo,
        defaultAmount: parseFloat(document.getElementById('feeAmount').value),
        description: document.getElementById('feeDescription').value.trim(),
        isRequired: document.getElementById('feeIsRequired').checked,
        isActive: document.getElementById('feeIsActive').checked
    };

    if (!payload.name || isNaN(payload.defaultAmount)) {
        window.notifyWarning('Vui lòng nhập đầy đủ tên và số tiền.');
        return;
    }

    const isEdit = !!id;
    const url = isEdit ? `/Tuition/Api/FeeItem/${id}` : '/Tuition/Api/FeeItem';
    const method = isEdit ? 'PUT' : 'POST';

    try {
        const response = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const result = await response.json();

        if (result.success) {
            closeFeeModal();
            loadFeeItems();
            showGlobalAlert(result.message, 'success');
        } else {
            window.notifyError(result.message);
        }
    } catch (error) {
        console.error('Error saving fee item:', error);
        window.notifyError('Lỗi kết nối máy chủ.');
    }
}

async function deleteFeeItem(id) {
    if (!(await window.appConfirm('Bạn có chắc chắn muốn xóa khoản thu này?'))) return;

    try {
        const response = await fetch(`/Tuition/Api/FeeItem/${id}`, { method: 'DELETE' });
        const result = await response.json();
        
        if (result.success) {
            loadFeeItems();
            showGlobalAlert(result.message, 'success');
        } else {
            window.notifyError(result.message);
        }
    } catch (error) {
        console.error('Error deleting fee item:', error);
    }
}

// Helpers
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
}

function formatDate(dateStr) {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('vi-VN');
}

function formatAgeRange(ageFrom, ageTo) {
    if (ageFrom === 2 && ageTo === 3) return '24 - 36 tháng';
    if (ageFrom && ageTo) return `${ageFrom} - ${ageTo} tuổi`;
    if (ageFrom) return `Từ ${ageFrom} tuổi`;
    if (ageTo) return `Đến ${ageTo} tuổi`;
    return '--';
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showGlobalAlert(message, type) {
    const alert = document.getElementById('feeAlert');
    if (alert) {
        alert.style.display = 'none';
        alert.textContent = '';
    }
    if (window.showToast) {
        window.showToast(type === 'success' ? 'Thành công' : 'Có lỗi', message, type === 'success' ? 'success' : 'error');
    }
}
