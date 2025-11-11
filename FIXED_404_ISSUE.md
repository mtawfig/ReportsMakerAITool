# ✅ FIXED: 404 Error Issues

## What Was Wrong

You were experiencing 404 errors when calling the API endpoints. There were **two critical issues**:

### Issue 1: Missing launchSettings.json Files
**Problem:** Both projects were missing `Properties/launchSettings.json` files, which are essential for configuring application URLs and ports in ASP.NET Core.

**Fix:** Added `Properties/launchSettings.json` to both projects with proper port configuration.

### Issue 2: Case-Sensitive Route Mismatch (Linux)
**Problem:** The MVC app was calling API endpoints with lowercase URLs:
- `/api/apps` ❌
- `/api/recents` ❌
- `/api/request` ❌

But the API controllers use the `[Route("api/[controller]")]` attribute, which creates routes with **capital letters**:
- `/api/Apps` ✅
- `/api/Recents` ✅
- `/api/Request` ✅

**On Linux (your environment), ASP.NET Core routes are case-sensitive!**

**Fix:** Updated all three MVC controllers to use proper casing.

---

## What Was Fixed

### Files Added:
1. ✅ `AIReportsTool.API/Properties/launchSettings.json`
2. ✅ `AIReportsTool.Web/Properties/launchSettings.json`
3. ✅ `TROUBLESHOOTING.md` (comprehensive guide)

### Files Modified:
1. ✅ `AIReportsTool.Web/Controllers/ApplicationsController.cs`
   - Line 33: Changed `/api/apps` → `/api/Apps`

2. ✅ `AIReportsTool.Web/Controllers/RecentsController.cs`
   - Line 33: Changed `/api/recents` → `/api/Recents`

3. ✅ `AIReportsTool.Web/Controllers/RequestController.cs`
   - Line 53: Changed `/api/request` → `/api/Request`

---

## How to Test

### 1. Pull the Latest Changes
```bash
git pull
```

### 2. Rebuild Both Projects
```bash
# Clean and rebuild API
cd AIReportsTool.API
dotnet clean
dotnet build

# Clean and rebuild Web
cd ../AIReportsTool.Web
dotnet clean
dotnet build
```

### 3. Test the API Directly

#### Start the API:
```bash
cd AIReportsTool.API
dotnet run
```

Wait for:
```
Now listening on: http://localhost:5100
Now listening on: https://localhost:5101
```

#### Test with Swagger (Easiest):
Open browser: **http://localhost:5100/swagger**

You should see three endpoints. Click "Try it out" on each to test.

#### Test with curl:
```bash
# Test Apps endpoint
curl http://localhost:5100/api/Apps

# Expected: JSON with Status: 1 and array of 5 applications
```

```bash
# Test Recents endpoint
curl http://localhost:5100/api/Recents

# Expected: JSON with Status: 1 and array of 10 recent requests
```

```bash
# Test Request endpoint
curl -X POST http://localhost:5100/api/Request \
  -H "Content-Type: application/json" \
  -d '{
    "Token": "",
    "Username": "testuser",
    "AppId": 1,
    "Subject": "Test Report",
    "RequestText": "Generate test report"
  }'

# Expected: JSON with Status: 1 and success message
```

### 4. Test the Full Application

#### Terminal 1 - Start API:
```bash
cd AIReportsTool.API
dotnet run
```

#### Terminal 2 - Start MVC:
```bash
cd AIReportsTool.Web
dotnet run
```

#### Open Browser:
Navigate to: **http://localhost:5000**

**Expected Results:**
- ✅ Page loads with "AI Reports Tool" header
- ✅ Language toggle (EN/AR) works
- ✅ Sidebar "Applications" section shows 5 apps (not "Loading...")
- ✅ Sidebar "Recents" section shows 10 recent items (not "Loading...")
- ✅ Click on an app → Success message appears
- ✅ Click on a recent item → Form populates with data
- ✅ Fill form and Submit → Response appears
- ✅ Share button becomes enabled

---

## Correct API URLs Reference

### GET Endpoints:
```
http://localhost:5100/api/Apps        ✅ Correct (capital A)
http://localhost:5100/api/Recents     ✅ Correct (capital R)
```

### POST Endpoint:
```
http://localhost:5100/api/Request     ✅ Correct (capital R)
```

### ❌ These will NOT work on Linux:
```
http://localhost:5100/api/apps        ❌ Wrong (lowercase)
http://localhost:5100/api/recents     ❌ Wrong (lowercase)
http://localhost:5100/api/request     ❌ Wrong (lowercase)
```

---

## Visual Studio Testing

If using Visual Studio:

1. **Pull latest changes** from git
2. **Rebuild solution** (right-click solution → Rebuild)
3. **Set multiple startup projects**:
   - Right-click solution → Properties
   - Set both AIReportsTool.API and AIReportsTool.Web to "Start"
4. **Press F5** to run
5. **Both console windows** should appear
6. **Browser opens** to http://localhost:5000
7. **Manually navigate** to http://localhost:5100/swagger to test API

---

## Verification Checklist

After pulling and rebuilding, verify:

- [ ] File exists: `AIReportsTool.API/Properties/launchSettings.json`
- [ ] File exists: `AIReportsTool.Web/Properties/launchSettings.json`
- [ ] API builds without errors
- [ ] Web builds without errors
- [ ] API starts and shows listening messages
- [ ] Swagger UI loads at http://localhost:5100/swagger
- [ ] GET /api/Apps returns 5 applications
- [ ] GET /api/Recents returns 10 items
- [ ] POST /api/Request returns success
- [ ] MVC app loads at http://localhost:5000
- [ ] Sidebar shows applications (not "Loading...")
- [ ] Sidebar shows recents (not "Loading...")
- [ ] No 404 errors in browser console

---

## Still Getting 404?

If you're still getting 404 errors after applying these fixes:

1. **Verify you pulled the latest changes:**
   ```bash
   git status
   # Should show: Your branch is up to date
   ```

2. **Check the URL you're using:**
   - ✅ Use: `http://localhost:5100/api/Apps`
   - ❌ Don't use: `http://localhost:5100/api/apps`

3. **Verify API is running:**
   ```bash
   curl http://localhost:5100/swagger/index.html
   # Should return HTML (Swagger UI)
   ```

4. **Check console output:**
   - Look for "Now listening on: http://localhost:5100"
   - Look for any error messages

5. **Review the comprehensive guide:**
   - See `TROUBLESHOOTING.md` for detailed debugging steps

---

## Summary

**Root Causes:**
1. Missing launchSettings.json files (prevents proper URL binding)
2. Case-sensitive routes on Linux (API expects capital letters)

**Solutions:**
1. Added launchSettings.json to both projects
2. Fixed all API calls to use proper casing (/api/Apps, /api/Recents, /api/Request)

**Result:**
All API endpoints should now work correctly on both HTTP and HTTPS!

---

## Need More Help?

See these files for additional information:
- `TROUBLESHOOTING.md` - Comprehensive troubleshooting guide
- `QUICKSTART.md` - Step-by-step setup guide
- `API_DOCUMENTATION.md` - Complete API reference
- `README.md` - Project overview

All fixes have been committed to the git repository on branch:
`claude/aspnet-core-9-full-stack-011CV2AC4C1syRF8q4XDeeYo`
