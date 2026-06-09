let currentWeekStart = getMonday(new Date());

document.addEventListener('DOMContentLoaded', function() {
    document.getElementById('weekPicker').value = currentWeekStart.toISOString().split('T')[0];
    loadWeeklyMenu();

    document.getElementById('weekPicker').addEventListener('change', function(e) {
        currentWeekStart = getMonday(new Date(e.target.value));
        loadWeeklyMenu();
    });
});

function getMonday(d) {
    d = new Date(d);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    return new Date(d.setDate(diff));
}

async function loadWeeklyMenu() {
    showLoading();
    try {
        const response = await fetch(`/Manager/Nutrition/GetWeeklyMenu?start=${currentWeekStart.toISOString()}`);
        const data = await response.json();
        renderGrid(data);
    } catch (error) {
        console.error('Error loading menu:', error);
        showToast('Không thể tải thực đơn', 'error');
    } finally {
        hideLoading();
    }
}

function renderGrid(menuItems) {
    const cells = document.querySelectorAll('.menu-cell');
    cells.forEach(cell => {
        cell.innerHTML = '<i class="fa-solid fa-plus text-muted opacity-50"></i>';
        cell.classList.remove('has-data');
        cell.onclick = () => openMenuModal(cell.dataset.day, cell.dataset.type);
    });

    menuItems.forEach(item => {
        const itemDate = new Date(item.date);
        const diff = Math.floor((itemDate - currentWeekStart) / (1000 * 60 * 60 * 24)) + 1;
        const cell = document.querySelector(`.menu-cell[data-day="${diff}"][data-type="${item.mealType}"]`);

        if (cell) {
            cell.classList.add('has-data');
            cell.innerHTML = `
                <div class="dish-name">${item.dishName}</div>
                <div class="ingredients-tag">${item.ingredients || ''}</div>
                ${item.menuOverrides && item.menuOverrides.length > 0
                    ? `<div class="allergy-badge" title="Có ${item.menuOverrides.length} suất ăn đặc biệt">${item.menuOverrides.length}</div>`
                    : ''}
            `;
            cell.onclick = () => openMenuModal(diff, item.mealType, item);
        }
    });

    updateAllergyAlerts(menuItems);
}

function openMenuModal(dayDiff, mealType, existingData = null) {
    const targetDate = new Date(currentWeekStart);
    targetDate.setDate(targetDate.getDate() + (parseInt(dayDiff) - 1));

    document.getElementById('menuId').value = existingData ? existingData.id : 0;
    document.getElementById('menuDate').value = targetDate.toISOString().split('T')[0];
    document.getElementById('menuType').value = mealType;
    document.getElementById('dishName').value = existingData ? existingData.dishName : '';
    document.getElementById('ingredients').value = existingData ? (existingData.ingredients || '') : '';
    document.getElementById('isActive').value = existingData ? existingData.isActive.toString() : 'true';
    document.getElementById('menuNote').value = existingData ? (existingData.note || '') : '';

    const modal = new bootstrap.Modal(document.getElementById('menuModal'));
    modal.show();
}

async function saveMenu() {
    const menu = {
        id: parseInt(document.getElementById('menuId').value),
        date: document.getElementById('menuDate').value,
        mealType: parseInt(document.getElementById('menuType').value),
        dishName: document.getElementById('dishName').value,
        ingredients: document.getElementById('ingredients').value,
        isActive: document.getElementById('isActive').value === 'true',
        note: document.getElementById('menuNote').value
    };

    if (!menu.dishName) {
        showToast('Vui lòng nhập tên món ăn', 'warning');
        return;
    }

    try {
        const response = await fetch('/Manager/Nutrition/SaveMenu', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(menu)
        });
        const result = await response.json();
        if (result.success) {
            showToast('Lưu thực đơn thành công', 'success');
            bootstrap.Modal.getInstance(document.getElementById('menuModal')).hide();
            loadWeeklyMenu();
        }
    } catch (error) {
        showToast('Lỗi khi lưu thực đơn', 'error');
    }
}

function updateAllergyAlerts(menuItems) {
    const container = document.getElementById('allergyAlertsContainer');
    const grid = document.getElementById('allergyAlertsGrid');
    grid.innerHTML = '';

    let hasAlerts = false;
    menuItems.forEach(item => {
        if (item.menuOverrides && item.menuOverrides.length > 0) {
            hasAlerts = true;
            item.menuOverrides.forEach(ov => {
                if (ov.studentId) {
                    const card = document.createElement('div');
                    card.className = 'col-md-4';
                    card.innerHTML = `
                        <div class="alert-card-premium">
                            <div class="d-flex justify-content-between">
                                <strong>${ov.student ? ov.student.firstName + ' ' + ov.student.lastName : 'Học sinh'}</strong>
                                <span class="badge bg-soft-warning text-warning">${ov.reason}</span>
                            </div>
                            <div class="mt-2 small text-muted">
                                <div>Món gốc: ${item.dishName}</div>
                                <div class="text-primary">Đổi thành: ${ov.newDishName}</div>
                            </div>
                        </div>
                    `;
                    grid.appendChild(card);
                }
            });
        }
    });

    container.style.display = hasAlerts ? 'block' : 'none';
}

function showToast(msg, type) {
    const normalizedType = type === 'error' || type === 'warning' || type === 'success' ? type : 'info';
    const title = normalizedType === 'success' ? 'Thành công' : normalizedType === 'error' ? 'Có lỗi' : normalizedType === 'warning' ? 'Cảnh báo' : 'Thông tin';
    if (window.notifySuccess && normalizedType === 'success') return window.notifySuccess(msg, title);
    if (window.notifyError && normalizedType === 'error') return window.notifyError(msg, title);
    if (window.notifyWarning && normalizedType === 'warning') return window.notifyWarning(msg, title);
    if (window.notifyInfo) return window.notifyInfo(msg, title);
}

function showLoading() {
    if (window.appLoading) {
        window.appLoading.setTable('gridBody', 7);
    }
}
function hideLoading() { }
