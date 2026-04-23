document.addEventListener('DOMContentLoaded', loadMonitoring);

async function loadMonitoring() {
    const monthElem = document.getElementById('monMonth');
    const yearElem = document.getElementById('monYear');
    const statusElem = document.getElementById('monStatus');

    if(!monthElem || !yearElem || !statusElem) return;

    const month = monthElem.value;
    const year = yearElem.value;
    const isPaid = statusElem.value;

    try {
        const res = await fetch(`/Tuition/Api/Monitoring?month=${month}&year=${year}${isPaid ? '&isPaid=' + isPaid : ''}`);
        const data = await res.json();
        if (data.success) {
            const tbody = document.getElementById('monitoringTable');
            if(!tbody) return;

            if (data.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="5" class="text-center py-5 text-muted"><div class="empty-state"><i class="fa-solid fa-file-invoice"></i><p>Không có dữ liệu hóa đơn nào cho kỳ này.</p></div></td></tr>';
                return;
            }

            tbody.innerHTML = data.data.map(t => `
                <tr>
                    <td>
                        <div style="font-weight: 600;">${t.studentName}</div>
                        <div class="text-muted" style="font-size: 0.85rem;">ID: #${t.id}</div>
                    </td>
                    <td>
                        <div class="badge badge-info">${t.className}</div>
                    </td>
                    <td>
                        <div style="font-weight: 700; color: var(--text-main);">${new Intl.NumberFormat('vi-VN').format(t.total)} đ</div>
                        ${t.extraFee > 0 ? `<div style="font-size: 0.8rem; color: var(--success);"><i class="fa-solid fa-plus"></i> Phụ phí: ${new Intl.NumberFormat('vi-VN').format(t.extraFee)} đ</div>` : ''}
                    </td>
                    <td style="text-align: center;">
                        ${t.isPaid 
                            ? '<span class="badge badge-success"><i class="fa-solid fa-check-circle"></i> Đã nộp</span>' 
                            : '<span class="badge badge-warning"><i class="fa-solid fa-clock"></i> Chờ nộp</span>'}
                    </td>
                    <td style="text-align: right;">
                        ${!t.isPaid ? `
                            <button class="btn btn-primary" onclick="confirmPaid(${t.id})" style="padding: 6px 12px; font-size: 0.85rem; background: var(--success); border-color: var(--success);">
                                <i class="fa-solid fa-check"></i> Xác nhận nộp
                            </button>` : `
                            <button class="btn btn-outline" disabled style="padding: 6px 12px; font-size: 0.85rem; color: var(--success); border-color: var(--success);">
                                <i class="fas fa-check"></i> Đã xác nhận
                            </button>
                        `}
                    </td>
                </tr>
            `).join('');
            
            if (window.initPagination) initPagination('monitoringTableEl', 15);
        }
    } catch (e) { console.error("Load monitoring failed", e); }
}

async function confirmPaid(id) {
    if (!confirm("Bạn có chắc chắn xác nhận học sinh này đã hoàn thành nghĩa vụ học phí?")) return;

    try {
        const res = await fetch(`/Tuition/Api/ConfirmPaid/${id}`, { method: 'POST' });
        const result = await res.json();
        if (result.success) {
            loadMonitoring();
            if (window.showToast) window.showToast('Thành công', result.message, 'success');
        }
    } catch (e) { console.error("Confirm paid failed", e); }
}

// Expose to global scope
window.loadMonitoring = loadMonitoring;
window.confirmPaid = confirmPaid;
