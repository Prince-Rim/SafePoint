const userId = localStorage.getItem('userId') || sessionStorage.getItem('userId');
const userRole = localStorage.getItem('userRole') || sessionStorage.getItem('userRole');

const map = L.map('map').setView([0, 0], 2);

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '© OpenStreetMap'
}).addTo(map);

let userMarker = null;
let incidentMarker = null;
let currentIncidentId = null;
let allIncidents = [];
let locationWatchId = null;
let accuracyCircle = null;
let lastGeocodedPos = { lat: 0, lng: 0 };
let isHeatmapActive = false;
let heatLayer = null;

const icons = {
    high: new L.Icon({
        iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-red.png',
        shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
        iconSize: [25, 41],
        iconAnchor: [12, 41],
        popupAnchor: [1, -34],
        shadowSize: [41, 41]
    }),
    moderate: new L.Icon({
        iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-yellow.png',
        shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
        iconSize: [25, 41],
        iconAnchor: [12, 41],
        popupAnchor: [1, -34],
        shadowSize: [41, 41]
    }),
    low: new L.Icon({
        iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-green.png',
        shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
        iconSize: [25, 41],
        iconAnchor: [12, 41],
        popupAnchor: [1, -34],
        shadowSize: [41, 41]
    }),
    default: new L.Icon({
        iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-blue.png',
        shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
        iconSize: [25, 41],
        iconAnchor: [12, 41],
        popupAnchor: [1, -34],
        shadowSize: [41, 41]
    })
};

let incidentLayerGroup = L.layerGroup().addTo(map);

function getAddress(lat, lng, callback) {
    fetch(`https://nominatim.openstreetmap.org/reverse?lat=${lat}&lon=${lng}&format=json`)
        .then(res => res.json())
        .then(data => callback(data.display_name || "Address not found"))
        .catch(() => callback("Error fetching address"));
}

function locateUser() {
    if (!navigator.geolocation) {
        alert("Geolocation not supported by this browser.");
        return;
    }

    if (locationWatchId) {
        navigator.geolocation.clearWatch(locationWatchId);
    }

    // UX: Show searching state
    const locateBtn = document.getElementById("locateBtn");
    const originalBtnText = locateBtn ? locateBtn.textContent : "Locate Me";
    if (locateBtn) {
        locateBtn.textContent = "Locating...";
        locateBtn.disabled = true;
    }

    function startWatch(highAccuracy) {
        if (locationWatchId) navigator.geolocation.clearWatch(locationWatchId);

        console.log(`Starting location watch. High Accuracy: ${highAccuracy}`);

        locationWatchId = navigator.geolocation.watchPosition(
            pos => {
                const { latitude, longitude, accuracy } = pos.coords;
                const latLng = [latitude, longitude];

                // UI Reset
                if (locateBtn) {
                    locateBtn.textContent = originalBtnText;
                    locateBtn.disabled = false;
                }

                // Determine zoom level and color based on accuracy
                const zoomLevel = accuracy > 100 ? 15 : 17;
                let circleColor = '#136AEC';
                if (accuracy > 100) circleColor = '#ff9800'; // Orange warning

                if (!userMarker) {
                    map.setView(latLng, zoomLevel);
                    userMarker = L.marker(latLng, { title: "Your Location" }).addTo(map);
                } else {
                    userMarker.setLatLng(latLng);
                }

                if (!accuracyCircle) {
                    accuracyCircle = L.circle(latLng, {
                        radius: accuracy,
                        color: circleColor,
                        fillColor: circleColor,
                        fillOpacity: 0.15
                    }).addTo(map);
                } else {
                    accuracyCircle.setLatLng(latLng);
                    accuracyCircle.setRadius(accuracy);
                    accuracyCircle.setStyle({ color: circleColor, fillColor: circleColor });
                }

                // Only reverse geocode if moved significantly (> ~10m)
                if (Math.abs(latitude - lastGeocodedPos.lat) > 0.0001 || Math.abs(longitude - lastGeocodedPos.lng) > 0.0001) {
                    lastGeocodedPos = { lat: latitude, lng: longitude };

                    getAddress(latitude, longitude, (address) => {
                        let accuracyWarning = "";
                        if (accuracy > 100) {
                            accuracyWarning = `<br><b style="color: #e65100;">⚠️ Low Accuracy (±${Math.round(accuracy)}m)</b>`;
                        }

                        const popupContent = `
                            <b>Your Location</b><br>
                            📍 <b>Coords:</b> ${latitude.toFixed(5)}, ${longitude.toFixed(5)}<br>
                            🎯 <b>Accuracy:</b> ±${Math.round(accuracy)}m${accuracyWarning}<br>
                            🏠 <b>Address:</b> ${address}
                        `;

                        userMarker.bindPopup(popupContent);
                        if (userMarker.isPopupOpen()) userMarker.setPopupContent(popupContent);

                        // If extremely bad accuracy (>500m), auto-open popup to warn user
                        if (accuracy > 500 && !userMarker.isPopupOpen()) {
                            userMarker.openPopup();
                        }

                        localStorage.setItem("userLocation", JSON.stringify({ lat: latitude, lng: longitude, address: address }));
                    });
                }
            },
            err => {
                console.warn(`Location error (High Accuracy: ${highAccuracy}): ${err.message}`);

                // If High Accuracy fails and it's not a permission issue (code 1), try Low Accuracy
                if (highAccuracy && err.code !== 1) {
                    console.log("High accuracy failed. Falling back to low accuracy...");
                    startWatch(false);
                } else {
                    // Final failure
                    if (locateBtn) {
                        locateBtn.textContent = originalBtnText;
                        locateBtn.disabled = false;
                    }
                    if (err.code === 1) {
                        alert("Location permission denied. Please enable location services.");
                    } else {
                        console.warn("Could not determine location. Using default view.");
                        // Optional: Show a toast or small message instead of an alert
                        // alert("Could not determine location. Please check your GPS settings."); 
                    }
                }
            },
            {
                enableHighAccuracy: highAccuracy,
                timeout: highAccuracy ? 20000 : 60000, // Increased timeout (20s High, 60s Low)
                maximumAge: highAccuracy ? 0 : 300000
            }
        );
    }

    // Start with High Accuracy
    startWatch(true);
}

function resetMap() {
    map.setView([0, 0], 2);
    if (incidentMarker) {
        map.removeLayer(incidentMarker);
        incidentMarker = null;
    }
    // Also clear the stored location so it doesn't persist to the report page
    localStorage.removeItem("incidentLocation");
    sessionStorage.removeItem("incidentLocation");
}

map.on("click", (e) => {
    const { lat, lng } = e.latlng;

    getAddress(lat, lng, (address) => {
        const popupContent = `
            <b>Incident Location</b><br>
            📍 <b>Coords:</b> ${lat.toFixed(5)}, ${lng.toFixed(5)}<br>
            🏠 <b>Address:</b> ${address}
        `;

        if (incidentMarker) map.removeLayer(incidentMarker);

        // Create draggable marker
        incidentMarker = L.marker([lat, lng], { draggable: true })
            .addTo(map)
            .bindPopup(popupContent)
            .openPopup();

        localStorage.setItem("incidentLocation", JSON.stringify({ lat, lng, address }));

        // Handle Drag Event
        incidentMarker.on('dragend', function (event) {
            const position = event.target.getLatLng();
            const newLat = position.lat;
            const newLng = position.lng;

            getAddress(newLat, newLng, (newAddress) => {
                const newPopupContent = `
                    <b>Incident Location</b><br>
                    📍 <b>Coords:</b> ${newLat.toFixed(5)}, ${newLng.toFixed(5)}<br>
                    🏠 <b>Address:</b> ${newAddress}
                `;
                incidentMarker.setPopupContent(newPopupContent).openPopup();
                localStorage.setItem("incidentLocation", JSON.stringify({ lat: newLat, lng: newLng, address: newAddress }));
            });
        });
    });
});

async function loadValidatedIncidents(type = '', severity = '') {
    try {
        let url = `${API_BASE_URL}/Incidents`;
        const params = new URLSearchParams();
        if (type) params.append('type', type);
        if (severity) params.append('severity', severity);

        if (params.toString()) {
            url += `?${params.toString()}`;
        }

        const response = await fetch(url);
        if (!response.ok) {
            console.error('Failed to fetch incidents');
            return;
        }

        const text = await response.text();
        if (!text) return;

        let data = JSON.parse(text);

        // Apply Status Filter
        const statusEl = document.getElementById('status');
        const status = statusEl ? statusEl.value : 'active'; // Default to active

        if (status === 'active') {
            data = data.filter(i => !i.isResolved);
        } else if (status === 'resolved') {
            data = data.filter(i => i.isResolved);
        }
        // status === 'all' -> no filter

        allIncidents = data;
        renderIncidents(allIncidents);

    } catch (error) {
        console.error('Error loading incidents:', error);
    }
}

function renderIncidents(incidentsToRender) {
    incidentLayerGroup.clearLayers();
    if (heatLayer) {
        map.removeLayer(heatLayer);
        heatLayer = null;
    }

    // Heatmap Mode
    if (isHeatmapActive) {
        const heatPoints = incidentsToRender
            .filter(i => i.latitude && i.longitude)
            .map(i => {
                // intensity numbers: 0 to 1
                // We lower them further so it takes MANY incidents to trigger Red.
                // Low = 0.1 (Need 10 for pure red)
                // Moderate = 0.2 (Need 5 for pure red)
                // High = 0.3 (Need 3-4 for pure red)
                let intensity = 0.1;
                if (i.severity && i.severity.toLowerCase() === 'moderate') intensity = 0.2;
                if (i.severity && i.severity.toLowerCase() === 'high') intensity = 0.3;
                return [i.latitude, i.longitude, intensity];
            });

        if (heatPoints.length > 0) {
            heatLayer = L.heatLayer(heatPoints, {
                radius: 40,
                blur: 25,
                maxZoom: 17,
                gradient: { 0.4: 'green', 0.65: 'yellow', 1: 'red' }
            }).addTo(map);
        }
    }

    const recentReportsContainer = document.querySelector('.recent-reports');
    if (recentReportsContainer) {
        recentReportsContainer.innerHTML = '';
    }

    const iconMapping = {
        'fire': 'local_fire_department',
        'flood': 'water_drop',
        'road': 'car_crash',
        'accident': 'emergency',
        'earthquake': 'public',
        'other': 'warning'
    };

    const typeMapping = {
        'fire': 'Fire',
        'flood': 'Flood',
        'road': 'Accident',
        'accident': 'Accident',
        'earthquake': 'Environmental/Nature',
        'other': 'Others'
    };

    incidentsToRender.forEach((incident, index) => {
        if (incident.latitude && incident.longitude) {

            // Marker Logic: Only add markers if Heatmap is NOT active
            if (!isHeatmapActive) {
                let displayType = typeMapping[incident.incident_Code] || incident.incident_Code || 'N/A';

                if (incident.incident_Code === 'other' && incident.otherHazard) {
                    displayType = incident.otherHazard;
                }

                let severityRaw = incident.severity || 'N/A';
                if (severityRaw.toLowerCase() === 'moderate' || severityRaw.toLowerCase() === 'medium') severityRaw = 'Moderate';
                const severityDisplay = severityRaw.charAt(0).toUpperCase() + severityRaw.slice(1).toLowerCase();

                const popupContent = `
                <div style="text-align:center; min-width:150px;">
                    <b style="font-size:1.1em; color:#333;">${filterProfanity(incident.title) || 'Incident'}</b><br>
                    
                    <span style="font-size:0.9em; color:#666;">
                        <b>Type:</b> ${displayType}
                    </span><br>
                    
                    <span style="font-size:0.9em; color:#555;">
                        <b>Severity:</b> ${severityDisplay}
                    </span><br>
                    
                    <button onclick='openViewModal(${JSON.stringify(incident).replace(/'/g, "&#39;")})' 
                    style="margin-top:10px; padding:6px 12px; background:#397DFA; color:white; border:none; border-radius:4px; cursor:pointer; font-weight:600;">
                    View Details</button>
                </div>
            `;

                let severityKey = incident.severity ? incident.severity.toLowerCase() : 'default';
                if (severityKey === 'medium') severityKey = 'moderate';

                const iconName = iconMapping[incident.incident_Code] || 'place';
                let markerClass = `custom-map-marker map-severity-${severityKey}`;

                let markerHtml = `<span class="material-icons">${iconName}</span>`;

                if (incident.isResolved) {
                    markerClass += ' marker-resolved';
                }

                const customIcon = L.divIcon({
                    className: markerClass,
                    html: markerHtml,
                    iconSize: [32, 32],
                    iconAnchor: [16, 16],
                    popupAnchor: [0, -18]
                });

                L.marker([incident.latitude, incident.longitude], { icon: customIcon })
                    .bindPopup(popupContent)
                    .addTo(incidentLayerGroup);
            }

            // List Logic: Always populate list regardless of Heatmap status
            if (index < 5 && recentReportsContainer) {
                const reportBox = document.createElement('div');
                reportBox.className = 'report-box';

                if (incident.isResolved) {
                    reportBox.classList.add('resolved-report');
                }

                let severityClassRaw = incident.severity ? incident.severity.toLowerCase() : 'low';
                if (severityClassRaw === 'moderate') severityClassRaw = 'moderate';
                const severityClass = `severity-${severityClassRaw}`;

                const iconName = iconMapping[incident.incident_Code] || 'place';

                const dateObj = new Date(incident.incidentDateTime);
                const dateStr = dateObj.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
                const timeStr = dateObj.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });

                let displayType = typeMapping[incident.incident_Code] || incident.incident_Code || 'N/A';
                if (incident.incident_Code === 'other' && incident.otherHazard) {
                    displayType = incident.otherHazard;
                }

                reportBox.innerHTML = `
                    <div class="report-icon-box ${severityClass}">
                        <span class="material-icons">${iconName}</span>
                    </div>
                    <div class="report-content">
                        <strong>${filterProfanity(incident.title) || 'Untitled Incident'} ${incident.isResolved ? '<span style="color:green; font-size:0.8em; font-weight:bold;">(RESOLVED)</span>' : ''}</strong>
                        <div class="report-meta">
                            <span>${displayType}</span>
                            <span>•</span>
                            <span>${dateStr}, ${timeStr}</span>
                        </div>
                        <div class="report-address">
                            <span class="material-icons" style="font-size:12px; vertical-align: middle;">location_on</span>
                            ${incident.locationAddress || 'Location unavailable'}
                        </div>
                    </div>
                `;

                reportBox.addEventListener('click', () => {
                    map.setView([incident.latitude, incident.longitude], 17);
                    openViewModal(incident);
                });

                recentReportsContainer.appendChild(reportBox);
            }
        }
    }); // End loop
}

function openViewModal(incident) {
    const modal = document.getElementById('viewIncidentModal');
    currentIncidentId = incident.incidentID;

    document.getElementById('modalTitle').textContent = filterProfanity(incident.title) || 'Untitled Incident';

    const typeMapping = {
        'fire': 'Fire',
        'flood': 'Flood',
        'road': 'Accident',
        'accident': 'Accident',
        'earthquake': 'Environmental/Nature',
        'other': 'Others'
    };
    let displayType = typeMapping[incident.incident_Code] || incident.incident_Code || 'N/A';
    if (incident.incident_Code === 'other' && incident.otherHazard) {
        displayType = incident.otherHazard;
    }
    document.getElementById('modalType').textContent = displayType;

    const reporterName = incident.user ? (incident.user.username || incident.user.userName) : 'Anonymous';
    let reporterHtml = reporterName;

    // Render Badges if present
    if (incident.user && incident.user.badges && incident.user.badges.length > 0) {
        const badgeIconMap = {
            'Certified Reporter': 'shield',
            'Community Guardian': 'star',
            'Top Contributor': 'award',
            'Reliable Source': 'check-circle',
            'Sociable': 'forum'
        };

        reporterHtml += ' <span class="reporter-badges" style="margin-left:5px;">';
        incident.user.badges.forEach(b => {
            const icon = badgeIconMap[b.badgeName] || 'certificate';
            // Simple tooltip with title attribute
            let matIcon = 'verified';
            if (icon === 'shield') matIcon = 'security';
            if (icon === 'star') matIcon = 'star';
            if (icon === 'award') matIcon = 'emoji_events';
            if (icon === 'forum') matIcon = 'forum';

            reporterHtml += `<span class="material-icons" title="${b.badgeName}" style="font-size:16px; color:#FFD700; vertical-align:middle; margin-left:2px;">${matIcon}</span>`;
            // Note: Mapping FontAwesome names to Material Icons where possible for consistency in this modal, 
            // or just use FA if included. The map page uses Material Icons primarily.
            // Certified Reporter (Shield) -> security
            // Community Guardian (Star) -> star
            // Top Contributor (Award) -> emoji_events
            // Reliable Source (Check) -> verified
        });
        reporterHtml += '</span>';
    }

    document.getElementById('modalReporter').innerHTML = reporterHtml;

    let severity = incident.severity || 'N/A';
    if (severity.toLowerCase() === 'moderate' || severity.toLowerCase() === 'medium') severity = 'Moderate';
    else severity = severity.charAt(0).toUpperCase() + severity.slice(1).toLowerCase();

    // SEVERITY BADGE
    const badge = document.getElementById('modalSeverityBadge');
    badge.textContent = severity;
    badge.className = 'severity-badge badge-' + (severity.toLowerCase() || 'default');
    badge.style.display = 'inline-block';
    badge.style.backgroundColor = '';

    // RESOLVED BADGE LOGIC
    // Remove existing resolved badge if any
    const existingResolved = document.getElementById('modalResolvedBadge');
    if (existingResolved) existingResolved.remove();

    if (incident.isResolved) {
        const resolvedBadge = document.createElement('span');
        resolvedBadge.id = 'modalResolvedBadge';
        resolvedBadge.textContent = "RESOLVED";
        resolvedBadge.className = 'severity-badge';
        resolvedBadge.style.backgroundColor = '#4caf50';
        resolvedBadge.style.color = 'white';
        resolvedBadge.style.marginLeft = '10px';

        badge.parentNode.appendChild(resolvedBadge);
    }

    const dateObj = new Date(incident.incidentDateTime);
    document.getElementById('modalDate').textContent = dateObj.toLocaleString();
    document.getElementById('modalLocation').textContent = incident.locationAddress || 'Lat: ' + incident.latitude + ', Lng: ' + incident.longitude;
    document.getElementById('modalDescription').textContent = filterProfanity(incident.descr) || 'No description available.';

    // Inject Resolve Button if Admin/Mod and not resolved
    const existingResolveBtn = document.getElementById('resolve-btn-container');
    if (existingResolveBtn) existingResolveBtn.remove();

    const userRole = localStorage.getItem('userRole') || sessionStorage.getItem('userRole');
    if (!incident.isResolved && (userRole === 'Admin' || userRole === 'Moderator')) {
        const headerSection = document.querySelector('.modal-header-section');
        const resolveBtnContainer = document.createElement('div');
        resolveBtnContainer.id = 'resolve-btn-container';
        resolveBtnContainer.style.marginTop = '10px';
        resolveBtnContainer.innerHTML = `
            <button onclick="resolveIncident(${incident.incidentID})" 
                style="background-color: #4caf50; color: white; border: none; padding: 8px 16px; border-radius: 4px; cursor: pointer; font-weight: bold;">
                <span class="material-icons" style="vertical-align: middle; font-size: 18px; margin-right: 5px;">check_circle</span>
                Mark as Resolved
            </button>
        `;
        headerSection.appendChild(resolveBtnContainer);
    }

    const modalImage = document.getElementById('modalImage');
    const noImageText = document.getElementById('modalNoImage');

    if (incident.img) {
        const srcPrefix = incident.img.startsWith('data:image') ? '' : 'data:image/jpeg;base64,';
        modalImage.src = `${srcPrefix}${incident.img}`;
        modalImage.style.display = 'block';
        noImageText.style.display = 'none';
    } else {
        modalImage.style.display = 'none';
        noImageText.style.display = 'flex';
    }

    loadComments(incident.incidentID);

    modal.style.display = 'flex';
}

window.resolveIncident = async function (incidentId) {
    if (!confirm("Are you sure you want to mark this incident as RESOLVED? This will update the status for everyone.")) return;

    try {
        const response = await fetch(`/api/incidents/${incidentId}/resolve`, {
            method: 'PUT'
        });

        if (response.ok) {
            alert("Incident marked as resolved!");
            document.getElementById('viewIncidentModal').style.display = 'none';
            // Reload incidents to reflect change
            // We need to trigger a map refresh. Assuming getIncidents is global or we can reload page.
            if (window.getIncidents) window.getIncidents();
            else location.reload();
        } else {
            alert("Failed to resolve incident.");
        }
    } catch (e) {
        console.error(e);
        alert("Error resolving incident.");
    }
};

async function loadComments(incidentId) {
    const commentsList = document.getElementById('commentsList');
    const countSpan = document.getElementById('commentCount');

    commentsList.innerHTML = '<p style="text-align:center; color:#888;">Loading comments...</p>';

    try {
        const response = await fetch(`${API_BASE_URL}/Comments/incident/${incidentId}`);
        if (response.ok) {
            const comments = await response.json();
            commentsList.innerHTML = '';
            countSpan.textContent = comments.length;

            if (comments.length === 0) {
                commentsList.innerHTML = '<p class="no-comments">No comments yet. Be the first!</p>';
            } else {
                comments.forEach(c => {
                    const commentItem = document.createElement('div');
                    commentItem.className = 'comment-item';

                    if (c.user && (c.user.userRole === 'Admin' || c.user.userRole === 'Moderator')) {
                        commentItem.classList.add('comment-official');
                    }

                    let dateVal = c.dttm || c.createdAt;
                    if (dateVal && typeof dateVal === 'string' && !dateVal.endsWith('Z') && !dateVal.includes('+')) {
                        dateVal += 'Z';
                    }
                    const date = dateVal ? new Date(dateVal).toLocaleString() : 'Unknown Date';

                    const userName = c.user ? (c.user.username || c.user.userName) : (c.userName || 'Anonymous');

                    // BADGE RENDERING FOR COMMENTS
                    let badgesHtml = '';
                    if (c.user && c.user.badges && Array.isArray(c.user.badges) && c.user.badges.length > 0) {
                        const badgeIconMap = {
                            'Certified Reporter': 'shield',
                            'Community Guardian': 'star',
                            'Top Contributor': 'award',
                            'Reliable Source': 'check-circle',
                            'Sociable': 'forum'
                        };
                        badgesHtml += ' <span class="comment-badges" style="margin-left:5px;">';
                        c.user.badges.forEach(b => {
                            const bName = b.badgeName || b; // Handle object or string
                            const icon = badgeIconMap[bName] || 'certificate';
                            // Use FontAwesome or Material Icons. Material Icons preferred for index.js consistency, but user-accounts uses FA.
                            // Let's use Material Icons mapping:
                            // Shield -> security
                            // Star -> star
                            // Award -> emoji_events
                            // Check -> verified
                            let matIcon = 'verified';
                            if (icon === 'shield') matIcon = 'security';
                            if (icon === 'star') matIcon = 'star';
                            if (icon === 'award') matIcon = 'emoji_events';
                            if (icon === 'forum') matIcon = 'forum';

                            badgesHtml += `<span class="material-icons" title="${bName}" style="font-size:14px; color:#FFD700; vertical-align:middle; margin-left:2px;">${matIcon}</span>`;
                        });
                        badgesHtml += '</span>';
                    }

                    let buttonsHtml = '';
                    if (userId && c.userid && userId.toLowerCase() === c.userid.toLowerCase()) {
                        buttonsHtml = `
                            <div class="comment-actions" style="margin-top: 5px;">
                                <button onclick="editComment(${c.comment_ID}, '${(c.comment || '').replace(/'/g, "\\'")}')" style="font-size: 0.8em; margin-right: 5px; cursor: pointer; color: blue; background: none; border: none; padding: 0;">Edit</button>
                                <button onclick="deleteComment(${c.comment_ID})" style="font-size: 0.8em; cursor: pointer; color: red; background: none; border: none; padding: 0;">Delete</button>
                            </div>
                        `;
                    }

                    commentItem.innerHTML = `
                    <div class="comment-user">
                        ${userName}
                        ${badgesHtml}
                    </div>
                    <div class="comment-text">${filterProfanity(c.comment || c.content)}</div>
                    <span class="comment-date">${date}</span>
                    ${buttonsHtml}
                `;
                    commentsList.appendChild(commentItem);
                });
            }
        } else {
            commentsList.innerHTML = '<p class="no-comments">No comments yet.</p>';
            countSpan.textContent = '0';
        }
    } catch (error) {
        console.error("Error loading comments:", error);
        commentsList.innerHTML = '<p class="no-comments">Failed to load comments.</p>';
    }
}

async function postComment() {
    const commentInput = document.getElementById('newComment');
    const content = commentInput.value.trim();

    if (!content) {
        return;
    }

    if (!userId) {
        alert("You must be logged in to comment.");
        return;
    }

    const payload = {
        IncidentId: currentIncidentId,
        CommentText: content
    };

    try {
        const response = await fetch(`${API_BASE_URL}/Comments`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requester-Id': userId
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            commentInput.value = '';
            loadComments(currentIncidentId);
        } else {
            alert("Failed to post comment.");
        }
    } catch (error) {
        console.error("Error posting comment:", error);
        alert("Error posting comment.");
    }
}

window.editComment = async function (commentId, oldContent) {
    const newContent = prompt("Edit your comment:", oldContent);
    if (newContent === null || newContent.trim() === "") return;
    if (newContent === oldContent) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Comments/${commentId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'X-Requester-Id': userId
            },
            body: JSON.stringify({ IncidentId: currentIncidentId, CommentText: newContent })
        });

        if (response.ok || response.status === 204) {
            loadComments(currentIncidentId);
        } else {
            alert("Failed to edit comment.");
        }
    } catch (error) {
        console.error("Error editing comment:", error);
        alert("Error editing comment.");
    }
};

window.deleteComment = async function (commentId) {
    if (!confirm("Are you sure you want to delete this comment?")) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Comments/${commentId}`, {
            method: 'DELETE',
            headers: {
                'X-Requester-Id': userId
            }
        });

        if (response.ok || response.status === 204) {
            loadComments(currentIncidentId);
        } else {
            alert("Failed to delete comment.");
        }
    } catch (error) {
        console.error("Error deleting comment:", error);
        alert("Error deleting comment.");
    }
};

document.getElementById('submitCommentBtn').addEventListener('click', postComment);

const viewModal = document.getElementById('viewIncidentModal');
const closeViewModal = document.getElementById('closeViewModal');

if (closeViewModal) {
    closeViewModal.addEventListener('click', () => {
        viewModal.style.display = 'none';
    });
}

window.addEventListener('click', (event) => {
    if (event.target === viewModal) {
        viewModal.style.display = 'none';
    }
});

function handlePostIncident() {
    // If user hasn't selected a specific point (no Red Marker), clear the stored incident location
    // This ensures the Report page uses the current GPS location, not an old click
    if (!incidentMarker) {
        localStorage.removeItem("incidentLocation");
        sessionStorage.removeItem("incidentLocation");
    }

    if (userId) {
        window.location.href = 'report.html';
    } else {
        if (confirm("You must be logged in to post an incident. Do you want to log in now?")) {
            window.location.href = 'login.html';
        }
    }
}

function checkDashboardAccess() {
    const dashboardLink = document.getElementById('dashboardLink');
    const myReportsLink = document.getElementById('myReportsLink');
    const postBtn = document.querySelector('.post-btn');

    if (dashboardLink) dashboardLink.style.display = 'none';
    if (myReportsLink) myReportsLink.style.display = 'none';

    if (userId) {
        if (userRole === 'Admin' || userRole === 'Moderator') {
            if (dashboardLink) dashboardLink.style.display = 'block';
            if (postBtn) postBtn.style.display = 'none';
        } else {
            if (postBtn) postBtn.style.display = 'block';
            if (myReportsLink) myReportsLink.style.display = 'block';
        }
    } else {
        if (postBtn) postBtn.style.display = 'block';
    }
}

document.getElementById("locateBtn").addEventListener("click", locateUser);
document.getElementById("locateBtn").addEventListener("click", locateUser);
document.getElementById("resetBtn").addEventListener("click", resetMap);

const heatmapBtn = document.getElementById("heatmapBtn");
if (heatmapBtn) {
    heatmapBtn.addEventListener("click", () => {
        isHeatmapActive = !isHeatmapActive;
        if (isHeatmapActive) {
            heatmapBtn.textContent = "Show Pins";
            heatmapBtn.classList.add("active");
        } else {
            heatmapBtn.textContent = "Heatmap";
            heatmapBtn.classList.remove("active");
        }
        renderIncidents(allIncidents);
    });
}

const username = localStorage.getItem("username") || sessionStorage.getItem("username");
const userBtn = document.getElementById("userBtn");
if (username && userBtn) {
    userBtn.textContent = username + " ▾";
}

const categoryFilter = document.getElementById('category');
const severityFilter = document.getElementById('severity');
const statusFilter = document.getElementById('status');

function applyFilters() {
    const type = categoryFilter.value;
    let severity = severityFilter.value;
    if (severity === 'moderate') severity = 'Moderate';
    loadValidatedIncidents(type, severity);
}

if (categoryFilter) categoryFilter.addEventListener('change', applyFilters);
if (severityFilter) severityFilter.addEventListener('change', applyFilters);
if (statusFilter) statusFilter.addEventListener('change', applyFilters);

document.addEventListener('DOMContentLoaded', () => {
    checkDashboardAccess();
    loadValidatedIncidents();
    locateUser();
});

window.openViewModal = openViewModal;

const searchInput = document.getElementById('searchInput');
if (searchInput) {
    searchInput.addEventListener('input', (e) => {
        const searchTerm = e.target.value.toLowerCase();
        filterAndRenderIncidents(searchTerm);
    });
}

function filterAndRenderIncidents(searchTerm) {
    if (!allIncidents || allIncidents.length === 0) return;

    const filteredIncidents = allIncidents.filter(incident => {
        const title = incident.title ? incident.title.toLowerCase() : '';
        const description = incident.descr ? incident.descr.toLowerCase() : '';
        const type = incident.incident_Code ? incident.incident_Code.toLowerCase() : '';

        const typeMapping = {
            'fire': 'fire',
            'flood': 'flood',
            'road': 'accident',
            'accident': 'accident',
            'earthquake': 'environmental',
            'other': 'others'
        };
        const mappedType = typeMapping[type] || type;

        return title.includes(searchTerm) || description.includes(searchTerm) || mappedType.includes(searchTerm);
    });
    renderIncidents(filteredIncidents);
}