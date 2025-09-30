# Celebration API Specification

## Base Configuration
- **Namespace**: `EdPicker.Api.Controllers`
- **Controller**: `CelebrationController : BaseApiController`
- **Route Prefix**: `/api/celebration`
- **Authentication**: `[Authorize(Roles = Roles.Principal)]`
- **Versioning**: Align with existing `ApiVersion("1.0")` attribute pattern.
- **Dependencies**:
  - `ICelebrationProfileService`
  - `ICelebrationDashboardService`
  - `ICelebrationSmsService`
  - `ICelebrationAuditService`
  - `IHttpContextAccessor` for AdminId resolution.

## Endpoints

### POST `/profiles/upload`
Imports bulk profiles from CSV or Excel.

- **Request**: `multipart/form-data` with `file` field.
- **Validation**:
  - MIME types: `text/csv`, `application/vnd.ms-excel`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.
  - Mandatory columns: `Type,Name,DOB,Anniversary,Phone,Consent` (case-insensitive).
- **Processing**:
  - Parse using `ICelebrationImportParser` (NgxCsvParser counterpart on UI).
  - Deduplicate only by composite `(Type, Name, DOB, Phone)` within the upload batch.
  - Persist via `ProfilesService.UpsertRangeAsync` with transactional scope.
  - Append audit trail entry with details count and filename.
- **Response**:
```json
{
  "success": true,
  "payload": {
    "imported": 150,
    "errors": ["Row 5: Invalid DOB"]
  }
}
```

### POST `/profiles/students`
Create or update a student profile.

- **Body**:
```json
{
  "studentId": 0,
  "name": "Rahul Kumar",
  "dob": "2008-09-29",
  "phone": "+919876543210",
  "consent": true
}
```
- **Rules**:
  - `studentId` optional; when >0 triggers update with optimistic concurrency (timestamp).
  - Validate phone format, DOB (past date), consent default true.
- **Response**: `{ "success": true, "payload": { "studentId": 42 } }`

### POST `/profiles/teachers`
Analogous to students with additional nullable `anniversary`.

### GET `/dashboard/today`
Return aggregated data for the current date.

- **Query Params**: none.
- **Behavior**:
  - Teachers: join latest SMS status from `CelebrationLogs` when available, otherwise mark as `Pending`.
  - Students: return count and optional preview list (first 50 items when requested via `includeStudents=true`).
  - Delivery summary derived from logs within the current day window (IST midnight to 23:59:59).
- **Response**:
```json
{
  "success": true,
  "payload": {
    "teachers": [
      {
        "teacherId": 3,
        "name": "Anita Sharma",
        "eventType": "Anniversary",
        "phone": "+919812345678",
        "status": "Delivered",
        "sentAt": "2025-09-29T09:05:12+05:30"
      }
    ],
    "students": {
      "count": 125,
      "preview": []
    },
    "summary": {
      "totalEvents": 150,
      "delivered": 147,
      "failed": 3
    },
    "alerts": {
      "failedCount": 3
    }
  }
}
```

### GET `/dashboard/history`
Fetch paginated logs for a specific date range.

- **Query**: `date=yyyy-MM-dd` (default today), `status`, `page`, `pageSize` (default 1, 50).
- **Response** includes total count for client pagination.

### POST `/sms/test`
Manual test SMS (non-production safeguard).
- Restricted to development environment via feature flag.
- Logs to audit trail with reason `Send` and `Details` referencing manual trigger.

### POST `/webhook/optout`
Processes STOP replies from MSG91.

- **Body**:
```json
{
  "phone": "+919812345678",
  "message": "STOP"
}
```
- **Behavior**:
  - Normalize phone.
  - Set `Consent=false` across Students and Teachers with matching phone.
  - Append audit entry `Action=OptOut`.
  - Respond with `{ "success": true, "payload": { "updated": true } }`.

### POST `/config/school`
Persist the SMS personalization name.

- **Body**: `{ "schoolName": "Springfield High" }`
- **Validation**: 4–100 characters.
- **Response**: `{ "success": true }`

## Background Jobs
- Register Hangfire recurring job `CelebrationService.SendDailyReminders` with cron `"30 3 * * *"` (UTC aligning with 9:00 AM IST).
- `SendDailyReminders` pipeline:
  1. Query eligible students/teachers (consent true, DOB/Anniversary matches date).
  2. Enqueue per-recipient job to respect 100/hour throughput; use Hangfire batches or Azure Queue fallback if Hangfire unavailable.
  3. Invoke `ICelebrationSmsProvider.SendAsync` using MSG91 template ids from configuration.
  4. Record log entries with status transitions (`Sent` -> `Delivered` via callback/webhook).
  5. Retry policy: exponential backoff up to three attempts; terminal failure flagged in dashboard summary.

## Error Contract
All responses adhere to the shared error format:
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid phone number",
    "details": ["Phone must be +91XXXXXXXXXX"],
    "timestamp": "2025-09-29T22:30:00+05:30"
  }
}
```

## Security & Auditing
- Leverage existing ASP.NET Identity lockout rules (5 failed attempts, 15 minutes).
- Every endpoint logs success/failure with `AuditTrail` entries for 90-day retention.
- Rate limit webhook and test endpoints to prevent abuse.

