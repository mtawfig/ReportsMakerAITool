# API Documentation - AIReportsTool.API

## Base URL
- Development: `http://localhost:5100`
- HTTPS: `https://localhost:5101`

## Authentication
Currently: **No authentication required**  
Future: JWT token-based authentication (see TODO comments in code)

## Response Format
All endpoints return JSON with consistent structure:

```json
{
  "Status": 1,        // 1 = success, 0 = error
  "Message": "...",   // Human-readable message
  "Data": { ... }     // Response data (varies by endpoint)
}
```

---

## Endpoints

### 1. Get Applications

Retrieves the list of available applications.

**Endpoint:** `GET /api/apps`

**Request:**
```
GET /api/apps HTTP/1.1
Host: localhost:5100
```

**Response:**
```json
{
  "Status": 1,
  "Message": "Success",
  "Data": [
    {
      "AppId": 1,
      "AppNameEn": "Sales Analyzer",
      "AppNameAr": "محلل المبيعات",
      "AppUrl": "https://app1.com",
      "AppIcon": "/icons/sales.png"
    },
    {
      "AppId": 2,
      "AppNameEn": "HR Insights",
      "AppNameAr": "تحليلات الموارد البشرية",
      "AppUrl": "https://app2.com",
      "AppIcon": "/icons/hr.png"
    },
    {
      "AppId": 3,
      "AppNameEn": "Finance Tracker",
      "AppNameAr": "متتبع المالية",
      "AppUrl": "https://app3.com",
      "AppIcon": "/icons/finance.png"
    },
    {
      "AppId": 4,
      "AppNameEn": "Inventory Control",
      "AppNameAr": "مراقبة المخزون",
      "AppUrl": "https://app4.com",
      "AppIcon": "/icons/inventory.png"
    },
    {
      "AppId": 5,
      "AppNameEn": "Customer Reports",
      "AppNameAr": "تقارير العملاء",
      "AppUrl": "https://app5.com",
      "AppIcon": "/icons/customers.png"
    }
  ]
}
```

**Response Fields:**
- `AppId` (integer): Unique application identifier
- `AppNameEn` (string): Application name in English
- `AppNameAr` (string): Application name in Arabic
- `AppUrl` (string): Application URL
- `AppIcon` (string): Path to application icon

**cURL Example:**
```bash
curl http://localhost:5100/api/apps
```

---

### 2. Get Recent Requests

Retrieves the 10 most recent report requests.

**Endpoint:** `GET /api/recents`

**Request:**
```
GET /api/recents HTTP/1.1
Host: localhost:5100
```

**Response:**
```json
{
  "Status": 1,
  "Message": "Success",
  "Data": [
    {
      "DateTime": "10112025123045",
      "AppId": 1,
      "AppNameEn": "Sales Analyzer",
      "AppNameAr": "محلل المبيعات",
      "Subject": "Monthly Sales Report",
      "Details": "Generate Q1 summary with charts and KPIs",
      "Status": 1,
      "Response": "Report generated successfully"
    },
    // ... 9 more items
  ]
}
```

**Response Fields:**
- `DateTime` (string): Timestamp in format DDMMYYYYHHmmss
- `AppId` (integer): Application ID reference
- `AppNameEn` (string): Application name in English
- `AppNameAr` (string): Application name in Arabic
- `Subject` (string): Request subject/title
- `Details` (string): Request details/description
- `Status` (integer): Request status (1 = completed)
- `Response` (string): Generated response text

**cURL Example:**
```bash
curl http://localhost:5100/api/recents
```

---

### 3. Submit Request

Submits a new report generation request.

**Endpoint:** `POST /api/request`

**Request Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "Token": "",
  "Username": "john.doe",
  "AppId": 1,
  "Subject": "Sales Performance Report",
  "RequestText": "Generate a detailed sales performance report for Q1 2025 including regional breakdowns and trend analysis"
}
```

**Request Fields:**
- `Token` (string): Authentication token (currently not validated)
- `Username` (string): User making the request
- `AppId` (integer): Target application ID (1-5)
- `Subject` (string): Request subject/title (required)
- `RequestText` (string): Detailed request description (required)

**Success Response:**
```json
{
  "Status": 1,
  "Message": "Success",
  "ResponseText": "Report generated successfully for 'Sales Performance Report'. Your request has been processed and the report is ready."
}
```

**Error Response (Invalid AppId):**
```json
{
  "Status": 0,
  "Message": "Invalid application ID",
  "ResponseText": ""
}
```

**Error Response (Missing Subject):**
```json
{
  "Status": 0,
  "Message": "Subject is required",
  "ResponseText": ""
}
```

**Error Response (Missing Request Text):**
```json
{
  "Status": 0,
  "Message": "Request text is required",
  "ResponseText": ""
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:5100/api/request \
  -H "Content-Type: application/json" \
  -d '{
    "Token": "",
    "Username": "john.doe",
    "AppId": 1,
    "Subject": "Sales Report",
    "RequestText": "Generate quarterly sales report"
  }'
```

**JavaScript Fetch Example:**
```javascript
const payload = {
  Token: '',
  Username: 'john.doe',
  AppId: 1,
  Subject: 'Sales Report',
  RequestText: 'Generate quarterly sales report'
};

const response = await fetch('http://localhost:5100/api/request', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(payload)
});

const result = await response.json();
console.log(result);
```

---

## Error Handling

### HTTP Status Codes
- `200 OK`: Request successful
- `400 Bad Request`: Invalid request payload or missing required fields
- `500 Internal Server Error`: Server error

### Error Response Structure
```json
{
  "Status": 0,
  "Message": "Error description",
  "ResponseText": ""
}
```

---

## CORS Configuration

The API is configured to accept requests from:
- `http://localhost:5000` (MVC app)
- `https://localhost:5001` (MVC app HTTPS)

**Allowed:**
- All HTTP methods
- All headers
- Credentials

---

## Data Models

### AppModel
```csharp
public class AppModel
{
    public int AppId { get; set; }
    public string AppNameEn { get; set; }
    public string AppNameAr { get; set; }
    public string AppUrl { get; set; }
    public string AppIcon { get; set; }
}
```

### RecentRequestModel
```csharp
public class RecentRequestModel
{
    public string DateTime { get; set; }
    public int AppId { get; set; }
    public string AppNameEn { get; set; }
    public string AppNameAr { get; set; }
    public string Subject { get; set; }
    public string Details { get; set; }
    public int Status { get; set; }
    public string Response { get; set; }
}
```

### RequestPayloadModel
```csharp
public class RequestPayloadModel
{
    public string Token { get; set; }
    public string Username { get; set; }
    public int AppId { get; set; }
    public string Subject { get; set; }
    public string RequestText { get; set; }
}
```

### RequestResponseModel
```csharp
public class RequestResponseModel
{
    public int Status { get; set; }
    public string Message { get; set; }
    public string ResponseText { get; set; }
}
```

---

## Testing with Swagger

The API includes Swagger UI for interactive testing.

**Access:** http://localhost:5100/swagger

**Features:**
- View all endpoints
- Test requests directly from browser
- See request/response schemas
- Try different parameters

---

## Rate Limiting

**Current:** No rate limiting  
**Future:** Implement rate limiting to prevent abuse

---

## Versioning

**Current Version:** v1 (implicit)  
**Future:** Consider API versioning (e.g., `/api/v1/apps`)

---

## Webhooks

**Current:** Not implemented  
**Future:** Add webhooks for request completion notifications

---

## Pagination

**Current:** Not implemented  
**Future:** Add pagination for large result sets

Example future endpoint:
```
GET /api/recents?page=1&pageSize=10
```

---

## Filtering and Sorting

**Current:** Not implemented  
**Future:** Add query parameters for filtering and sorting

Example future endpoints:
```
GET /api/recents?appId=1
GET /api/recents?sortBy=dateTime&order=desc
```

---

## Security Implementation (TODO)

### Token Validation
```csharp
// TODO: Implement in all controllers
if (string.IsNullOrWhiteSpace(token) || !ValidateToken(token))
{
    return Unauthorized();
}
```

### Rate Limiting
```csharp
// TODO: Add middleware
app.UseRateLimiter();
```

### Input Sanitization
```csharp
// TODO: Add input validation and sanitization
```

---

## Production Checklist

Before deploying to production:

- [ ] Implement authentication
- [ ] Add authorization checks
- [ ] Enable HTTPS only
- [ ] Configure proper CORS
- [ ] Add rate limiting
- [ ] Implement logging
- [ ] Add monitoring
- [ ] Set up error tracking
- [ ] Configure database
- [ ] Add caching
- [ ] Enable compression
- [ ] Set up CDN for static files
- [ ] Configure environment variables
- [ ] Add health check endpoints
- [ ] Implement request/response validation
- [ ] Add API documentation versioning

---

## Support

For API issues or questions:
1. Check server logs
2. Review Swagger documentation
3. Examine controller code
4. Check CORS configuration
5. Verify request format

---

## Changelog

### Version 1.0.0 (Current)
- Initial release
- Three endpoints: apps, recents, request
- Hardcoded data responses
- CORS enabled for local development
- Swagger documentation included
