let currentId = null;
let showInactiveCurriculums = false;

document.addEventListener('DOMContentLoaded', async () => {
    await Promise.all([loadCurriculums(), loadSubjects()]);
    
    const entityForm = document.getElementById('entityForm');
    const resetFormBtn = document.getElementById('resetFormBtn');
    const clearBtn = document.getElementById('clearBtn');

    if(entityForm) entityForm.addEventListener('submit', saveEntity);
    if(resetFormBtn) resetFormBtn.addEventListener('click', resetForm);
    if(clearBtn) clearBtn.addEventListener('click', resetForm);

    // Status Tabs
    document.querySelectorAll('.status-tab').forEach(tab => {
        tab.addEventListener('click', function() {
            document.querySelectorAll('.status-tab').forEach(t => t.classList.remove('active'));
            this.classList.add('active');
            showInactiveCurriculums = this.getAttribute('data-show-inactive') === 'true';
            loadCurriculums();
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
        return { success: false, message: 'Lỗi kết nối.' }; 
    } 
}

async function loadCurriculums() {
    const result = await fetchJson(`/Manager/Api/Curriculums?showInactive=${showInactiveCurriculums}`);
    const tbody = document.getElementById('tableBody'); 
    if(!tbody) return;
    tbody.innerHTML = '';
    
    if (!result.success) {
        tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted" style="padding:24px;">Lỗi tải dữ liệu.</td></tr>`;
        return;
    }

    if (result.data.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted" style="padding:24px;">Không có dữ liệu.</td></tr>`;
        return;
    }

    result.data.forEach(c => { 
        const actionBtns = c.isActive
            ? `<button class="btn-table" onclick="editEntity(${c.id})">Sửa</button>
               <button class="btn-table delete" onclick="deleteEntity(${c.id})">Ẩn</button>`
            : `<button class="btn-table" onclick="reactivateEntity(${c.id})" style="color:var(--primary);">Khôi phục</button>`;

        tbody.innerHTML += `<tr>
            <td><strong>${c.title}</strong></td>
            <td>${c.subjectName||'--'}</td>
            <td>${c.ageFrom||'?'}-${c.ageTo||'?'} tuổi</td>
            <td class="note-muted">${c.description||''}</td>
            <td>${actionBtns}</td>
        </tr>`; 
    });
    
    if (typeof initPagination === 'function') {
        initPagination('curriculumsTable', 10);
    }
}

async function loadSubjects() {
    const result = await fetchJson('/Manager/Api/Subjects');
    const s = document.getElementById('subjectId'); 
    if(!s) return;
    s.innerHTML = '<option value="">-- Chọn --</option>';
    if (result.success) result.data.forEach(x => { s.innerHTML += `<option value="${x.id}">${x.name}</option>`; });
}

async function saveEntity(e) {
    e.preventDefault();
    const data = { 
        title: document.getElementById('title')?.value, 
        description: document.getElementById('description')?.value, 
        content: document.getElementById('content')?.value, 
        subjectId: parseInt(document.getElementById('subjectId')?.value) || null, 
        ageFrom: parseInt(document.getElementById('ageFrom')?.value) || null, 
        ageTo: parseInt(document.getElementById('ageTo')?.value) || null 
    };
    const url = currentId ? `/Manager/Api/Curriculum/${currentId}` : '/Manager/Api/Curriculum';
    const result = await fetchJson(url, { method: currentId ? 'PUT' : 'POST', body: JSON.stringify(data) });
    showAlert(result.success, result.message);
    if (result.success) { resetForm(); loadCurriculums(); }
}

async function editEntity(id) {
    const result = await fetchJson('/Manager/Api/Curriculums');
    if (!result.success) return;
    const item = result.data.find(c => c.id === id); if(!item) return;
    
    currentId = id;
    const titleElem = document.getElementById('title');
    const descElem = document.getElementById('description');
    const contentElem = document.getElementById('content');
    const subjectElem = document.getElementById('subjectId');
    const ageFromElem = document.getElementById('ageFrom');
    const ageToElem = document.getElementById('ageTo');
    const saveBtn = document.getElementById('saveBtn');
    const form = document.getElementById('entityForm');

    if(titleElem) titleElem.value = item.title;
    if(descElem) descElem.value = item.description || '';
    if(contentElem) contentElem.value = item.content || '';
    if(subjectElem) subjectElem.value = item.subjectId || '';
    if(ageFromElem) ageFromElem.value = item.ageFrom || '';
    if(ageToElem) ageToElem.value = item.ageTo || '';
    if(saveBtn) saveBtn.textContent = 'Cập nhật';
    if(form) form.scrollIntoView({ behavior: 'smooth' });
}

async function deleteEntity(id) { 
    if(!confirm('Ẩn chương trình học này?')) return; 
    const r = await fetchJson(`/Manager/Api/Curriculum/${id}`, { method: 'DELETE' }); 
    showAlert(r.success, r.message); 
    if(r.success) loadCurriculums(); 
}

async function reactivateEntity(id) {
    if(!confirm('Khôi phục chương trình học này?')) return;
    const r = await fetchJson(`/Manager/Api/Curriculum/Reactivate/${id}`, { method: 'POST' });
    showAlert(r.success, r.message);
    if(r.success) loadCurriculums();
}

function resetForm() { 
    currentId = null; 
    const form = document.getElementById('entityForm');
    const entityId = document.getElementById('entityId');
    const saveBtn = document.getElementById('saveBtn');

    if(form) form.reset(); 
    if(entityId) entityId.value = ''; 
    if(saveBtn) saveBtn.textContent = 'Lưu chương trình'; 
}

function showAlert(success, message) { 
    const a = document.getElementById('alert'); 
    if(!a) return;
    a.textContent = message; 
    a.className = `page-alert ${success ? 'success' : 'error'}`; 
    a.style.display = 'block'; 
    setTimeout(() => { a.style.display = 'none'; }, 3000); 
}

// Expose to global scope for onclick handlers
window.editEntity = editEntity;
window.deleteEntity = deleteEntity;
window.reactivateEntity = reactivateEntity;
window.resetForm = resetForm;
