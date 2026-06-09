let studentId = new URLSearchParams(window.location.search).get("studentId") || 0;
let growthChart = null;

document.addEventListener("DOMContentLoaded", function () {
    const idElem = document.getElementById("studentIdData");
    if (!studentId && idElem) {
        studentId = idElem.value;
    }

    document.getElementById("logDate")?.addEventListener("change", loadDailyLog);

    if (idElem) {
        idElem.addEventListener("change", function () {
            studentId = this.value;
            loadDailyLog();
        });
    }

    loadDailyLog();
});

async function loadDailyLog() {
    const date = document.getElementById("logDate")?.value;
    if (!studentId || !date) return;

    if (window.appLoading) {
        window.appLoading.setContent("dailyTimeline", "\u0110ang t\u1ea3i nh\u1eadt k\u00fd...");
        window.appLoading.setContent("healthOverview", "\u0110ang t\u1ea3i th\u00f4ng tin s\u1ee9c kh\u1ecfe...");
    }

    try {
        const response = await fetch(`/DailyReport/GetChildDailyLog?studentId=${studentId}&date=${date}`);
        const data = await response.json();
        renderTimeline(data);
        renderHealthOverview(data.health);
        renderGrowthChart(data.history || []);
    } catch (error) {
        console.error("Error loading log", error);
        renderTimeline({});
        renderHealthOverview(null);
        renderGrowthChart([]);
    }
}

function renderTimeline(data) {
    const timeline = document.getElementById("dailyTimeline");
    if (!timeline) return;

    const items = [];

    if (data.report) {
        items.push({
            time: "07:30 - 08:30",
            title: "Chào buổi sáng",
            content: "Bé đã đến trường an toàn. Chào cô và các bạn rất ngoan!",
            icon: "school"
        });
    }

    if (data.meals && data.meals.meals) {
        data.meals.meals.forEach(meal => {
            const mealMeta = getMealMeta(meal.mealType);
            items.push({
                time: mealMeta.time,
                title: mealMeta.title,
                content: `Hôm nay bé ăn: <strong>${escapeHtml(meal.effectiveDish || "Chưa cập nhật")}</strong>. ${data.report ? getEatingReview(data.report.eatingStatus) : ""}`,
                icon: mealMeta.icon
            });
        });
    }

    if (data.report && data.report.sleepingStatus !== null && data.report.sleepingStatus !== undefined) {
        items.push({
            time: "12:00 - 14:00",
            title: "Giấc ngủ trưa",
            content: `${getSleepingReview(data.report.sleepingStatus)}${data.report.sleepingNote ? `. ${escapeHtml(data.report.sleepingNote)}` : ""}`,
            icon: "moon"
        });
    }

    if (data.report && (data.report.moodNote || data.report.activityNote)) {
        items.push({
            time: "Cả ngày",
            title: "Hoạt động & Tâm trạng",
            content: `${escapeHtml(data.report.moodNote || "")} ${escapeHtml(data.report.activityNote || "")}`.trim(),
            icon: "star"
        });
    }

    items.sort((a, b) => a.time.localeCompare(b.time));

    if (items.length === 0) {
        timeline.innerHTML = renderEmptyState(
            "fa-clipboard-list",
            "Chưa có nhật ký cho ngày này",
            "Giáo viên chưa cập nhật hoạt động trong ngày đã chọn."
        );
        return;
    }

    timeline.innerHTML = items.map(item => `
        <div class="timeline-item fade-in">
            <div class="timeline-time">${escapeHtml(item.time)}</div>
            <div class="timeline-icon"><i class="fa-solid fa-${item.icon}"></i></div>
            <div class="timeline-content">
                <div class="timeline-title">${escapeHtml(item.title)}</div>
                <div class="small">${item.content}</div>
            </div>
        </div>
    `).join("");
}

function getMealMeta(mealType) {
    switch (mealType) {
        case 0:
            return { title: "Bữa sáng", icon: "egg", time: "08:30" };
        case 1:
            return { title: "Bữa trưa", icon: "bowl-food", time: "11:00" };
        case 2:
            return { title: "Bữa xế", icon: "cookie", time: "15:00" };
        default:
            return { title: "Bữa ăn", icon: "utensils", time: "09:00" };
    }
}

function getEatingReview(status) {
    const reviews = ["Bé ăn rất giỏi, hết suất!", "Bé ăn khá tốt.", "Bé ăn hơi kém, cần cố gắng hơn."];
    return reviews[status] || "";
}

function getSleepingReview(status) {
    const reviews = ["Bé ngủ sâu giấc.", "Bé ngủ khá tốt.", "Bé ngủ chập chờn.", "Bé không ngủ."];
    return reviews[status] || "";
}

function renderHealthOverview(health) {
    const container = document.getElementById("healthOverview");
    if (!container) return;

    if (!health) {
        container.innerHTML = renderEmptyState(
            "fa-heart-pulse",
            "Chưa có dữ liệu sức khỏe",
            "Giáo viên chưa ghi nhận nhiệt độ, cân nặng hoặc chiều cao cho bé."
        );
        return;
    }

    container.innerHTML = `
        <div class="health-stat-grid">
            ${renderHealthStat("fa-temperature-half", formatValue(health.temperature, "°C"), "Nhiệt độ")}
            ${renderHealthStat("fa-weight-scale", formatValue(health.weight, " kg"), "Cân nặng")}
            ${renderHealthStat("fa-ruler-vertical", formatValue(health.height, " cm"), "Chiều cao")}
        </div>
    `;
}

function renderHealthStat(icon, value, label) {
    return `
        <div class="health-stat-card">
            <div class="health-stat-icon"><i class="fa-solid ${icon}"></i></div>
            <div class="health-stat-value">${escapeHtml(value)}</div>
            <div class="health-stat-label">${escapeHtml(label)}</div>
        </div>`;
}

function renderGrowthChart(history) {
    const chartEl = document.querySelector("#growthChart");
    if (!chartEl) return;

    if (growthChart) {
        growthChart.destroy();
        growthChart = null;
    }

    const validHistory = (history || [])
        .filter(h => h.weight !== null || h.height !== null)
        .slice(0, 6)
        .reverse();

    if (!validHistory.length) {
        chartEl.innerHTML = renderEmptyState(
            "fa-chart-line",
            "Chưa có biểu đồ tăng trưởng",
            "Cần ít nhất một lần ghi nhận cân nặng hoặc chiều cao."
        );
        return;
    }

    chartEl.innerHTML = "";
    const options = {
        series: [{
            name: "Cân nặng (kg)",
            data: validHistory.map(h => h.weight)
        }, {
            name: "Chiều cao (cm)",
            data: validHistory.map(h => h.height)
        }],
        chart: {
            height: 250,
            type: "line",
            toolbar: { show: false }
        },
        colors: ["#e91e63", "#00bcd4"],
        dataLabels: { enabled: false },
        stroke: { curve: "smooth", width: 3 },
        xaxis: {
            categories: validHistory.map(h => h.date),
        },
        tooltip: {
            y: {
                formatter: (val) => val === null || val === undefined ? "Chưa ghi" : val
            }
        }
    };

    growthChart = new ApexCharts(chartEl, options);
    growthChart.render();
}

function renderEmptyState(icon, title, description) {
    return `
        <div class="daily-empty fade-in">
            <div class="daily-empty-icon">
                <i class="fa-solid ${icon}"></i>
            </div>
            <h4>${escapeHtml(title)}</h4>
            <p>${escapeHtml(description)}</p>
        </div>`;
}

function formatValue(value, suffix) {
    if (value === null || value === undefined || value === "") return "Chưa ghi";
    return `${value}${suffix}`;
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
