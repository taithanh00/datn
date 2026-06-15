// ====== MANAGER ACTIVITIES PAGE ======

let currentActivityId = null;
let isActivityEditMode = false;
let showInactiveActivities = false;
let showInactiveLocations = false;
let activitiesCache = [];
let locationsCache = [];
let currentLocationId = null;

document.addEventListener("DOMContentLoaded", async () => {
  setupActivityPanelListeners();
  await Promise.all([loadActivities(), loadLocations(), loadClassesAndTeachers()]);
});

function setupActivityPanelListeners() {
  document
    .getElementById("btnCreateActivity")
    .addEventListener("click", openCreateActivityPanel);
  document
    .getElementById("closeActivityPanelBtn")
    .addEventListener("click", closeActivityPanel);
  document
    .getElementById("activityModalOverlay")
    .addEventListener("click", closeActivityPanel);
  document
    .getElementById("activityForm")
    .addEventListener("submit", saveActivity);

  document
    .getElementById("btnCreateLocation")
    .addEventListener("click", openCreateLocationPanel);
  document
    .getElementById("closeLocationPanelBtn")
    .addEventListener("click", closeLocationPanel);
  document
    .getElementById("locationModalOverlay")
    .addEventListener("click", closeLocationPanel);
  document
    .getElementById("locationForm")
    .addEventListener("submit", saveLocation);

  document.querySelectorAll("#activityTabActive, #activityTabInactive").forEach((tab) => {
    tab.addEventListener("click", function () {
      document
        .querySelectorAll("#activityTabActive, #activityTabInactive")
        .forEach((t) => t.classList.remove("active"));
      this.classList.add("active");
      showInactiveActivities = this.getAttribute("data-show-inactive") === "true";
      loadActivities();
    });
  });

  document.querySelectorAll("#locationTabActive, #locationTabInactive").forEach((tab) => {
    tab.addEventListener("click", function () {
      document
        .querySelectorAll("#locationTabActive, #locationTabInactive")
        .forEach((t) => t.classList.remove("active"));
      this.classList.add("active");
      showInactiveLocations = this.getAttribute("data-show-inactive") === "true";
      loadLocations();
    });
  });
}

async function fetchJson(url, options = {}) {
  try {
    const r = await fetch(url, {
      headers: { "Content-Type": "application/json" },
      ...options,
    });
    return await r.json();
  } catch (e) {
    return { success: false, message: "Lỗi kết nối." };
  }
}

// ====== ACTIVITY PANEL ======
function openCreateActivityPanel() {
  isActivityEditMode = false;
  currentActivityId = null;
  document.getElementById("activityForm").reset();
  document.getElementById("activityId").value = "";
  document.getElementById("activityPanelTitle").textContent = "Thêm hoạt động mới";
  document.getElementById("saveActivityBtn").innerHTML =
    '<i class="fa-solid fa-floppy-disk"></i> Lưu hoạt động';
  document.querySelectorAll('input[name="participatingClasses"]').forEach((cb) => {
    cb.checked = false;
  });
  hideActivityFormAlert();
  openActivityPanel();
}

async function openEditActivityPanel(id) {
  const a = activitiesCache.find((x) => x.id === id);
  if (!a) return;

  isActivityEditMode = true;
  currentActivityId = id;
  document.getElementById("activityId").value = id;
  document.getElementById("activityName").value = a.name;
  document.getElementById("activityDescription").value = a.description || "";
  document.getElementById("activityDate").value = a.date;
  document.getElementById("locationSelect").value = a.locationId || "";
  document.getElementById("organizerSelect").value = a.organizerId || "";
  document.querySelectorAll('input[name="participatingClasses"]').forEach((cb) => {
    cb.checked = a.classes.some((c) => c.id === parseInt(cb.value, 10));
  });
  document.getElementById("activityPanelTitle").textContent = `Sửa hoạt động - ${a.name}`;
  document.getElementById("saveActivityBtn").innerHTML =
    '<i class="fa-solid fa-floppy-disk"></i> Cập nhật hoạt động';
  hideActivityFormAlert();
  openActivityPanel();
}

function openActivityPanel() {
  document.getElementById("activityModalOverlay").classList.add("active");
  document.getElementById("activitySlidePanel").classList.add("active");
  document.body.style.overflow = "hidden";
}

function closeActivityPanel() {
  document.getElementById("activityModalOverlay").classList.remove("active");
  document.getElementById("activitySlidePanel").classList.remove("active");
  document.body.style.overflow = "auto";
  isActivityEditMode = false;
  currentActivityId = null;
}

function openCreateLocationPanel() {
  currentLocationId = null;
  document.getElementById("locationForm").reset();
  document.getElementById("locationId").value = "";
  document.getElementById("locationPanelTitle").textContent = "Thêm địa điểm mới";
  document.getElementById("saveLocationBtn").innerHTML =
    '<i class="fa-solid fa-floppy-disk"></i> Thêm địa điểm';
  hideLocationFormAlert();
  document.getElementById("locationModalOverlay").classList.add("active");
  document.getElementById("locationSlidePanel").classList.add("active");
  document.body.style.overflow = "hidden";
}

function openEditLocationPanel(id) {
  const loc = locationsCache.find((l) => l.id === id);
  if (!loc) return;

  currentLocationId = id;
  document.getElementById("locationId").value = id;
  document.getElementById("locationName").value = loc.name || "";
  document.getElementById("locationCapacity").value =
    loc.capacity !== null && loc.capacity !== undefined ? loc.capacity : "";
  document.getElementById("locationPanelTitle").textContent = `Sửa địa điểm - ${loc.name}`;
  document.getElementById("saveLocationBtn").innerHTML =
    '<i class="fa-solid fa-floppy-disk"></i> Cập nhật địa điểm';
  hideLocationFormAlert();
  document.getElementById("locationModalOverlay").classList.add("active");
  document.getElementById("locationSlidePanel").classList.add("active");
  document.body.style.overflow = "hidden";
}

function closeLocationPanel() {
  document.getElementById("locationModalOverlay").classList.remove("active");
  document.getElementById("locationSlidePanel").classList.remove("active");
  currentLocationId = null;
  if (!document.getElementById("activitySlidePanel").classList.contains("active")) {
    document.body.style.overflow = "auto";
  }
}

// ====== DATA ======
async function loadActivities() {
  const activitiesTbody = document.getElementById("activitiesTableBody");
  if (window.appLoading && activitiesTbody) {
    window.appLoading.setTable(activitiesTbody, 6);
  }
  const result = await fetchJson(
    `/Manager/Api/Activities?showInactive=${showInactiveActivities}`,
  );
  const tbody = document.getElementById("activitiesTableBody");
  tbody.innerHTML = "";

  if (!result.success) {
    if (window.appLoading) {
      tbody.innerHTML = window.appLoading.tableError(6, "Lỗi tải dữ liệu.");
      return;
    }
    tbody.innerHTML =
      '<tr><td colspan="6" class="text-center text-muted" style="padding:24px;">Lỗi tải dữ liệu.</td></tr>';
    return;
  }

  activitiesCache = result.data;

  if (result.data.length === 0) {
    if (window.appLoading) {
      tbody.innerHTML = window.appLoading.tableEmpty(6, showInactiveActivities
        ? "Không có hoạt động nào trong kho lưu trữ."
        : "Không có dữ liệu.");
      return;
    }
    const emptyMsg = showInactiveActivities
      ? "Không có hoạt động nào trong kho lưu trữ."
      : "Không có dữ liệu.";
    tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted" style="padding:24px;">${emptyMsg}</td></tr>`;
    return;
  }

  result.data.forEach((a) => {
    const cls = a.classes.map((c) => `<span class="tag">${c.name}</span>`).join("");
    const actionBtns = a.isActive
      ? `<button class="btn-table" onclick="openEditActivityPanel(${a.id})">Sửa</button><button class="btn-table delete" onclick="deleteActivity(${a.id})">Ẩn</button>`
      : `<button class="btn-table" onclick="reactivateActivity(${a.id})" style="color:var(--primary);">Khôi phục</button>`;
    tbody.innerHTML += `<tr>
        <td><strong>${a.name}</strong></td><td>${a.date}</td>
        <td>${a.locationName || "--"}</td><td>${a.organizerName || "--"}</td>
        <td><div class="tag-list">${cls}</div></td>
        <td>${actionBtns}</td>
      </tr>`;
  });

  if (typeof initPagination === "function") {
    initPagination("activitiesTable", 10);
  }
}

async function loadLocations() {
  const locationsTbody = document.getElementById("locationsTableBody");
  if (window.appLoading && locationsTbody) {
    window.appLoading.setTable(locationsTbody, 3);
  }
  const result = await fetchJson(
    `/Manager/Api/Locations?showInactive=${showInactiveLocations}`,
  );
  const tbody = document.getElementById("locationsTableBody");
  const select = document.getElementById("locationSelect");
  tbody.innerHTML = "";
  select.innerHTML = '<option value="">-- Chọn --</option>';

  if (result.success) {
    locationsCache = result.data;
    if (result.data.length === 0) {
      if (window.appLoading) {
        tbody.innerHTML = window.appLoading.tableEmpty(3, showInactiveLocations
          ? "Không có địa điểm nào trong kho lưu trữ."
          : "Không có địa điểm nào đang sử dụng.");
        return;
      }
      const emptyMsg = showInactiveLocations
        ? "Không có địa điểm nào trong kho lưu trữ."
        : "Không có địa điểm nào đang sử dụng.";
      tbody.innerHTML = `<tr><td colspan="3" class="text-center text-muted" style="padding:24px;">${emptyMsg}</td></tr>`;
      return;
    }
    result.data.forEach((l) => {
      const actionBtn = l.isActive
        ? `<button class="btn-table" onclick="openEditLocationPanel(${l.id})">Sửa</button><button class="btn-table delete" onclick="deleteLocation(${l.id})">Ẩn</button>`
        : `<button class="btn-table" onclick="reactivateLocation(${l.id})" style="color:var(--primary);">Khôi phục</button>`;
      const capacityDisplay =
        l.capacity !== null && l.capacity !== undefined ? l.capacity : "--";
      tbody.innerHTML += `<tr><td>${l.name}</td><td>${capacityDisplay}</td><td class="text-end">${actionBtn}</td></tr>`;
      if (l.isActive) {
        select.innerHTML += `<option value="${l.id}">${l.name}</option>`;
      }
    });
  }
}

async function loadClassesAndTeachers() {
  const [cR, tR] = await Promise.all([
    fetchJson("/Manager/Api/Classes"),
    fetchJson("/Manager/Api/Teachers"),
  ]);
  if (cR.success) {
    document.getElementById("classCheckboxList").innerHTML = cR.data
      .map(
        (c) =>
          `<div class="class-item"><input type="checkbox" id="class_${c.id}" value="${c.id}" name="participatingClasses"/><label for="class_${c.id}">${c.name}</label></div>`,
      )
      .join("");
  }
  if (tR.success) {
    document.getElementById("organizerSelect").innerHTML =
      '<option value="">-- Chọn --</option>' +
      tR.data.map((t) => `<option value="${t.id}">${t.fullName}</option>`).join("");
  }
}

async function saveActivity(e) {
  e.preventDefault();
  const classIds = Array.from(
    document.querySelectorAll('input[name="participatingClasses"]:checked'),
  ).map((cb) => parseInt(cb.value, 10));
  const data = {
    name: document.getElementById("activityName").value,
    description: document.getElementById("activityDescription").value,
    date: document.getElementById("activityDate").value,
    locationId: parseInt(document.getElementById("locationSelect").value, 10) || null,
    organizerId: parseInt(document.getElementById("organizerSelect").value, 10) || null,
    classIds,
  };
  const url = currentActivityId
    ? `/Manager/Api/Activity/${currentActivityId}`
    : "/Manager/Api/Activity";
  const saveBtn = document.getElementById("saveActivityBtn");
  saveBtn.disabled = true;

  const result = await fetchJson(url, {
    method: currentActivityId ? "PUT" : "POST",
    body: JSON.stringify(data),
  });

  saveBtn.disabled = false;

  if (result.success) {
    showActivityFormAlert(result.message || "Lưu thành công", "success");
    setTimeout(() => {
      closeActivityPanel();
      loadActivities();
    }, 1200);
  } else {
    showActivityFormAlert(result.message || "Lỗi lưu hoạt động", "error");
  }
}

async function deleteActivity(id) {
  if (!confirm("Ẩn hoạt động này?")) return;
  const r = await fetchJson(`/Manager/Api/Activity/${id}`, { method: "DELETE" });
  showPageAlert("activityPageAlert", r.success, r.message);
  if (r.success) loadActivities();
}

async function reactivateActivity(id) {
  if (!confirm("Khôi phục hoạt động này?")) return;
  const r = await fetchJson(`/Manager/Api/Activity/Reactivate/${id}`, {
    method: "POST",
  });
  showPageAlert("activityPageAlert", r.success, r.message);
  if (r.success) loadActivities();
}

function parseLocationCapacityInput() {
  const raw = document.getElementById("locationCapacity").value.trim();
  if (raw === "") return null;
  const value = parseInt(raw, 10);
  return Number.isFinite(value) && value >= 0 ? value : null;
}

async function saveLocation(e) {
  e.preventDefault();
  const data = {
    name: document.getElementById("locationName").value.trim(),
    capacity: parseLocationCapacityInput(),
  };
  const saveBtn = document.getElementById("saveLocationBtn");
  saveBtn.disabled = true;

  const url = currentLocationId
    ? `/Manager/Api/Location/${currentLocationId}`
    : "/Manager/Api/Location";
  const r = await fetchJson(url, {
    method: currentLocationId ? "PUT" : "POST",
    body: JSON.stringify(data),
  });

  saveBtn.disabled = false;

  if (r.success) {
    showLocationFormAlert(
      r.message || (currentLocationId ? "Cập nhật thành công" : "Thêm địa điểm thành công"),
      "success",
    );
    setTimeout(() => {
      closeLocationPanel();
      loadLocations();
    }, 1200);
  } else {
    showLocationFormAlert(r.message || "Lỗi lưu địa điểm", "error");
  }
}

async function deleteLocation(id) {
  if (!confirm("Ẩn địa điểm này?")) return;
  const r = await fetchJson(`/Manager/Api/Location/${id}`, { method: "DELETE" });
  showPageAlert("locationPageAlert", r.success, r.message);
  if (r.success) loadLocations();
}

async function reactivateLocation(id) {
  if (!confirm("Khôi phục địa điểm này?")) return;
  const r = await fetchJson(`/Manager/Api/Location/Reactivate/${id}`, {
    method: "POST",
  });
  showPageAlert("locationPageAlert", r.success, r.message);
  if (r.success) loadLocations();
}

function showActivityFormAlert(message, type) {
  const el = document.getElementById("activityFormAlert");
  if (el) {
    el.style.display = "none";
    el.textContent = "";
  }
  if (window.showToast) window.showToast(type === "success" ? "Thành công" : "Có lỗi", message, type === "success" ? "success" : "error");
}

function hideActivityFormAlert() {
  document.getElementById("activityFormAlert").style.display = "none";
}

function showLocationFormAlert(message, type) {
  const el = document.getElementById("locationFormAlert");
  if (el) {
    el.style.display = "none";
    el.textContent = "";
  }
  if (window.showToast) window.showToast(type === "success" ? "Thành công" : "Có lỗi", message, type === "success" ? "success" : "error");
}

function hideLocationFormAlert() {
  document.getElementById("locationFormAlert").style.display = "none";
}

function showPageAlert(id, success, message) {
  const el = document.getElementById(id);
  if (el) {
    el.style.display = "none";
    el.textContent = "";
  }
  if (window.showToast) window.showToast(success ? "Thành công" : "Có lỗi", message, success ? "success" : "error");
}

// Global handlers for inline onclick
window.openEditActivityPanel = openEditActivityPanel;
window.openEditLocationPanel = openEditLocationPanel;
window.deleteActivity = deleteActivity;
window.reactivateActivity = reactivateActivity;
window.deleteLocation = deleteLocation;
window.reactivateLocation = reactivateLocation;