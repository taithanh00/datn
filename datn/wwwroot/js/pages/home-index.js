async function refreshManagerCounts() {
    try {
        const [aRes, lRes] = await Promise.all([
            fetch("/LeaveApproval/Api/PendingAttendance"),
            fetch("/LeaveApproval/Api/PendingLeaveRequests")
        ]);
        const aData = await aRes.json();
        const lData = await lRes.json();
        
        const attendanceCountElem = document.getElementById("pendingAttendanceCount");
        const leaveCountElem = document.getElementById("pendingLeaveCount");
        const statusElem = document.getElementById("managerRealtimeStatus");
        
        if(attendanceCountElem) attendanceCountElem.textContent = aData.success ? aData.data.length : 0;
        if(leaveCountElem) leaveCountElem.textContent = lData.success ? lData.data.length : 0;
        if(statusElem) statusElem.textContent = "Realtime đang hoạt động. Số liệu tự cập nhật khi có phát sinh mới.";
    } catch(e) {
        const statusElem = document.getElementById("managerRealtimeStatus");
        if(statusElem) statusElem.textContent = "Không thể tải dữ liệu.";
    }
}

async function fetchParentStats() {
    try {
        const response = await fetch("/Parent/Api/DashboardStats");
        const data = await response.json();
        if (data.success) {
            const childrenCount = document.getElementById("parentChildrenCount");
            const tuitionCount = document.getElementById("parentTuitionCount");
            const attendanceStatus = document.getElementById("parentAttendanceStatus");

            if (childrenCount) childrenCount.textContent = data.data.childrenCount;
            
            if (tuitionCount) {
                tuitionCount.textContent = data.data.unpaidTuitions;
                if (data.data.unpaidTuitions > 0) {
                    tuitionCount.classList.remove("text-success");
                    tuitionCount.classList.add("text-danger");
                } else {
                    tuitionCount.classList.remove("text-danger");
                    tuitionCount.classList.add("text-success");
                }
            }
            
            if (attendanceStatus) {
                if (data.data.totalRecorded === 0) {
                    attendanceStatus.textContent = "Chưa có dữ liệu";
                    attendanceStatus.classList.add("text-muted");
                } else {
                    attendanceStatus.textContent = `${data.data.presentCount} / ${data.data.totalRecorded} bé có mặt`;
                    attendanceStatus.classList.add("text-primary");
                }
            }
        }
    } catch(e) {
        console.error("Failed to load parent stats", e);
    }
}

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById("managerRealtimeStatus")) {
        refreshManagerCounts();
    }
    if (document.getElementById("parentDashboardGrid")) {
        fetchParentStats();
    }
});
