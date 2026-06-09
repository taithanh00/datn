let currentPage = 1;
const pageSize = 20;
let currentSearch = '';

document.addEventListener('DOMContentLoaded', () => {
    loadLogs();
    loadFilterOptions();

    const searchInput = document.getElementById('searchLogs');
    let debounceTimer;
    searchInput.addEventListener('input', (e) => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            currentSearch = e.target.value;
            currentPage = 1;
            loadLogs();
        }, 500);
    });

    // Toggle Filter Panel
    document.getElementById('btnToggleFilter').addEventListener('click', function() {
        this.classList.toggle('active');
        document.getElementById('filterPanel').classList.toggle('active');
    });

    // Apply Filter
    document.getElementById('btnApplyFilter').addEventListener('click', () => {
        currentPage = 1;
        loadLogs();
    });

    // Reset Filter
    document.getElementById('btnResetFilter').addEventListener('click', () => {
        document.getElementById('filterStartDate').value = '';
        document.getElementById('filterEndDate').value = '';
        document.getElementById('filterEntity').value = '';
        document.getElementById('filterUser').value = '';
        document.getElementById('filterAction').value = '';
        currentPage = 1;
        loadLogs();
    });

    document.getElementById('closeLogPanel').addEventListener('click', closePanel);
    document.getElementById('logModalOverlay').addEventListener('click', closePanel);
});

async function loadFilterOptions() {
    if (window.appLoading) {
        tableBody.innerHTML = window.appLoading.tableRow(5, "\u0110ang t\u1ea3i nh\u1eadt k\u00fd...");
    }

    try {
        const response = await fetch('/Manager/SystemLog/GetFilterOptions');
        const options = await response.json();

        const entitySelect = document.getElementById('filterEntity');
        const userSelect = document.getElementById('filterUser');
        const actionSelect = document.getElementById('filterAction');

        options.entities.forEach(e => entitySelect.innerHTML += `<option value="${e}">${e}</option>`);
        options.users.forEach(u => userSelect.innerHTML += `<option value="${u}">${u}</option>`);
        options.actions.forEach(a => actionSelect.innerHTML += `<option value="${a}">${a}</option>`);
    } catch (e) { console.error('Error loading filter options:', e); }
}

async function loadLogs() {
    const tableBody = document.getElementById('logsTableBody');
    tableBody.innerHTML = `
        <tr class="loading-row">
            <td colspan="5" style="text-align:center; padding:40px;">
                <div class="spinner"></div>
                <p class="text-muted">Đang tải nhật ký...</p>
            </td>
        </tr>
    `;

    try {
        const params = new URLSearchParams({
            page: currentPage,
            pageSize: pageSize,
            search: currentSearch,
            startDate: document.getElementById('filterStartDate').value,
            endDate: document.getElementById('filterEndDate').value,
            entityName: document.getElementById('filterEntity').value,
            userName: document.getElementById('filterUser').value,
            logAction: document.getElementById('filterAction').value
        });

        const response = await fetch(`/Manager/SystemLog/GetData?${params.toString()}`);
        const result = await response.json();

        if (result.success) {
            renderTable(result.data);
            renderPagination(result.totalPages, result.totalItems);
        }
    } catch (error) {
        console.error('Error loading logs:', error);
        if (window.appLoading) {
            tableBody.innerHTML = window.appLoading.tableError(5, "L\u1ed7i khi t\u1ea3i d\u1eef li\u1ec7u. Vui l\u00f2ng th\u1eed l\u1ea1i.");
            return;
        }
        tableBody.innerHTML = `<tr><td colspan="5" class="text-danger" style="text-align:center; padding:40px;">Lỗi khi tải dữ liệu. Vui lòng thử lại.</td></tr>`;
    }
}

function renderTable(logs) {
    const tableBody = document.getElementById('logsTableBody');
    if (logs.length === 0) {
        if (window.appLoading) {
            tableBody.innerHTML = window.appLoading.tableEmpty(5, "Kh\u00f4ng t\u00ecm th\u1ea5y b\u1ea3n ghi n\u00e0o.");
            return;
        }
        tableBody.innerHTML = `<tr><td colspan="5" class="text-muted" style="text-align:center; padding:40px;">Không tìm thấy bản ghi nào.</td></tr>`;
        return;
    }

    tableBody.innerHTML = logs.map(log => `
        <tr class="log-item">
            <td>
                <div class="font-bold">${formatDate(log.createdAtUtc)}</div>
                <div class="text-muted small">${formatTime(log.createdAtUtc)}</div>
            </td>
            <td>
                <div class="d-flex align-center gap-2">
                    <div class="avatar-sm" style="background: var(--primary-soft); color: var(--primary); width: 28px; height: 28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 0.7rem; font-weight: 700;">
                        ${(log.userName || 'AD').substring(0, 2).toUpperCase()}
                    </div>
                    <span>${log.userName || 'Hệ thống'}</span>
                </div>
            </td>
            <td>
                <span class="action-badge badge-${log.action.toLowerCase()}">${log.action}</span>
            </td>
            <td><span class="entity-badge">${log.entityName}</span></td>
            <td class="text-end">
                <button class="btn-table" onclick='showDetails(${JSON.stringify(log).replace(/'/g, "&#39;")})'>Xem chi tiết</button>
            </td>
        </tr>
    `).join('');
}

function renderPagination(totalPages, totalItems) {
    const container = document.getElementById('paginationButtons');
    const info = document.getElementById('paginationInfo');
    
    const startIdx = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
    const endIdx = Math.min(totalItems, currentPage * pageSize);
    info.innerHTML = `Đang hiển thị <span class="font-bold">${startIdx} - ${endIdx}</span> trong tổng số <span class="font-bold">${totalItems}</span> bản ghi`;
    
    let html = '';
    
    // Prev
    html += `<button class="pagination-btn" ${currentPage === 1 ? 'disabled' : ''} onclick="changePage(${currentPage - 1})"><i class="fa-solid fa-chevron-left"></i></button>`;
    
    // Pages
    const startPage = Math.max(1, currentPage - 2);
    const endPage = Math.min(totalPages, startPage + 4);
    
    if (startPage > 1) {
        html += `<button class="pagination-btn" onclick="changePage(1)">1</button>`;
        if (startPage > 2) html += `<span class="text-muted" style="padding: 0 4px;">...</span>`;
    }

    for (let i = startPage; i <= endPage; i++) {
        html += `<button class="pagination-btn ${i === currentPage ? 'active' : ''}" onclick="changePage(${i})">${i}</button>`;
    }
    
    if (endPage < totalPages) {
        if (endPage < totalPages - 1) html += `<span class="text-muted" style="padding: 0 4px;">...</span>`;
        html += `<button class="pagination-btn" onclick="changePage(${totalPages})">${totalPages}</button>`;
    }
    
    // Next
    html += `<button class="pagination-btn" ${currentPage === totalPages || totalPages === 0 ? 'disabled' : ''} onclick="changePage(${currentPage + 1})"><i class="fa-solid fa-chevron-right"></i></button>`;
    
    container.className = 'pagination-buttons';
    container.innerHTML = html;
}

function changePage(page) {
    currentPage = page;
    loadLogs();
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function showDetails(log) {
    document.getElementById('detailUser').innerText = log.userName || 'Hệ thống';
    document.getElementById('detailTime').innerText = formatDate(log.createdAtUtc) + ' ' + formatTime(log.createdAtUtc);
    document.getElementById('detailEntity').innerText = log.entityName;
    document.getElementById('detailIp').innerText = log.ipAddress || 'Internal';
    
    // Parse EntityId if it's JSON
    let entityIdDisplay = log.entityId;
    try {
        const idObj = JSON.parse(log.entityId);
        entityIdDisplay = Object.values(idObj).join(', ');
    } catch(e) {}
    document.getElementById('detailEntityId').innerText = entityIdDisplay;

    // Action Badge
    document.getElementById('detailActionBadge').innerHTML = `<span class="action-badge badge-${log.action.toLowerCase()}">${log.action}</span>`;
    
    const diffBody = document.getElementById('diffTableBody');
    diffBody.innerHTML = '';

    const oldVals = log.oldValues ? JSON.parse(log.oldValues) : {};
    const newVals = log.newValues ? JSON.parse(log.newValues) : {};

    const allKeys = [...new Set([...Object.keys(oldVals), ...Object.keys(newVals)])];

    if (allKeys.length === 0) {
        diffBody.innerHTML = `<tr><td colspan="2" class="text-center text-muted" style="padding: 30px;">Không có dữ liệu thay đổi chi tiết</td></tr>`;
    } else {
        allKeys.forEach(key => {
            const oldVal = oldVals[key];
            const newVal = newVals[key];
            
            if (oldVal !== newVal || log.action === 'Added' || log.action === 'Deleted') {
                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td style="width: 35%;"><span class="font-bold">${localizeKey(key)}</span></td>
                    <td style="width: 65%;">
                        <div class="d-flex align-center flex-wrap gap-1">
                            ${log.action !== 'Added' ? `<span class="val-old">${formatValue(oldVal)}</span>` : ''}
                            ${log.action === 'Modified' ? '<i class="fa-solid fa-arrow-right arrow-icon"></i>' : ''}
                            ${log.action !== 'Deleted' ? `<span class="val-new">${formatValue(newVal)}</span>` : ''}
                        </div>
                    </td>
                `;
                diffBody.appendChild(tr);
            }
        });
    }
    
    document.getElementById('logModalOverlay').classList.add('active');
    document.getElementById('logDetailPanel').classList.add('active');
}

function localizeKey(key) {
    const dictionary = {
        'FirstName': 'Tên',
        'LastName': 'Họ đệm',
        'FullName': 'Họ và Tên',
        'Phone': 'Số điện thoại',
        'Email': 'Email',
        'Address': 'Địa chỉ',
        'IsActive': 'Trạng thái hoạt động',
        'Status': 'Trạng thái',
        'Gender': 'Giới tính',
        'DateOfBirth': 'Ngày sinh',
        'BaseSalary': 'Lương cơ bản',
        'Position': 'Chức vụ',
        'WorkingDays': 'Ngày công',
        'SalaryAmount': 'Số tiền lương',
        'Amount': 'Số tiền',
        'IsPaid': 'Đã thanh toán',
        'Note': 'Ghi chú',
        'Reason': 'Lý do',
        'StartDate': 'Ngày bắt đầu',
        'EndDate': 'Ngày kết thúc'
    };
    return dictionary[key] || key;
}

function formatValue(val) {
    if (val === null || val === undefined) return '<span class="text-muted italic">Trống</span>';
    if (typeof val === 'boolean') return val ? 'Bật/Nam' : 'Tắt/Nữ';
    if (typeof val === 'string' && val.includes('T00:00:00')) return formatDate(val);
    return val;
}

function closePanel() {
    document.getElementById('logModalOverlay').classList.remove('active');
    document.getElementById('logDetailPanel').classList.remove('active');
}

function formatJson(jsonStr) {
    if (!jsonStr) return null;
    try {
        const obj = JSON.parse(jsonStr);
        return JSON.stringify(obj, null, 2);
    } catch (e) {
        return jsonStr;
    }
}

function formatDate(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN');
}

function formatTime(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}
