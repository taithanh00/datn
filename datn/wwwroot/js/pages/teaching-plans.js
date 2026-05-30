let allPlans = [];
let showInactivePlans = false;
let planPanelOverlay = null;
let planSlidePanel = null;
let planPanelTitle = null;

document.addEventListener('DOMContentLoaded', async () => {
    planPanelOverlay = document.getElementById('planPanelOverlay');
    planSlidePanel = document.getElementById('planSlidePanel');
    planPanelTitle = document.getElementById('planPanelTitle');
    if (planPanelOverlay) planPanelOverlay.addEventListener('click', closePlanPanel);

    await Promise.all([loadClasses(), loadCurriculums()]);
    loadPlans();
    const planForm = document.getElementById('planForm');
    if(planForm) planForm.addEventListener('submit', savePlan);

    // Status Tabs
    document.querySelectorAll('.status-tab').forEach(tab => {
        tab.addEventListener('click', function() {
            document.querySelectorAll('.status-tab').forEach(t => t.classList.remove('active'));
            this.classList.add('active');
            showInactivePlans = this.getAttribute('data-show-inactive') === 'true';
            loadPlans();
        });
    });
});

async function fetchJson(url, options={}) { 
    try { 
        const r = await fetch(url, {
            headers: { 'Content-Type': 'application/json' },
            ...options
        }); 
        return await r.json(); 
    } catch(e) { 
        return { success: false, message: 'Lỗi.' }; 
    } 
}

async function loadClasses() { 
    const r = await fetchJson('/Manager/Api/Classes'); 
    const select = document.getElementById('classId'); 
    const filter = document.getElementById('filterClassId'); 
    if(select) select.innerHTML = '<option value="">-- Chọn --</option>'; 
    if(filter) filter.innerHTML = '<option value="">Tất cả lớp</option>'; 
    if(r.success) {
        r.data.forEach(c => {
            const option = `<option value="${c.id}">${c.name}</option>`;
            if(select) select.innerHTML += option;
            if(filter) filter.innerHTML += option;
        });
    }
}

async function loadCurriculums() { 
    const r = await fetchJson('/Manager/Api/Curriculums'); 
    const s = document.getElementById('curriculumId'); 
    if(!s) return;
    s.innerHTML = '<option value="">-- Chọn --</option>'; 
    if(r.success) r.data.forEach(c => { s.innerHTML += `<option value="${c.id}">${c.title}</option>`; }); 
}

async function loadPlans() {
    const filterClassElem = document.getElementById('filterClassId');
    const classId = filterClassElem?.value;
    let url = classId ? `/Manager/Api/TeachingPlans?classId=${classId}` : '/Manager/Api/TeachingPlans';
    url += (classId ? '&' : '?') + `showInactive=${showInactivePlans}`;
    const r = await fetchJson(url);
    
    if (r.success) {
        allPlans = r.data;
        renderPlans(allPlans);
    }
}

function renderPlans(plans) {
    const tbody = document.getElementById('tableBody'); 
    if(!tbody) return;
    tbody.innerHTML = '';
    
    if (plans.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="text-muted" style="text-align:center; padding:20px;">Không có kế hoạch nào khớp.</td></tr>';
        return;
    }

    plans.forEach(p => { 
        const statusText = p.status === 'Planned' ? 'Đã lên lịch' : p.status === 'InProgress' ? 'Đang thực hiện' : 'Hoàn thành';
        const actionBtns = p.isActive 
            ? `<button class="btn-table" onclick="editPlan(${p.classId},${p.curriculumId},'${p.startDate}')">Sửa</button>
               <button class="btn-table delete" onclick="deletePlan(${p.classId},${p.curriculumId},'${p.startDate}')">Ẩn</button>`
            : `<button class="btn-table" onclick="reactivatePlan(${p.classId},${p.curriculumId},'${p.startDate}')" style="color:var(--primary);">Khôi phục</button>`;
        tbody.innerHTML += `<tr>
            <td>${p.className}</td>
            <td><strong>${p.curriculumTitle}</strong></td>
            <td>${p.startDate}</td>
            <td>${p.endDate||'--'}</td>
            <td><span class="status-badge ${p.status.toLowerCase()}">${statusText}</span></td>
            <td>${actionBtns}</td>
        </tr>`; 
    });
    
    if (typeof initPagination === 'function') {
        initPagination('plansTable', 10);
    }
}

function applyFilters() {
    const query = document.getElementById('searchPlan')?.value.toLowerCase();
    const filtered = allPlans.filter(p => p.curriculumTitle.toLowerCase().includes(query));
    renderPlans(filtered);
}

function openPlanPanel(title = 'Thêm kế hoạch mới') {
    if (planSlidePanel) planSlidePanel.classList.add('active');
    if (planPanelOverlay) planPanelOverlay.classList.add('active');
    if (planPanelTitle) planPanelTitle.textContent = title;
}

function closePlanPanel() {
    if (planSlidePanel) planSlidePanel.classList.remove('active');
    if (planPanelOverlay) planPanelOverlay.classList.remove('active');
    resetEditMode();
}

function prepareCreate() {
    const form = document.getElementById('planForm');
    if (form) form.reset();
    document.getElementById('isEdit').value = 'false';
    document.getElementById('classId').disabled = false;
    document.getElementById('curriculumId').disabled = false;
    document.getElementById('startDate').disabled = false;
    const submitBtn = document.querySelector('#planForm button[type="submit"]');
    if (submitBtn) submitBtn.textContent = 'Lưu kế hoạch';
    const resetBtn = document.getElementById('resetPlanBtn');
    if (resetBtn) resetBtn.style.display = 'none';
    openPlanPanel('Thêm kế hoạch mới');
}

async function savePlan(e) {
    e.preventDefault();
    const isEdit = document.getElementById('isEdit').value === 'true';
    const data = { 
        classId: parseInt(document.getElementById('classId')?.value), 
        curriculumId: parseInt(document.getElementById('curriculumId')?.value), 
        startDate: document.getElementById('startDate')?.value, 
        endDate: document.getElementById('endDate')?.value || null, 
        status: document.getElementById('status')?.value 
    };
    
    const url = '/Manager/Api/TeachingPlan';
    const method = isEdit ? 'PUT' : 'POST';
    
    const r = await fetchJson(url, { method: method, body: JSON.stringify(data) });
    showAlert(r.success, r.message);
    if(r.success) {
        closePlanPanel();
        loadPlans();
    }
}

function editPlan(cId, cuId, sd) {
    const plan = allPlans.find(p => p.classId == cId && p.curriculumId == cuId && p.startDate == sd);
    if(!plan) return;

    document.getElementById('classId').value = plan.classId;
    document.getElementById('curriculumId').value = plan.curriculumId;
    document.getElementById('startDate').value = plan.startDate;
    document.getElementById('endDate').value = plan.endDate || '';
    document.getElementById('status').value = plan.status;
    
    // Cho phép sửa lớp và chương trình khi cần
    document.getElementById('classId').disabled = false;
    document.getElementById('curriculumId').disabled = false;
    document.getElementById('startDate').disabled = true;
    
    document.getElementById('isEdit').value = 'true';
    if (planPanelTitle) planPanelTitle.textContent = 'Chỉnh sửa kế hoạch';
    const submitBtn = document.querySelector('#planForm button[type="submit"]');
    if (submitBtn) submitBtn.textContent = 'Cập nhật kế hoạch';
    openPlanPanel('Chỉnh sửa kế hoạch');
}

function resetEditMode() {
    document.getElementById('isEdit').value = 'false';
    const classSelect = document.getElementById('classId');
    const curriculumSelect = document.getElementById('curriculumId');
    const startDate = document.getElementById('startDate');
    if (classSelect) classSelect.disabled = false;
    if (curriculumSelect) curriculumSelect.disabled = false;
    if (startDate) startDate.disabled = false;
    const submitBtn = document.querySelector('#planForm button[type="submit"]');
    if (submitBtn) submitBtn.textContent = 'Lưu kế hoạch';
    const form = document.getElementById('planForm');
    if (form) form.reset();
}

async function deletePlan(cId, cuId, sd) { 
    if(!confirm('Ẩn kế hoạch này?')) return; 
    const r = await fetchJson(`/Manager/Api/TeachingPlan?classId=${cId}&curriculumId=${cuId}&startDate=${sd}`, { method: 'DELETE' }); 
    showAlert(r.success, r.message); 
    if(r.success) loadPlans(); 
}

async function reactivatePlan(cId, cuId, sd) {
    if(!confirm('Khôi phục kế hoạch này?')) return;
    const r = await fetchJson(`/Manager/Api/TeachingPlan/Reactivate?classId=${cId}&curriculumId=${cuId}&startDate=${sd}`, { method: 'POST' });
    showAlert(r.success, r.message);
    if(r.success) loadPlans();
}

function showAlert(success, msg) { 
    const a = document.getElementById('planPanelAlert') || document.getElementById('alert'); 
    if(!a) return;
    a.textContent = msg; 
    a.className = `page-alert ${success ? 'success' : 'error'}`; 
    a.style.display = 'block'; 
    setTimeout(() => { a.style.display = 'none'; }, 3000); 
}

// Expose to global scope
window.loadPlans = loadPlans;
window.deletePlan = deletePlan;
window.reactivatePlan = reactivatePlan;
window.savePlan = savePlan;
window.editPlan = editPlan;
window.applyFilters = applyFilters;
window.resetEditMode = resetEditMode;
window.prepareCreate = prepareCreate;
window.closePlanPanel = closePlanPanel;
window.openPlanPanel = openPlanPanel;
