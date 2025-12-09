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
        const response = await fetch("/api/Auth/Login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ Username: username, Password: password })
        });

        if (response.ok) {
            const data = await response.json();
            console.log("Login Response Data:", data); // DEBUG: Check what the server returns

            localStorage.setItem("username", username);
            localStorage.setItem("userId", data.userId);
            localStorage.setItem("userRole", data.userRole);
            localStorage.setItem("userType", data.userType);
            localStorage.setItem("userEmail", data.email);
            localStorage.setItem("firstName", data.firstName);
            localStorage.setItem("lastName", data.lastName);
            localStorage.setItem("middleName", data.middleName || "");

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

// --- Forgot Password Logic ---
(function () {
    const PUBLIC_KEY = "q_WaGpOvP6-Rea8xD"; // From register.js
    const SERVICE_ID = "service_0yfgbor";   // From register.js
    const TEMPLATE_ID = "template_i7i1bux"; // Updated Template ID provided by user

    // Initialize EmailJS
    if (typeof emailjs !== 'undefined') {
        emailjs.init(PUBLIC_KEY);
    }

    // Containers
    const loginContainer = document.getElementById("loginContainer");
    const fpContainer = document.getElementById("forgotPasswordContainer");
    const fpLink = document.getElementById("forgotPasswordLink");
    const backToLoginLink = document.getElementById("backToLoginLink");

    // Steps
    const step1 = document.getElementById("fp-step-1");
    const step2 = document.getElementById("fp-step-2");
    const step3 = document.getElementById("fp-step-3");

    // Inputs & Buttons
    const emailInput = document.getElementById("fp-email");
    const otpInput = document.getElementById("fp-otp");
    const newPassInput = document.getElementById("fp-new-pass");
    const confirmPassInput = document.getElementById("fp-confirm-pass");

    const btnK1 = document.getElementById("fp-btn-k1"); // Send Code
    const btnK2 = document.getElementById("fp-btn-k2"); // Verify
    const btnK3 = document.getElementById("fp-btn-k3"); // Reset

    let generatedOTP = null;
    let targetEmail = "";

    // Show Forgot Password Form (Hide Login)
    if (fpLink) {
        fpLink.onclick = function (e) {
            e.preventDefault();
            if (loginContainer) loginContainer.style.display = "none";
            if (fpContainer) fpContainer.style.display = "block";
            resetForms();
        }
    }

    // Back to Login (Hide FP, Show Login)
    if (backToLoginLink) {
        backToLoginLink.onclick = function (e) {
            e.preventDefault();
            if (fpContainer) fpContainer.style.display = "none";
            if (loginContainer) loginContainer.style.display = "block";
        }
    }

    function resetForms() {
        if (step1) step1.style.display = "block";
        if (step2) step2.style.display = "none";
        if (step3) step3.style.display = "none";
        if (emailInput) emailInput.value = "";
        if (otpInput) otpInput.value = "";
        if (newPassInput) newPassInput.value = "";
        if (confirmPassInput) confirmPassInput.value = "";
        targetEmail = "";
    }

    // Step 1: Check Email & Send OTP
    if (btnK1) {
        btnK1.onclick = async function () {
            const email = emailInput.value.trim();
            if (!email) { alert("Please enter your email."); return; }

            // 1. API Check if email exists
            try {
                const res = await fetch(`/api/User/CheckEmail?email=${encodeURIComponent(email)}`);
                if (res.ok) {
                    const data = await res.json();
                    if (!data.exists) {
                        alert("Email not found in our system.");
                        return;
                    }
                } else {
                    throw new Error("Server error");
                }
            } catch (err) {
                console.error(err);
                alert("Error checking email.");
                return;
            }

            // 2. Generate OTP & Send
            generatedOTP = Math.floor(100000 + Math.random() * 900000);

            btnK1.textContent = "Sending...";
            btnK1.disabled = true;

            try {
                await emailjs.send(SERVICE_ID, TEMPLATE_ID, {
                    to_email: email,
                    passcode: generatedOTP
                });

                targetEmail = email;
                document.getElementById("fp-email-display").textContent = email;

                // Move to Step 2
                step1.style.display = "none";
                step2.style.display = "block";

            } catch (err) {
                console.error("EmailJS Error:", err);
                alert("Failed to send OTP. Please check console.");
            } finally {
                btnK1.textContent = "Send Code";
                btnK1.disabled = false;
            }
        };
    }

    // Step 2: Verify OTP
    if (btnK2) {
        btnK2.onclick = function () {
            if (otpInput.value == generatedOTP) {
                step2.style.display = "none";
                step3.style.display = "block";
            } else {
                alert("Incorrect OTP.");
            }
        };
    }

    // Step 3: Reset Password
    if (btnK3) {
        // Real-time Strength Check
        if (newPassInput) {
            const strengthDiv = document.getElementById("fp-password-strength");

            newPassInput.addEventListener("input", function () {
                const val = newPassInput.value;
                if (!val) {
                    if (strengthDiv) strengthDiv.textContent = "";
                    return;
                }

                const strength = checkPasswordStrength(val);
                if (strengthDiv) {
                    if (strength === "weak") {
                        strengthDiv.textContent = "Weak (Needs: 10+ chars, Upper, Lower, Number, Special)";
                        strengthDiv.style.color = "red";
                    } else if (strength === "medium") {
                        strengthDiv.textContent = "Medium";
                        strengthDiv.style.color = "orange";
                    } else if (strength === "strong") {
                        strengthDiv.textContent = "Strong";
                        strengthDiv.style.color = "green";
                    }
                }
            });
        }

        btnK3.onclick = async function () {
            const p1 = newPassInput.value;
            const p2 = confirmPassInput.value;

            if (!p1 || !p2) { alert("Please enter password."); return; }
            if (p1 !== p2) { alert("Passwords do not match."); return; }

            // Strong Password Validation
            const strength = checkPasswordStrength(p1);
            if (strength !== "strong") {
                alert("Password is too weak. Please meet the requirements.");
                return;
            }

            try {
                const res = await fetch("/api/User/ResetPassword", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        Email: targetEmail,
                        NewPassword: p1
                    })
                });

                if (res.ok) {
                    alert("Password reset successfully! Please login.");
                    if (fpContainer) fpContainer.style.display = "none";
                    if (loginContainer) loginContainer.style.display = "block";
                } else {
                    const data = await res.json();
                    alert("Error: " + (data.error || "Failed to reset"));
                }
            } catch (err) {
                console.error(err);
                alert("Error resetting password.");
            }
        };
    }

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
        return "weak";
    }

})();