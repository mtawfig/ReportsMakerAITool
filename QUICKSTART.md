# Quick Start Guide - AI Reports Tool

## Prerequisites

Before you begin, ensure you have:
- ✅ .NET 9.0 SDK installed
- ✅ Visual Studio 2022 (or VS Code with C# extension)
- ✅ Modern web browser (Chrome, Firefox, Edge, or Safari)

## Installation Steps

### Step 1: Verify .NET Installation
```bash
dotnet --version
# Should output: 9.0.x
```

If not installed, download from: https://dotnet.microsoft.com/download/dotnet/9.0

### Step 2: Navigate to Solution Directory
```bash
cd /path/to/AIReportsTool
```

### Step 3: Restore Dependencies
```bash
# Restore all NuGet packages
dotnet restore
```

## Running the Application

### Method 1: Using Visual Studio (Recommended)

#### Step 1: Open Solution
1. Double-click `AIReportsTool.sln`
2. Visual Studio will open with both projects loaded

#### Step 2: Configure Multiple Startup Projects
1. Right-click on the solution in Solution Explorer
2. Select "Properties" or "Set Startup Projects"
3. Choose "Multiple startup projects"
4. Set both projects to "Start":
   - ✅ AIReportsTool.API → Start
   - ✅ AIReportsTool.Web → Start
5. Click "OK"

#### Step 3: Run the Application
1. Press **F5** (or click the Start button)
2. Two console windows will open:
   - API: http://localhost:5100
   - Web: http://localhost:5000
3. Your default browser will open automatically

### Method 2: Using Command Line

#### Step 1: Open Two Terminal Windows

**Terminal 1 - Start the API:**
```bash
cd AIReportsTool.API
dotnet run
```

Wait for the message:
```
Now listening on: http://localhost:5100
Now listening on: https://localhost:5101
```

**Terminal 2 - Start the MVC App:**
```bash
cd AIReportsTool.Web
dotnet run
```

Wait for the message:
```
Now listening on: http://localhost:5000
Now listening on: https://localhost:5001
```

#### Step 2: Open Browser
Navigate to: **http://localhost:5000**

### Method 3: Using Hot Reload (Development)

For automatic reloading during development:

**Terminal 1:**
```bash
cd AIReportsTool.API
dotnet watch run
```

**Terminal 2:**
```bash
cd AIReportsTool.Web
dotnet watch run
```

## Testing the Application

### Test 1: Verify API is Running
Open browser and navigate to:
```
http://localhost:5100/swagger
```

You should see the Swagger UI with three endpoints:
- GET /api/apps
- GET /api/recents
- POST /api/request

### Test 2: Test API Endpoints Manually

#### Get Applications:
```bash
curl http://localhost:5100/api/apps
```

Expected response: JSON with 5 applications

#### Get Recent Requests:
```bash
curl http://localhost:5100/api/recents
```

Expected response: JSON with 10 recent requests

#### Submit Request:
```bash
curl -X POST http://localhost:5100/api/request \
  -H "Content-Type: application/json" \
  -d '{
    "Token": "",
    "Username": "testuser",
    "AppId": 1,
    "Subject": "Test Report",
    "RequestText": "Generate a test report"
  }'
```

Expected response: Success message with status 1

### Test 3: Test MVC Application

#### Test Language Switching:
1. Open: http://localhost:5000
2. Click the "AR" button in header
3. Page should reload with Arabic text and RTL layout
4. Click "EN" to switch back

#### Test Application Selection:
1. Look at the "Applications" section in sidebar
2. Click on "Sales Analyzer"
3. You should see a success message
4. The application is now selected

#### Test Recent Requests:
1. Look at the "Recents" section in sidebar
2. Click on "Monthly Sales Report"
3. The form should populate with:
   - Subject: "Monthly Sales Report"
   - Request: "Generate Q1 summary..."

#### Test Form Submission:
1. Select an application (if not already selected)
2. Enter a subject (e.g., "Test Report")
3. Enter request text (e.g., "Generate a sample report")
4. Click one of the format buttons (Tables, Charts, Report, Email)
5. Click "Submit"
6. You should see:
   - A response displayed below the form
   - The "Share" button becomes enabled

#### Test Share Functionality:
1. After successful submission, click "Share"
2. A dropdown menu appears with three options:
   - Send Email
   - Download File
   - Print File
3. Click "Download File"
4. A text file should download with the response

## Common Issues and Solutions

### Issue 1: Port Already in Use
**Error:** `Failed to bind to address http://localhost:5000: address already in use`

**Solution:**
```bash
# Windows
netstat -ano | findstr :5000
taskkill /F /PID <PID>

# Linux/Mac
lsof -i :5000
kill -9 <PID>
```

Or change ports in appsettings.json

### Issue 2: CORS Error in Browser Console
**Error:** `Access to fetch at 'http://localhost:5100/api/apps' from origin 'http://localhost:5000' has been blocked by CORS policy`

**Solution:**
1. Verify API is running
2. Check CORS configuration in API Program.cs
3. Ensure MVC app origin is listed in AllowedOrigins

### Issue 3: API Not Responding
**Error:** Browser shows "Cannot GET /api/apps"

**Solution:**
1. Verify API is running on port 5100
2. Check console for API startup errors
3. Verify controllers are in correct namespace
4. Run `dotnet restore` in API project

### Issue 4: MVC Can't Reach API
**Error:** Console shows network errors when fetching data

**Solution:**
1. Verify both API and MVC are running
2. Check ApiSettings:BaseUrl in MVC appsettings.json
3. Ensure it points to http://localhost:5100

### Issue 5: CSS/JS Not Loading
**Error:** Page appears unstyled

**Solution:**
1. Verify wwwroot folder exists
2. Check browser console for 404 errors
3. Verify static files middleware is enabled
4. Clear browser cache (Ctrl+Shift+R)

## Verification Checklist

After starting the application, verify:

- [ ] API is running on port 5100
- [ ] MVC is running on port 5000
- [ ] Browser opens to http://localhost:5000
- [ ] Page displays "AI Reports Tool" header
- [ ] Language toggle buttons (EN/AR) are visible
- [ ] Sidebar shows three sections (Home, Applications, Recents)
- [ ] Applications list loads (5 items)
- [ ] Recents list loads (10 items)
- [ ] Form has Subject input, Request textarea, Format buttons
- [ ] Submit button is enabled
- [ ] Share button is disabled (until submission)
- [ ] Browser console has no errors

## Development Tips

### Hot Reload
Use `dotnet watch run` to automatically reload on file changes

### Swagger UI
Access API documentation at: http://localhost:5100/swagger

### Browser DevTools
- F12: Open developer tools
- Network tab: Monitor API calls
- Console tab: View JavaScript errors
- Application tab: Inspect cookies

### Debugging in Visual Studio
1. Set breakpoints in controllers or JavaScript
2. Press F5 to start with debugging
3. Step through code with F10/F11

### Testing Different Scenarios
1. Test with different applications
2. Test with different languages
3. Test form validation (empty fields)
4. Test all share options
5. Test on different browsers

## Next Steps

After successfully running the application:

1. **Explore the Code:**
   - Review controller implementations
   - Examine Razor views
   - Study JavaScript interactions

2. **Customize:**
   - Change color scheme in CSS
   - Add new applications to hardcoded data
   - Modify layout structure

3. **Extend:**
   - Add database integration
   - Implement real AI processing
   - Add user authentication
   - Create additional views

4. **Deploy:**
   - Configure for production
   - Set up hosting (IIS, Azure, AWS)
   - Enable HTTPS
   - Configure environment variables

## Support

For issues or questions:
1. Check the main README.md
2. Review PROJECT_STRUCTURE.md
3. Examine TODO comments in code
4. Check .NET 9 documentation

## Summary

You should now have:
- ✅ Both projects running
- ✅ MVC app accessible at http://localhost:5000
- ✅ API accessible at http://localhost:5100
- ✅ Functional user interface
- ✅ Working API integration
- ✅ Language switching capability
- ✅ Form submission and response display
- ✅ Share functionality

Enjoy exploring the AI Reports Tool!
