# C# ETL –Data Import Service

A production Windows Service built in C# that automates the ingestion of ACO (Accountable Care Organization) performance data from raw CSV extracts into a SQL Server database. The service runs continuously, polls for new files on a schedule, and loads hundreds of thousands of records using bulk insert with full error handling, logging, and email alerting.

---

## Architecture Overview

```
CSV Files (Network Drive)
        │
        ▼
  Windows Service (6-hour polling)
        │
        ├── File Discovery
        │     └── HashSet comparison to skip already-processed folders
        │
        ├── Extract
        │     └── CSV parsing with custom delimiter handling
        │
        ├── Transform
        │     ├── Field validation (18 fields / practices, 20 fields / providers)
        │     ├── Type mapping via DataParser
        │     └── Duplicate detection via pre-load DB query
        │
        └── Load
              ├── SqlBulkCopy in batches of 100,000 rows
              ├── Processed folders → destination path
              └── Failed folders → error path
```

---

## Key Components

### `Empire_BSAImportService.cs` – Windows Service Host
- Runs as a background Windows Service, executing the ETL pipeline every **6 hours**
- Implements a **watchdog mechanism**: if execution is still running after 180 minutes, the service self-terminates to prevent hangs
- Skips execution cycles when a prior run has not finished, logging the event for monitoring

### `ProgramExecution.cs` – Core ETL Logic
- Polls a configured network directory for new CSV subfolders
- Identifies unprocessed folders using `HashSet` comparison against already-loaded datasets
- Processes two file types:
  - `EACO_ACOInsights_Extract_Practices` — practice-level performance data (18 fields)
  - `EACO_ACOInsights_Extract_Providers` — provider-level performance data (20 fields)
- Loads data into SQL Server via **`SqlBulkCopy`** in 100,000-row batches for memory efficiency

### `DataParser.cs` – Field Mapping
- Maps raw parsed string arrays to strongly-typed Entity Framework objects
- Handles type conversion (integer keys, string metrics, performance values)
- Supports both `EMPACO_ACOInsights_DataExtraction_Practices` and `..._Providers` entities

### `LogManager.cs` – Centralized Logging (Singleton)
- Writes timestamped log entries to daily log files (`yyyyMMdd_Empire_BSA_Import_logfile.log`)
- Classifies messages as standard, `ERROR:`, or `PRIORITY ALERT:`
- **Throttles email alerts** to one per hour to prevent notification spam; `enforce=true` bypasses throttling for critical failures
- Auto-creates log directory if missing

### `CredentialManager.cs` – Configuration & Secrets
- Centralizes database connection strings, file paths, and credentials
- Keeps sensitive configuration out of hardcoded values

### `BSA.edmx` / Entity Framework Models
- Auto-generated Entity Framework data model for the target SQL Server database
- Defines entity classes for practices and providers with full performance metric schemas (QTR1–QTR4, averages, weighted averages, YTD, COVID-19 rule flags)

---

## Data Model

Each loaded record captures ACO performance metrics including:

| Field Category | Fields |
|---|---|
| Identity | NPI, TIN, Provider Name, Practice Name |
| Membership | Member Source, Enrollment Count |
| Performance | QTR1–QTR4 results, Average, Weighted Average, YTD |
| Classification | Year, Category, Metric Type, COVID-19 Rule, SAHS Designation |
| Administrative | Unique Key, Record ID, Dataset ID |

---

## Technical Stack

| Component | Technology |
|---|---|
| Language | C# (.NET Framework) |
| Service Type | Windows Service (`ServiceBase`) |
| ORM | Entity Framework (EDMX / Database-First) |
| Bulk Load | `SqlBulkCopy` |
| Database | Microsoft SQL Server |
| Scheduling | `System.Timers.Timer` (6-hour interval) |
| Logging | Custom singleton `LogManager` with daily log files |
| Alerting | SMTP email via `SendMails.cs` with throttle control |

---

## Reliability Features

- **Duplicate prevention** — queries the database before each load to skip already-imported datasets
- **Batch processing** — 100,000-row batches prevent memory exhaustion on large files
- **Stuck-thread watchdog** — self-terminates if a single execution exceeds 180 minutes
- **File routing** — processed files move to a success path; failures route to a dedicated error path for investigation
- **Dual execution mode** — configurable to run as a Windows Service or console app for local debugging
