const username = localStorage.getItem("username");
const userRole = localStorage.getItem("userRole");
const userBtn = document.getElementById("userBtn");
const userDropdown = document.querySelector(".user-dropdown");
const dashboardLink = document.getElementById("dashboardLink");
const myReportsLink = document.querySelector('a[href="mypost.html"]');
const hazardSelect = document.getElementById("hazard");
const otherHazardInput = document.getElementById("otherHazard");
const locationInput = document.getElementById("location");
const latitudeInput = document.getElementById("latitude");
const longitudeInput = document.getElementById("longitude");
const reportForm = document.querySelector('#reportForm');
const cancelButton = document.querySelector('.cancel');
const imageInput = document.getElementById("image");
const imagePreview = document.getElementById("imagePreview");
const uploadContent = document.getElementById("uploadContent");
const imageUploadArea = document.getElementById("imageUploadArea");

let selectedImageFile = null;
let locationWatchId = null;
let lastGeocodedPos = { lat: 0, lng: 0 };
let map = null;
let marker = null;
let userManuallySetLocation = false;

if (userRole === 'Admin' || userRole === 'Moderator') {
    if (dashboardLink) dashboardLink.style.display = 'inline-block';
}

if (hazardSelect) {
    hazardSelect.addEventListener('change', function () {
        if (this.value === 'other') {
            otherHazardInput.style.display = 'block';
            otherHazardInput.required = true;
        } else {
            otherHazardInput.style.display = 'none';
            otherHazardInput.required = false;
            otherHazardInput.value = '';
        }
    });
}

function displayImage(file) {
    if (file) {
        const reader = new FileReader();
        reader.onload = (e) => {
            imagePreview.src = e.target.result;
            imagePreview.style.display = "block";
            uploadContent.style.display = "none";
            imageUploadArea.style.padding = "10px";
            selectedImageFile = file;
        };
        reader.readAsDataURL(file);
    } else {
        imagePreview.style.display = "none";
        imagePreview.src = "#";
        uploadContent.style.display = "flex";
        imageUploadArea.style.padding = "0";
        selectedImageFile = null;
    }
}

imageInput.addEventListener("change", function () {
    const file = this.files[0];

    if (file) {
        displayImage(file);
    } else if (selectedImageFile) {
        console.log("File selection cancelled. Keeping previous image.");
    } else {
        displayImage(null);
    }
});


['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
    imageUploadArea.addEventListener(eventName, preventDefaults, false);
    document.body.addEventListener(eventName, preventDefaults, false);
});

function preventDefaults(e) {
    e.preventDefault();
    e.stopPropagation();
}

imageUploadArea.addEventListener('dragenter', highlight, false);
imageUploadArea.addEventListener('dragover', highlight, false);
imageUploadArea.addEventListener('dragleave', unhighlight, false);

function highlight() {
    imageUploadArea.classList.add('drag-over');
}

function unhighlight(e) {
    if (e.target === imageUploadArea) {
        imageUploadArea.classList.remove('drag-over');
    }
}

imageUploadArea.addEventListener('drop', handleDrop, false);

function handleDrop(e) {
    unhighlight(e);
    let dt = e.dataTransfer;
    let files = dt.files;
    const imageFile = Array.from(files).find(file => file.type.startsWith('image/'));

    if (imageFile) {
        displayImage(imageFile);
    } else {
        alert("Only image files are supported.");
    }
}

function loadLocation() {
    const storedLocationString = localStorage.getItem("incidentLocation");

    if (storedLocationString) {
        try {
            const storedLocation = JSON.parse(storedLocationString);
            if (storedLocation.address) locationInput.value = storedLocation.address;
            if (storedLocation.lat) latitudeInput.value = storedLocation.lat;
            if (storedLocation.lng) longitudeInput.value = storedLocation.lng;

            // If we have stored location, init map there and stop auto-watching
            userManuallySetLocation = true;
            initMap(storedLocation.lat, storedLocation.lng);
            return;
        } catch (e) {
            console.error("Error parsing stored location:", e);
        }
    }

    fetchCurrentLocation();
}

function initMap(lat, lng) {
    if (!map) {
        map = L.map('map').setView([lat, lng], 15);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap'
        }).addTo(map);

        marker = L.marker([lat, lng], { draggable: true }).addTo(map);

        marker.on('dragstart', () => {
            userManuallySetLocation = true;
            if (locationWatchId) navigator.geolocation.clearWatch(locationWatchId);
        });

        marker.on('dragend', function (e) {
            const position = marker.getLatLng();
            updateLocationFields(position.lat, position.lng);
        });
    } else {
        map.setView([lat, lng], 15);
        if (marker) marker.setLatLng([lat, lng]);
    }
}

function updateLocationFields(lat, lng) {
    latitudeInput.value = lat;
    longitudeInput.value = lng;

    // Loading indicator
    locationInput.placeholder = "Fetching address...";

    fetch(`https://nominatim.openstreetmap.org/reverse?lat=${lat}&lon=${lng}&format=json`)
        .then((response) => response.json())
        .then((data) => {
            const address = data.display_name || "Unknown location";
            locationInput.value = address;
        })
        .catch(() => {
            locationInput.value = "Unable to fetch address. Please type manually.";
        });
}

function fetchCurrentLocation() {
    if (!navigator.geolocation) {
        alert("Geolocation not supported by your browser.");
        // Fallback to default map view if no geo
        initMap(0, 0);
        return;
    }

    if (locationWatchId) navigator.geolocation.clearWatch(locationWatchId);

    locationInput.placeholder = "Locating...";

    locationWatchId = navigator.geolocation.watchPosition(
        (position) => {
            // STOP if user took control
            if (userManuallySetLocation) return;

            const { latitude, longitude } = position.coords;

            // Initialize map on first valid fix
            if (!map) {
                initMap(latitude, longitude);
            } else {
                // If map exists and user hasn't taken control, update pin
                if (marker) marker.setLatLng([latitude, longitude]);
                map.setView([latitude, longitude], 15);
            }

            latitudeInput.value = latitude;
            longitudeInput.value = longitude;

            // Throttle address lookup
            if (Math.abs(latitude - lastGeocodedPos.lat) > 0.0001 || Math.abs(longitude - lastGeocodedPos.lng) > 0.0001) {
                lastGeocodedPos = { lat: latitude, lng: longitude };

                // Only auto-fill text if empty or contains "Locating..." to respect manual typing
                // Actually, let's just always update it IF the user hasn't dragged the pin.
                fetch(`https://nominatim.openstreetmap.org/reverse?lat=${latitude}&lon=${longitude}&format=json`)
                    .then((response) => response.json())
                    .then((data) => {
                        if (!userManuallySetLocation) {
                            locationInput.value = data.display_name || "Unknown location";
                        }
                    })
                    .catch(() => {
                        if (!userManuallySetLocation) {
                            locationInput.value = "Location found (Address lookup failed)";
                        }
                    });
            }
        },
        (error) => {
            console.warn("Could not get location: " + error.message);
            if (!map) initMap(0, 0); // Init map anyway so they can drag to find themselves
            if (!latitudeInput.value) {
                locationInput.value = "";
                locationInput.placeholder = "Location Error. Please select on map.";
            }
        },
        { enableHighAccuracy: true, timeout: 20000, maximumAge: 0 }
    );
}

if (cancelButton) {
    cancelButton.addEventListener("click", () => {
        localStorage.removeItem("incidentLocation");
        sessionStorage.removeItem("incidentLocation");
    });
}

reportForm.addEventListener("submit", async function (e) {
    e.preventDefault();
    console.log("Submit event fired");


    localStorage.removeItem("incidentLocation");
    sessionStorage.removeItem("incidentLocation");

    const submitBtn = reportForm.querySelector('button[type="submit"]');
    if (!submitBtn) {
        console.error("Submit button not found");
        return;
    }

    const originalBtnText = submitBtn.textContent;


    submitBtn.disabled = true;
    submitBtn.textContent = "Submitting...";
    console.log("Button disabled");

    const userId = localStorage.getItem("userId");
    if (!userId) {
        alert("Authentication error: User ID is missing. Please log in.");
        submitBtn.disabled = false;
        submitBtn.textContent = originalBtnText;
        return;
    }

    const datetimeInput = document.getElementById("datetime");
    const datetimeValue = datetimeInput.value;

    if (!datetimeValue) {
        alert("Please select a valid date and time.");
        submitBtn.disabled = false;
        submitBtn.textContent = originalBtnText;
        return;
    }

    const baseDateTime = datetimeValue.trim();
    const formattedDatetime = baseDateTime.replace("T", " ") + ":00";

    if (!/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$/.test(formattedDatetime)) {
        alert("The date/time value format is invalid. Please re-select.");
        submitBtn.disabled = false;
        submitBtn.textContent = originalBtnText;
        return;
    }

    const formData = new FormData();

    formData.append("Userid", userId);
    formData.append("Title", document.getElementById("title").value);
    formData.append("Incident_Code", document.getElementById("hazard").value);
    formData.append("OtherHazard", document.getElementById("otherHazard").value);
    formData.append("Severity", document.getElementById("severity").value);
    formData.append("IncidentDateTime", formattedDatetime);
    formData.append(
        "Area_Code",
        document.getElementById("areaCode") ? document.getElementById("areaCode").value || "QC" : "QC"
    );
    formData.append("LocationAddress", document.getElementById("location").value);
    formData.append("Latitude", document.getElementById("latitude").value);
    formData.append("Longitude", document.getElementById("longitude").value);
    formData.append("Descr", document.getElementById("description").value);

    if (selectedImageFile) {
        formData.append("Img", selectedImageFile);
    }

    try {
        console.log("Sending fetch request");
        const response = await fetch("/api/Incidents", {
            method: "POST",
            body: formData
        });

        if (!response.ok) {
            const errorBody = await response.text();
            console.error("Server Response Error:", errorBody);

            let errorMessage = "Failed to submit incident.";
            try {
                const errorJson = JSON.parse(errorBody);
                errorMessage += " Details: " + (errorJson.message || errorJson.title || JSON.stringify(errorJson));
            } catch {
                errorMessage += " Status: " + response.status;
            }

            alert(errorMessage);
            submitBtn.disabled = false;
            submitBtn.textContent = originalBtnText;
            return;
        }

        alert("Incident submitted successfully!");
        window.location.href = "index.html";

    } catch (error) {
        console.error("Submission error:", error);
        alert("Error submitting incident.");
        submitBtn.disabled = false;
        submitBtn.textContent = originalBtnText;
    }
});

loadLocation();