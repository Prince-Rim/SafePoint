const API_BASE_URL = 'https://localhost:44373/api';

const adminId = localStorage.getItem('adminId');
const adminRole = localStorage.getItem('adminRole');

function checkAuthorization() {
    if (!adminId || (adminRole !== 'Admin' && adminRole !== 'Moderator')) {
        alert("Access denied. You must be an Admin or Moderator.");
        window.location.href = "index.html";
        return false;
    }
    return true;
}

async function loadDashboardStats() {
    try {
        const headers = {
            'X-Requester-Id': adminId,
            'X-Requester-Role': adminRole
        };

        const pendingResponse = await fetch(`${API_BASE_URL}/Admin/pending`, { headers });
        const pendingIncidents = await pendingResponse.json();
        const pendingCount = Array.isArray(pendingIncidents) ? pendingIncidents.length : 0;

        const verifiedResponse = await fetch(`${API_BASE_URL}/Incidents`);
        const verifiedIncidents = await verifiedResponse.json();
        const verifiedCount = Array.isArray(verifiedIncidents) ? verifiedIncidents.length : 0;

        const usersResponse = await fetch(`${API_BASE_URL}/Admin/users`, { headers });
        const users = await usersResponse.json();
        const totalUsers = Array.isArray(users) ? users.length : 0;

        const recentActivityResponse = await fetch(`${API_BASE_URL}/Admin/recent-activity`, { headers });
        const recentActivity = await recentActivityResponse.json();

        updateStatCard('pending', pendingCount);
        updateStatCard('verified', verifiedCount);
        updateStatCard('users', totalUsers);

        if (Array.isArray(verifiedIncidents)) {
            loadSeverityChart(verifiedIncidents);
            loadTypeChart(verifiedIncidents);
        }

        loadRecentActivity(Array.isArray(recentActivity) ? recentActivity : []);

    } catch (error) {
        console.error('Error loading dashboard stats:', error);
    }
}

function updateStatCard(type, count) {
    let elementId;
    switch (type) {
        case 'pending':
            elementId = 'pending-count';
            break;
        case 'verified':
            elementId = 'verified-count';
            break;
        case 'users':
            elementId = 'users-count';
            break;
        default:
            console.error('Unknown card type:', type);
            return;
    }

    const element = document.getElementById(elementId);
    if (element) {
        element.textContent = count;
    } else {
        console.error(`Element with ID ${elementId} not found`);
    }
}

function loadSeverityChart(incidents) {
    const severityCounts = { Red: 0, Yellow: 0, Green: 0 };
    const totalVerified = incidents.length;
    const ctx = document.getElementById('severityChart');

    if (totalVerified === 0 || !ctx) {
        const chartContainer = document.querySelector('.combined-chart-card .chart-box:first-child');
        if (chartContainer) {
            chartContainer.innerHTML = '<p style="text-align: center; color: #777;">No verified incidents to display severity.</p>';
        }
        return;
    }

    incidents.forEach(incident => {
        const severity = (incident.severity || '').toLowerCase();
        if (severity === 'high') severityCounts.Red++;
        else if (severity === 'moderate' || severity === 'medium') severityCounts.Yellow++;
        else if (severity === 'low') severityCounts.Green++;
    });

    const severityData = [
        { label: 'High', count: severityCounts.Red, color: '#dc3545' },
        { label: 'Moderate', count: severityCounts.Yellow, color: '#ffc107' },
        { label: 'Low', count: severityCounts.Green, color: '#198754' }
    ];

    const filteredAndSortedData = severityData
        .filter(d => d.count > 0)
        .sort((a, b) => b.count - a.count);

    const labels = filteredAndSortedData.map(d => `${d.label} (${((d.count / totalVerified) * 100).toFixed(1)}%)`);
    const data = filteredAndSortedData.map(d => d.count);
    const colors = filteredAndSortedData.map(d => d.color);

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: colors,
                hoverOffset: 4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
                padding: {
                    top: 10,
                    bottom: 15
                }
            },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 8,
                        boxWidth: 10,
                        font: {
                            family: 'Poppins',
                            size: 10
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            let label = context.label || '';
                            if (label) {
                                label = label.split('(')[0].trim();
                            }
                            return `${label}: ${context.raw} reports`;
                        }
                    },
                    bodyFont: { family: 'Poppins', size: 11 },
                    titleFont: { family: 'Poppins', size: 11 }
                }
            }
        }
    });
}

function loadTypeChart(incidents) {
    const typeCounts = {};
    const totalVerified = incidents.length;
    const ctx = document.getElementById('typeChart');
    const colorPalette = ['#397DFA', '#20c997', '#fd7e14', '#6f42c1', '#28a745', '#e83e8c', '#17a2b8', '#ffc107'];

    if (totalVerified === 0 || !ctx) {
        const chartContainer = document.querySelector('.combined-chart-card .chart-box:last-child');
        if (chartContainer) {
            chartContainer.innerHTML = '<p style="text-align: center; color: #777;">No verified incidents to display types.</p>';
        }
        return;
    }

    incidents.forEach(incident => {
        let type = incident.incident_Code || incident.IncidentType || 'Unknown';

        if (type.toLowerCase() === 'road') type = 'Vehicle Accident';
        else if (type.toLowerCase().includes('earthquake')) type = 'Natural Hazard';
        else if (type.toLowerCase() === 'flood') type = 'Flood';
        else if (type.toLowerCase() === 'fire') type = 'Fire';
        else if (type.toLowerCase() === 'other' && incident.otherHazard) type = incident.otherHazard;
        else if (type.toLowerCase() === 'robbery') type = 'Theft/Robbery';
        else if (type.toLowerCase() === 'landslide') type = 'Landslide';
        else if (type.toLowerCase() === 'environmental/nature') type = 'Natural Hazard';

        type = type.charAt(0).toUpperCase() + type.slice(1);

        typeCounts[type] = (typeCounts[type] || 0) + 1;
    });

    const typeData = Object.keys(typeCounts).map(key => ({
        label: key,
        count: typeCounts[key]
    }));

    const filteredAndSortedData = typeData
        .filter(d => d.count > 0)
        .sort((a, b) => b.count - a.count);

    const labels = filteredAndSortedData.map(d => `${d.label} (${((d.count / totalVerified) * 100).toFixed(1)}%)`);
    const data = filteredAndSortedData.map(d => d.count);
    const colors = filteredAndSortedData.map((d, index) => colorPalette[index % colorPalette.length]);

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: colors,
                hoverOffset: 4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
                padding: {
                    top: 10,
                    bottom: 15
                }
            },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 8,
                        boxWidth: 10,
                        font: {
                            family: 'Poppins',
                            size: 10
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            let label = context.label || '';
                            if (label) {
                                label = label.split('(')[0].trim();
                            }
                            return `${label}: ${context.raw} reports`;
                        }
                    },
                    bodyFont: { family: 'Poppins', size: 11 },
                    titleFont: { family: 'Poppins', size: 11 }
                }
            }
        }
    });
}

async function loadRecentActivity(activities) {
    const activityList = document.querySelector('.activity-list');
    if (!activityList) return;

    activityList.innerHTML = '';

    if (activities.length === 0) {
        activityList.innerHTML = '<div class="activity-item"><p>No recent activity</p></div>';
        return;
    }

    activities.forEach(activity => {
        const activityItem = document.createElement('div');
        activityItem.className = 'activity-item';

        const date = new Date(activity.validationDate);
        const formattedDate = date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
        const statusClass = activity.status === 'Approved' ? 'text-success' : 'text-danger';
        const validatorInfo = activity.validatorName && activity.validatorName !== 'Unknown' ? ` by <strong>${activity.validatorName}</strong>` : '';

        activityItem.innerHTML = `
            <p>Incident <strong>${activity.title || 'Untitled'}</strong> was <span class="${statusClass}">${activity.status}</span>${validatorInfo}</p>
            <span>${formattedDate}</span>
        `;
        activityList.appendChild(activityItem);
    });
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
    loadDashboardStats();
    setupLogout();
});
