let selectedActivityId = null;

document.addEventListener('DOMContentLoaded', () => {
    loadActivities();

    const saveBtn = document.getElementById('saveParticipantsBtn');
    if(saveBtn) saveBtn.addEventListener('click', saveParticipants);
    
    const selectAllCheckbox = document.getElementById('selectAll');
    if(selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function() {
            const checkboxes = document.querySelectorAll('.student-checkbox');
            checkboxes.forEach(cb => cb.checked = this.checked);
        });
    }
});

async function fetchJson(url, options = {}) {
    try {
        const response = await fetch(url, { headers: { 'Content-Type': 'application/json' }, ...options });
        return await response.json();
    } catch (e) {
        return { success: false, message: 'Lỗi kết nối.' };
    }
}

async function loadActivities() {
    const result = await fetchJson('/Employee/Api/Activities');
    const tbody = document.getElementById('activitiesTableBody');
    if (!tbody) return;
    
    if (result.success) {
        if (result.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-5"><div class="empty-state"><i class="fa-solid fa-calendar-xmark"></i><p>Không có hoạt động nào được gán cho lớp.</p></div></td></tr>';
            return;
        }
        tbody.innerHTML = result.data.map(a => `
            <tr>
                <td><strong>${a.name}</strong></td>
                <td><div class="badge badge-info"><i class="fa-regular fa-calendar"></i> ${a.date}</div></td>
                <td>${a.locationName || '--'}</td>
                <td style="text-align: right;">
                    <button class="btn-table" onclick="viewParticipants(${a.id}, '${a.name.replace(/'/g, "\\'")}')">
                        <i class="fa-solid fa-list-check"></i> Chọn HS
                    </button>
                </td>
            </tr>
        `).join('');
        if(typeof initPagination === 'function') initPagination('activitiesTable', 10);
    } else {
        tbody.innerHTML = `<tr><td colspan="4" class="text-center text-danger">${result.message}</td></tr>`;
    }
}

async function viewParticipants(id, name) {
    selectedActivityId = id;
    document.getElementById('currentActivityName').textContent = `HS tham gia: ${name}`;
    
    // Show slide panel
    document.getElementById('modalOverlay').classList.add('active');
    document.getElementById('slidePanel').classList.add('active');

    const tbody = document.getElementById('participantsTableBody');
    tbody.innerHTML = '<tr><td colspan="2" class="text-center py-4"><div class="spinner"></div></td></tr>';
    document.getElementById('selectAll').checked = false;

    const result = await fetchJson(`/Employee/Api/Activity/${id}/Participants`);

    if (result.success) {
        if (result.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="2" class="text-center text-muted py-4">Lớp không có học sinh.</td></tr>';
            return;
        }
        
        tbody.innerHTML = result.data.map(s => `
            <tr>
                <td style="text-align: center;">
                    <input type="checkbox" class="student-checkbox" value="${s.id}" ${s.isParticipating ? 'checked' : ''} />
                </td>
                <td>
                    <div class="d-flex align-center gap-2">
                        <img src="${s.avatarPath || '/images/lion_orange.png'}" style="width:32px; height:32px; border-radius:50%; object-fit:cover;" onerror="this.src='/images/lion_orange.png'">
                        <div style="font-weight: 600;">${s.fullName}</div>
                    </div>
                </td>
            </tr>
        `).join('');
    } else {
        showAlert('participantAlert', false, result.message);
        tbody.innerHTML = '';
    }
}

async function saveParticipants() {
    if (!selectedActivityId) return;

    const btn = document.getElementById('saveParticipantsBtn');
    btn.disabled = true;
    btn.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> Đang lưu...';

    const studentIds = Array.from(document.querySelectorAll('.student-checkbox:checked')).map(cb => parseInt(cb.value));
    
    const result = await fetchJson(`/Employee/Api/Activity/${selectedActivityId}/Participants`, {
        method: 'POST',
        body: JSON.stringify(studentIds)
    });

    showAlert('participantAlert', result.success, result.message);
    
    btn.disabled = false;
    btn.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Lưu danh sách';
    
    if (result.success) {
        setTimeout(closePanel, 1500);
    }
}

function closePanel() {
    document.getElementById('modalOverlay').classList.remove('active');
    document.getElementById('slidePanel').classList.remove('active');
    selectedActivityId = null;
}

function showAlert(id, success, message) {
    const alert = document.getElementById(id);
    alert.textContent = message;
    alert.className = `page-alert ${success ? 'success' : 'error'}`;
    alert.style.display = 'block';
    setTimeout(() => { alert.style.display = 'none'; }, 3000);
}
