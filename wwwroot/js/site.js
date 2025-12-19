const API_BASE_URL = '/api';



const profileDisplayButton = document.getElementById("profileDisplayButton");
const profileModal = document.getElementById("profileModal");
const closeProfileModal = document.getElementById("closeProfileModal");
const profileActionBar = document.getElementById("profileActionBar");
const saveProfileChangesBtn = document.getElementById("saveProfileChanges");
const cancelProfileEditBtn = document.getElementById("cancelProfileEdit");
const editButtons = document.querySelectorAll(".edit-btn");

console.log(`Found ${editButtons.length} edit buttons`);

const passwordEditFields = document.getElementById('passwordEditFields');
const currentPasswordInput = document.getElementById('currentPasswordInput');
const newPasswordInput = document.getElementById('newPasswordInput');
const passwordStrengthMsg = document.getElementById('passwordStrengthMsg');
const passwordRequirementsMsg = document.getElementById('passwordRequirementsMsg');
const passwordInfoRow = document.querySelector('.info-row button[data-field="password"]')?.closest('.info-row');

function checkPasswordStrength(password) {
    let score = 0;
    if (password.length >= 10) score++;
    if (/[a-z]/.test(password)) score++;
    if (/[A-Z]/.test(password)) score++;
    if (/\d/.test(password)) score++;
    if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) score++;
    if (score <= 2) return "weak";
    if (score === 3 || score === 4) return "medium";
    if (score === 5) return "strong";
}

function updatePasswordStrengthDisplay() {
    const password = newPasswordInput.value;
    const strengthMsg = passwordStrengthMsg;
    const requirementsMsg = passwordRequirementsMsg;

    if (!strengthMsg || !requirementsMsg) return;

    if (password === "") {
        strengthMsg.textContent = "";
        strengthMsg.className = "strength-msg";
        requirementsMsg.textContent = "";
        return;
    }

    const strength = checkPasswordStrength(password);
    strengthMsg.className = "strength-msg";

    if (strength === "weak") {
        strengthMsg.textContent = "Weak password";
        strengthMsg.classList.add("strength-weak");
    } else if (strength === "medium") {
        strengthMsg.textContent = "Medium password";
        strengthMsg.classList.add("strength-medium");
    } else if (strength === "strong") {
        strengthMsg.textContent = "Strong password";
        strengthMsg.classList.add("strength-strong");
    }
    requirementsMsg.textContent = "Password must be at least 10 characters, include uppercase, lowercase, number, and special character.";
}

if (newPasswordInput) {
    newPasswordInput.addEventListener("input", updatePasswordStrengthDisplay);
}

async function loadProfileData() {
    const displayName = localStorage.getItem("username") || sessionStorage.getItem("username") || "Guest";
    const userIdentifier = localStorage.getItem("userId") || sessionStorage.getItem("userId") || "N/A";
    const role = localStorage.getItem("userRole") || sessionStorage.getItem("userRole") || "User";
    const email = localStorage.getItem("userEmail") || sessionStorage.getItem("userEmail") || "N/A";
    const firstName = localStorage.getItem("firstName") || sessionStorage.getItem("firstName") || "";
    const lastName = localStorage.getItem("lastName") || sessionStorage.getItem("lastName") || "";
    const middleName = localStorage.getItem("middleName") || sessionStorage.getItem("middleName") || "";

    const fullName = (firstName && lastName)
        ? `${firstName} ${middleName ? middleName + ' ' : ''}${lastName}`
        : displayName;

    const userBtn = document.getElementById("userBtn");
    if (userBtn) userBtn.textContent = `${displayName} ▾`;

    if (document.getElementById("dropdownUsername")) document.getElementById("dropdownUsername").textContent = displayName;
    if (document.getElementById("dropdownEmail")) document.getElementById("dropdownEmail").textContent = email;
    if (document.getElementById("dropdownRole")) document.getElementById("dropdownRole").textContent = role;

    if (profileModal) {
        if (document.getElementById("profileUserId")) document.getElementById("profileUserId").textContent = userIdentifier;
        if (document.getElementById("profileDisplayName")) document.getElementById("profileDisplayName").textContent = displayName;
        if (document.getElementById("profileRole")) document.getElementById("profileRole").textContent = role;
        if (document.getElementById("profileFullName")) document.getElementById("profileFullName").textContent = fullName;

        // NEW: specific header name
        if (document.getElementById("modalProfileName")) document.getElementById("modalProfileName").textContent = displayName;

        const obscuredEmail = email.replace(/^(.{3}).*(@.*)$/, "$1*******$2");
        if (document.getElementById("profileEmail")) document.getElementById("profileEmail").textContent = obscuredEmail;

        profileModal.dataset.fullDisplayName = displayName;
        profileModal.dataset.fullEmail = email;

        // FETCH AND RENDER BADGES
        const badgesContainer = document.getElementById('profileBadgesContainer');
        if (badgesContainer && userIdentifier !== "N/A") {
            badgesContainer.innerHTML = ''; // Clear previous
            try {
                const response = await fetch(`${API_BASE_URL}/User/profile/${userIdentifier}`);
                if (response.ok) {
                    const data = await response.json();
                    if (data.badges && data.badges.length > 0) {
                        const badgeIconMap = {
                            'Certified Reporter': 'security',
                            'Community Guardian': 'star',
                            'Top Contributor': 'emoji_events',
                            'Reliable Source': 'verified',
                            'Sociable': 'forum'
                        };

                        data.badges.forEach(b => {
                            const icon = badgeIconMap[b.badgeName] || 'verified';

                            const badgeDiv = document.createElement('div');
                            badgeDiv.className = 'badge-item';
                            badgeDiv.title = b.badgeName;

                            const iconSpan = document.createElement('span');
                            iconSpan.className = 'material-icons';
                            iconSpan.textContent = icon;

                            badgeDiv.appendChild(iconSpan);
                            badgesContainer.appendChild(badgeDiv);
                        });
                    } else {
                        // Optional: empty state or nothing
                        badgesContainer.style.display = 'none';
                    }
                }
            } catch (err) {
                console.error("Failed to fetch profile badges", err);
            }
        }
    }

    const loggedInActions = document.getElementById("loggedInActions");
    const guestActions = document.getElementById("guestActions");

    if (userIdentifier !== "N/A") {
        if (loggedInActions) loggedInActions.style.display = 'block';
        if (guestActions) guestActions.style.display = 'none';
    } else {
        if (loggedInActions) loggedInActions.style.display = 'none';
        if (guestActions) guestActions.style.display = 'block';
    }
}

async function toggleEditField(button) {
    console.log("toggleEditField called for:", button.dataset.field, "Text:", button.textContent);

    const infoRow = button.closest('.info-row');
    const valueElement = infoRow.querySelector('.value');
    const field = button.dataset.field;
    const fieldName = field.charAt(0).toUpperCase() + field.slice(1);
    const userBtn = document.getElementById("userBtn");

    if (field === 'password') {
        if (button.textContent === 'Edit') {
            if (passwordInfoRow) passwordInfoRow.style.display = 'none';
            if (passwordEditFields) passwordEditFields.style.display = 'block';

            if (currentPasswordInput) currentPasswordInput.value = '';
            if (newPasswordInput) newPasswordInput.value = '';
            updatePasswordStrengthDisplay();

            if (profileActionBar) profileActionBar.style.display = 'flex';
        } else if (button.textContent === 'Save') {
            return;
        }

    } else {
        if (button.textContent === 'Edit') {
            console.log("Switching field to EDIT mode");
            const currentValue = profileModal.dataset[`full${fieldName}`] || valueElement.textContent;

            const input = document.createElement('input');
            input.type = (field === 'email') ? 'email' : 'text';
            input.value = (field === 'email') ? profileModal.dataset.fullEmail : currentValue;
            input.dataset.originalValue = input.value;
            input.id = `editInput${fieldName}`;
            input.classList.add('info-row-input');

            valueElement.style.display = 'none';
            infoRow.insertBefore(input, button);
            button.textContent = 'Save';

            if (profileActionBar) profileActionBar.style.display = 'flex';
        } else if (button.textContent === 'Save') {
            console.log("Attempting to SAVE field:", field);
            const input = infoRow.querySelector('input');
            const newValue = input.value;


            const userId = localStorage.getItem('userId') || sessionStorage.getItem('userId');
            const userRole = localStorage.getItem('userRole') || sessionStorage.getItem('userRole');
            let currentDisplayName = profileModal.dataset.fullDisplayName;
            let currentEmail = profileModal.dataset.fullEmail;

            if (field === 'email') {
                const emailPattern = /^[a-zA-Z0-9._%+-]+@gmail\.com$/;
                if (!emailPattern.test(newValue.trim())) {
                    alert('Email must be a valid @gmail.com address.');
                    return;
                }
                currentEmail = newValue.trim();
            } else if (field === 'displayName') {
                currentDisplayName = newValue.trim();
            }

            console.log("Sending UpdateProfile payload:", {
                UserId: userId,
                UserRole: userRole,
                DisplayName: currentDisplayName,
                Email: currentEmail
            });

            try {
                const response = await fetch(`${API_BASE_URL}/User/UpdateProfile`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        UserId: userId,
                        UserRole: userRole,
                        DisplayName: currentDisplayName,
                        Email: currentEmail
                    })
                });

                console.log("UpdateProfile response status:", response.status);

                if (!response.ok) {
                    const errorText = await response.text();
                    console.error("UpdateProfile failed:", errorText);
                    try {
                        const errorJson = JSON.parse(errorText);
                        alert(`Failed to update profile: ${errorJson.error || errorJson.title || errorText}`);
                    } catch {
                        alert(`Failed to update profile: ${errorText}`);
                    }
                    return false;
                }
            } catch (error) {
                console.error("Error updating profile:", error);
                alert("An error occurred while updating the profile. Check console for details.");
                return false;
            }


            if (field === 'email') {
                const obscuredEmail = currentEmail.replace(/^(.{3}).*(@.*)$/, "$1*******$2");
                valueElement.textContent = obscuredEmail;
                const storage = localStorage.getItem("userEmail") ? localStorage : sessionStorage;
                storage.setItem("userEmail", currentEmail);
                profileModal.dataset.fullEmail = currentEmail;
            } else if (field === 'displayName') {
                valueElement.textContent = currentDisplayName;
                const storage = localStorage.getItem("username") ? localStorage : sessionStorage;
                storage.setItem("username", currentDisplayName);
                if (userBtn) userBtn.textContent = `${currentDisplayName} ▾`;
                profileModal.dataset.fullDisplayName = currentDisplayName;
            }

            infoRow.removeChild(input);
            valueElement.style.display = 'block';
            button.textContent = 'Edit';

            const isAnyFieldEditing = Array.from(editButtons).some(btn => btn.textContent === 'Save');
            if (!isAnyFieldEditing && passwordEditFields.style.display !== 'block') {
                if (profileActionBar) profileActionBar.style.display = 'none';
            }
            alert("Profile updated successfully.");
            loadProfileData();
            return true;
        }
    }
}

function resetEditFields() {
    editButtons.forEach(button => {
        if (button.textContent === 'Save') {
            const infoRow = button.closest('.info-row');
            const valueElement = infoRow.querySelector('.value');
            const input = infoRow.querySelector('input');

            if (input) {
                infoRow.removeChild(input);
                valueElement.style.display = 'block';
            }
            button.textContent = 'Edit';
        }
    });

    if (passwordEditFields) {
        passwordEditFields.style.display = 'none';
        if (passwordInfoRow) passwordInfoRow.style.display = 'flex';
        if (passwordStrengthMsg) {
            passwordStrengthMsg.textContent = "";
            passwordStrengthMsg.className = "strength-msg";
        }
        if (passwordRequirementsMsg) passwordRequirementsMsg.textContent = "";
    }

    if (profileActionBar) profileActionBar.style.display = 'none';
    loadProfileData();
}


if (profileDisplayButton) {
    profileDisplayButton.addEventListener('click', (e) => {
        e.stopPropagation();
        const userDropdown = document.querySelector(".user-dropdown");
        if (userDropdown) userDropdown.classList.remove("show");
        profileModal.style.display = 'flex';
        resetEditFields();
    });
}

if (closeProfileModal) {
    closeProfileModal.addEventListener('click', () => {
        profileModal.style.display = 'none';
        resetEditFields();
    });
}

if (profileModal) {
    window.addEventListener('click', (event) => {
        if (event.target == profileModal) {
            profileModal.style.display = 'none';
            resetEditFields();
        }
    });
}

editButtons.forEach(button => {
    button.addEventListener('click', () => {
        const field = button.dataset.field;
        console.log("Edit/Save button clicked for field:", field);
        if (field === 'username') return;

        toggleEditField(button);
    });
});

if (saveProfileChangesBtn) {
    saveProfileChangesBtn.addEventListener('click', async () => {
        console.log("Global Save Profile Changes clicked");
        let changesMade = false;

        for (const button of editButtons) {
            if (button.textContent === 'Save') {
                console.log("Found a pending save button for field:", button.dataset.field);
                const success = await toggleEditField(button);
                if (!success) return;
                changesMade = true;
            }
        }

        if (passwordEditFields.style.display === 'block') {
            const currentPass = currentPasswordInput.value;
            const newPass = newPasswordInput.value;

            if (newPass.length > 0) {
                const strength = checkPasswordStrength(newPass);

                if (!currentPass) {
                    alert("Please enter your current password.");
                    return;
                }

                if (strength !== 'strong') {
                    alert("New password is too weak. Please meet all complexity requirements.");
                    return;
                }

                try {
                    const userId = localStorage.getItem('userId') || sessionStorage.getItem('userId');
                    const userRole = localStorage.getItem('userRole') || sessionStorage.getItem('userRole');

                    const response = await fetch(`${API_BASE_URL}/User/UpdatePassword`, {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            UserId: userId,
                            CurrentPassword: currentPass,
                            NewPassword: newPass,
                            UserRole: userRole
                        })
                    });

                    if (response.ok) {
                        alert("Password updated successfully.");
                        changesMade = true;
                    } else {
                        const errorText = await response.text();
                        try {
                            const errorJson = JSON.parse(errorText);
                            alert(`Failed to update password: ${errorJson.error || errorText}`);
                        } catch {
                            alert(`Failed to update password: ${errorText}`);
                        }
                        return;
                    }
                } catch (error) {
                    console.error("Error updating password:", error);
                    alert("An error occurred while updating the password.");
                    return;
                }
            }
        }

        if (!changesMade && passwordEditFields.style.display !== 'block') {
            alert("No pending changes to save.");
        }
        resetEditFields();
        loadProfileData();
    });
}

if (cancelProfileEditBtn) {
    cancelProfileEditBtn.addEventListener('click', () => {
        resetEditFields();
    });
}

document.addEventListener('DOMContentLoaded', () => {
    loadProfileData();

    const userBtn = document.getElementById("userBtn");
    const userDropdown = document.querySelector(".user-dropdown");

    if (userBtn && userDropdown) {
        userBtn.addEventListener("click", (e) => {
            e.stopPropagation();

            userDropdown.classList.toggle("show");
        });

        window.addEventListener("click", (e) => {
            if (!e.target.matches('.user-btn')) {
                if (userDropdown.classList.contains('show')) {
                    userDropdown.classList.remove("show");
                }
            }
        });
    }

    checkDashboardAccess();

    const logoutBtn = document.getElementById("logoutBtn");
    if (logoutBtn) {
        logoutBtn.addEventListener("click", (e) => {
            e.preventDefault();
            if (confirm('Are you sure you want to log out?')) {
                localStorage.clear();
                sessionStorage.clear();
                window.location.href = "landing.html";
            }
        });
    }

    const mobileMenu = document.getElementById('mobile-menu');
    const navbar = document.querySelector('.navbar');

    if (mobileMenu && navbar) {
        mobileMenu.addEventListener('click', () => {
            navbar.classList.toggle('active');
        });
    }
});

function checkDashboardAccess() {
    const userId = localStorage.getItem('userId') || sessionStorage.getItem('userId');
    const userRole = localStorage.getItem('userRole') || sessionStorage.getItem('userRole');
    const dashboardLink = document.getElementById('dashboardLink');
    const myReportsLink = document.getElementById('myReportsLink');

    if (userId) {
        if (userRole === 'Admin' || userRole === 'Moderator') {
            if (dashboardLink) dashboardLink.style.display = 'block';
            if (myReportsLink) myReportsLink.style.display = 'none';
        } else {
            if (dashboardLink) dashboardLink.style.display = 'none';
            if (myReportsLink) myReportsLink.style.display = '';
        }
    } else {
        if (dashboardLink) dashboardLink.style.display = 'none';
        if (myReportsLink) myReportsLink.style.display = 'none';
    }
}