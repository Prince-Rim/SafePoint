# SafePoint !

[![Framework: ASP.NET Core](https://img.shields.io/badge/Framework-ASP.NET%20Core%20(Razor)-512bd4)](https://dotnet.microsoft.com/en-us/apps/aspnet)
[![Database: SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)](https://www.microsoft.com/en-us/sql-server)
[![Map: Leaflet.js](https://img.shields.io/badge/Map-Leaflet.js-199900)](https://leafletjs.com/)

**SafePoint** is an Interactive Incident Map Reporting System designed to disseminate real-time information about road hazards and potential risks. Specifically targeting Quezon City civilians and commuters, it empowers the community to report and track incidents to ensure safer travel.

## 📍 Core Features

* **Interactive Incident Map:** Real-time visualization using **Leaflet.js** and **OpenStreetMap** with custom incident pins.
* **Crowdsourced Reporting:** Users can submit reports including specific locations, detailed descriptions, and severity levels.
* **Moderator Dashboard:** A "Human-in-the-Loop" validation system where admins verify reports before they are visible to the public.
* **Smart Map Filtering:** Filter incidents by type (e.g., road hazard, flood), severity level, or date.
* **Community Comment System:** Facilitates discussion and updates on specific incidents for better situational awareness.
* **External API Integrations:**
    * **Windy API:** Real-time weather overlays for hazard anticipation.
    * **EmailJS:** Secure OTP (One-Time Password) delivery for user verification.
    * **Formspree:** Automated contact and feedback handling.
    * * **Leaflet:** Mapping of the world for visual reporting, and viewing.

## 🛠 Tech Stack

| Layer | Technology |
| :--- | :--- |
| **Backend** | C# / ASP.NET Core (Razor Pages) |
| **Frontend** | HTML5, CSS3, JavaScript |
| **Mapping** | Leaflet.js / OpenStreetMap API |
| **Database** | Microsoft SQL Server |
| **APIs** | Windy API, EmailJS, Formspree |

## 🚀 Getting Started

### Prerequisites
* [.NET SDK](https://dotnet.microsoft.com/download) (Version 6.0 or higher)
* [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
* [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)

### Installation & Setup

1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/Prince-Rim/safepoint.git](https://github.com/Prince-Rim/safepoint.git)
    ```

2.  **Configure Database:**
    Update the connection string in `appsettings.json`:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=SafePointDB;Trusted_Connection=True;"
    }
    ```

3.  **Apply Migrations:**
    In the Visual Studio Package Manager Console, run:
    ```powershell
    Update-Database
    ```

4.  **Run the Application:**
    Press `F5` in Visual Studio or execute:
    ```bash
    dotnet run
    ```

## 🛡 Security & Moderation
SafePoint ensures data integrity through:
* **Identity Verification:** OTP-based registration via EmailJS.
* **Report Vetting:** All submissions are held in a pending state until approved by a moderator.
* **Data Persistence:** Secure storage of incident logs and user interaction history in SQL Server.

## 🤝 Contributing
1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/NewFeature`)
3. Commit your Changes (`git commit -m 'Add NewFeature'`)
4. Push to the Branch (`git push origin feature/NewFeature`)
5. Open a Pull Request

---
*Developed as part of an initiative to improve commuter safety in Quezon City.*
