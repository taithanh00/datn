document.addEventListener('DOMContentLoaded', () => {
    loadPlans();
    initializeMonthlyStudentFees();
});

let monthlyStudentFeeRows = [];

async function loadPlans() {
    try {
        const res = await fetch('/Tuition/Api/Plans');
        const data = await res.json();
        const list = document.getElementById('plansList');
        if(!list) return;
        
        if (data.success) {
            if (data.data.length === 0) {
                list.innerHTML = '<div style="grid-column: 1/-1;" class="empty-state py-5"><i class="fa-solid fa-folder-open"></i><p>Chưa có kế hoạch học phí nào được thiết lập.</p></div>';
                return;
            }

            list.innerHTML = data.data.map(p => `
                <div class="card" style="border: 1px solid var(--border); box-shadow: none;">
                    <div class="d-flex justify-between align-start mb-2">
                        <div class="badge badge-info">${p.ageFrom} - ${p.ageTo} tuổi</div>
                        <span style="font-size: 0.75rem; color: var(--success);"><i class="fa-solid fa-circle-check"></i> Áp dụng</span>
                    </div>
                    <div class="text-muted" style="font-size: 0.85rem; margin-top: 12px; margin-bottom: 4px;">Mức phí cố định mỗi tháng</div>
                    <div style="font-size: 1.5rem; font-weight: 800; color: var(--text-main); margin-bottom: 16px;">${new Intl.NumberFormat('vi-VN').format(p.amount)} <span style="font-size: 1rem; color: var(--text-muted); font-weight: 500;">đ</span></div>
                    
                    <div style="display: flex; gap: 8px;">
                        <button class="btn btn-outline" style="flex: 1; justify-content: center; padding: 4px; font-size: 0.85rem;"><i class="fa-solid fa-pen"></i> Sửa</button>
                        <button class="btn btn-outline" style="border-color: var(--danger); color: var(--danger); padding: 4px 10px;"><i class="fas fa-trash"></i></button>
                    </div>
                </div>
            `).join('');
        }
    } catch (e) { console.error("Load plans failed", e); }
}

function showCreatePlanModal() {
    const form = document.getElementById('planForm');
    const overlay = document.getElementById('planModalOverlay');
    if(form) form.reset();
    if(overlay) overlay.classList.add('show');
}

function closePlanModal() {
    const overlay = document.getElementById('planModalOverlay');
    if(overlay) overlay.classList.remove('show');
}

async function savePlan() {
    const amountElem = document.getElementById('planAmount');
    const ageFromElem = document.getElementById('planAgeFrom');
    const ageToElem = document.getElementById('planAgeTo');

    if(!amountElem || !ageFromElem || !ageToElem) return;

    const model = {
        amount: parseFloat(amountElem.value),
        ageFrom: parseInt(ageFromElem.value),
        ageTo: parseInt(ageToElem.value)
    };

    if (!model.amount || isNaN(model.ageFrom)) {
        window.notifyWarning("Vui lòng nhập đầy đủ thông tin.");
        return;
    }

    try {
        const res = await fetch('/Tuition/Api/Plans/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(model)
        });
        const result = await res.json();
        if (result.success) {
            closePlanModal();
            loadPlans();
            if(window.showToast) window.showToast('Thành công', result.message, 'success');
        }
    } catch (e) { console.error(e); }
}

async function generateTuitions() {
    const btn = document.getElementById('btnGenerate');
    const monthElem = document.getElementById('genMonth');
    const yearElem = document.getElementById('genYear');
    if(!monthElem || !yearElem || !btn) return;

    const month = monthElem.value;
    const year = yearElem.value;
    
    if (!(await window.appConfirm(`Xác nhận khởi tạo học phí Tháng ${month}/${year} cho toàn bộ học sinh?`))) return;

    // Frontend Protection: Disable button
    const originalText = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Đang xử lý...';

    try {
        const res = await fetch(`/Tuition/Api/GenerateMonthlyTuition?month=${month}&year=${year}`, {
            method: 'POST'
        });
        const result = await res.json();
        if (result.success) {
            if(window.showToast) window.showToast('Khởi tạo xong', result.message, 'success');
        } else {
            window.notifyError(result.message);
        }
    } catch (e) { 
        window.notifyError("Lỗi hệ thống khi khởi tạo."); 
    } finally {
        // Re-enable button
        btn.disabled = false;
        btn.innerHTML = originalText;
    }
}

async function initializeMonthlyStudentFees() {
    const classSelect = document.getElementById('feeConfigClass');
    const feeSelect = document.getElementById('feeConfigItem');
    if (!classSelect || !feeSelect) return;

    try {
        const [classRes, feeRes] = await Promise.all([
            fetch('/Manager/Api/Classes'),
            fetch('/Tuition/Api/FeeItems')
        ]);
        const classPayload = await classRes.json();
        const feePayload = await feeRes.json();

        classSelect.innerHTML = '<option value="">-- Chọn lớp --</option>';
        if (classPayload.success) {
            classPayload.data.forEach(c => {
                classSelect.innerHTML += `<option value="${c.id}">${escapeHtml(c.name)}</option>`;
            });
        }

        feeSelect.innerHTML = '<option value="">-- Chọn khoản thu --</option>';
        if (feePayload.success) {
            feePayload.data.filter(f => f.isActive).forEach(f => {
                feeSelect.innerHTML += `<option value="${f.id}">${escapeHtml(f.name)} (${formatCurrency(f.defaultAmount)})</option>`;
            });
        }
    } catch (e) {
        console.error(e);
    }
}

async function loadMonthlyStudentFees() {
    const month = document.getElementById('feeConfigMonth')?.value;
    const year = document.getElementById('feeConfigYear')?.value;
    const classId = document.getElementById('feeConfigClass')?.value;
    const feeItemId = document.getElementById('feeConfigItem')?.value;
    const body = document.getElementById('monthlyStudentFeeBody');
    if (!body || !month || !year || !classId || !feeItemId) return;

    if (window.appLoading) {
        window.appLoading.setTable(body, 4);
    }

    try {
        const res = await fetch(`/Tuition/Api/MonthlyStudentFees?month=${month}&year=${year}&classId=${classId}&feeItemId=${feeItemId}`);
        const payload = await res.json();
        if (!payload.success) {
            body.innerHTML = `<tr><td colspan="4" class="text-center text-danger py-4">${escapeHtml(payload.message)}</td></tr>`;
            return;
        }

        monthlyStudentFeeRows = payload.data;
        if (!monthlyStudentFeeRows.length) {
            body.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-5">Lớp này chưa có học sinh.</td></tr>';
            return;
        }

        body.innerHTML = monthlyStudentFeeRows.map(row => `
            <tr data-student-id="${row.id}">
                <td>
                    <input type="checkbox" class="fee-row-applied" ${row.isApplied ? 'checked' : ''} style="width:18px;height:18px;accent-color:var(--primary);">
                </td>
                <td>
                    <div class="d-flex align-center gap-2">
                        <img src="${row.avatarPath || '/images/lion_orange.png'}" alt="Avatar" style="width:34px;height:34px;border-radius:50%;object-fit:cover;border:1px solid var(--border);" onerror="this.src='/images/lion_orange.png'">
                        <strong>${escapeHtml(row.fullName)}</strong>
                    </div>
                </td>
                <td>
                    <input type="number" class="form-input fee-row-amount" min="0" value="${row.amount}" />
                </td>
                <td>
                    <input type="text" class="form-input fee-row-note" value="${escapeHtml(row.note || '')}" placeholder="Ghi chú riêng..." />
                </td>
            </tr>
        `).join('');
    } catch (e) {
        console.error(e);
        body.innerHTML = '<tr><td colspan="4" class="text-center text-danger py-4">Lỗi kết nối.</td></tr>';
    }
}

async function saveMonthlyStudentFees() {
    const month = parseInt(document.getElementById('feeConfigMonth')?.value || '0', 10);
    const year = parseInt(document.getElementById('feeConfigYear')?.value || '0', 10);
    const classId = parseInt(document.getElementById('feeConfigClass')?.value || '0', 10);
    const feeItemId = parseInt(document.getElementById('feeConfigItem')?.value || '0', 10);

    if (!month || !year || !classId || !feeItemId) {
        window.notifyWarning('Vui lòng chọn tháng, năm, lớp và khoản thu.');
        return;
    }

    const students = Array.from(document.querySelectorAll('#monthlyStudentFeeBody tr[data-student-id]')).map(row => ({
        studentId: parseInt(row.dataset.studentId, 10),
        isApplied: row.querySelector('.fee-row-applied').checked,
        amount: parseFloat(row.querySelector('.fee-row-amount').value) || 0,
        note: row.querySelector('.fee-row-note').value.trim()
    }));

    try {
        const res = await fetch('/Tuition/Api/MonthlyStudentFees', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ month, year, classId, feeItemId, students })
        });
        const result = await res.json();
        if (result.success) {
            if (window.showToast) window.showToast('Thành công', result.message, 'success');
            await loadMonthlyStudentFees();
        } else {
            window.notifyError(result.message);
        }
    } catch (e) {
        console.error(e);
        window.notifyError('Lỗi hệ thống khi lưu cấu hình khoản thu.');
    }
}

function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount || 0);
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

// Expose to global scope
window.showCreatePlanModal = showCreatePlanModal;
window.closePlanModal = closePlanModal;
window.savePlan = savePlan;
window.generateTuitions = generateTuitions;
window.loadPlans = loadPlans;
window.loadMonthlyStudentFees = loadMonthlyStudentFees;
window.saveMonthlyStudentFees = saveMonthlyStudentFees;
