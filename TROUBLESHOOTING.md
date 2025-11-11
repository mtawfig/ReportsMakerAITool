# Troubleshooting Guide - AI Reports Tool

## ✅ FIXED: 404 Errors on API Endpoints

### Root Cause
The API project was missing the `Properties/launchSettings.json` file, which is essential for configuring application URLs and ports.

### Solution
Added `launchSettings.json` files to both projects. This fix has been committed to the repository.

---

## Testing the API - Correct URLs

### Important: Route Case Sensitivity

The API uses the `[Route("api/[controller]")]` attribute, which means:
- `AppsController` → `/api/Apps` (capital A)
- `RecentsController` → `/api/Recents` (capital R)  
- `RequestController` → `/api/Request` (capital R)

**Note:** On Linux/macOS, routes are case-sensitive. On Windows, they are typically case-insensitive.

### ✅ Correct API Endpoints

#### 1. Get Applications
```bash
# HTTP
curl http://localhost:5100/api/Apps

# HTTPS (may require -k flag for self-signed cert)
curl -k https://localhost:5101/api/Apps
```

**Expected Response:**
```json
{
  "Status": 1,
  "Message": "Success",
  "Data": [
    {
      "AppId": 1,
      "AppNameEn": "Sales Analyzer",
      "AppNameAr": "محلل المبيعات",
      ...
    }
  ]
}
```

#### 2. Get Recent Requests
```bash
# HTTP
curl http://localhost:5100/api/Recents

# HTTPS
curl -k https://localhost:5101/api/Recents
```

#### 3. Submit Request
```bash
# HTTP
curl -X POST http://localhost:5100/api/Request \
  -H "Content-Type: application/json" \
  -d '{
    "Token": "",
    "Username": "testuser",
    "AppId": 1,
    "Subject": "Test Report",
    "RequestText": "Generate test report"
  }'

# HTTPS
curl -k -X POST https://localhost:5101/api/Request \
  -H "Content-Type: application/json" \
  -d '{
    "Token": "",
    "Username": "testuser",
    "AppId": 1,
    "Subject": "Test Report",
    "RequestText": "Generate test report"
  }'
```

---

## Using Swagger UI (Recommended for Testing)

The easiest way to test the API is through Swagger:

1. **Start the API:**
   ```bash
   cd AIReportsTool.API
   dotnet run
   ```

2. **Open Swagger UI in browser:**
   - HTTP: http://localhost:5100/swagger
   - HTTPS: https://localhost:5101/swagger

3. **Swagger will show all three endpoints:**
   - GET /api/Apps
   - GET /api/Recents
   - POST /api/Request

4. **Click "Try it out" to test each endpoint directly**

---

## Common Issues and Solutions

### Issue 1: "Failed to load API definition"
**Cause:** API not running or Swagger not configured

**Solution:**
1. Verify API is running: `dotnet run` in AIReportsTool.API folder
2. Check console output for "Now listening on: http://localhost:5100"
3. Wait a few seconds for the app to fully start

### Issue 2: SSL/Certificate Errors (HTTPS)
**Cause:** Self-signed development certificate not trusted

**Solution Option 1 - Use HTTP:**
```bash
# Use HTTP instead of HTTPS for testing
curl http://localhost:5100/api/Apps
```

**Solution Option 2 - Trust the certificate:**
```bash
dotnet dev-certs https --trust
```

**Solution Option 3 - Ignore certificate (curl):**
```bash
curl -k https://localhost:5101/api/Apps
```

### Issue 3: "This site can't be reached"
**Cause:** Application not running or port already in use

**Solution:**
1. Verify the app is running
2. Check console for errors
3. Verify port is not in use:
   ```bash
   # Linux/Mac
   lsof -i :5100
   
   # Windows
   netstat -ano | findstr :5100
   ```

### Issue 4: 404 Not Found
**Cause:** Incorrect URL or route

**Solution:**
Use the correct URLs with proper casing:
- ✅ `/api/Apps` (correct)
- ❌ `/api/apps` (may not work on Linux)
- ✅ `/api/Recents` (correct)
- ❌ `/api/recents` (may not work on Linux)

### Issue 5: CORS Errors in Browser
**Cause:** MVC app trying to call API from different origin

**Solution:**
1. Verify API CORS policy in Program.cs includes MVC origin
2. Ensure API is running before MVC app makes requests
3. Check browser console for detailed CORS error message

Current CORS policy allows:
- http://localhost:5000
- https://localhost:5001

### Issue 6: Port Already in Use
**Error:** `Failed to bind to address http://localhost:5100: address already in use`

**Solution:**
```bash
# Find and kill process using the port
# Linux/Mac:
lsof -ti:5100 | xargs kill -9

# Windows:
netstat -ano | findstr :5100
taskkill /PID <PID> /F
```

Or change ports in both:
- `appsettings.json`
- `Properties/launchSettings.json`

---

## Verifying the Fix

### Step 1: Clean and Rebuild
```bash
cd AIReportsTool.API
dotnet clean
dotnet build
```

Look for "Build succeeded" message.

### Step 2: Run the API
```bash
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5100
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5101
```

### Step 3: Test Swagger
Open browser: http://localhost:5100/swagger

You should see:
- Swagger UI with API documentation
- Three endpoints listed
- "Authorize" button at the top (not used yet)

### Step 4: Test an Endpoint
In Swagger UI:
1. Click on "GET /api/Apps"
2. Click "Try it out"
3. Click "Execute"
4. Verify response shows Status: 1 and 5 applications

### Step 5: Test with curl
```bash
curl http://localhost:5100/api/Apps
```

Should return JSON with 5 applications.

---

## Testing from Postman or Thunder Client

### Setup
1. Create new request
2. Method: GET
3. URL: `http://localhost:5100/api/Apps`
4. Headers: (none required)
5. Send

### Expected Result
- Status: 200 OK
- Body: JSON with Status: 1 and array of 5 apps

### For POST Request (Submit Report)
1. Method: POST
2. URL: `http://localhost:5100/api/Request`
3. Headers: `Content-Type: application/json`
4. Body (raw JSON):
```json
{
  "Token": "",
  "Username": "testuser",
  "AppId": 1,
  "Subject": "Test Report",
  "RequestText": "Generate a sample report"
}
```

---

## Browser Testing

### Test GET Endpoint Directly
Simply paste in browser address bar:
```
http://localhost:5100/api/Apps
```

Browser should display JSON response.

### Test from MVC App
1. Start API: `cd AIReportsTool.API && dotnet run`
2. Start MVC: `cd AIReportsTool.Web && dotnet run`
3. Open browser: http://localhost:5000
4. Open browser console (F12)
5. Watch for API calls in Network tab
6. Applications should load in sidebar

If sidebar shows "Loading..." instead of apps:
- Check browser console for errors
- Verify API is running
- Check CORS configuration

---

## Visual Studio Testing

### Running Both Projects
1. Open AIReportsTool.sln
2. Right-click solution → Properties
3. Set "Multiple startup projects"
4. Set both projects to "Start"
5. Press F5

### Viewing API Output
- Look for console window titled "AIReportsTool.API"
- Should show "Now listening on..." messages

### Testing in Browser
- Browser will open to http://localhost:5000 (MVC)
- Manually navigate to http://localhost:5100/swagger (API)

---

## Debugging Tips

### Enable Detailed Errors
In API Program.cs, add:
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
```

### Check Application is Listening
```bash
# Linux/Mac
netstat -tuln | grep 5100

# Windows
netstat -an | findstr 5100
```

### View All Routes
Add this temporarily in Program.cs before `app.Run()`:
```csharp
var endpoints = app.Services.GetRequiredService<IEndpointRouteBuilder>()
    .DataSources
    .SelectMany(ds => ds.Endpoints)
    .OfType<RouteEndpoint>();

foreach (var endpoint in endpoints)
{
    Console.WriteLine($"Route: {endpoint.RoutePattern.RawText}");
}
```

---

## Quick Reference Card

### API URLs (Correct Case)
```
GET  http://localhost:5100/api/Apps
GET  http://localhost:5100/api/Recents
POST http://localhost:5100/api/Request
```

### Swagger UI
```
http://localhost:5100/swagger
```

### MVC Web App
```
http://localhost:5000
```

### Running from Command Line
```bash
# Terminal 1 - API
cd AIReportsTool.API && dotnet run

# Terminal 2 - Web
cd AIReportsTool.Web && dotnet run
```

---

## Still Having Issues?

### Check These Files Exist
- ✅ `AIReportsTool.API/Properties/launchSettings.json`
- ✅ `AIReportsTool.Web/Properties/launchSettings.json`
- ✅ `AIReportsTool.API/Controllers/AppsController.cs`
- ✅ `AIReportsTool.API/Controllers/RecentsController.cs`
- ✅ `AIReportsTool.API/Controllers/RequestController.cs`

### Verify Project Build
```bash
cd AIReportsTool.API
dotnet build
# Should see: Build succeeded
```

### Check for Typos in URLs
Common mistakes:
- Using lowercase: `api/apps` instead of `api/Apps`
- Missing http://: `localhost:5100` instead of `http://localhost:5100`
- Wrong port: `5000` instead of `5100`

---

## Success Checklist

- [ ] launchSettings.json files exist in both Properties folders
- [ ] `dotnet build` succeeds without errors
- [ ] API runs and shows "Now listening on..." messages
- [ ] Swagger UI loads at http://localhost:5100/swagger
- [ ] GET /api/Apps returns JSON with 5 applications
- [ ] MVC app can fetch applications (sidebar populates)
- [ ] No CORS errors in browser console

If all checked, the API is working correctly!
