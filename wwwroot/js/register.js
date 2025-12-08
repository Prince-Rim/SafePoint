const PUBLIC_KEY = "q_WaGpOvP6-Rea8xD";
const SERVICE_ID = "service_0yfgbor";
const TEMPLATE_ID = "template_9we53op";

emailjs.init(PUBLIC_KEY);

let generatedOTP = null;
let userDataStorage = {};

const registerContainer = document.getElementById("registrationContainer");
const otpContainer = document.getElementById("otpContainer");
const registerForm = document.getElementById("registerForm");
const otpForm = document.getElementById("otpForm");
const resendOtpBtn = document.getElementById("resendOtpBtn");

const passwordInput = document.getElementById("password");
const togglePassword = document.getElementById("togglePassword");
const strengthMsg = document.getElementById("passwordStrength");
const requirementsMsg = document.getElementById("passwordRequirements");

const modal = document.getElementById('termsModal');
const termsLink = document.getElementById('termsLink');
const closeModal = modal.querySelector('.close');
const acceptBtn = document.getElementById('acceptTerms');
const rejectBtn = document.getElementById('rejectTerms');
const checkbox = document.getElementById('terms');

togglePassword.addEventListener("click", () => {
    if (passwordInput.type === "password") {
        passwordInput.type = "text";
        togglePassword.textContent = "visibility";
    } else {
        passwordInput.type = "password";
        togglePassword.textContent = "visibility_off";
    }
});

termsLink.addEventListener('click', (e) => {
    e.preventDefault();
    modal.style.display = 'block';
});

closeModal.addEventListener('click', () => {
    modal.style.display = 'none';
});

acceptBtn.addEventListener('click', () => {
    checkbox.checked = true;
    modal.style.display = 'none';
});

rejectBtn.addEventListener('click', () => {
    checkbox.checked = false;
    modal.style.display = 'none';
});

window.addEventListener('click', (e) => {
    if (e.target === modal) {
        modal.style.display = 'none';
    }
});

passwordInput.addEventListener("input", () => {
    const password = passwordInput.value;

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
});

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

function generateOTP() {
    return Math.floor(100000 + Math.random() * 900000);
}

async function sendOtpEmail(email) {
    generatedOTP = generateOTP();
    try {
        await emailjs.send(SERVICE_ID, TEMPLATE_ID, {
            to_email: email,
            passcode: generatedOTP,
        });
        return true;
    } catch (error) {
        console.error("Failed to send OTP:", error);
        return false;
    }
}

registerForm.addEventListener('submit', async function (event) {
    event.preventDefault();

    if (modal.style.display === 'block') {
        alert("Please accept or reject the Terms and Conditions first.");
        return;
    }

    const password = document.getElementById("password").value;
    const confirm = document.getElementById("confirmPassword").value;

    if (password !== confirm) {
        alert("Passwords do not match!");
        return;
    }

    if (checkPasswordStrength(password) !== "strong") {
        alert("Password is not strong enough. Please follow the requirements.");
        return;
    }

    const firstname = document.getElementById("firstname").value;
    const lastname = document.getElementById("lastname").value;
    const middlename = document.getElementById("middlename").value;
    const username = document.getElementById("username").value;
    const email = document.getElementById("email").value;
    const contact = document.getElementById("contact").value;

    // --- NEW VALIDATION START ---
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    if (!emailRegex.test(email)) {
        alert("Please enter a valid email address (e.g., user@example.com).");
        return;
    }

    const phoneRegex = /^09\d{9}$/;
    if (!phoneRegex.test(contact)) {
        alert("Please enter a valid mobile number (must be 11 digits and start with '09').");
        return;
    }
    // --- NEW VALIDATION END ---

    userDataStorage = {
        FirstName: firstname,
        LastName: lastname,
        MiddleName: middlename,
        Username: username,
        Email: email,
        Contact: contact,
        Password: password, // Note: DTO might expect 'Password' or 'Userpassword' depending on DTO
        IsActive: true
    };

    if (await sendOtpEmail(email)) {
        alert("OTP sent to your email. Please proceed to verification.");
        registerContainer.style.display = "none";
        otpContainer.style.display = "block";
        document.getElementById("otp").focus();
    } else {
        alert("Failed to send OTP. Please check your email address and try again.");
    }
});

resendOtpBtn.addEventListener('click', async function () {
    if (!userDataStorage.email) {
        alert("Please complete the registration form first.");
        return;
    }

    resendOtpBtn.disabled = true;
    let countdown = 60;
    resendOtpBtn.textContent = `Resend in ${countdown}s`;

    const timer = setInterval(() => {
        countdown--;
        resendOtpBtn.textContent = `Resend in ${countdown}s`;

        if (countdown <= 0) {
            clearInterval(timer);
            resendOtpBtn.disabled = false;
            resendOtpBtn.textContent = "Resend OTP";
        }
    }, 1000);

    if (await sendOtpEmail(userDataStorage.email)) {
        alert("New OTP sent to your email.");
    } else {
        alert("Failed to resend OTP. Try again later.");
        clearInterval(timer);
        resendOtpBtn.disabled = false;
        resendOtpBtn.textContent = "Resend OTP";
    }
});

otpForm.addEventListener('submit', async function (event) {
    event.preventDefault();

    const otpInput = document.getElementById("otp").value;

    if (otpInput == generatedOTP) {
        try {
            const response = await fetch("/api/Register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(userDataStorage)
            });

            if (!response.ok) {
                const err = await response.json();
                throw err;
            }

            await response.json();
            alert("Registration successful! You can now log in.");
            window.location.href = "login.html";

        } catch (err) {
            console.error("Server error:", err);
            alert("Error saving to database: " + (err.error || JSON.stringify(err)));
        }
    } else {
        alert("Incorrect OTP. Please try again.");
    }
});