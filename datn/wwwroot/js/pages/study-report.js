var currentClassId = Number(window.selectedClassId || 0);
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
    } catch (e) {
        console.error("Load rankings failed", e);
    }
}

async function loadMyClasses() {
    try {
        const response = await fetch('/Employee/Api/MyClasses');
        const result = await response.json();
        if (result.success) {
            let html = '<option value="">-- Chọn lớp học --</option>';
            result.data.forEach(item => {
                html += `<option value="${item.classId}" ${item.classId == currentClassId ? 'selected' : ''}>${escapeHtml(item.className)}</option>`;
            });

            const classSelect = document.getElementById('classSelect');
            classSelect.innerHTML = html;

            if (result.data.length === 1 && !currentClassId) {
                currentClassId = Number(result.data[0].classId);
                classSelect.value = currentClassId;
            }

            if (classSelect.value) {
                loadStudents();
            }
        }
    } catch (e) {
        console.error(e);
    }
}

function isPastReportPeriod() {
    const month = parseInt(document.getElementById('reportMonth').value, 10);
    const year = parseInt(document.getElementById('reportYear').value, 10);
    const now = new Date();
    return year < now.getFullYear() || (year === now.getFullYear() && month < now.getMonth() + 1);
}

async function loadStudents() {
    const classId = document.getElementById('classSelect').value;
    const month = document.getElementById('reportMonth').value;
    const year = document.getElementById('reportYear').value;

    if (!classId) {
        document.getElementById('reportContent').innerHTML = '<div class="empty-state"><i class="fa-solid fa-clipboard-check"></i><p>Vui lòng chọn một lớp học</p></div>';
        return;
    }

    document.getElementById('reportContent').innerHTML = window.appLoading
        ? window.appLoading.content("Đang tải danh sách học sinh...")
        : '<div style="text-align:center; padding:40px;"><div class="spinner"></div><p class="text-muted mt-2">Đang tải danh sách học sinh...</p></div>';

    try {
        const response = await fetch(`/Employee/Api/ManagedStudentsForReport/${classId}?month=${month}&year=${year}`);
        const result = await response.json();

        if (!result.success) {
            document.getElementById('reportContent').innerHTML = `<div class="page-alert error">${escapeHtml(result.message)}</div>`;
            return;
        }

        if (result.data.length === 0) {
            document.getElementById('reportContent').innerHTML = '<div class="empty-state"><i class="fa-solid fa-user-slash"></i><p>Lớp này chưa có học sinh nào.</p></div>';
            return;
        }

        const lockedByPeriod = result.isPastPeriod || isPastReportPeriod();
        let statusHtml = '';
        if (lockedByPeriod) {
            statusHtml = '<div class="page-alert warning" style="margin-bottom:16px;">Không thể gửi đánh giá cho tháng đã qua.</div>';
        } else if (!result.isLead) {
            statusHtml = '<div class="page-alert warning" style="margin-bottom:16px;">Bạn chỉ được xem đánh giá. Chỉ giáo viên phụ trách mới được gửi đánh giá học tập tháng.</div>';
        }

        let html = `
            ${statusHtml}
            <div class="table-container">
                <table class="report-table" id="reportTable">
                    <thead>
                        <tr>
                            <th width="60">Học sinh</th>
                            <th>Họ và tên</th>
                            <th>Xếp loại</th>
                            <th>Nhận xét của giáo viên</th>
                            <th style="text-align:right;">Trạng thái</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        result.data.forEach(student => {
            const report = student.report || null;
            const isSubmitted = !!report;
            const isLocked = isSubmitted || !result.isLead || lockedByPeriod;
            const rankingId = report?.rankingId || "";
            const comment = report?.comment || "";

            let rankingOptions = '<option value="">-- Xếp loại --</option>';
            rankings.forEach(r => {
                rankingOptions += `<option value="${r.id}" ${r.id == rankingId ? 'selected' : ''}>${escapeHtml(r.name)}</option>`;
            });

            const actionHtml = isSubmitted
                ? '<span class="badge badge-success"><i class="fa-solid fa-circle-check"></i> Đã gửi</span>'
                : `<button class="btn btn-primary btn-sm report-submit-btn" type="button" onclick="submitStudentReport(${student.id})" ${isLocked ? 'disabled' : ''}>
                        <i class="fa-solid fa-paper-plane"></i> Gửi
                   </button>`;

            html += `
                <tr data-student-id="${student.id}">
                    <td>
                        <img src="${student.avatarPath || '/images/lion_orange.png'}" class="avatar" style="width: 40px; height: 40px; object-fit: cover; border-radius: 50%; border: 2px solid var(--border);" onerror="this.src='/images/lion_orange.png'">
                    </td>
                    <td>
                        <div style="font-weight: 600;">${escapeHtml(student.fullName)}</div>
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
                                  rows="2" placeholder="Nhập nhận xét..." ${isLocked ? 'disabled' : ''}>${escapeHtml(comment)}</textarea>
                    </td>
                    <td style="text-align:right;">${actionHtml}</td>
                </tr>
            `;
        });

        html += '</tbody></table></div>';
        document.getElementById('reportContent').innerHTML = html;
        if (typeof initPagination === 'function') initPagination('reportTable', 15);
    } catch (e) {
        console.error(e);
        document.getElementById('reportContent').innerHTML = '<div class="page-alert error">Lỗi kết nối khi tải danh sách học sinh.</div>';
    }
}

async function submitStudentReport(studentId) {
    const classId = parseInt(document.getElementById('classSelect').value, 10);
    const month = parseInt(document.getElementById('reportMonth').value, 10);
    const year = parseInt(document.getElementById('reportYear').value, 10);
    const rankingEl = document.querySelector(`.report-ranking[data-student-id="${studentId}"]`);
    const commentEl = document.querySelector(`.report-comment[data-student-id="${studentId}"]`);
    const rankingId = rankingEl?.value;
    const comment = commentEl?.value?.trim() || "";

    if (isPastReportPeriod()) {
        alert('Không thể gửi đánh giá cho tháng đã qua.');
        return;
    }

    if (!rankingId && !comment) {
        alert('Vui lòng nhập xếp loại hoặc nhận xét cho học sinh này.');
        return;
    }

    const btn = document.querySelector(`tr[data-student-id="${studentId}"] .report-submit-btn`);
    const originalHtml = btn?.innerHTML;
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> Đang gửi...';
    }

    try {
        const response = await fetch('/Employee/Api/SubmitStudyReport', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                classId,
                month,
                year,
                records: [{
                    studentId,
                    rankingId: rankingId ? parseInt(rankingId, 10) : null,
                    comment
                }]
            })
        });

        const result = await response.json();
        if (result.success) {
            if (window.showToast) window.showToast('Thành công', result.message, 'success');
            await loadStudents();
        } else {
            alert('Lỗi: ' + result.message);
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = originalHtml;
            }
        }
    } catch (e) {
        console.error(e);
        alert('Có lỗi xảy ra khi gửi dữ liệu.');
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = originalHtml;
        }
    }
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
