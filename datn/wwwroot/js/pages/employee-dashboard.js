document.addEventListener('DOMContentLoaded', function() { loadTeacherDashboard(); });

async function loadTeacherDashboard() {
    try {
        const res = await fetch('/Employee/Api/DashboardStats');
        const data = await res.json();
        if (data.success) {
            const statClassSize = document.getElementById('statClassSize');
            const statClassName = document.getElementById('statClassName');
            const statCheckIn = document.getElementById('statCheckIn');
            const statLastSalary = document.getElementById('statLastSalary');
            const currentDate = document.getElementById('currentDate');

            if(statClassSize) statClassSize.textContent = data.stats.presentToday + ' / ' + data.stats.classSize;
            if(statClassName) statClassName.textContent = 'Sĩ số: ' + data.stats.className;
            if(statCheckIn) statCheckIn.textContent = data.stats.checkInTime;
            if(statLastSalary) statLastSalary.textContent = new Intl.NumberFormat('vi-VN').format(data.stats.lastSalary) + ' đ';
            if(currentDate) currentDate.textContent = data.todaySchedule.length > 0 ? 'Hôm nay' : 'Không có lịch';
            
            renderTimeline(data.todaySchedule);
            renderRankingChart(data.charts.ranking);
        }
    } catch(e) { console.error(e); }
}

function renderTimeline(schedules) {
    const c = document.getElementById('todaySchedule');
    if(!c) return;
    if (!schedules.length) { c.innerHTML = '<div class="empty-state"><i class="fa-solid fa-calendar-xmark"></i><p>Hôm nay không có tiết dạy</p></div>'; return; }
    c.innerHTML = schedules.map(s => `<div class="timeline-item"><div class="timeline-dot"></div><div class="timeline-time">${s.time}</div><div><div class="timeline-subject">${s.subject}</div><div class="timeline-class">Lớp: ${s.className}</div></div></div>`).join('');
}

function renderRankingChart(data) {
    const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
    const canvas = document.getElementById('rankingChart');
    if(!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!data.length) { ctx.font='14px Plus Jakarta Sans'; ctx.fillStyle=isDark?'#64748B':'#9ca3af'; ctx.textAlign='center'; ctx.fillText('Chưa có dữ liệu',150,150); return; }
    
    if(window.Chart) {
        new Chart(ctx, { 
            type:'bar', 
            data:{ 
                labels:data.map(x=>x.label), 
                datasets:[{label:'Số HS',data:data.map(x=>x.value),backgroundColor:'#818CF8',borderRadius:8}] 
            }, 
            options:{ 
                responsive:true, 
                maintainAspectRatio:false, 
                plugins:{legend:{display:false}}, 
                scales:{ 
                    y:{beginAtZero:true,ticks:{stepSize:1,color:isDark?'#94A3B8':'#6b7280'},grid:{color:isDark?'rgba(255,255,255,0.06)':'#f3f4f6'}}, 
                    x:{grid:{display:false},ticks:{color:isDark?'#94A3B8':'#6b7280'}} 
                } 
            } 
        });
    }
}
