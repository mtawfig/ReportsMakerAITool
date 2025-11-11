# Project Structure - Detailed Documentation

## Complete File Listing

### AIReportsTool.API (Web API Project)

#### Project Configuration
- **AIReportsTool.API.csproj**: Project file defining target framework (net9.0) and dependencies
- **Program.cs**: Entry point with service configuration, CORS setup, and middleware pipeline
- **appsettings.json**: API configuration (ports: 5100, 5101)
- **appsettings.Development.json**: Development-specific settings

#### Controllers (API Endpoints)
```
Controllers/
├── AppsController.cs
│   └── GET /api/apps
│       Returns: 5 hardcoded applications with bilingual names
│
├── RecentsController.cs
│   └── GET /api/recents
│       Returns: 10 recent request records
│
└── RequestController.cs
    └── POST /api/request
        Accepts: RequestPayloadModel
        Returns: RequestResponseModel
```

#### Models (Data Structures)
```
Models/
├── BaseResponseModel.cs      # Generic response wrapper
├── AppModel.cs               # Application data structure
├── RecentRequestModel.cs     # Recent request data structure
├── RequestPayloadModel.cs    # Request submission payload
└── RequestResponseModel.cs   # Request submission response
```

### AIReportsTool.Web (MVC Web Application)

#### Project Configuration
- **AIReportsTool.Web.csproj**: Project file defining target framework and HTTP client dependency
- **Program.cs**: Entry point with MVC configuration, HTTP client factory, and session setup
- **appsettings.json**: MVC configuration (ports: 5000, 5001) and API base URL
- **appsettings.Development.json**: Development-specific settings

#### Controllers (MVC Routing)
```
Controllers/
├── HomeController.cs
│   └── GET /Home/Index (default route)
│       Purpose: Displays main application interface
│       Features: Reads language cookie, sets ViewBag properties
│
├── LanguageController.cs
│   ├── GET /Language/Switch?lang=en|ar
│   └── POST /Language/Switch
│       Purpose: Sets language cookie and redirects
│       Cookie: 30-day expiration
│
├── ApplicationsController.cs
│   └── GET /Applications/GetApps
│       Purpose: Fetches applications from API
│       Returns: JSON response
│
├── RecentsController.cs
│   └── GET /Recents/GetRecents
│       Purpose: Fetches recent requests from API
│       Returns: JSON response
│
└── RequestController.cs
    └── POST /Request/Submit
        Purpose: Submits request to API
        Returns: JSON response
```

#### Models (View Models)
```
Models/
├── ApiResponseModel.cs       # Generic API response wrapper
├── AppModel.cs               # Application view model
├── RecentRequestModel.cs     # Recent request view model
├── RequestPayloadModel.cs    # Request submission view model
└── RequestResponseModel.cs   # Request submission response model
```

#### Views (Razor Templates)
```
Views/
├── _ViewStart.cshtml         # Sets default layout for all views
├── _ViewImports.cshtml       # Imports namespaces and tag helpers
│
├── Shared/
│   └── _Layout.cshtml        # Main layout template
│       Structure:
│       ├── Header (Title + Language Toggle)
│       ├── Main Container
│       │   ├── Sidebar (Home, Applications, Recents)
│       │   └── Main Content (RenderBody)
│       └── Scripts (site.js)
│
└── Home/
    └── Index.cshtml          # Home page view
        Components:
        ├── Welcome Section
        ├── Request Form
        │   ├── Subject Input
        │   ├── Request Textarea
        │   ├── Format Buttons
        │   └── Submit/Share Actions
        └── Response Container
```

#### Static Assets
```
wwwroot/
├── css/
│   └── site.css              # Complete stylesheet
│       Features:
│       ├── Reset and base styles
│       ├── Header styling
│       ├── Sidebar navigation
│       ├── Form components
│       ├── Button styles
│       ├── Share menu
│       ├── Response display
│       ├── RTL/LTR support
│       └── Responsive design
│
├── js/
│   └── site.js               # Client-side application logic
│       Functions:
│       ├── initializeApp()
│       ├── loadApplications()
│       ├── loadRecents()
│       ├── selectApplication()
│       ├── loadRecentRequest()
│       ├── handleSubmit()
│       ├── displayResponse()
│       ├── enableShareButton()
│       ├── handleShareAction()
│       ├── handleEmailShare()
│       ├── handleDownload()
│       ├── handlePrint()
│       └── showMessage()
│
└── icons/
    └── README.md             # Icon specifications and requirements
```

## Data Flow

### Loading Applications
1. Browser loads Home/Index page
2. site.js calls loadApplications()
3. AJAX GET request to /Applications/GetApps
4. ApplicationsController calls API /api/apps
5. API returns 5 hardcoded applications
6. MVC returns JSON to browser
7. JavaScript renders apps in sidebar

### Loading Recent Requests
1. Browser loads Home/Index page
2. site.js calls loadRecents()
3. AJAX GET request to /Recents/GetRecents
4. RecentsController calls API /api/recents
5. API returns 10 hardcoded recent requests
6. MVC returns JSON to browser
7. JavaScript renders recents in sidebar

### Submitting a Request
1. User fills form and clicks Submit
2. site.js validates input
3. AJAX POST request to /Request/Submit
4. RequestController calls API /api/request
5. API validates and processes request
6. API returns success response
7. MVC forwards response to browser
8. JavaScript displays response and enables Share

### Language Switching
1. User clicks EN or AR button
2. Browser navigates to /Language/Switch?lang=X
3. LanguageController sets Language cookie
4. Controller redirects to /Home/Index
5. HomeController reads cookie
6. ViewBag.Language and ViewBag.Direction set
7. Layout and views render in selected language

## Configuration Details

### API CORS Policy
```csharp
AllowMvcApp:
  - Origins: http://localhost:5000, https://localhost:5001
  - Methods: Any
  - Headers: Any
  - Credentials: Allowed
```

### Port Configuration
- API HTTP: 5100
- API HTTPS: 5101
- MVC HTTP: 5000
- MVC HTTPS: 5001

### API Base URL
MVC app configured to call API at: http://localhost:5100

## Security Placeholders

### API Controllers
All controllers contain TODO comments:
```csharp
// TODO: Add authentication/authorization validation here when implementing security
```

### Request Controller
Additional TODO for token validation:
```csharp
// TODO: Validate token when implementing security
```

### Future Security Implementation
1. Add JWT token authentication
2. Implement user authentication
3. Add authorization policies
4. Validate tokens in API requests
5. Add rate limiting
6. Implement HTTPS enforcement in production

## Styling Details

### Color Scheme
- Primary: #667eea (Purple)
- Secondary: #764ba2 (Dark Purple)
- Background: #f5f5f5 (Light Gray)
- Text: #333 (Dark Gray)
- Border: #e0e0e0 (Light Gray)

### Typography
- Font Family: System fonts (-apple-system, BlinkMacSystemFont, Segoe UI, etc.)
- Base Font Size: 16px
- Line Height: 1.6

### Responsive Breakpoints
- Desktop: > 768px (Sidebar + Content side-by-side)
- Mobile: ≤ 768px (Sidebar above Content, stacked layout)

## JavaScript State Management

### Global Variables
- `selectedAppId`: Currently selected application ID
- `selectedAppName`: Currently selected application name
- `selectedFormat`: Selected response format (tables/charts/report/email)
- `lastResponseText`: Last response text for sharing

### Event Listeners
- Format button clicks
- Submit button click
- Share button click
- Share option clicks
- Outside click for share menu
- Application link clicks
- Recent request link clicks

## Hardcoded Data

### Applications (5 items)
1. Sales Analyzer (محلل المبيعات)
2. HR Insights (تحليلات الموارد البشرية)
3. Finance Tracker (متتبع المالية)
4. Inventory Control (مراقبة المخزون)
5. Customer Reports (تقارير العملاء)

### Recent Requests (10 items)
Each contains:
- DateTime stamp
- AppId reference
- Subject and Details
- Status (all = 1)
- Response text

## Extension Points

### Database Integration
1. Replace hardcoded data with database queries
2. Add Entity Framework Core
3. Create database context and entities
4. Implement repository pattern

### AI Integration
1. Add AI service interface
2. Implement actual report generation
3. Add async processing with queues
4. Store generated reports

### User Management
1. Add Identity framework
2. Implement user registration/login
3. Store user preferences
4. Track user-specific requests

### Additional Features
1. Real-time notifications (SignalR)
2. File upload capability
3. Advanced filtering and search
4. Export to multiple formats
5. Scheduled report generation
6. Email notifications
