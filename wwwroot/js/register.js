const PUBLIC_KEY = "q_WaGpOvP6-Rea8xD";
const SERVICE_ID = "service_0yfgbor";
const TEMPLATE_ID = "template_9we53op";

emailjs.init(PUBLIC_KEY);

let generatedOTP = null;

const form = document.getElementById("registerForm");
const otpGroup = document.getElementById("otp-group");
const registerBtn = document.getElementById("registerBtn");

const modal = document.getElementById('termsModal');
const termsLink = document.getElementById('termsLink');
const closeModal = modal.querySelector('.close');
const acceptBtn = document.getElementById('acceptTerms');
const rejectBtn = document.getElementById('rejectTerms');
const checkbox = document.getElementById('terms');

const passwordInput = document.getElementById("password");
const togglePassword = document.getElementById("togglePassword");
const strengthMsg = document.getElementById("passwordStrength");
const requirementsMsg = document.getElementById("passwordRequirements");

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

form.addEventListener('submit', async function (event) {
    event.preventDefault();

    if (modal.style.display === 'block') {
        alert("Please accept or reject the Terms and Conditions first.");
        return;
    }

    const username = document.getElementById("username").value;
    const email = document.getElementById("email").value;
    const contact = document.getElementById("contact").value;
    const password = passwordInput.value;
    const confirm = document.getElementById("confirmPassword").value;
    const otpInput = document.getElementById("otp").value;

    if (password !== confirm) {
        alert("Passwords do not match!");
        return;
    }

    if (checkPasswordStrength(password) !== "strong") {
        alert("Password is not strong enough. Please follow the requirements.");
        return;
    }

    if (otpGroup.style.display === "block") {
        if (otpInput == generatedOTP) {

            const userData = {
                username,
                email,
                contact,
                userpassword: password,
                isActive: true
            };

            try {
                const response = await fetch("/api/Register", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(userData)
                });

                if (!response.ok) {
                    const err = await response.json();
                    throw err;
                }

                await response.json();
                window.location.href = "login.html";

            } catch (err) {
                console.error("Server error:", err);
                alert("Error saving to database: " + (err.error || JSON.stringify(err)));
            }

        } else {
            alert("Incorrect OTP. Please try again.");
        }
        return;
    }

    generatedOTP = generateOTP();
    try {
        await emailjs.send(SERVICE_ID, TEMPLATE_ID, {
            to_email: email,
            passcode: generatedOTP,
        });

        alert("OTP sent to your email. Please enter it below.");
        otpGroup.style.display = "block";
        registerBtn.textContent = "Verify OTP";

    } catch (error) {
        alert("Failed to send OTP. Try again.");
        console.error(error);
    }
});