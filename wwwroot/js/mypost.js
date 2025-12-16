const userId = localStorage.getItem('userId');
const userRole = localStorage.getItem('userRole');
const username = localStorage.getItem('username');

let selectedIncident = null;
let allIncidents = [];
let filteredIncidents = [];

function checkAuth() {
    if (!userId) {
        alert("You must be logged in to view your reports.");
        window.location.href = "login.html";
        return false;
    }
    return true;
}

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
        console.log(`Fetching ${status} incidents from ${endpoint}...`);
        const response = await fetch(`${API_BASE_URL}${endpoint}`);
        if (!response.ok) {
            console.error(`Error loading ${status} incidents: HTTP ${response.status}`);
            return [];
        }
        const data = await response.json();
        console.log(`Raw data for ${status}:`, data);

        if (userId) {
            const filtered = data.filter(i => {
                const incUserId = i.userid || i.Userid || i.userId || i.UserId;
                const nestedUserId =
                    (i.user && (i.user.userid || i.user.Userid || i.user.userId || i.user.UserId)) ||
                    (i.User && (i.User.userid || i.User.Userid || i.User.userId || i.User.UserId));

                const recordId = incUserId || nestedUserId;

                if (!recordId) {
                    console.warn('Incident missing user ID:', i);
                    return false;
                }

                return recordId.toString().toLowerCase() === userId.toLowerCase();
            });

            console.log(`Filtered data for ${status}:`, filtered);
            return filtered;
        }
        return [];
    } catch (error) {
        console.error(`Error fetching ${status} incidents:`, error);
        return [];
    }
}

async function loadInitialIncidents() {
    if (!checkAuth()) return;
    const activeBtn = document.querySelector('.filter-btn.active');
    await handleFilterClick(activeBtn, 'Pending');
}

async function handleFilterClick(targetBtn, status) {
    const filterButtons = document.querySelectorAll('.filter-btn');
    filterButtons.forEach(btn => btn.classList.remove('active'));
    if (targetBtn) targetBtn.classList.add('active');

    const queueList = document.querySelector('.incident-queue-list');

    try {
        selectedIncident = null;
        if (queueList) queueList.innerHTML = '<div class="report-item"><p>Loading...</p></div>';

        allIncidents = await fetchIncidentsByStatus(status);
        filteredIncidents = [...allIncidents];

        if (window.resetAdvancedFilters) window.resetAdvancedFilters();

        const searchInput = document.querySelector('.search-bar input');
        if (searchInput) searchInput.value = '';

        renderIncidentList(status);

        if (filteredIncidents.length > 0) {
            selectIncident(filteredIncidents[0], status);
        } else {
            clearDetailPanel();
        }
    } catch (error) {
        console.error('Error handling filter click:', error);
        if (queueList) queueList.innerHTML = '<div class="report-item"><p>Error loading reports.</p></div>';
    }
}

function renderIncidentList(status) {
    const queueList = document.querySelector('.incident-queue-list');
    if (!queueList) return;

    queueList.innerHTML = '';

    if (filteredIncidents.length === 0) {
        queueList.innerHTML = '<div class="report-item"><p>No reports found.</p></div>';
        return;
    }

    filteredIncidents.forEach(incident => {
        const item = document.createElement('div');
        item.className = 'report-item';
        if (selectedIncident && selectedIncident.incidentID === incident.incidentID) {
            item.classList.add('selected');
        }

        let dateStr = 'N/A';
        if (incident.incidentDateTime) {
            const d = new Date(incident.incidentDateTime);
            dateStr = d.toLocaleDateString();
        }

        let imgSrc = '';
        if (incident.img) {
            const prefix = incident.img.toString().startsWith('data:image') ? '' : 'data:image/jpeg;base64,';
            imgSrc = `<img src="${prefix}${incident.img}" alt="Thumbnail" style="width:100%; height:100%; object-fit:cover;">`;
        } else {
            imgSrc = '<span class="material-icons" style="font-size:24px; color:#ccc;">image</span>';
        }

        item.innerHTML = `
            <div class="report-thumbnail">${imgSrc}</div>
            <div class="report-info">
                <h4>${incident.title || 'Untitled'}</h4>
                <p>${dateStr}</p>
            </div>
            <span class="badge ${status.toLowerCase()}">${status}</span>
        `;

        item.addEventListener('click', () => selectIncident(incident, status));
        queueList.appendChild(item);
    });
}

function selectIncident(incident, status) {
    selectedIncident = incident;
    renderIncidentList(status);
    renderIncidentDetails(incident, status);
}

function renderIncidentDetails(incident, status) {
    const detailPanel = document.querySelector('.report-detail-panel');
    const scrollableContent = detailPanel.querySelector('.scrollable-detail-content');
    if (!detailPanel || !scrollableContent) return;

    detailPanel.querySelector('.detail-header h2').textContent = incident.title || 'Untitled';
    detailPanel.querySelector('.report-id-text').textContent = `Report ID: #${incident.incidentID}`;

    const mediaPlaceholder = scrollableContent.querySelector('.media-placeholder');
    mediaPlaceholder.innerHTML = '';
    if (incident.img) {
        const prefix = incident.img.toString().startsWith('data:image') ? '' : 'data:image/jpeg;base64,';
        const img = document.createElement('img');
        img.src = prefix + incident.img;
        img.style.width = '100%';
        img.style.maxHeight = '300px';
        img.style.objectFit = 'contain';
        mediaPlaceholder.appendChild(img);
    } else {
        mediaPlaceholder.innerHTML = '<p>No image available</p>';
    }

    const incidentDetailsDiv = scrollableContent.querySelector('.incident-details');
    const date = incident.incidentDateTime ? new Date(incident.incidentDateTime) : null;
    const formattedDate = date ? date.toLocaleString() : 'N/A';

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

    incidentDetailsDiv.innerHTML = `
        <h3>Incident Details</h3>
        <div class="detail-group">
            <p>Status:</p> <span class="detail-value">${status}</span>
            <p>Incident Title:</p> <span class="detail-value">${incident.title || 'N/A'}</span>
            <p>Type:</p> <span class="detail-value">${typeDisplay}</span>
            <p>Severity:</p> <span class="detail-value">${(incident.severity ? incident.severity.charAt(0).toUpperCase() + incident.severity.slice(1).toLowerCase() : 'N/A')}</span>
            <p>Date & Time:</p> <span class="detail-value">${formattedDate}</span>
        </div>
        <div class="full-width-field" style="margin-top: 15px;">
            <p style="width: auto;">Description:</p>
            <span class="detail-value" style="display: block; margin-top: 5px;">${incident.descr || 'No description'}</span>
        </div>
    `;

    const locEl = scrollableContent.querySelector('.location-details p');
    let loc = incident.locationAddress || '';
    if (!loc && incident.latitude && incident.longitude) {
        loc = `Coords: ${incident.latitude}, ${incident.longitude}`;
    }
    locEl.textContent = loc || 'Location not specified';

    const editBtn = document.getElementById('editBtn');
    const deleteBtn = document.getElementById('deleteBtn');

    editBtn.style.display = 'none';
    deleteBtn.style.display = 'none';

    if (status === 'Pending') {
        editBtn.style.display = 'flex';
        deleteBtn.style.display = 'flex';

        editBtn.onclick = () => openEditModal(incident);
        deleteBtn.onclick = () => deleteIncident(incident.incidentID);
    }

    scrollableContent.scrollTop = 0;
}

function clearDetailPanel() {
    const detailPanel = document.querySelector('.report-detail-panel');
    const scrollableContent = detailPanel.querySelector('.scrollable-detail-content');
    if (!detailPanel || !scrollableContent) return;

    detailPanel.querySelector('.detail-header h2').textContent = 'Select a Report';
    detailPanel.querySelector('.report-id-text').textContent = '';

    scrollableContent.querySelector('.media-placeholder').innerHTML = '<p>Select a report to view details.</p>';

    const incidentDetailsDiv = scrollableContent.querySelector('.incident-details');
    incidentDetailsDiv.innerHTML = '<h3>Incident Details</h3><div class="detail-group"></div>';

    scrollableContent.querySelector('.location-details p').textContent = '--';

    document.getElementById('editBtn').style.display = 'none';
    document.getElementById('deleteBtn').style.display = 'none';
}

function openEditModal(incident) {
    const modal = document.getElementById('editModal');

    document.getElementById('editIncidentId').value = incident.incidentID;
    document.getElementById('editTitle').value = incident.title || '';

    const typeSelect = document.getElementById('editType');
    const sevSelect = document.getElementById('editSeverity');

    if (typeSelect) typeSelect.value = incident.incident_Code || 'other';
    if (sevSelect) sevSelect.value = (incident.severity || 'low').toLowerCase();

    document.getElementById('editLocationAddress').value = incident.locationAddress || '';
    document.getElementById('editDescription').value = incident.descr || '';

    const otherGroup = document.getElementById('editOtherGroup');
    const otherInput = document.getElementById('editOtherHazard');

    if (incident.incident_Code === 'other') {
        otherGroup.style.display = 'block';
        otherInput.value = incident.otherHazard || '';
    } else {
        otherGroup.style.display = 'none';
        otherInput.value = '';
    }

    modal.style.display = 'flex';
}

function closeEditModal() {
    document.getElementById('editModal').style.display = 'none';
}

document.getElementById('closeEditModal').addEventListener('click', closeEditModal);

document.getElementById('editType').addEventListener('change', e => {
    const otherGroup = document.getElementById('editOtherGroup');
    otherGroup.style.display = e.target.value === 'other' ? 'block' : 'none';
});

document.getElementById('editForm').addEventListener('submit', async e => {
    e.preventDefault();

    const id = document.getElementById('editIncidentId').value;

    const formData = new FormData();
    formData.append('Title', document.getElementById('editTitle').value);
    formData.append('Incident_Code', document.getElementById('editType').value);
    formData.append('Severity', document.getElementById('editSeverity').value);
    formData.append('Descr', document.getElementById('editDescription').value);
    formData.append('LocationAddress', document.getElementById('editLocationAddress').value);

    if (document.getElementById('editType').value === 'other') {
        formData.append('OtherHazard', document.getElementById('editOtherHazard').value);
    }

    try {
        const response = await fetch(`${API_BASE_URL}/Incidents/${id}`, {
            method: 'PUT',
            body: formData
        });

        if (response.ok) {
            alert("Report updated successfully!");
            closeEditModal();
            const activeBtn = document.querySelector('.filter-btn.active');
            handleFilterClick(activeBtn, 'Pending');
        } else {
            alert("Failed to update report.");
        }
    } catch (err) {
        console.error(err);
        alert("Error updating report.");
    }
});

async function deleteIncident(id) {
    if (!confirm("Are you sure you want to delete this report?")) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Incidents/${id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            alert("Report deleted.");
            const activeBtn = document.querySelector('.filter-btn.active');
            handleFilterClick(activeBtn, 'Pending');
        } else {
            alert("Failed to delete report.");
        }
    } catch (err) {
        console.error(err);
        alert("Error deleting report.");
    }
}

function setupFilters() {
    document.querySelectorAll('.filter-btn').forEach(btn => {
        btn.addEventListener('click', e => {
            const status = e.target.dataset.filter || e.target.textContent.trim();
            handleFilterClick(e.target, status);
        });
    });
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
        // Toggle dropdown
        filterBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            filterMenu.classList.toggle('show');
        });

        // Close dropdown when clicking outside
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

        performFiltering(searchTerm);
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

    // Expose reset for handleFilterClick
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
    }
}


function performFiltering(term) {
    const activeStatus = document.querySelector('.filter-btn.active')?.dataset.filter || 'Pending';

    filteredIncidents = allIncidents.filter(i => {

        const matchesSearch = !term ||
            (i.title || '').toLowerCase().includes(term) ||
            (i.descr || '').toLowerCase().includes(term) ||
            (i.incident_Code || '').toLowerCase().includes(term) ||
            (i.severity || '').toLowerCase().includes(term) ||
            (i.locationAddress || '').toLowerCase().includes(term) ||
            (i.otherHazard || '').toLowerCase().includes(term);


        const incidentType = (i.incident_Code || i.IncidentType || '').toLowerCase();
        let filterTypeVal = filterType ? filterType.toLowerCase() : '';


        if (filterTypeVal === 'accident' || filterTypeVal === 'road') {
            const isAccident = incidentType === 'accident' || incidentType === 'road';
            if (!isAccident && filterTypeVal) return false;
        } else {
            const matchesType = !filterTypeVal || incidentType.includes(filterTypeVal);
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
            }
        }

        return matchesSearch && matchesType && matchesSeverity && matchesDate;
    });

    renderIncidentList(activeStatus);

    if (filteredIncidents.length > 0) {
        selectIncident(filteredIncidents[0], activeStatus);
    } else {
        clearDetailPanel();
    }
}


function setupSearch() {

}

document.addEventListener('DOMContentLoaded', () => {
    loadInitialIncidents();
    setupFilters();
    setupAdvancedFilters();
});