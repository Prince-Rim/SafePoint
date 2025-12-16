const apiKey = "9ac93f3566cf94568d6594a62f9b4924";

async function fetchWeather(lat, lon) {
    const url = `https://api.openweathermap.org/data/2.5/weather?lat=${lat}&lon=${lon}&appid=${apiKey}&units=metric`;

    try {
        const response = await fetch(url);
        const data = await response.json();

        document.getElementById("temperature").textContent = `${Math.round(data.main.temp)} °C`;
        document.getElementById("description").textContent = data.weather[0].description
            .replace(/\b\w/g, c => c.toUpperCase());
        document.getElementById("humidity").textContent = `${data.main.humidity}%`;
        document.getElementById("wind").textContent = `${data.wind.speed} m/s`;

        fetchAddress(lat, lon);

        const icon = document.getElementById("weatherIcon");
        const condition = data.weather[0].main.toLowerCase();

        if (condition.includes("cloud")) icon.textContent = "wb_cloudy";
        else if (condition.includes("rain")) icon.textContent = "umbrella";
        else if (condition.includes("clear")) icon.textContent = "wb_sunny";
        else if (condition.includes("storm")) icon.textContent = "flash_on";
        else icon.textContent = "wb_twilight";

    } catch (error) {
        document.getElementById("description").textContent = "Unable to load weather data.";
        console.error("Error fetching weather:", error);
    }
}

async function fetchAddress(lat, lon) {
    try {
        const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lon}`);
        const data = await res.json();
        const address = data.address;
        const road = address.road || address.neighbourhood || "Unknown street";
        const city = address.city || address.town || address.village || "Unknown city";

        document.getElementById("coords").innerHTML = `<b>Location:</b> ${road}, ${city}`;
        document.getElementById("city").textContent = city;
    } catch (error) {
        document.getElementById("coords").innerHTML = "<b>Location:</b> Unable to retrieve address.";
        console.error("Error fetching address:", error);
    }
}

function getUserLocation() {
    if (navigator.geolocation) {
        document.getElementById("description").textContent = "Getting your current location...";

        navigator.geolocation.getCurrentPosition(
            position => {
                const lat = position.coords.latitude;
                const lon = position.coords.longitude;
                fetchWeather(lat, lon);
            },
            error => {
                document.getElementById("description").textContent = "Location access denied. Showing Manila weather.";
                document.getElementById("coords").innerHTML = "<b>Location:</b> Default (Manila)";
                fetchWeather(14.5995, 120.9842);
            }
        );
    } else {
        document.getElementById("description").textContent = "Geolocation not supported. Showing Manila weather.";
        document.getElementById("coords").innerHTML = "<b>Location:</b> Default (Manila)";
        fetchWeather(14.5995, 120.9842);
    }
}


document.addEventListener('DOMContentLoaded', () => {

    getUserLocation();
});