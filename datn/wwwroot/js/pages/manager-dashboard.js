let revenueChartInstance = null;
let attendanceChartInstance = null;

document.addEventListener('DOMContentLoaded', function() { loadDashboardData(); });

async function loadDashboardData() {
    try {
        const res = await fetch('/Manager/Api/DashboardStats');
        const data = await res.json();
        if (data.success) {
            document.getElementById('statStudents').textContent = data.stats.totalStudents;
            document.getElementById('statTeachers').textContent = data.stats.totalTeachers;
            document.getElementById('statRevenue').textContent = new Intl.NumberFormat('vi-VN').format(data.stats.monthlyRevenue) + ' đ';
            document.getElementById('statPendingLeaves').textContent = data.stats.pendingLeaves;
            renderRevenueChart(data.charts.revenue);
            renderAttendanceChart(data.charts.attendance);
            renderLeavesTable(data.latestLeaves);
        }
    } catch (e) { console.error("Load dashboard failed", e); }
}

function renderRevenueChart(data) {
    if (revenueChartInstance) { revenueChartInstance.destroy(); }
    const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
    const gridColor = isDark ? 'rgba(255,255,255,0.06)' : '#f3f4f6';
    const ctx = document.getElementById('revenueChart').getContext('2d');
    if(!ctx) return;
    revenueChartInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(x => x.label),
            datasets: [{
                label: 'Doanh thu (VNĐ)', data: data.map(x => x.value),
                borderColor: '#818CF8', backgroundColor: 'rgba(129,140,248,0.1)',
                fill: true, tension: 0.4, borderWidth: 3,
                pointRadius: 4, pointBackgroundColor: isDark ? '#1A1D27' : '#fff',
                pointBorderColor: '#818CF8', pointBorderWidth: 2
            }]
        },
        options: {
            responsive: true, maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: {
                y: { beginAtZero: true, grid: { color: gridColor }, ticks: { color: isDark ? '#94A3B8' : '#6b7280', callback: v => v >= 1000000 ? (v/1000000)+'M' : v } },
                x: { grid: { display: false }, ticks: { color: isDark ? '#94A3B8' : '#6b7280' } }
            }
        }
    });
}

function renderAttendanceChart(data) {
    if (attendanceChartInstance) { attendanceChartInstance.destroy(); }
    const ctxElem = document.getElementById('attendanceChart');
    if(!ctxElem) return;
    const ctx = ctxElem.getContext('2d');
    const total = data.present + data.absent;
    const percent = total > 0 ? Math.round((data.present / total) * 100) : 0;
    const infoElem = document.getElementById('attendanceInfo');
    if(infoElem) infoElem.innerHTML = `Hiện diện: <strong>${data.present}</strong> / Vắng: <strong>${data.absent}</strong> (${percent}%)`;
    attendanceChartInstance = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Hiện diện', 'Vắng mặt'],
            datasets: [{ data: [data.present, data.absent], backgroundColor: ['#34D399', '#F87171'], borderWidth: 0, cutout: '75%' }]
        },
        options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
    });
}

function renderLeavesTable(leaves) {
    const tbody = document.getElementById('latestLeavesTable');
    if(!tbody) return;
    if (!leaves || leaves.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" class="text-muted" style="text-align:center; padding:32px;">Không có đơn nghỉ phép nào</td></tr>';
        return;
    }
    tbody.innerHTML = leaves.map(l => `
        <tr>
            <td><strong>${l.name}</strong></td>
            <td>${l.startDate} - ${l.endDate}</td>
            <td title="${l.reason}">${l.reason.length > 30 ? l.reason.substring(0,30)+'...' : l.reason}</td>
            <td style="text-align:right;"><a href="/LeaveApproval" class="btn btn-outline" style="padding:6px 12px; font-size:0.8rem;" data-transition>Duyệt</a></td>
        </tr>
    `).join('');
}
