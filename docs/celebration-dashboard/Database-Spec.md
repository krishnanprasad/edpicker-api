# Celebration Database Specification

## Schema Overview
- **Database**: `EdPickerDB`
- **Schema**: `celebration` (new)
- Tables and indexes extend existing database while maintaining isolation from current lesson planner data.

## Tables

### `celebration.Students`
| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| `StudentId` | `INT IDENTITY(1,1)` | PK | |
| `AdminId` | `INT` | FK `dbo.Admins(AdminId)` | Enforces per-school isolation |
| `Name` | `NVARCHAR(100)` | NOT NULL | |
| `DOB` | `DATE` | NOT NULL | Stored as calendar date |
| `Phone` | `VARCHAR(13)` | NOT NULL | Format `+91XXXXXXXXXX`; unique per AdminId + Student |
| `Consent` | `BIT` | NOT NULL DEFAULT 1 | |
| `CreatedAt` | `DATETIMEOFFSET(0)` | NOT NULL DEFAULT `SYSUTCDATETIME()` | Convert to IST in app |
| `UpdatedAt` | `DATETIMEOFFSET(0)` | NOT NULL DEFAULT `SYSUTCDATETIME()` | Updated via trigger |
| `RowVersion` | `ROWVERSION` | | Supports optimistic concurrency |

**Indexes**:
- `IX_Students_Admin_DOB_Consent` on (`AdminId`, `DOB`, `Consent`).
- `IX_Students_Phone` unique on (`AdminId`, `Phone`, `Consent`).

### `celebration.Teachers`
Similar structure with additional nullable `Anniversary` column (`DATE NULL`).

**Indexes**:
- `IX_Teachers_Admin_DOB_Consent` on (`AdminId`, `DOB`, `Consent`).
- `IX_Teachers_Admin_Anniversary_Consent` on (`AdminId`, `Anniversary`, `Consent`).
- `IX_Teachers_Phone` unique on (`AdminId`, `Phone`, `Consent`).

### `celebration.CelebrationLogs`
| Column | Type | Constraints |
| --- | --- | --- |
| `LogId` | `BIGINT IDENTITY(1,1)` | PK |
| `AdminId` | `INT` | FK |
| `RecipientType` | `VARCHAR(10)` | CHECK IN ('Student','Teacher') |
| `RecipientId` | `INT` | NOT NULL |
| `RecipientName` | `NVARCHAR(100)` | NOT NULL |
| `EventType` | `VARCHAR(12)` | CHECK IN ('Birthday','Anniversary') |
| `Phone` | `VARCHAR(13)` | NOT NULL |
| `Message` | `NVARCHAR(160)` | NOT NULL |
| `Status` | `VARCHAR(10)` | CHECK IN ('Pending','Sent','Delivered','Failed') |
| `SentAt` | `DATETIMEOFFSET(0)` | NOT NULL |
| `DeliveredAt` | `DATETIMEOFFSET(0)` | NULL |
| `FailureReason` | `NVARCHAR(200)` | NULL |
| `CreatedAt` | `DATETIMEOFFSET(0)` | NOT NULL DEFAULT `SYSUTCDATETIME()` |

**Indexes**:
- `IX_Logs_Admin_SentAt` on (`AdminId`, `SentAt` DESC).
- `IX_Logs_Phone_SentAt` on (`Phone`, `SentAt` DESC).
- `IX_Logs_Status` on (`AdminId`, `Status`).

### `celebration.AuditTrail`
Extends existing audit structure while scoped per schema.

| Column | Type |
| --- | --- |
| `AuditId` | `BIGINT IDENTITY(1,1)` |
| `AdminId` | `INT` |
| `Action` | `VARCHAR(20)` | Values: Upload, Send, OptOut |
| `Details` | `NVARCHAR(400)` |
| `Timestamp` | `DATETIMEOFFSET(0)` | DEFAULT `SYSUTCDATETIME()` |

**Indexes**: `IX_Audit_Admin_Timestamp` on (`AdminId`, `Timestamp` DESC).

### `celebration.FailedLogins`
If reuse not desired, create new table mirroring security requirements.

| Column | Type |
| --- | --- |
| `LogId` | `BIGINT IDENTITY(1,1)` |
| `Username` | `NVARCHAR(100)` |
| `Timestamp` | `DATETIMEOFFSET(0)` | DEFAULT `SYSUTCDATETIME()` |
| `IpAddress` | `VARCHAR(45)` | NULL |

Index on (`Username`, `Timestamp` DESC).

### `celebration.Configuration`
Stores per-admin preferences.

| Column | Type | Notes |
| --- | --- | --- |
| `AdminId` | `INT` | PK/FK |
| `SchoolName` | `NVARCHAR(100)` | |
| `CreatedAt` | `DATETIMEOFFSET(0)` | |
| `UpdatedAt` | `DATETIMEOFFSET(0)` | |

## Stored Procedures & Functions
- `celebration.usp_GetTodayEvents @AdminId INT, @Date DATE` – returns teacher rows with SMS status and student counts.
- `celebration.usp_GetLogs @AdminId INT, @Date DATE, @Status VARCHAR(10), @Page INT, @PageSize INT` – returns paginated logs with total count via `OUTPUT` parameter.
- `celebration.usp_PruneOldData @RetentionDays INT` – deletes logs/audit records older than retention (default 90 days). Scheduled nightly.

## Triggers
- `celebration.trg_Students_SetUpdatedAt` and `celebration.trg_Teachers_SetUpdatedAt` update `UpdatedAt` on modifications.
- Optional trigger to cascade consent changes to associated logs if required for compliance notes.

## Seed Data
Migration seeds ten sample records (five students, five teachers) per admin for QA environments. Use deterministic names and IST timestamps.

## Retention & Maintenance
- Hangfire job `celebration-PruneRetention` executes `usp_PruneOldData` daily at 01:00 IST.
- Monitor row counts; add partitioning if >1M logs.

