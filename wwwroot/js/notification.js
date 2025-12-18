"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/notificationHub").build();

// --- Notification State Management ---
// Base key, but we will append UserID dynamically
const BASE_STORAGE_KEY = 'safepoint_notifications';

function getStorageKey() {
    const userId = localStorage.getItem("userId") || sessionStorage.getItem("userId");
    return userId ? `${BASE_STORAGE_KEY}_${userId}` : `${BASE_STORAGE_KEY}_guest`;
}

function getNotifications() {
    const key = getStorageKey();
    const stored = localStorage.getItem(key);
    return stored ? JSON.parse(stored) : [];
}

function saveNotifications(notifications) {
    const key = getStorageKey();
    localStorage.setItem(key, JSON.stringify(notifications));
    updateNotificationUI();
}

function addNotification(title, location, lat, lng, incidentId, statusHeader) {
    const notifications = getNotifications();
    const newNotification = {
        id: Date.now(),
        incidentId: incidentId,
        title: statusHeader || "New Incident", // Use header (e.g. "Report Rejected") or default
        message: title, // Store actual title as message
        location: location,
        lat: lat,
        lng: lng,
        timestamp: new Date().toISOString(),
        read: false
    };
    notifications.unshift(newNotification);
    if (notifications.length > 20) notifications.pop(); // Keep last 20
    saveNotifications(notifications);
}

function markAllAsRead() {
    const notifications = getNotifications();
    notifications.forEach(n => n.read = true);
    saveNotifications(notifications);
}

function clearNotifications() {
    if (confirm("Are you sure you want to clear all notifications?")) {
        const key = getStorageKey();
        localStorage.removeItem(key);
        updateNotificationUI();
    }
}

function formatTime(isoString) {
    const date = new Date(isoString);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

// --- UI Updates ---
function updateNotificationUI() {
    const notifications = getNotifications();
    const unreadCount = notifications.filter(n => !n.read).length;

    // Update Badge
    const badge = document.getElementById('notificationBadge');
    if (badge) {
        badge.innerText = unreadCount;
        badge.style.display = unreadCount > 0 ? 'flex' : 'none';

        const btn = document.getElementById('notificationBtn');
        if (btn && unreadCount > 0) {
            btn.querySelector('.material-icons').style.color = '#ff4444';
        } else if (btn) {
            btn.querySelector('.material-icons').style.color = '';
        }
    }

    // Update List
    const list = document.getElementById('notificationList');
    const clearBtn = document.getElementById('clearNotifications');

    if (list) {
        list.innerHTML = '';
        if (notifications.length === 0) {
            list.innerHTML = '<div class="empty-state">No new notifications</div>';
            if (clearBtn) clearBtn.style.display = 'none';
        } else {
            if (clearBtn) clearBtn.style.display = 'block';
            notifications.forEach(n => {
                const item = document.createElement('div');
                item.className = `notification-item ${n.read ? '' : 'unread'}`;
                item.innerHTML = `
                    <span class="material-icons notification-icon">${n.title.includes('Rejected') ? 'block' : 'gpp_good'}</span>
                    <div class="notification-content">
                        <span class="notification-title">${n.title}</span>
                        <span class="notification-location">${n.message}</span>
                        <span class="notification-time">${formatTime(n.timestamp)}</span>
                    </div>
                `;
                item.onclick = () => handleNotificationClick(n);
                list.appendChild(item);
            });
        }
    }
}

function handleNotificationClick(notification) {
    // 1. Mark as read (optional logic, usually handled by opening menu but good to be safe)
    // 2. Zoom logic
    if (notification.lat && notification.lng) {
        if (typeof map !== 'undefined' && map.setView) {
            map.setView([notification.lat, notification.lng], 16, { animate: true });

            L.popup()
                .setLatLng([notification.lat, notification.lng])
                .setContent(`
                    <b>${notification.title}</b><br>
                    ${notification.location}
                `)
                .openOn(map);
        } else {
            // Check if we are not on index.html, redirect
            if (window.location.pathname.indexOf('index.html') === -1 && window.location.pathname !== '/') {
                window.location.href = `index.html?lat=${notification.lat}&lng=${notification.lng}`;
            }
        }
    } else {
        alert("Location coordinates not available for this incident.");
    }

    const dropdown = document.getElementById('notificationDropdown');
    if (dropdown) dropdown.classList.remove('show');
}

// --- Event Listeners ---
document.addEventListener('DOMContentLoaded', () => {
    updateNotificationUI();

    const btn = document.getElementById('notificationBtn');
    const dropdown = document.getElementById('notificationDropdown');
    const clearBtn = document.getElementById('clearNotifications');

    if (btn && dropdown) {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const isOpen = dropdown.classList.contains('show');
            if (!isOpen) {
                dropdown.classList.add('show');
                markAllAsRead();
                updateNotificationUI();
            } else {
                dropdown.classList.remove('show');
            }
        });

        window.addEventListener('click', (e) => {
            if (!dropdown.contains(e.target) && !btn.contains(e.target)) {
                dropdown.classList.remove('show');
            }
        });
    }

    if (clearBtn) {
        clearBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            clearNotifications();
        });
    }

    // Check for query params to auto-zoom (if redirected from another page)
    const urlParams = new URLSearchParams(window.location.search);
    const latParam = urlParams.get('lat');
    const lngParam = urlParams.get('lng');
    if (latParam && lngParam && typeof map !== 'undefined') {
        setTimeout(() => {
            map.setView([parseFloat(latParam), parseFloat(lngParam)], 16);
            L.popup()
                .setLatLng([parseFloat(latParam), parseFloat(lngParam)])
                .setContent("<b>Incident Location</b>")
                .openOn(map);
        }, 1000); // Small delay to let map load
    }
});

// --- SignalR Connection ---
connection.on("ReceiveIncidentNotification", function (title, location, lat, lng, incidentId, status, reporterId) {
    const currentUserId = localStorage.getItem("userId") || sessionStorage.getItem("userId");

    // Log for debugging
    // console.log(`Notification: ${status}, Reporter: ${reporterId}, Me: ${currentUserId}`);

    if (status === "Validated") {
        if (currentUserId && reporterId && currentUserId.trim() === reporterId.trim()) {
            // Specific message for the reporter
            addNotification(title, location, lat, lng, incidentId, "Report Approved");
            showToast("Report Approved", `Your report "${title}" is now public.`);
        } else {
            // General message for everyone else
            addNotification(title, location, lat, lng, incidentId, "New Incident");
            showToast("New Incident Reported", `${title}<br>${location}`);
        }
    }
    else if (status === "Rejected") {
        // ONLY show to the reporter
        if (currentUserId && reporterId && currentUserId.trim() === reporterId.trim()) {
            addNotification(title, location, lat, lng, incidentId, "Report Rejected");
            showToast("Report Rejected", `Your report "${title}" was not approved.`);
        }
    }
});

connection.start().then(function () {
    console.log("SignalR Connected!");
}).catch(function (err) {
    return console.error(err.toString());
});

function showToast(header, message) {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.style.position = 'fixed';
        container.style.top = '20px';
        container.style.right = '20px';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = 'toast-notification';
    toast.style.backgroundColor = '#fff';
    toast.style.color = '#333';
    toast.style.padding = '15px 20px';
    toast.style.marginTop = '10px';
    toast.style.borderRadius = '8px';
    toast.style.boxShadow = '0 4px 12px rgba(0,0,0,0.15)';
    toast.style.display = 'flex';
    toast.style.alignItems = 'center';
    toast.style.minWidth = '300px';

    // Color coding based on header
    const accentColor = header.includes("Rejected") ? '#666' : '#ff4444';
    const iconName = header.includes("Rejected") ? 'block' : 'warning';

    toast.style.borderLeft = `5px solid ${accentColor}`;
    toast.style.animation = 'slideIn 0.3s ease-out';

    toast.innerHTML = `
        <span class="material-icons" style="color: ${accentColor}; margin-right: 10px;">${iconName}</span>
        <div>
            <strong style="display: block; font-size: 14px;">${header}</strong>
            <span style="font-size: 12px; color: #666;">${message}</span>
        </div>
        <button onclick="this.parentElement.remove()" style="margin-left: auto; background: none; border: none; font-size: 16px; cursor: pointer; color: #999;">&times;</button>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'fadeOut 0.5s ease-out';
        toast.addEventListener('animationend', () => toast.remove());
    }, 5000);
}

// Add CSS for toast animations if not present
const style = document.createElement('style');
style.innerHTML = `
@keyframes slideIn {
    from { transform: translateX(100%); opacity: 0; }
    to { transform: translateX(0); opacity: 1; }
}
@keyframes fadeOut {
    from { opacity: 1; }
    to { opacity: 0; }
}
`;
document.head.appendChild(style);
