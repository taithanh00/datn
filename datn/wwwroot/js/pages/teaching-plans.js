let allPlans = [];
let showInactivePlans = false;

document.addEventListener('DOMContentLoaded', async () => {
    await Promise.all([loadClasses(), loadCurriculums()]);
    loadPlans();
    const planForm = document.getElementById('planForm');
    if(planForm) planForm.addEventListener('submit', savePlan);
    
    const resetPlanBtn = document.getElementById('resetPlanBtn');
    if(resetPlanBtn) resetPlanBtn.addEventListener('click', resetEditMode);

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
    const s = document.getElementById('classId'); 
    if(!s) return;
    s.innerHTML = '<option value="">-- Tất cả --</option>'; 
    if(r.success) r.data.forEach(c => { s.innerHTML += `<option value="${c.id}">${c.name}</option>`; }); 
}

async function loadCurriculums() { 
    const r = await fetchJson('/Manager/Api/Curriculums'); 
    const s = document.getElementById('curriculumId'); 
    if(!s) return;
    s.innerHTML = '<option value="">-- Chọn --</option>'; 
    if(r.success) r.data.forEach(c => { s.innerHTML += `<option value="${c.id}">${c.title}</option>`; }); 
}

async function loadPlans() {
    const classIdElem = document.getElementById('classId');
    if(!classIdElem) return;
    const classId = classIdElem.value;
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
        if(isEdit) resetEditMode();
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
    
    // Khóa các trường khóa chính khi sửa
    document.getElementById('classId').disabled = true;
    document.getElementById('curriculumId').disabled = true;
    document.getElementById('startDate').disabled = true;
    
    document.getElementById('isEdit').value = 'true';
    document.getElementById('resetPlanBtn').style.display = 'block';
    document.querySelector('#planForm button[type="submit"]').textContent = 'Cập nhật kế hoạch';
    
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function resetEditMode() {
    document.getElementById('isEdit').value = 'false';
    document.getElementById('classId').disabled = false;
    document.getElementById('curriculumId').disabled = false;
    document.getElementById('startDate').disabled = false;
    document.getElementById('resetPlanBtn').style.display = 'none';
    document.querySelector('#planForm button[type="submit"]').textContent = 'Lưu kế hoạch';
    document.getElementById('planForm').reset();
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
    const a = document.getElementById('alert'); 
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
