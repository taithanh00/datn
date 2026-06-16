var currentClassId = window.selectedClassId || 0;
document.addEventListener('DOMContentLoaded', function() { loadMyClasses(); });

function loadMyClasses() {
    fetch('/Employee/Api/MyClasses').then(r=>r.json()).then(response => {
        if (response.success) {
            if (response.data.length === 0) {
                document.getElementById('classSelect').innerHTML = '<option value="">-- Không có lớp --</option>';
                document.getElementById('attendanceContent').innerHTML = '<div class="empty-state"><i class="fa-solid fa-user-slash"></i><p>Bạn chưa được phân công phụ trách lớp nào.</p></div>';
                return;
            }

            let html = '';
            // Nếu không có classId từ URL, mặc định chọn lớp đầu tiên
            if (currentClassId == 0 && response.data.length > 0) {
                currentClassId = response.data[0].classId;
            }

            response.data.forEach(item => {
                html += `<option value="${item.classId}" ${item.classId == currentClassId ? 'selected' : ''}>${item.className}</option>`;
            });
            document.getElementById('classSelect').innerHTML = html;
            
            // Tự động tải danh sách học sinh
            if (currentClassId > 0) loadStudents(currentClassId);
        } else {
            document.getElementById('attendanceContent').innerHTML = `<div class="page-alert error">${response.message || 'Không tải được danh sách lớp.'}</div>`;
        }
    }).catch(() => {
        document.getElementById('attendanceContent').innerHTML = '<div class="page-alert error">Lỗi kết nối khi tải danh sách lớp.</div>';
    });
}

function changeClass(classId) {
    if (classId) loadStudents(classId);
    else { document.getElementById('attendanceContent').innerHTML = '<div class="empty-state"><i class="fa-solid fa-arrow-up"></i><p>Chọn lớp để điểm danh</p></div>'; document.getElementById('footerActions').style.display='none'; }
}

function loadStudents(classId) {
    document.getElementById('attendanceContent').innerHTML = window.appLoading
        ? window.appLoading.content("\u0110ang t\u1ea3i danh s\u00e1ch h\u1ecdc sinh...")
        : '<div style="text-align:center; padding:40px;"><div class="spinner"></div></div>';
    document.getElementById('footerActions').style.display = 'none';
    fetch(`/Employee/Api/ClassStudents/${classId}`).then(r=>r.json()).then(response => {
        if (response.success) {
            if (response.data.length === 0) { document.getElementById('attendanceContent').innerHTML = '<div class="empty-state"><i class="fa-solid fa-user-slash"></i><p>Lớp chưa có học sinh</p></div>'; return; }
            let html = '';
            response.data.forEach((s, i) => {
                const st = s.attendanceStatus || 'Present';
                html += `<div class="attendance-row">
                    <div class="d-flex align-center gap-2">
                        <div class="avatar" style="background:var(--primary);">${(i+1)}</div>
                        <div><strong>${s.fullName}</strong><div class="text-muted" style="font-size:0.8rem;">${s.gender}</div></div>
                    </div>
                    <div class="att-options">
                        <button type="button" class="att-btn ${st==='Present'?'selected-present':''}" onclick="selectAtt(this,'${s.id}','Present')" data-sid="${s.id}" data-val="Present"><i class="fa-solid fa-check"></i> Có mặt</button>
                        <button type="button" class="att-btn ${st==='Excused'?'selected-excused':''}" onclick="selectAtt(this,'${s.id}','Excused')" data-sid="${s.id}" data-val="Excused"><i class="fa-solid fa-info"></i> Có phép</button>
                        <button type="button" class="att-btn ${st==='Absent'?'selected-absent':''}" onclick="selectAtt(this,'${s.id}','Absent')" data-sid="${s.id}" data-val="Absent"><i class="fa-solid fa-xmark"></i> Vắng</button>
                    </div>
                </div>`;
            });
            document.getElementById('attendanceContent').innerHTML = html;
            document.getElementById('footerActions').style.display = 'flex';
        } else {
            document.getElementById('attendanceContent').innerHTML = `<div class="page-alert error">${response.message || 'Không tải được danh sách học sinh.'}</div>`;
        }
    }).catch(() => {
        document.getElementById('attendanceContent').innerHTML = '<div class="page-alert error">Lỗi kết nối khi tải danh sách học sinh.</div>';
    });
}

function selectAtt(btn, sid, val) {
    const row = btn.parentElement;
    row.querySelectorAll('.att-btn').forEach(b => b.className = 'att-btn');
    btn.classList.add('selected-' + val.toLowerCase());
}

function submitAttendance() {
    const records = [];
    document.querySelectorAll('.att-btn[class*="selected-"]').forEach(btn => {
        records.push({ studentId: parseInt(btn.dataset.sid), status: btn.dataset.val });
    });
    if (!records.length) { window.notifyWarning('Không có dữ liệu'); return; }
    fetch('/Employee/Api/SubmitAttendance', {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify({records})})
        .then(r=>r.json()).then(result => {
            if (result.success) showToast('Thành công', result.message, 'success');
            else window.notifyError('Lỗi: ' + result.message);
        });
}
