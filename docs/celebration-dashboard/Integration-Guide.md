# Celebration Dashboard Integration Guide

## Purpose
This guide outlines how the Celebration feature set plugs into the current EdPicker platform architecture across UI, API, and operations. It assumes existing principal authentication flows and reuses infrastructure choices already adopted for the Lesson Planner.

## High-Level Architecture
1. **edpicker-ui** delivers the `/celebration/dashboard` Angular module following HomeComponent conventions.
2. **edpicker-api** exposes `/api/celebration` endpoints backed by a dedicated `celebration` schema inside `EdPickerDB`.
3. **Hangfire** (or Azure WebJob alternative) orchestrates daily SMS jobs and retention pruning.
4. **MSG91** handles transactional SMS delivery using DLT-compliant templates stored in configuration.

## Authentication & Authorization
- Reuse existing login experience; principal role gains access to the Celebration card and API endpoints.
- Server side leverages ASP.NET Identity with lockout (5 attempts, 15 minutes) and 30-minute session expiration.
- Audit trail entries record login failures under `celebration.FailedLogins` when relevant.

## Navigation Integration Steps
1. Add a new card definition in the home dashboard component referencing the `/celebration/dashboard` route.
2. Ensure lazy loading module registration to prevent unnecessary bundle size increase for other flows.
3. Guard the route with the same `AuthGuard` used by planner features.

## State & Data Flow
- **Profiles**: Uploads and manual edits call `POST /api/celebration/profiles/*`. Successful imports raise a green toast and trigger a refresh of Today data.
- **Dashboard**: UI retrieves `GET /api/celebration/dashboard/today` on load and subsequent polling. History tab issues `GET /api/celebration/dashboard/history` with chosen filters.
- **Configuration**: School name persisted via `POST /api/celebration/config/school`; backend stores per AdminId for template personalization.
- **Opt-Out**: MSG91 webhook posts to `/api/celebration/webhook/optout` updating consent flags. UI surfaces opt-out in History via status column.

## Background Processing
- Register Hangfire server in `Program.cs` with SQL Server storage using existing connection string.
- Configure recurring jobs during application startup:
  ```csharp
  RecurringJob.AddOrUpdate(
      "celebration-daily-sms",
      () => celebrationService.SendDailyRemindersAsync(),
      "30 3 * * *", // 9:00 AM IST
      TimeZoneInfo.Utc);

  RecurringJob.AddOrUpdate(
      "celebration-retention",
      () => celebrationService.PruneRetentionAsync(),
      Cron.Daily(19, 30)); // 1:00 AM IST
  ```
- Wire MSG91 delivery callbacks to update `CelebrationLogs` status; fallback to polling if webhook unavailable.

## Configuration Management
Add the following keys to `appsettings.json` (and secure secrets in Azure Key Vault or App Service settings):
```json
"Celebration": {
  "DlTTemplateBirthday": "<template-id>",
  "DlTTemplateAnniversary": "<template-id>",
  "SenderId": "SCHLBDY",
  "PeId": "<pe-id>",
  "BatchSizePerHour": 100,
  "RetentionDays": 90
},
"Msg91": {
  "ApiKey": "<msg91-key>",
  "BaseUrl": "https://api.msg91.com"
}
```

## Deployment Considerations
- Ensure Hangfire dashboard access is restricted (e.g., behind admin auth or IP safelist).
- Include migration execution step in CI/CD to create `celebration` schema.
- Coordinate DLT template approval and MSG91 sender header setup before production launch.
- Configure Application Insights custom events for SMS send outcomes and webhook failures.

## Testing Strategy Alignment
- Unit tests cover services (import parsing, SMS scheduling, webhook handling).
- Integration tests mimic CSV upload -> cron job -> dashboard retrieval -> opt-out flows.
- Performance tests confirm 5,000 profile uploads <10s and daily dashboard response <2s.

## Future Enhancements Placeholder
- Personalised SMS content once AI personalization is permitted.
- Multi-role support leveraging `Role` field in `Admins` table.
- Optional Azure Function replacement if Hangfire adoption changes.

