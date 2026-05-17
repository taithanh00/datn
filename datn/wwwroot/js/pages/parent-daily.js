let studentId = window.location.search.split('studentId=')[1] || 0;

document.addEventListener('DOMContentLoaded', function() {
    const idElem = document.getElementById('studentIdData');
    if (!studentId && idElem) {
        studentId = idElem.value;
    }

    loadDailyLog();
    loadHealthHistory();

    document.getElementById('logDate').addEventListener('change', loadDailyLog);
    
    if (idElem) {
        idElem.addEventListener('change', function() {
            studentId = this.value;
            loadDailyLog();
            loadHealthHistory();
        });
    }
});

async function loadDailyLog() {
    const date = document.getElementById('logDate').value;
    try {
        const response = await fetch(`/DailyReport/GetChildDailyLog?studentId=${studentId}&date=${date}`);
        const data = await response.json();
        renderTimeline(data);
        renderHealthOverview(data.health);
    } catch (e) {
        console.error('Error loading log', e);
    }
}

function renderTimeline(data) {
    const timeline = document.getElementById('dailyTimeline');
    timeline.innerHTML = '';

    const items = [];

    // 1. Arrival (Mock if report exists)
    if (data.report) {
        items.push({
            time: '07:30 - 08:30',
            title: '👋 Chào buổi sáng',
            content: 'Bé đã đến trường an toàn. Chào cô và các bạn rất ngoan!',
            icon: 'school'
        });
    }

    // 2. Nutrition
    if (data.meals && data.meals.meals) {
        data.meals.meals.forEach(meal => {
            let title = '';
            let icon = '';
            switch(meal.mealType) {
                case 0: title = '🍳 Bữa sáng'; icon = 'egg'; break;
                case 1: title = '🍱 Bữa trưa'; icon = 'bowl-food'; break;
                case 2: title = '🍪 Bữa xế'; icon = 'cookie'; break;
            }

            items.push({
                time: meal.mealType === 1 ? '11:00' : (meal.mealType === 0 ? '08:30' : '15:00'),
                title: title,
                content: `Hôm nay bé ăn: <strong>${meal.effectiveDish}</strong>. ${data.report ? getEatingReview(data.report.eatingStatus) : ''}`,
                icon: icon
            });
        });
    }

    // 3. Sleeping
    if (data.report && data.report.sleepingStatus !== null) {
        items.push({
            time: '12:00 - 14:00',
            title: '💤 Giấc ngủ trưa',
            content: getSleepingReview(data.report.sleepingStatus) + (data.report.sleepingNote ? `. ${data.report.sleepingNote}` : ''),
            icon: 'moon'
        });
    }

    // 4. Learning/Mood
    if (data.report && (data.report.moodNote || data.report.activityNote)) {
        items.push({
            time: 'Cả ngày',
            title: '🌟 Hoạt động & Tâm trạng',
            content: `${data.report.moodNote || ''} ${data.report.activityNote || ''}`,
            icon: 'star'
        });
    }

    // Sort by time (simple)
    items.sort((a, b) => a.time.localeCompare(b.time));

    items.forEach(item => {
        const div = document.createElement('div');
        div.className = 'timeline-item fade-in';
        div.innerHTML = `
            <div class="timeline-time">${item.time}</div>
            <div class="timeline-icon"><i class="fa-solid fa-${item.icon}"></i></div>
            <div class="timeline-content">
                <div class="timeline-title">${item.title}</div>
                <div class="small">${item.content}</div>
            </div>
        `;
        timeline.appendChild(div);
    });

    if (items.length === 0) {
        timeline.innerHTML = `
            <div class="text-center py-5 fade-in">
                <img src="/img/illustrations/empty-box.svg" alt="No data" style="width: 150px; opacity: 0.5; margin-bottom: 20px;">
                <p class="text-muted">Chưa có nhật ký cho ngày này.</p>
            </div>
        `;
    }
}

function getEatingReview(status) {
    const reviews = ['Bé ăn rất giỏi, hết suất!', 'Bé ăn khá tốt.', 'Bé ăn hơi kém, cần cố gắng hơn.'];
    return reviews[status] || '';
}

function getSleepingReview(status) {
    const reviews = ['Bé ngủ sâu giấc.', 'Bé ngủ khá tốt.', 'Bé ngủ chập chờn.', 'Bé không ngủ.'];
    return reviews[status] || '';
}

function renderHealthOverview(health) {
    const container = document.getElementById('healthOverview');
    if (!health) {
        container.innerHTML = '<p class="text-muted small">Chưa có dữ liệu sức khỏe mới nhất.</p>';
        return;
    }

    container.innerHTML = `
        <div class="row g-3">
            <div class="col-4">
                <div class="health-stat-card">
                    <div class="health-stat-value">${health.temperature || '--'}°</div>
                    <div class="health-stat-label">Nhiệt độ</div>
                </div>
            </div>
            <div class="col-4">
                <div class="health-stat-card">
                    <div class="health-stat-value">${health.weight || '--'}</div>
                    <div class="health-stat-label">Cân nặng (kg)</div>
                </div>
            </div>
            <div class="col-4">
                <div class="health-stat-card">
                    <div class="health-stat-value">${health.height || '--'}</div>
                    <div class="health-stat-label">Chiều cao (cm)</div>
                </div>
            </div>
        </div>
    `;
}

async function loadHealthHistory() {
    try {
        const response = await fetch(`/DailyReport/GetHealthHistory?studentId=${studentId}`);
        const history = await response.json();
        renderGrowthChart(history);
    } catch (e) {
        console.error('Error loading history', e);
    }
}

function renderGrowthChart(history) {
    if (!history || history.length === 0) return;

    const options = {
        series: [{
            name: 'Cân nặng (kg)',
            data: history.slice(0, 6).reverse().map(h => h.weight)
        }, {
            name: 'Chiều cao (cm)',
            data: history.slice(0, 6).reverse().map(h => h.height)
        }],
        chart: {
            height: 250,
            type: 'line',
            toolbar: { show: false }
        },
        colors: ['#e91e63', '#00bcd4'],
        dataLabels: { enabled: false },
        stroke: { curve: 'smooth', width: 3 },
        xaxis: {
            categories: history.slice(0, 6).reverse().map(h => h.date),
        },
        tooltip: { y: { formatter: (val) => val } }
    };

    const chart = new ApexCharts(document.querySelector("#growthChart"), options);
    chart.render();
}
