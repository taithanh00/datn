var currentClassId = window.selectedClassId || 0;
var rankings = [];

function setSaveButtonVisible(isVisible) {
    const btnSave = document.getElementById('btnSave');
    if (btnSave) {
        btnSave.style.display = isVisible ? 'inline-flex' : 'none';
    }
}

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
        setSaveButtonVisible(false);
        return;
    }

    document.getElementById('reportContent').innerHTML = '<div style="text-align:center; padding:40px;"><div class="spinner"></div><p class="text-muted mt-2">Đang tải danh sách học sinh...</p></div>';
    setSaveButtonVisible(false);

    try {
        const response = await fetch(`/Employee/Api/ManagedStudentsForReport/${classId}?month=${month}&year=${year}`);
        const result = await response.json();
        
        if (result.success) {
            if (result.data.length === 0) {
                document.getElementById('reportContent').innerHTML = '<div class="empty-state"><i class="fa-solid fa-user-slash"></i><p>Lớp này chưa có học sinh nào.</p></div>';
                return;
            }

            const isLocked = result.isSubmitted || !result.isLead;
            let statusHtml = '';
            if (result.isSubmitted) {
                statusHtml = '<div class="page-alert info" style="margin-bottom:16px;">Lớp này đã gửi đánh giá cho tháng đã chọn. Mỗi lớp chỉ được gửi 1 lần trong một tháng.</div>';
            } else if (!result.isLead) {
                statusHtml = '<div class="page-alert warning" style="margin-bottom:16px;">Bạn chỉ được xem đánh giá. Chỉ Giáo viên phụ trách mới được gửi đánh giá học tập tháng.</div>';
            }

            let html = `
                ${statusHtml}
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
                            <select class="form-select ranking-select report-ranking" data-student-id="${student.id}" ${isLocked ? 'disabled' : ''}>
                                ${rankingOptions}
                            </select>
                        </td>
                        <td>
                            <textarea class="form-input comment-area report-comment" 
                                      data-student-id="${student.id}" 
                                      rows="2" placeholder="Nhập nhận xét..." ${isLocked ? 'disabled' : ''}>${comment}</textarea>
                        </td>
                    </tr>
                `;
            });

            html += '</tbody></table></div>';
            document.getElementById('reportContent').innerHTML = html;
            setSaveButtonVisible(!isLocked);
            initPagination('reportTable', 15);
        } else {
            document.getElementById('reportContent').innerHTML = `<div class="page-alert error">${result.message}</div>`;
        }
    } catch (e) {
        console.error(e);
    }
}

async function submitReports() {
    const classId = parseInt(document.getElementById('classSelect').value);
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
        btnSave.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> Đang gửi...';
        
        const response = await fetch('/Employee/Api/SubmitStudyReport', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ classId, month, year, records })
        });
        
        const result = await response.json();
        if (result.success) {
            if (window.showToast) window.showToast('Thành công', result.message, 'success');
            else alert(result.message);
            await loadStudents();
        } else {
            alert('Lỗi: ' + result.message);
        }
    } catch (e) {
        console.error(e);
        alert('Có lỗi xảy ra khi gửi dữ liệu.');
    } finally {
        const btnSave = document.getElementById('btnSave');
        btnSave.disabled = false;
        btnSave.innerHTML = '<i class="fa-solid fa-paper-plane"></i> Gửi đánh giá cho Phụ huynh';
    }
}

