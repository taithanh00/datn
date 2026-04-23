var currentClassId = window.selectedClassId || 0;
var rankings = [];

document.addEventListener('DOMContentLoaded', async function () {
    await loadRankings();
    loadMyClasses();
});

async function loadRankings() {
    try {
        const response = await fetch('/Employee/Api/Rankings');
        const result = await response.json();
        if (result.success) {
            rankings = result.data;
        }
    } catch (e) { console.error("Load rankings failed", e); }
}

async function loadMyClasses() {
    try {
        const response = await fetch('/Employee/Api/MyClasses');
        const result = await response.json();
        if (result.success) {
            let html = '<option value="">-- Chọn lớp học --</option>';
            result.data.forEach(item => {
                html += `<option value="${item.classId}" ${item.classId == currentClassId ? 'selected' : ''}>${item.className}</option>`;
            });
            document.getElementById('classSelect').innerHTML = html;

            if (document.getElementById('classSelect').value) {
                loadStudents();
            }
        }
    } catch(e) { console.error(e); }
}

async function loadStudents() {
    const classId = document.getElementById('classSelect').value;
    const month = document.getElementById('reportMonth').value;
    const year = document.getElementById('reportYear').value;

    if (!classId) {
        document.getElementById('reportContent').innerHTML = '<div class="empty-state"><i class="fa-solid fa-clipboard-check"></i><p>Vui lòng chọn một lớp học</p></div>';
        document.getElementById('btnSave').style.display = 'none';
        return;
    }

    document.getElementById('reportContent').innerHTML = '<div style="text-align:center; padding:40px;"><div class="spinner"></div><p class="text-muted mt-2">Đang tải danh sách học sinh...</p></div>';
    document.getElementById('btnSave').style.display = 'none';

    try {
        const response = await fetch(`/Employee/Api/ManagedStudentsForReport/${classId}?month=${month}&year=${year}`);
        const result = await response.json();
        
        if (result.success) {
            if (result.data.length === 0) {
                document.getElementById('reportContent').innerHTML = '<div class="empty-state"><i class="fa-solid fa-user-slash"></i><p>Lớp này chưa có học sinh nào.</p></div>';
                return;
            }

            let html = `
                <div class="table-container">
                    <table class="report-table" id="reportTable">
                        <thead>
                            <tr>
                                <th width="60">Học sinh</th>
                                <th>Họ và Tên</th>
                                <th>Xếp loại</th>
                                <th>Nhận xét của giáo viên</th>
                            </tr>
                        </thead>
                        <tbody>
            `;

            result.data.forEach(student => {
                const report = student.report || {};
                const rankingId = report.rankingId || "";
                const comment = report.comment || "";

                let rankingOptions = '<option value="">-- Xếp loại --</option>';
                rankings.forEach(r => {
                    rankingOptions += `<option value="${r.id}" ${r.id == rankingId ? 'selected' : ''}>${r.name}</option>`;
                });

                html += `
                    <tr>
                        <td>
                            <img src="${student.avatarPath || '/images/lion_orange.png'}" class="avatar" style="width: 40px; height: 40px; object-fit: cover; border-radius: 50%; border: 2px solid var(--border);" onerror="this.src='/images/lion_orange.png'">
                        </td>
                        <td>
                            <div style="font-weight: 600;">${student.fullName}</div>
                            <div class="text-muted" style="font-size: 0.8rem;">ID: #${student.id}</div>
                        </td>
                        <td>
                            <select class="form-select ranking-select report-ranking" data-student-id="${student.id}">
                                ${rankingOptions}
                            </select>
                        </td>
                        <td>
                            <textarea class="form-input comment-area report-comment" 
                                      data-student-id="${student.id}" 
                                      rows="2" placeholder="Nhập nhận xét...">${comment}</textarea>
                        </td>
                    </tr>
                `;
            });

            html += '</tbody></table></div>';
            document.getElementById('reportContent').innerHTML = html;
            document.getElementById('btnSave').style.display = 'inline-flex';
            initPagination('reportTable', 15);
        } else {
            document.getElementById('reportContent').innerHTML = `<div class="page-alert error">${result.message}</div>`;
        }
    } catch (e) {
        console.error(e);
    }
}

async function submitReports() {
    const month = parseInt(document.getElementById('reportMonth').value);
    const year = parseInt(document.getElementById('reportYear').value);
    const records = [];

    document.querySelectorAll('.report-ranking').forEach(el => {
        const studentId = el.dataset.studentId;
        const rankingId = el.value;
        const commentEl = document.querySelector(`.report-comment[data-student-id="${studentId}"]`);
        const comment = commentEl ? commentEl.value : "";

        if (rankingId || comment) {
            records.push({
                studentId: parseInt(studentId),
                rankingId: rankingId ? parseInt(rankingId) : null,
                comment: comment
            });
        }
    });

    if (records.length === 0) {
        alert('Vui lòng nhập ít nhất một đánh giá học sinh.');
        return;
    }

    try {
        const btnSave = document.getElementById('btnSave');
        btnSave.disabled = true;
        btnSave.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> Đang lưu...';
        
        const response = await fetch('/Employee/Api/SubmitStudyReport', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ month, year, records })
        });
        
        const result = await response.json();
        if (result.success) {
            if (window.showToast) window.showToast('Thành công', result.message, 'success');
            else alert(result.message);
        } else {
            alert('Lỗi: ' + result.message);
        }
    } catch (e) {
        console.error(e);
        alert('Có lỗi xảy ra khi gửi dữ liệu.');
    } finally {
        const btnSave = document.getElementById('btnSave');
        btnSave.disabled = false;
        btnSave.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Lưu toàn bộ đánh giá';
    }
}
