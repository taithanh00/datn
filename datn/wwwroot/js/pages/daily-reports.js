let currentClassId = 0;
let currentReports = [];
let currentMeals = [];

document.addEventListener('DOMContentLoaded', async function() {
    await loadClasses();
    loadClassReports();

    document.getElementById('classSelector').addEventListener('change', function() {
        currentClassId = this.value;
        loadClassReports();
    });

    document.getElementById('reportDate').addEventListener('change', loadClassReports);

    // Setup Emoji Buttons
    setupStatusButtons('eatingStatusGroup');
    setupStatusButtons('sleepingStatusGroup');
});

function setupStatusButtons(groupId) {
    const group = document.getElementById(groupId);
    const btns = group.querySelectorAll('.status-btn');
    btns.forEach(btn => {
        btn.onclick = function() {
            btns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
        };
    });
}

async function loadClasses() {
    try {
        const response = await fetch('/Manager/GetClassesOverview'); // Reusing existing API
        const classes = await response.json();
        const selector = document.getElementById('classSelector');
        classes.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = c.name;
            selector.appendChild(opt);
        });
        currentClassId = selector.value;
    } catch (e) {
        console.error('Error loading classes', e);
    }
}

async function loadClassReports() {
    const date = document.getElementById('reportDate').value;
    if (!currentClassId) return;

    try {
        const response = await fetch(`/DailyReport/GetClassStatus?classId=${currentClassId}&date=${date}`);
        const data = await response.json();
        currentReports = data.reports;
        currentMeals = data.meals;
        renderStudentGrid();
    } catch (e) {
        showToast('Không thể tải dữ liệu lớp', 'error');
    }
}

function renderStudentGrid() {
    const grid = document.getElementById('studentReportsGrid');
    grid.innerHTML = '';

    currentMeals.forEach(studentMeal => {
        const report = currentReports.find(r => r.studentId === studentMeal.studentId);
        const col = document.createElement('div');
        col.className = 'col-md-3 mb-4';
        
        const isFever = false; // Add logic if temperature > 37.5
        
        col.innerHTML = `
            <div class="student-report-card" onclick="openReportModal(${studentMeal.studentId})">
                ${studentMeal.allergies ? '<div class="allergy-alert-ribbon">DỊ ỨNG</div>' : ''}
                <div class="d-flex align-items-center gap-3 mb-3">
                    <img src="/images/lion_orange.png" class="student-avatar-small" />
                    <div>
                        <h6 class="mb-0 text-truncate" style="max-width:140px;">${studentMeal.studentName}</h6>
                        <div class="small text-muted">${studentMeal.allergies || 'Sức khỏe tốt'}</div>
                    </div>
                </div>
                
                <div class="status-indicator">
                    <div class="status-badge-mini ${report ? 'active' : ''}" title="Ăn uống">
                        ${report ? getEatingEmoji(report.eatingStatus) : '🍽️'}
                    </div>
                    <div class="status-badge-mini ${report ? 'active' : ''}" title="Ngủ nghỉ">
                        ${report ? getSleepingEmoji(report.sleepingStatus) : '💤'}
                    </div>
                    <div class="ms-auto temp-badge ${isFever ? 'fever' : ''}">
                        ${report?.healthNote?.match(/\d+\.\d+/)?.[0] || '--'}°C
                    </div>
                </div>
            </div>
        `;
        grid.appendChild(col);
    });

    updateCompletionRate();
}

function openReportModal(studentId) {
    const studentMeal = currentMeals.find(sm => sm.studentId === studentId);
    const report = currentReports.find(r => r.studentId === studentId);

    document.getElementById('studentNameHeader').textContent = `Nhật ký bé: ${studentMeal.studentName}`;
    document.getElementById('editStudentId').value = studentId;

    // Reset buttons
    resetStatusButtons('eatingStatusGroup', report?.eatingStatus);
    resetStatusButtons('sleepingStatusGroup', report?.sleepingStatus);

    document.getElementById('eatingNote').value = report?.eatingNote || '';
    document.getElementById('sleepingNote').value = report?.sleepingNote || '';
    document.getElementById('moodNote').value = report?.moodNote || '';
    document.getElementById('generalNote').value = report?.activityNote || '';
    
    // Get temperature if exists
    const tempMatch = report?.healthNote?.match(/\d+\.\d+/);
    document.getElementById('temperature').value = tempMatch ? tempMatch[0] : '';

    new bootstrap.Modal(document.getElementById('reportModal')).show();
}

function resetStatusButtons(groupId, value) {
    const btns = document.getElementById(groupId).querySelectorAll('.status-btn');
    btns.forEach(b => b.classList.remove('active'));
    if (value !== undefined && value !== null) {
        btns[value].classList.add('active');
    }
}

async function saveReport() {
    const studentId = parseInt(document.getElementById('editStudentId').value);
    const report = {
        id: currentReports.find(r => r.studentId === studentId)?.id || 0,
        studentId: studentId,
        date: document.getElementById('reportDate').value,
        eatingStatus: getActiveValue('eatingStatusGroup'),
        eatingNote: document.getElementById('eatingNote').value,
        sleepingStatus: getActiveValue('sleepingStatusGroup'),
        sleepingNote: document.getElementById('sleepingNote').value,
        moodNote: document.getElementById('moodNote').value,
        activityNote: document.getElementById('generalNote').value,
        healthNote: `Thân nhiệt: ${document.getElementById('temperature').value || '--'}°C`
    };

    try {
        // Save Daily Report
        const res1 = await fetch('/DailyReport/SaveReport', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(report)
        });

        // Save Health Record (Temperature)
        const temp = parseFloat(document.getElementById('temperature').value);
        if (!isNaN(temp)) {
            await fetch('/DailyReport/SaveHealth', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    studentId: studentId,
                    date: report.date,
                    temperature: temp
                })
            });
        }

        const result = await res1.json();
        if (result.success) {
            showToast('Đã cập nhật nhật ký', 'success');
            bootstrap.Modal.getInstance(document.getElementById('reportModal')).hide();
            loadClassReports();
        }
    } catch (e) {
        showToast('Lỗi khi lưu dữ liệu', 'error');
    }
}

function getActiveValue(groupId) {
    const btn = document.getElementById(groupId).querySelector('.status-btn.active');
    return btn ? parseInt(btn.dataset.val) : 0;
}

function getEatingEmoji(status) {
    const emojis = ['😋', '🙂', '😟'];
    return emojis[status] || '🍽️';
}

function getSleepingEmoji(status) {
    const emojis = ['💤', '🥱', '😫', '❌'];
    return emojis[status] || '💤';
}

function updateCompletionRate() {
    const rate = Math.round((currentReports.length / currentMeals.length) * 100) || 0;
    document.getElementById('completionRate').textContent = `Tiến độ: ${rate}%`;
}

function showToast(msg, type) {
    if (typeof Swal !== 'undefined') {
        Swal.fire({ icon: type, title: msg, toast: true, position: 'top-end', showConfirmButton: false, timer: 3000 });
    }
}
