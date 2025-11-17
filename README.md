# AI-Powered Reports Assistant Tool - ASP.NET Core 9 Solution

A complete full-stack application built with ASP.NET Core 9, featuring an MVC web application and a RESTful Web API.

## 🎯 Overview

This solution demonstrates a modern AI-powered reports tool with:
- **MVC Web Application** (AIReportsTool.Web) - User interface for report generation
- **Web API** (AIReportsTool.API) - Backend API with hardcoded data responses

## 🏗️ Solution Structure

```
AIReportsTool/
├── AIReportsTool.sln                 # Solution file
├── AIReportsTool.API/                # Web API Project
│   ├── Controllers/                  # API Controllers
│   │   ├── AppsController.cs        # Returns 5 applications
│   │   ├── RecentsController.cs     # Returns 10 recent requests
│   │   └── RequestController.cs     # Handles report requests
│   ├── Models/                       # Data models
│   ├── Program.cs                    # API entry point
│   ├── appsettings.json              # API configuration
│   └── AIReportsTool.API.csproj
│
└── AIReportsTool.Web/                # MVC Web Project
    ├── Controllers/                  # MVC Controllers
    ├── Models/                       # View models
    ├── Views/                        # Razor views
    ├── wwwroot/                      # Static files
    ├── Program.cs                    # MVC entry point
    ├── appsettings.json              # MVC configuration
    └── AIReportsTool.Web.csproj
```

## 🚀 Features

### MVC Web Application
- **Bilingual Support**: English and Arabic with RTL/LTR support
- **Cookie-based Language Switching**: Persists user language preference
- **Responsive Layout**: Header, sidebar, and main content area
- **Sidebar Navigation**: Home, Applications, Recent requests
- **Main Form**: Subject, request, format selection, submit/share
- **Share Functionality**: Email, Download, Print

### Web API
- **GET /api/apps**: Returns 5 hardcoded applications
- **GET /api/recents**: Returns 10 recent requests
- **POST /api/request**: Accepts and processes report requests

## 🔧 Configuration

### Ports
- **API**: http://localhost:5100, https://localhost:5101
- **MVC**: http://localhost:5000, https://localhost:5001

### CORS
The API is configured to accept requests from the MVC application.

## 📋 Requirements

- .NET 9.0 SDK
- Visual Studio 2022 or later (or VS Code with C# extension)
- Modern web browser

## 🏃 Running the Solution

### Option 1: Visual Studio
1. Open `AIReportsTool.sln` in Visual Studio
2. Right-click the solution → Properties
3. Set multiple startup projects:
   - AIReportsTool.API: Start
   - AIReportsTool.Web: Start
4. Press F5 to run

### Option 2: Command Line
```bash
# Terminal 1 - Run API
cd AIReportsTool.API
dotnet run

# Terminal 2 - Run MVC
cd AIReportsTool.Web
dotnet run
```

## 🌐 Accessing the Application

1. Navigate to **http://localhost:5000** in your browser
2. The MVC application will load with the main interface
3. The API runs on **http://localhost:5100** (backend)

## 🧪 Testing the Application

1. **Language Switching**: Click EN/AR buttons in header
2. **Select Application**: Click any app in the sidebar
3. **Load Recent Request**: Click any item in Recents section
4. **Submit Request**: Select app, enter details, click Submit
5. **Share**: After successful submission, use Share button

## 📝 API Endpoints

### GET /api/apps
Returns list of 5 applications with English and Arabic names.

### GET /api/recents
Returns list of 10 recent requests.

### POST /api/request
Submits a new report generation request.

## 🔒 Security Notes

**Current Implementation**: No authentication or authorization  
**Future Implementation**: TODO comments mark where security should be added

## 🎨 UI Features

- Responsive modern design with top navbar, sidebar, and content
- Clean form design
- Dropdown save file and print menu
- Success/error notifications
- Smooth animations
- Full RTL support for Arabic

## 📦 NuGet Packages

### AIReportsTool.API
- Microsoft.AspNetCore.OpenApi (9.0.0)
- Swashbuckle.AspNetCore (6.9.0)

### AIReportsTool.Web
- Microsoft.Extensions.Http (9.0.0)

## 🛠️ Development Notes

- API uses minimal hosting model
- MVC uses conventional routing
- Static files served from wwwroot
- Cookie-based language preference (30-day expiration)

## 👨‍💻 Author

Created by Mostafa Wahbi as a full-stack ASP.NET Core 9 project.
