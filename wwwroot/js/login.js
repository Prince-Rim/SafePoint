const loginForm = document.getElementById("loginForm");
const errorMessage = document.getElementById("loginError");
const passwordInput = document.getElementById("password");
const togglePassword = document.getElementById("togglePassword");

if (togglePassword && passwordInput) {
    togglePassword.addEventListener("click", () => {
        const type = passwordInput.getAttribute("type") === "password" ? "text" : "password";
        passwordInput.setAttribute("type", type);
        togglePassword.textContent = type === "password" ? "visibility_off" : "visibility";
    });
}

loginForm.addEventListener("submit", async (e) => {
    e.preventDefault();

    const username = document.getElementById("username").value.trim();
    const password = passwordInput.value.trim();

    if (errorMessage) errorMessage.textContent = "";

    try {
        const response = await fetch("https://localhost:44373/api/Auth/Login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ Username: username, Password: password })
        });

        if (response.ok) {
            const data = await response.json();

            localStorage.setItem("username", username);
            localStorage.setItem("userId", data.userId);
            localStorage.setItem("userRole", data.userRole);
            localStorage.setItem("userType", data.userType);
            localStorage.setItem("userEmail", data.email);

            if (data.userType === 'Admin' || data.userType === 'Moderator') {
                window.location.href = 'dashboard.html';
            } else {
                window.location.href = 'index.html';
            }
        } else {
            const data = await response.json();
            if (errorMessage) {
                errorMessage.textContent = data.error || "Login failed.";
            } else {
                console.error(data.error);
            }
        }
    } catch (err) {
        console.error("Login failed:", err);
        if (errorMessage) errorMessage.textContent = "An unexpected error occurred. Try again.";
    }
});