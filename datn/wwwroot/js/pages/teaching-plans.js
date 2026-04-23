document.addEventListener('DOMContentLoaded', async () => {
    await Promise.all([loadClasses(), loadCurriculums()]);
    loadPlans();
    const planForm = document.getElementById('planForm');
    if(planForm) planForm.addEventListener('submit', savePlan);
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
    const url = classId ? `/Manager/Api/TeachingPlans?classId=${classId}` : '/Manager/Api/TeachingPlans';
    const r = await fetchJson(url);
    const tbody = document.getElementById('tableBody'); 
    if(!tbody) return;
    tbody.innerHTML = '';
    
    if (r.success) {
        r.data.forEach(p => { 
            tbody.innerHTML += `<tr><td>${p.className}</td><td><strong>${p.curriculumTitle}</strong></td><td>${p.startDate}</td><td>${p.endDate||'--'}</td><td><span class="status-badge ${p.status.toLowerCase()}">${p.status}</span></td><td><button class="btn-table" onclick="deletePlan(${p.classId},${p.curriculumId},'${p.startDate}')">Xóa</button></td></tr>`; 
        });
        
        if (typeof initPagination === 'function') {
            initPagination('plansTable', 10);
        }
    }
}

async function savePlan(e) {
    e.preventDefault();
    const data = { 
        classId: parseInt(document.getElementById('classId')?.value), 
        curriculumId: parseInt(document.getElementById('curriculumId')?.value), 
        startDate: document.getElementById('startDate')?.value, 
        endDate: document.getElementById('endDate')?.value || null, 
        status: document.getElementById('status')?.value 
    };
    const r = await fetchJson('/Manager/Api/TeachingPlan', { method: 'POST', body: JSON.stringify(data) });
    showAlert(r.success, r.message); 
    if(r.success) loadPlans();
}

async function deletePlan(cId, cuId, sd) { 
    if(!confirm('Xóa?')) return; 
    const r = await fetchJson(`/Manager/Api/TeachingPlan?classId=${cId}&curriculumId=${cuId}&startDate=${sd}`, { method: 'DELETE' }); 
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
window.savePlan = savePlan;
