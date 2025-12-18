const API_BASE_URL = '/api';


let allUsers = [];
let allModerators = [];
let allAdmins = [];
let allAreas = [];

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

checkAuthorization();

async function loadAllAccounts() {
    if (!checkAuthorization()) return;

    try {
        const adminId = localStorage.getItem('userId') || sessionStorage.getItem('userId');
        const adminRole = localStorage.getItem('userRole') || sessionStorage.getItem('userRole');
        const headers = {
            'X-Requester-Id': adminId,
            'X-Requester-Role': adminRole
        };

        const [usersResp, modsResp, adminsResp, areasResp] = await Promise.all([
            fetch(`${API_BASE_URL}/Admin/users`, { headers }),
            fetch(`${API_BASE_URL}/Admin/moderators`, { headers }),
            fetch(`${API_BASE_URL}/Admin/admins`, { headers }),
            fetch(`${API_BASE_URL}/Admin/areas`, { headers })
        ]);

        allUsers = await usersResp.json();
        allModerators = await modsResp.json();
        allAdmins = await adminsResp.json();
        allAreas = await areasResp.json();

        populateAreaDropdown('area');
        populateAreaDropdown('edit-area');

        renderAccountsTable();
        setupAccountFilters();
    } catch (error) {
        console.error('Error loading accounts:', error);
        alert('Failed to load accounts. Please check API connection.');
    }
}

function populateAreaDropdown(selectId) {
    const areaSelect = document.getElementById(selectId);
    if (!areaSelect) return;

    areaSelect.innerHTML = '<option value="DEFAULT">DEFAULT (Default Area)</option>';

    allAreas.forEach(area => {
        const areaCode = area.Area_Code || area.area_Code;
        if (areaCode === 'DEFAULT') return;

        const option = document.createElement('option');
        const areaLocation = area.ALocation || area.aLocation;
        option.value = areaCode;
        option.textContent = `${areaCode} - ${areaLocation}`;
        areaSelect.appendChild(option);
    });
}

function getAllAllAccountsCombined() {
    return [
        ...allUsers.map(u => ({
            ...u,
            accountType: 'User',
            id: u.userid,
            isActive: (u.isActive !== undefined ? u.isActive : u.IsActive),
            suspensionEndTime: u.SuspensionEndTime || u.suspensionEndTime
        })),
        ...allModerators.map(m => ({
            ...m,
            accountType: 'Moderator',
            id: m.modid,
            area_Code: m.Area_Code,
            isActive: (m.isActive !== undefined ? m.isActive : m.IsActive),
            suspensionEndTime: m.SuspensionEndTime
        })),
        ...allAdmins.map(a => ({
            ...a,
            accountType: 'Admin',
            id: a.adminid,
            Permissions: a.permissions || a.Permissions,
            isActive: (a.isActive !== undefined ? a.isActive : a.IsActive),
            suspensionEndTime: a.SuspensionEndTime
        }))
    ];
}

let filterRole = '';
let filterStatus = '';
let filterSearch = '';

function setupAccountFilters() {
    const filterBtn = document.getElementById('filterBtn');
    const filterMenu = document.getElementById('filterMenu');
    const roleSelect = document.getElementById('filterRole');
    const statusSelect = document.getElementById('filterStatus');
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

    const apply = () => {
        filterRole = roleSelect ? roleSelect.value : '';
        filterStatus = statusSelect ? statusSelect.value : '';
        filterSearch = searchInput ? searchInput.value.toLowerCase() : '';
        renderAccountsTable();
    };

    if (roleSelect) roleSelect.addEventListener('change', apply);
    if (statusSelect) statusSelect.addEventListener('change', apply);
    if (searchInput) searchInput.addEventListener('input', apply);

    if (clearBtn) {
        clearBtn.addEventListener('click', () => {
            if (roleSelect) roleSelect.value = '';
            if (statusSelect) statusSelect.value = '';
            if (searchInput) searchInput.value = '';
            apply();
        });
    }
}

function renderAccountsTable() {
    const tbody = document.querySelector('.user-table-wrapper tbody');
    if (!tbody) return;

    tbody.innerHTML = '';

    let accounts = getAllAllAccountsCombined();

    accounts = accounts.filter(acc => {
        const searchMatch = !filterSearch ||
            (acc.username || '').toLowerCase().includes(filterSearch) ||
            (acc.email || '').toLowerCase().includes(filterSearch) ||
            (acc.contact || '').toLowerCase().includes(filterSearch);

        const roleMatch = !filterRole || acc.accountType === filterRole;

        const isActive = acc.isActive;
        let statusMatch = true;
        if (filterStatus === 'active') statusMatch = isActive === true;
        if (filterStatus === 'inactive') statusMatch = isActive === false;

        return searchMatch && roleMatch && statusMatch;
    });

    if (accounts.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6">No matching accounts found</td></tr>';
        return;
    }

    accounts.forEach(account => {
        const isActive = account.isActive;
        const statusText = isActive ? 'Active' : 'Inactive';
        const statusClass = isActive ? 'active-status' : 'inactive-status';

        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${account.username || 'N/A'}</td>
            <td>${account.email || 'N/A'}</td>
            <td>${account.contact || 'N/A'}</td>
            <td><span class="badge">${account.accountType}</span></td>
            <td><span class="badge ${statusClass}">${statusText}</span></td>
            <td>
                <button class="btn-edit" onclick="editAccount('${account.id}', '${account.accountType}')">Edit</button>
                <button class="btn-delete" onclick="deleteAccount('${account.id}', '${account.accountType}')">Delete</button>
            </td>
        `;
        tbody.appendChild(row);
    });
}

async function createAccount(formData) {
    const role = formData.get('role');
    const password = formData.get('password');
    const confirmPassword = formData.get('confirm-password');

    if (password !== confirmPassword) {
        alert('Passwords do not match!');
        return;
    }

    const email = formData.get('email').trim();
    const emailPattern = /^[a-zA-Z0-9._%+-]+@gmail\.com$/;
    if (!emailPattern.test(email)) {
        alert('Email must be a valid @gmail.com address.');
        return;
    }

    const contact = formData.get('contact');
    if (!/^09\d{9}$/.test(contact)) {
        alert("Contact number must be a valid number: 11 digits and start with '09'.");
        return;
    }

    try {
        const adminId = localStorage.getItem('userId') || sessionStorage.getItem('userId');
        const adminRole = localStorage.getItem('userRole') || sessionStorage.getItem('userRole');
        const headers = {
            'Content-Type': 'application/json',
            'X-Requester-Id': adminId,
            'X-Requester-Role': adminRole
        };
        const payload = {
            FirstName: formData.get('firstname'),
            LastName: formData.get('lastname'),
            MiddleName: formData.get('middlename'),

            Username: formData.get('username'),
            Email: formData.get('email'),
            Contact: formData.get('contact'),
            Password: password,
            UserRole: role === 'user' ? 'User' : undefined,
            Area_Code: role === 'moderator' ? (formData.get('area') || 'DEFAULT') : undefined
        };

        let endpoint;
        if (role === 'admin') {
            endpoint = '/Admin/create-admin';
            const selectedPermissions = Array.from(document.querySelectorAll('#createUserModal input[name="permissions"]:checked'))
                .map(cb => cb.value)
                .join(',');

            payload.Permissions = selectedPermissions;
        }
        else if (role === 'moderator') endpoint = '/Admin/create-moderator';
        else if (role === 'user') endpoint = '/Admin/create-user';
        else return;

        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || `Failed to create ${role}`);
        }

        const result = await response.json();
        alert(result.message || 'Account created successfully!');

        closeModal();
        loadAllAccounts();
    } catch (error) {
        console.error('Error creating account:', error);
        alert(error.message || 'Failed to create account.');
    }
}

function editAccount(id, type) {
    const targetType = type.toLowerCase();
    let account = null;

    if (targetType === 'user') {
        account = allUsers.find(u => u.userid === id);
    } else if (targetType === 'moderator') {
        account = allModerators.find(m => m.modid === id);
    } else if (targetType === 'admin') {
        account = allAdmins.find(a => a.adminid === id);
    }

    if (account) {
        const accountData = {
            ...account,
            accountType: type,
            id: id,
            area_Code: account.Area_Code || account.area_Code,
            suspensionEndTime: account.SuspensionEndTime || account.suspensionEndTime,
            Permissions: account.permissions || account.Permissions
        };
        openEditModal(accountData);
    } else {
        alert(`Account not found or data missing for ${type}.`);
    }
}

function openEditModal(account) {
    const modal = document.getElementById('editUserModal');
    console.log('Opening Edit Modal for account:', account);
    if (!modal) {
        alert("Edit Modal structure not found in HTML.");
        return;
    }

    document.getElementById('edit-password').value = '';
    document.getElementById('edit-confirm-password').value = '';

    document.getElementById('edit-id').value = account.id;
    document.getElementById('edit-original-role').value = account.accountType.toLowerCase();

    document.getElementById('edit-firstname').value = account.firstname || account.firstName || account.FirstName || '';
    document.getElementById('edit-lastname').value = account.lastname || account.lastName || account.LastName || '';
    document.getElementById('edit-middlename').value = account.middlename || account.middleName || account.MiddleName || '';

    document.getElementById('edit-username').value = account.username || '';
    document.getElementById('edit-email').value = account.email || '';
    document.getElementById('edit-contact').value = account.contact || '';

    const roleSelect = document.getElementById('edit-role');
    if (roleSelect) {
        roleSelect.value = account.accountType.toLowerCase();
    }

    const statusSelect = document.getElementById('edit-status');
    const suspensionGroup = document.getElementById('suspension-group');
    const suspensionInput = document.getElementById('edit-suspension-end');

    if (statusSelect) {
        const isActive = account.isActive !== undefined ? account.isActive : account.IsActive;
        statusSelect.value = isActive ? 'active' : 'inactive';

        if (statusSelect.value === 'inactive') {
            suspensionGroup.style.display = 'flex';
            if (account.suspensionEndTime && account.suspensionEndTime.length > 16) {
                suspensionInput.value = account.suspensionEndTime.substring(0, 16);
            } else {
                suspensionInput.value = account.suspensionEndTime || '';
            }
        } else {
            suspensionGroup.style.display = 'none';
            suspensionInput.value = '';
        }
    }

    const areaGroup = document.getElementById('edit-area-group');
    const areaSelect = document.getElementById('edit-area');

    if (account.accountType === 'Moderator') {
        if (areaGroup) areaGroup.style.display = 'flex';

        if (areaSelect && account.area_Code) {
            areaSelect.value = account.area_Code;
        } else if (areaSelect) {
            areaSelect.value = 'DEFAULT';
        }
    } else {
        if (areaGroup) areaGroup.style.display = 'none';
    }

    const permissionGroup = document.getElementById('edit-permissions-group');
    const permissionCheckboxes = document.querySelectorAll('#edit-permission-checkboxes input[name="permissions"]');

    if (account.accountType === 'Admin') {
        if (permissionGroup) permissionGroup.style.display = 'flex';

        const currentPermissions = (account.Permissions || "")
            .split(',')
            .map(p => p.trim())
            .filter(p => p);

        console.log('Parsed Permissions:', currentPermissions);

        permissionCheckboxes.forEach(checkbox => {
            checkbox.checked = currentPermissions.includes(checkbox.value);
        });

    } else {
        if (permissionGroup) permissionGroup.style.display = 'none';
        permissionCheckboxes.forEach(checkbox => checkbox.checked = false);
    }

    modal.style.display = 'flex';
}

async function updateAccount(formData) {
    const id = formData.get('id');
    const role = formData.get('role');
    const originalRole = formData.get('original-role');
    const password = formData.get('password');
    const confirmPassword = formData.get('confirm-password');

    const statusValue = formData.get('status');
    const isActive = statusValue === 'active';
    const suspensionEnd = formData.get('suspensionEnd');

    if (password && password !== confirmPassword) {
        alert('New Passwords do not match!');
        return;
    }

    const email = formData.get('email').trim();
    const emailPattern = /^[a-zA-Z0-9._%+-]+@gmail\.com$/;
    if (!emailPattern.test(email)) {
        alert('Email must be a valid @gmail.com address.');
        return;
    }

    const contact = formData.get('contact');
    if (!/^09\d{9}$/.test(contact)) {
        alert("Contact number must be 11 digits and start with '09'.");
        return;
    }

    try {
        const adminId = localStorage.getItem('userId') || sessionStorage.getItem('userId');
        const adminRole = localStorage.getItem('userRole') || sessionStorage.getItem('userRole');
        const headers = {
            'Content-Type': 'application/json',
            'X-Requester-Id': adminId,
            'X-Requester-Role': adminRole
        };

        let endpoint = '';


        if (role !== originalRole) {
            endpoint = '/Admin/change-role';

            const payload = {
                Id: id,
                CurrentRole: originalRole.charAt(0).toUpperCase() + originalRole.slice(1),
                TargetRole: role.charAt(0).toUpperCase() + role.slice(1),


                FirstName: formData.get('firstname'),
                LastName: formData.get('lastname'),
                MiddleName: formData.get('middlename'),
                Username: formData.get('username'),
                Email: formData.get('email'),
                Contact: formData.get('contact'),
                IsActive: isActive,
                SuspensionEndTime: (isActive === false && suspensionEnd) ? suspensionEnd : null
            };

            if (password) payload.Password = password;

            if (role === 'moderator') {
                payload.Area_Code = formData.get('area');
            } else if (role === 'admin') {
                const selectedPermissions = Array.from(document.querySelectorAll('#editUserModal input[name="permissions"]:checked'))
                    .map(cb => cb.value)
                    .join(',');
                payload.Permissions = selectedPermissions;
            }

            const response = await fetch(`${API_BASE_URL}${endpoint}`, {
                method: 'POST',
                headers: headers,
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.error || `Failed to change role from ${originalRole} to ${role}`);
            }

            alert(`Role changed successfully!`);
            closeEditModal();
            loadAllAccounts();
            return;
        }

        if (originalRole === 'admin') endpoint = `/Admin/update-admin/${id}`;
        else if (originalRole === 'moderator') endpoint = `/Admin/update-moderator/${id}`;
        else if (originalRole === 'user') endpoint = `/Admin/update-user/${id}`;
        else return;

        const payload = {
            FirstName: formData.get('firstname'),
            LastName: formData.get('lastname'),
            MiddleName: formData.get('middlename'),

            Username: formData.get('username'),
            Email: formData.get('email'),
            Contact: formData.get('contact'),
            IsActive: isActive,
            SuspensionEndTime: (isActive === false && suspensionEnd) ? suspensionEnd : null
        };

        if (password) {
            payload.password = password;
        }

        if (originalRole === 'moderator') {
            payload.area_Code = formData.get('area');
        } else if (originalRole === 'admin') {
            const selectedPermissions = Array.from(document.querySelectorAll('#editUserModal input[name="permissions"]:checked'))
                .map(cb => cb.value)
                .join(',');
            payload.Permissions = selectedPermissions;
        }

        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
            method: 'PUT',
            headers: headers,
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || `Failed to update ${originalRole}`);
        }

        alert(`${originalRole} updated successfully!`);
        closeEditModal();
        loadAllAccounts();
    } catch (error) {
        console.error('Error updating account:', error);
        alert(`Failed to update account: ${error.message}`);
    }
}

async function deleteAccount(id, type) {
    if (!confirm(`Are you sure you want to delete this ${type} account (ID: ${id})? This action cannot be undone.`)) {
        return;
    }

    const targetType = type.toLowerCase();
    let endpoint = '';

    if (targetType === 'user') endpoint = `/Admin/delete-user/${id}`;
    else if (targetType === 'moderator') endpoint = `/Admin/delete-moderator/${id}`;
    else if (targetType === 'admin') endpoint = `/Admin/delete-admin/${id}`;
    else {
        alert('Unknown account type.');
        return;
    }

    try {
        const adminId = localStorage.getItem('userId') || sessionStorage.getItem('userId');
        const adminRole = localStorage.getItem('userRole') || sessionStorage.getItem('userRole');
        const headers = {
            'X-Requester-Id': adminId,
            'X-Requester-Role': adminRole
        };

        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
            method: 'DELETE',
            headers: headers
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || `Failed to delete ${type}`);
        }

        alert(`${type} deleted successfully.`);
        loadAllAccounts();
    } catch (error) {
        console.error('Error deleting account:', error);
        alert(`Failed to delete account: ${error.message}`);
    }
}

function closeModal() {
    const modal = document.getElementById('createUserModal');
    if (modal) {
        modal.style.display = 'none';
        const form = modal.querySelector('form');
        if (form) form.reset();
        setupRoleChange();
    }
}

function closeEditModal() {
    const modal = document.getElementById('editUserModal');
    if (modal) {
        modal.style.display = 'none';
        modal.querySelector('form').reset();
    }
}

function setupForm() {
    const form = document.querySelector('#createUserModal form');
    if (!form) return;

    form.addEventListener('submit', (e) => {
        e.preventDefault();
        const formData = new FormData(form);
        createAccount(formData);
    });
}

function setupEditModalListeners() {
    const editModal = document.getElementById('editUserModal');
    const closeLink = document.getElementById('closeEditModalLink');
    const form = document.getElementById('editAccountForm');
    const roleSelect = document.getElementById('edit-role');
    const areaGroup = document.getElementById('edit-area-group');
    const statusSelect = document.getElementById('edit-status');
    const suspensionGroup = document.getElementById('suspension-group');

    if (closeLink) {
        closeLink.addEventListener('click', (e) => {
            e.preventDefault();
            closeEditModal();
        });
    }

    if (form) {
        form.addEventListener('submit', (e) => {
            e.preventDefault();
            updateAccount(new FormData(form));
        });
    }

    if (roleSelect && areaGroup) {
        roleSelect.addEventListener('change', (e) => {
            areaGroup.style.display = e.target.value === 'moderator' ? 'flex' : 'none';
            const permissionGroup = document.getElementById('edit-permissions-group');
            if (permissionGroup) {
                permissionGroup.style.display = e.target.value === 'admin' ? 'flex' : 'none';
            }
        });
    }

    if (statusSelect && suspensionGroup) {
        statusSelect.addEventListener('change', (e) => {
            if (e.target.value === 'inactive') {
                suspensionGroup.style.display = 'flex';
            } else {
                suspensionGroup.style.display = 'none';
                document.getElementById('edit-suspension-end').value = '';
            }
        });
    }

    if (editModal) {
        window.addEventListener('click', function (event) {
            if (event.target === editModal) {
                closeEditModal();
            }
        });
    }
}

function setupRoleChange() {
    const roleSelect = document.getElementById('role');
    const areaGroup = document.querySelector('.form-group:has(#area)');
    const permissionGroup = document.getElementById('create-permissions-group');

    if (roleSelect && areaGroup && permissionGroup) {
        const toggleVisibility = (role) => {
            areaGroup.style.display = role === 'moderator' ? 'flex' : 'none';
            permissionGroup.style.display = role === 'admin' ? 'flex' : 'none';
        };

        if (roleSelect.value !== 'moderator') {
            areaGroup.style.display = 'none';
        }
        if (roleSelect.value !== 'admin') {
            permissionGroup.style.display = 'none';
        }

        roleSelect.addEventListener('change', (e) => {
            toggleVisibility(e.target.value);

            if (e.target.value === 'moderator') {
                const areaSelect = document.getElementById('area');
                if (areaSelect) areaSelect.value = 'DEFAULT';
            } else if (e.target.value === 'admin') {
                document.querySelectorAll('#createUserModal input[name="permissions"]').forEach(cb => cb.checked = false);
            }
        });
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
    loadAllAccounts();
    setupForm();
    setupRoleChange();
    setupEditModalListeners();
    setupLogout();

    const createModal = document.getElementById('createUserModal');
    if (createModal) {
        const openBtn = document.getElementById('openModalBtn');
        if (openBtn) openBtn.addEventListener('click', () => createModal.style.display = 'flex');
        const closeLink = document.getElementById('closeModalLink');
        if (closeLink) closeLink.addEventListener('click', (e) => { e.preventDefault(); closeModal(); });

        window.addEventListener('click', (event) => {
            if (event.target === createModal) {
                closeModal();
            }
        });
    }
});