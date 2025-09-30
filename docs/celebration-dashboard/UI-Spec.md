# Celebration Dashboard UI Specification

## Overview
The Celebration Dashboard is a new single-function experience exposed at `/celebration/dashboard` within **edpicker-ui**. It targets the existing principal-level administrator persona and mirrors the established HomeComponent architectural patterns for state management, error handling, and responsive behavior.

## Navigation
- Add a **Celebration** card to the existing edpicker home grid. Selecting this card routes the user to `/celebration/dashboard`.
- Visibility aligns with the current principal login role; no alternate roles are introduced in this phase.

## Layout & Structure
The dashboard comprises three core feature areas organised into tabs:

1. **Today Tab (default)**
   - **Teacher Events Table**
     - Angular Material table with columns: `Name`, `Event` (Birthday/Anniversary), `Phone`, `Status` (Sent/Delivered/Failed), `SentAt` (IST, `dd MMM yyyy, hh:mm a`).
     - Paginate at 20 rows per page; maintain HomeComponent paginator style.
     - On mobile (<768px) switch to accordion list (one panel per teacher) with the same fields and quick status indicator chips.
   - **Student Summary Card**
     - Prominent card showing `X Birthdays Today`.
     - When the count is <50 provide an expandable list mirroring the teacher accordion styling.
   - **Delivery Summary Bar**
     - Display total events processed, delivery success percentage, and failure count using compact statistic cards.
   - **Alerts Region**
     - Red banner for failed SMS deliveries with count and CTA to open history filtered to failures.

2. **History Tab**
   - Angular Material table listing CelebrationLogs records for the selected date (default today).
   - Filters:
     - Date picker supporting past 90 days plus future preview.
     - Search box filtering by `RecipientName`.
     - Status filter (All/Sent/Delivered/Failed).
   - Paginate at 50 rows per page, matching backend response slices.

3. **Profiles & Configuration Drawer**
   - Right-side responsive drawer (modal fallback on narrow viewports) providing:
     - **Profile Upload Panel**
       - Drag-and-drop zone for `.csv` and `.xlsx` files.
       - Downloadable template link describing the required columns: `Type,Name,DOB,Anniversary,Phone,Consent`.
       - Upload preview grid with inline edit/delete before committing.
       - Submission triggers `POST /api/celebration/profiles/upload`.
     - **Manual Entry Forms**
       - Tabs for Student and Teacher entries matching upload columns.
       - Consent checkbox defaulted to checked; disabling shows a confirmation modal.
     - **School Name Configuration**
       - Input with validation (required, 4–100 chars) stored via `POST /api/celebration/config/school`.

## Interaction Patterns
- Reuse HomeComponent `LoaderService` for NgxSpinner integration during API calls.
- Errors follow the established notification hierarchy: green toast (success), yellow banner (warnings), red dialog (critical failures).
- Implement auto-refresh polling every 60 seconds on the Today tab to surface new SMS outcomes.
- Respect 44px minimum touch targets and responsive breakpoints (320px, 768px, 1024px).
- Cache the last selected tab and filters in local storage to improve mobile return visits.

## Accessibility & Localization
- All interactive controls support keyboard navigation and ARIA labels consistent with Angular Material defaults.
- Time fields are displayed in IST with tooltips showing absolute timestamps.

## Empty & Error States
- When no events exist for a date show an illustration placeholder with the message "No celebrations scheduled." and a shortcut to open profile upload.
- Failed API calls surface the descriptive error message returned from the backend and retain the user’s in-progress form data.

