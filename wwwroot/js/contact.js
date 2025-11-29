const username = localStorage.getItem("username");
const userBtn = document.getElementById("userBtn");
const userDropdownWrapper = document.getElementById("userDropdown");
const userDropdown = document.getElementById("dropdownContent");

if (username) userBtn.textContent = username + " ▾";

document.getElementById("contactForm").addEventListener("submit", async function (e) {
    e.preventDefault();
    const form = e.target;

    const response = await fetch(form.action, {
        method: form.method,
        body: new FormData(form),
        headers: {
            'Accept': 'application/json'
        }
    });

    if (response.ok) {
        alert("Message sent successfully!");
        form.reset();
    } else {
        alert("There was a problem sending your message. Please try again.");
    }
});

const userId = localStorage.getItem('userId');
const userRole = localStorage.getItem('userRole');

document.addEventListener('DOMContentLoaded', () => {
    checkDashboardAccess();
});

function checkDashboardAccess() {
    const dashboardLink = document.getElementById('dashboardLink');
    const myReportsLink = document.querySelector('a[href="mypost.html"]');

    // Default: Hide both
    if (dashboardLink) dashboardLink.style.display = 'none';
    if (myReportsLink) myReportsLink.style.display = 'none';

    if (userId) {
        if (userRole === 'Admin' || userRole === 'Moderator') {
            if (dashboardLink) dashboardLink.style.display = 'block';
        } else {
            if (myReportsLink) myReportsLink.style.display = 'inline-block';
        }
    }
}