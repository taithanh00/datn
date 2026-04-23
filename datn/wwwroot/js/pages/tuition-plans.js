document.addEventListener('DOMContentLoaded', loadPlans);

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
    if(overlay) overlay.classList.add('active');
}

function closePlanModal() {
    const overlay = document.getElementById('planModalOverlay');
    if(overlay) overlay.classList.remove('active');
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
        alert("Vui lòng nhập đầy đủ thông tin.");
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
    const monthElem = document.getElementById('genMonth');
    const yearElem = document.getElementById('genYear');
    if(!monthElem || !yearElem) return;

    const month = monthElem.value;
    const year = yearElem.value;
    
    if (!confirm(`Xác nhận khởi tạo học phí Tháng ${month}/${year} cho toàn bộ học sinh?`)) return;

    try {
        const res = await fetch(`/Tuition/Api/GenerateMonthlyTuition?month=${month}&year=${year}`, {
            method: 'POST'
        });
        const result = await res.json();
        if (result.success) {
            if(window.showToast) window.showToast('Khởi tạo xong', result.message, 'success');
        } else {
            alert(result.message);
        }
    } catch (e) { alert("Lỗi hệ thống khi khởi tạo."); }
}

// Expose to global scope
window.showCreatePlanModal = showCreatePlanModal;
window.closePlanModal = closePlanModal;
window.savePlan = savePlan;
window.generateTuitions = generateTuitions;
window.loadPlans = loadPlans;
