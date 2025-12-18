const API_BASE_URL = '/api';


let selectedIncident = null;

function checkAuthorization() {
    const adminId = localStorage.getItem('userId') || sessionStorage.getItem('userId');
    const adminRole = localStorage.getItem('userRole') || sessionStorage.getItem('userRole');

    if (!adminId || (adminRole !== 'Admin' && adminRole !== 'Moderator')) {
        document.body.style.display = 'none';
        window.location.href = "landing.html";
        return false;
    }
    return true;
}

window.addEventListener('pageshow', function (event) {
    checkAuthorization();
});

let pendingIncidents = [];
let validatedIncidents = [];
let deletedIncidents = [];
let allIncidents = [];
let filteredIncidents = [];

async function fetchIncidentsByStatus(status) {
    let endpoint = '';
    switch (status) {
        case 'Pending':
            endpoint = '/Admin/pending';
            break;
        case 'Validated':
            endpoint = '/Incidents';
            break;
        case 'Rejected':
            endpoint = '/Admin/deleted';
            break;
        default:
            return [];
    }

    try {
        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
            headers: {
                'X-Requester-Id': localStorage.getItem('userId') || sessionStorage.getItem('userId'),
                'X-Requester-Role': localStorage.getItem('userRole') || sessionStorage.getItem('userRole')
            }
        });
        if (!response.ok) {
            console.error(`Error loading ${status} incidents: HTTP ${response.status}`);
            return [];
        }
        return await response.json();
    } catch (error) {
        console.error(`Error fetching ${status} incidents:`, error);
        return [];
    }
}

async function loadInitialIncidents() {
    const adminId = localStorage.getItem('userId') || sessionStorage.getItem('userId');
    if (!adminId) {
        alert("Authorization required. Redirecting to login.");
        window.location.href = "login.html";
        return;
    }

    pendingIncidents = await fetchIncidentsByStatus('Pending');
    allIncidents = pendingIncidents;
    filteredIncidents = pendingIncidents;
    selectedIncident = filteredIncidents[0] || null;

    renderIncidentList();
    if (selectedIncident) selectIncident(selectedIncident);
    else clearDetailPanel();
}

function clearDetailPanel() {
    const detailPanel = document.querySelector('.report-detail-panel');
    if (!detailPanel) return;

    const titleDisplayEl = detailPanel.querySelector('#reportTitleDisplay');
    const reportIdDisplayEl = detailPanel.querySelector('#reportIdDisplay');

    if (titleDisplayEl) titleDisplayEl.textContent = '-- Select a Report --';
    if (reportIdDisplayEl) reportIdDisplayEl.textContent = '#--';

    detailPanel.querySelector('.location-details p').textContent = 'No data selected.';

    const detailGroup = detailPanel.querySelector('.detail-group');
    if (detailGroup) detailGroup.innerHTML = '';

    const mediaPlaceholder = detailPanel.querySelector('.media-placeholder');
    if (mediaPlaceholder) mediaPlaceholder.innerHTML = '<p>Select a report to view details.</p>';

    const approveBtn = detailPanel.querySelector('#approveBtn');
    const rejectBtn = detailPanel.querySelector('#rejectBtn');
    const unvalidateBtn = detailPanel.querySelector('#unvalidateBtn');
    const restoreBtn = detailPanel.querySelector('#restoreBtn');
    const deleteBtn = detailPanel.querySelector('#deletePermBtn');
    const editBtn = detailPanel.querySelector('#editBtn');

    const actionButtons = [approveBtn, rejectBtn, unvalidateBtn, restoreBtn, deleteBtn, editBtn];
    actionButtons.forEach(btn => {
        if (btn) btn.style.display = 'none';
    });
}

function renderIncidentList() {
    const queueList = document.querySelector('.incident-queue-list');
    if (!queueList) return;

    queueList.innerHTML = '';

    if (filteredIncidents.length === 0) {
        queueList.innerHTML = '<div class="report-item"><p>No reports found.</p></div>';
        clearDetailPanel();
        return;
    }

    filteredIncidents.forEach(incident => {
        const reportItem = document.createElement('div');
        reportItem.className = 'report-item';
        if (selectedIncident && selectedIncident.incidentID === incident.incidentID) {
            reportItem.classList.add('selected');
        }

        const reporterName = incident.user?.username || 'Anonymous User';

        let formattedDate = 'N/A';
        if (incident.incidentDateTime) {
            try {
                const d = new Date(incident.incidentDateTime);
                formattedDate = d.toLocaleDateString() + ' ' + d.toLocaleTimeString();
            } catch { }
        }

        let currentStatus = 'Pending';
        if (incident.validStatus) {
            if (incident.validStatus.validation_Status === true) currentStatus = 'Validated';
            else if (incident.validStatus.validation_Status === false && incident.validStatus.validation_Date) currentStatus = 'Rejected';
        } else if (incident.validationDate) {
            currentStatus = 'Rejected';
        }

        let thumbnailContent = '';
        if (incident.img) {
            thumbnailContent = `<img src="data:image/jpeg;base64,${incident.img}" alt="Thumbnail" style="width:100%; height:100%; object-fit:cover;">`;
        } else {
            thumbnailContent = '<span class="material-icons" style="font-size: 24px; color: #ccc;">image</span>';
        }

        reportItem.innerHTML = `
            <div class="report-thumbnail">${thumbnailContent}</div>
            <div class="report-info">
                <h4>${incident.title || 'Untitled'}</h4>
                <p>${formattedDate} – Reported by: ${reporterName}</p> 
            </div>
            <span class="badge ${currentStatus.toLowerCase()}">${currentStatus}</span>
            <button class="view-btn">View</button>
        `;

        reportItem.addEventListener('click', () => selectIncident(incident));
        queueList.appendChild(reportItem);
    });
}

function selectIncident(incident) {
    selectedIncident = incident;
    renderIncidentList();
    renderIncidentDetails(incident);
}

function renderIncidentDetails(incident) {
    const detailPanel = document.querySelector('.report-detail-panel');
    if (!detailPanel) return;

    const date = incident.incidentDateTime ? new Date(incident.incidentDateTime) : null;
    const formattedDate = date ? date.toLocaleDateString() + ' ' + date.toLocaleTimeString() : 'N/A';

    const areaObject = incident.area;
    let incidentLocation = 'Location not specified';

    if (areaObject) {
        incidentLocation = areaObject.ALocation || areaObject.aLocation || 'Location Name N/A';
    }

    if (incident.locationAddress) {
        incidentLocation = incident.locationAddress;
    }
    else if ((incidentLocation === 'Reported Incident' || incidentLocation === 'Location Name N/A' || incidentLocation === 'Unknown Area') && incident.latitude && incident.longitude) {
        incidentLocation = `Coordinates: ${incident.latitude}, ${incident.longitude}`;
    }

    let currentStatus = 'Pending';
    if (incident.validStatus) {
        if (incident.validStatus.validation_Status === true) currentStatus = 'Validated';
        else if (incident.validStatus.validation_Status === false && incident.validStatus.validation_Date) currentStatus = 'Rejected';
    } else if (incident.validationDate) {
        currentStatus = 'Rejected';
    }

    const mediaPlaceholder = detailPanel.querySelector('.media-placeholder');
    if (mediaPlaceholder) {
        mediaPlaceholder.innerHTML = '';
        if (incident.img) {
            try {
                const img = document.createElement('img');
                img.src = `data:image/jpeg;base64,${incident.img}`;
                img.style.width = '100%';
                img.style.maxHeight = '300px';
                img.style.objectFit = 'contain';
                img.onerror = () => { mediaPlaceholder.innerHTML = '<p>Error loading image.</p>'; };
                mediaPlaceholder.appendChild(img);
            } catch {
                mediaPlaceholder.innerHTML = '<p>Error displaying image.</p>';
            }
        } else {
            mediaPlaceholder.innerHTML = '<p>No image available</p>';
        }
    }

    const titleDisplayEl = detailPanel.querySelector('#reportTitleDisplay');
    const reportIdDisplayEl = detailPanel.querySelector('#reportIdDisplay');

    if (titleDisplayEl) titleDisplayEl.textContent = incident.title || 'Untitled Report';
    if (reportIdDisplayEl) reportIdDisplayEl.textContent = `#${incident.incidentID}`;

    const locEl = detailPanel.querySelector('.location-details p');
    if (locEl) locEl.textContent = incidentLocation;

    const detailGroup = detailPanel.querySelector('.detail-group');
    if (detailGroup) {
        let typeCode = incident.IncidentType || incident.incident_Code || 'N/A';
        const typeMapping = {
            'fire': 'Fire',
            'flood': 'Flood',
            'road': 'Accident',
            'accident': 'Accident',
            'earthquake': 'Environmental/Nature',
            'other': 'Others'
        };
        let typeDisplay = typeMapping[typeCode.toLowerCase()] || typeCode;

        const otherText = incident.otherHazard || incident.OtherHazard;
        if (typeDisplay.toLowerCase().includes('other') && otherText) {
            typeDisplay += ` (${otherText})`;
        }

        detailGroup.innerHTML = `
            <p>Reporter:</p> <span class="detail-value">${incident.user?.username || 'Unknown'}</span>
            <p>Status:</p> <span class="detail-value">${currentStatus}</span>
            <p>Incident Title:</p> <span class="detail-value">${incident.title || 'N/A'}</span>
            <p>Type:</p> <span class="detail-value">${typeDisplay}</span>
            <p>Severity:</p> <span class="detail-value">${(incident.severity ? incident.severity.charAt(0).toUpperCase() + incident.severity.slice(1).toLowerCase() : 'N/A')}</span>
            <p>Date & Time:</p> <span class="detail-value">${formattedDate}</span>
            <p>Description:</p> <span class="detail-value" style="grid-column: 1 / 3;">${incident.descr || 'No description'}</span>
        `;
    }

    const approveBtn = detailPanel.querySelector('#approveBtn');
    const rejectBtn = detailPanel.querySelector('#rejectBtn');
    const unvalidateBtn = detailPanel.querySelector('#unvalidateBtn');
    const restoreBtn = detailPanel.querySelector('#restoreBtn');
    const deleteBtn = detailPanel.querySelector('#deletePermBtn');
    const editBtn = detailPanel.querySelector('#editBtn');

    const actionButtons = [approveBtn, rejectBtn, unvalidateBtn, restoreBtn, deleteBtn, editBtn];
    actionButtons.forEach(btn => {
        if (btn) btn.style.display = 'none';
    });


    if (currentStatus === 'Pending') {
        if (approveBtn) approveBtn.style.display = 'flex';
        if (rejectBtn) rejectBtn.style.display = 'flex';
        if (editBtn) editBtn.style.display = 'flex';

        if (approveBtn) approveBtn.onclick = () => validateIncident(incident.incidentID, true);
        if (rejectBtn) rejectBtn.onclick = () => validateIncident(incident.incidentID, false);
    }
    else if (currentStatus === 'Validated') {
        if (unvalidateBtn) unvalidateBtn.style.display = 'flex';
        if (editBtn) editBtn.style.display = 'flex';

        if (unvalidateBtn) unvalidateBtn.onclick = () => unvalidateIncident(incident.incidentID);
    }
    else if (currentStatus === 'Rejected') {
        if (restoreBtn) restoreBtn.style.display = 'flex';
        if (deleteBtn) deleteBtn.style.display = 'flex';

        if (restoreBtn) restoreBtn.onclick = () => restoreIncident(incident.incidentID || incident.rejectedIncidentID);
        if (deleteBtn) deleteBtn.onclick = () => deleteIncidentPermanently(incident.incidentID || incident.rejectedIncidentID);
    }

    if (editBtn) editBtn.onclick = () => openEditModal(incident);
}

function openEditModal(incident) {
    const modal = document.getElementById('editModal');
    if (!modal) {
        alert("Edit Modal not implemented in HTML.");
        return;
    }

    document.getElementById('editIncidentId').value = incident.incidentID;
    document.getElementById('editTitle').value = incident.title || '';
    document.getElementById('editType').value = incident.incident_Code || 'other';
    let sev = (incident.severity || 'low').toLowerCase();
    if (sev === 'medium') sev = 'moderate';
    document.getElementById('editSeverity').value = sev;
    document.getElementById('editDescription').value = incident.descr || '';

    document.getElementById('editLocationAddress').value = incident.locationAddress || '';

    const otherGroup = document.getElementById('editOtherGroup');
    const otherInput = document.getElementById('editOtherHazard');

    if (incident.incident_Code && incident.incident_Code.toLowerCase() === 'other') {
        if (otherGroup) otherGroup.style.display = 'block';
        if (otherInput) otherInput.value = incident.otherHazard || incident.OtherHazard || '';
    } else {
        if (otherGroup) otherGroup.style.display = 'none';
        if (otherInput) otherInput.value = '';
    }

    modal.style.display = 'block';
}

function closeEditModal() {
    const modal = document.getElementById('editModal');
    if (modal) modal.style.display = 'none';
}

async function saveIncidentChanges(e) {
    e.preventDefault();

    const id = document.getElementById('editIncidentId').value;
    const title = document.getElementById('editTitle').value;
    const type = document.getElementById('editType').value;
    const severity = document.getElementById('editSeverity').value;
    const descr = document.getElementById('editDescription').value;
    const otherHazard = document.getElementById('editOtherHazard').value;
    const locationAddress = document.getElementById('editLocationAddress').value;

    const formData = new FormData();
    formData.append('Title', title);
    formData.append('Incident_Code', type);
    formData.append('Severity', severity);
    formData.append('Descr', descr);
    formData.append('LocationAddress', locationAddress);

    if (type.toLowerCase() === 'other') {
        formData.append('OtherHazard', otherHazard);
    }

    try {
        const response = await fetch(`${API_BASE_URL}/Incidents/${id}`, {
            method: 'PUT',
            headers: {
                'X-Requester-Id': localStorage.getItem('userId') || sessionStorage.getItem('userId'),
                'X-Requester-Role': localStorage.getItem('userRole') || sessionStorage.getItem('userRole')
            },
            body: formData
        });

        if (!response.ok) {
            const errorBody = await response.json();
            throw new Error(errorBody.message || "Failed to update incident on server.");
        }

        const result = await response.json();
        alert("Incident updated successfully!");
        closeEditModal();

        const activeBtn = document.querySelector('.filter-btn.active');
        const status = activeBtn ? activeBtn.textContent.trim() : 'Pending';
        await handleFilterClick(activeBtn, status);

    } catch (error) {
        console.error("Error updating incident:", error);
        alert(`Error updating incident: ${error.message}. Please check console.`);
    }
}

function setupEditModal() {
    const closeBtn = document.querySelector('.close-modal');
    const form = document.getElementById('editForm');
    const typeSelect = document.getElementById('editType');
    const editButton = document.querySelector('#editBtn');

    if (closeBtn) closeBtn.onclick = closeEditModal;
    if (form) form.onsubmit = saveIncidentChanges;

    if (typeSelect) {
        typeSelect.onchange = () => {
            const otherGroup = document.getElementById('editOtherGroup');
            if (otherGroup) {
                otherGroup.style.display = typeSelect.value.toLowerCase() === 'other' ? 'block' : 'none';
            }
        };
    }

    window.onclick = (event) => {
        const modal = document.getElementById('editModal');
        if (event.target === modal) closeEditModal();
    };
}

async function handleFilterClick(targetBtn, status) {
    const filterButtons = document.querySelectorAll('.filter-btn');

    filterButtons.forEach(btn => btn.classList.remove('active'));
    targetBtn.classList.add('active');

    const scrollPosition = window.scrollY;

    try {
        selectedIncident = null;

        const queueList = document.querySelector('.incident-queue-list');
        if (queueList) queueList.innerHTML = '<div class="report-item"><p>Loading...</p></div>';

        allIncidents = await fetchIncidentsByStatus(status);
        filteredIncidents = [...allIncidents];

        if (window.resetAdvancedFilters) {
            window.resetAdvancedFilters();
        }

        const searchInput = document.querySelector('.search-bar input');
        if (searchInput) searchInput.value = '';

        renderIncidentList();

        if (filteredIncidents.length > 0) {
            selectIncident(filteredIncidents[0]);
        } else {
            clearDetailPanel();
        }

        window.scrollTo(0, scrollPosition);

    } catch (error) {
        console.error('Error handling filter click:', error);
    }
}



let filterType = '';
let filterSeverity = '';
let filterDateFrom = '';
let filterDateTo = '';

function setupAdvancedFilters() {
    const filterBtn = document.getElementById('searchBtn');
    const filterMenu = document.getElementById('filterMenu');
    const typeSelect = document.getElementById('filterType');
    const severitySelect = document.getElementById('filterSeverity');
    const dateFromInput = document.getElementById('filterDateFrom');
    const dateToInput = document.getElementById('filterDateTo');
    const clearBtn = document.getElementById('clearFiltersBtn');
    const searchInput = document.getElementById('searchInput');

    if (filterBtn && filterMenu) {
        filterBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            filterMenu.classList.toggle('show');
        });

        window.addEventListener('click', (e) => {
            if (!filterMenu.contains(e.target) && !filterBtn.contains(e.target)) {
                filterMenu.classList.remove('show');
            }
        });
    }

    const applyFilters = () => {
        const searchTerm = searchInput ? searchInput.value.toLowerCase() : '';
        filterType = typeSelect ? typeSelect.value : '';
        filterSeverity = severitySelect ? severitySelect.value : '';
        filterDateFrom = dateFromInput ? dateFromInput.value : '';
        filterDateTo = dateToInput ? dateToInput.value : '';

        filteredIncidents = allIncidents.filter(i => {
            const matchesSearch = !searchTerm ||
                (i.title || '').toLowerCase().includes(searchTerm) ||
                (i.descr || '').toLowerCase().includes(searchTerm) ||
                (i.incidentID.toString() === searchTerm) ||
                (i.user?.username || '').toLowerCase().includes(searchTerm) ||
                (i.area?.ALocation || i.area?.aLocation || '').toLowerCase().includes(searchTerm) ||
                (i.locationAddress || '').toLowerCase().includes(searchTerm) ||
                (i.incident_Code || i.IncidentType || '').toLowerCase().includes(searchTerm) ||
                (i.severity || '').toLowerCase().includes(searchTerm) ||
                (i.otherHazard || '').toLowerCase().includes(searchTerm);


            const incidentType = (i.incident_Code || i.IncidentType || '').toLowerCase();
            let filterTypeVal = filterType ? filterType.toLowerCase() : '';


            if (filterTypeVal === 'accident' || filterTypeVal === 'road') {
                const isAccident = incidentType === 'accident' || incidentType === 'road';
                if (!isAccident && filterTypeVal) return false;
            } else {
                const matchesType = !filterType || incidentType.includes(filterTypeVal);
                if (!matchesType) return false;
            }

            const matchesType = true;


            const severity = (i.severity || '').toLowerCase();
            let normalizedSeverity = severity === 'medium' ? 'moderate' : severity;
            const matchesSeverity = !filterSeverity || normalizedSeverity === filterSeverity.toLowerCase();


            let matchesDate = true;
            if (filterDateFrom || filterDateTo) {
                const iDate = i.incidentDateTime ? new Date(i.incidentDateTime) : null;
                if (iDate) {
                    if (filterDateFrom) {
                        const fromDate = new Date(filterDateFrom);

                        fromDate.setHours(0, 0, 0, 0);
                        if (iDate < fromDate) matchesDate = false;
                    }
                    if (matchesDate && filterDateTo) {
                        const toDate = new Date(filterDateTo);

                        toDate.setHours(23, 59, 59, 999);
                        if (iDate > toDate) matchesDate = false;
                    }
                } else {
                    matchesDate = false;
                    matchesDate = false;
                }
            }

            return matchesSearch && matchesType && matchesSeverity && matchesDate;
        });

        renderIncidentList();

        if (filteredIncidents.length > 0) {

            if (selectedIncident && filteredIncidents.find(i => i.incidentID === selectedIncident.incidentID)) {
                selectIncident(selectedIncident);
            } else {
                selectIncident(filteredIncidents[0]);
            }
        } else {
            clearDetailPanel();
        }
    };

    if (typeSelect) typeSelect.addEventListener('change', applyFilters);
    if (severitySelect) severitySelect.addEventListener('change', applyFilters);
    if (dateFromInput) dateFromInput.addEventListener('change', applyFilters);
    if (dateToInput) dateToInput.addEventListener('change', applyFilters);

    if (searchInput) {
        searchInput.addEventListener('input', applyFilters);
    }

    if (clearBtn) {
        clearBtn.addEventListener('click', () => {
            if (typeSelect) typeSelect.value = '';
            if (severitySelect) severitySelect.value = '';
            if (dateFromInput) dateFromInput.value = '';
            if (dateToInput) dateToInput.value = '';
            if (searchInput) searchInput.value = '';
            applyFilters();
        });
    }


    window.resetAdvancedFilters = () => {
        if (typeSelect) typeSelect.value = '';
        if (severitySelect) severitySelect.value = '';
        if (dateFromInput) dateFromInput.value = '';
        if (dateToInput) dateToInput.value = '';
        if (searchInput) searchInput.value = '';
        filterType = '';
        filterSeverity = '';
        filterDateFrom = '';
        filterDateTo = '';
    };
}



function setupFilters() {
    document.querySelectorAll('.filter-btn').forEach(btn => {
        btn.addEventListener('click', e => {
            const status = e.currentTarget.getAttribute('data-status');
            console.log('Filter clicked:', status);
            if (status) {
                handleFilterClick(e.currentTarget, status);
            }
        });
    });
}


async function validateIncident(incidentId, isAccepted) {
    if (!confirm(`Are you sure you want to ${isAccepted ? 'approve' : 'reject'} this report?`)) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Admin/validate/${incidentId}?isAccepted=${isAccepted}`, {
            method: 'POST',
            headers: {
                'X-Requester-Id': localStorage.getItem('userId') || sessionStorage.getItem('userId'),
                'X-Requester-Role': localStorage.getItem('userRole') || sessionStorage.getItem('userRole')
            }
        });

        if (!response.ok) throw new Error("Validation failed");

        const data = await response.json();
        alert(data.message ?? "Action completed.");

        const active = document.querySelector('.filter-btn.active');
        if (active) handleFilterClick(active, active.textContent.trim());
        else loadInitialIncidents();
    } catch (err) {
        console.error(err);
        alert("Failed to validate incident.");
    }
}

async function unvalidateIncident(incidentId) {
    if (!confirm("Are you sure you want to INVALIDATE this report? It will be moved back to the Pending queue.")) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Admin/unvalidate/${incidentId}`, {
            method: 'POST',
            headers: {
                'X-Requester-Id': localStorage.getItem('userId') || sessionStorage.getItem('userId'),
                'X-Requester-Role': localStorage.getItem('userRole') || sessionStorage.getItem('userRole')
            }
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || "Unvalidation failed.");
        }

        const result = await response.json();
        alert(result.message || "Incident unvalidated successfully and moved to Pending.");

        const active = document.querySelector('.filter-btn.active');
        if (active) handleFilterClick(active, active.textContent.trim());
    } catch (error) {
        console.error("Error unvalidating incident:", error);
        alert(error.message);
    }
}

async function restoreIncident(id) {
    if (!confirm("Are you sure you want to restore this incident to Pending?")) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Admin/recover/${id}`, {
            method: 'POST',
            headers: {
                'X-Requester-Id': localStorage.getItem('userId') || sessionStorage.getItem('userId'),
                'X-Requester-Role': localStorage.getItem('userRole') || sessionStorage.getItem('userRole')
            }
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || "Failed to restore incident.");
        }

        const result = await response.json();
        alert(result.message || "Incident restored successfully.");

        const active = document.querySelector('.filter-btn.active');
        if (active) handleFilterClick(active, active.textContent.trim());
    } catch (error) {
        console.error("Error restoring incident:", error);
        alert(error.message);
    }
}

async function deleteIncidentPermanently(id) {
    if (!confirm("WARNING: Are you sure you want to PERMANENTLY DELETE this rejected report? This action cannot be undone.")) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Admin/delete-permanent/${id}`, {
            method: 'DELETE',
            headers: {
                'X-Requester-Id': localStorage.getItem('userId') || sessionStorage.getItem('userId'),
                'X-Requester-Role': localStorage.getItem('userRole') || sessionStorage.getItem('userRole')
            }
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || "Failed to permanently delete incident.");
        }

        alert("Incident permanently deleted.");

        const active = document.querySelector('.filter-btn.active');
        if (active) handleFilterClick(active, active.textContent.trim());
    } catch (error) {
        console.error("Error permanently deleting incident:", error);
        alert(error.message);
    }
}
function setupLogout() {
    const logoutBtn = document.querySelector('.logout-btn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', (e) => {
            e.preventDefault();
            if (confirm('Are you sure you want to logout?')) {
                localStorage.clear();
                sessionStorage.clear();
                window.location.href = 'landing.html';
            }
        });
    }
}

document.addEventListener('DOMContentLoaded', () => {
    if (!checkAuthorization()) return;
    loadInitialIncidents();
    setupFilters();

    setupAdvancedFilters();
    setupEditModal();
    setupLogout();

    const defaultBtn = document.querySelector('.filter-buttons .filter-btn:first-child');
    if (defaultBtn) defaultBtn.classList.add('active');
    window.selectIncident = selectIncident;
});
