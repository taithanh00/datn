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

// Chỉ chạy nếu là Manager (có phần tử managerRealtimeStatus)
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById("managerRealtimeStatus")) {
        refreshManagerCounts();
    }
});
