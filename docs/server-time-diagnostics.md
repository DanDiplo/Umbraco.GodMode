# Server time and scheduling diagnostics

GodMode normalizes `DateTime` response values to UTC. `DateTimeOffset` values retain their explicit offsets, including the server-local clock. Local / Server / UTC is a display preference; it never changes stored content, migration settings or schedules. Evidence drawers preserve raw API dates and use the same switch and three-zone tooltip as lists.

## Migration settings and historical evidence

The migration panel describes **current configuration**, not the configuration used by an earlier upgrade. Fresh Umbraco 17 installs have no historical system dates to convert. All migrations being applied does not establish whether date conversion ran, was disabled or was skipped for UTC. Changing settings after a completed upgrade does not rerun it.

An explicit SQL Server time zone is validated against `sys.time_zone_info` on the connected database. Umbraco passes this name unchanged to SQL Server's `AT TIME ZONE`; a locally resolvable IANA ID is not sufficient. SQLite resolves the zone on the web server and converts using its base offset, ignoring historical daylight saving. Rule comparisons use the effective migration zone, not just today's offset or the current server's DST support. A SQL-only zone may be valid even when its adjustment rules cannot be resolved locally.

Local migration logs and four system-date columns are checked in a shared snapshot cached for up to two minutes, independently of changing log-file signatures. The check time and cutoff are shown. Missing/unreadable/malformed logs and failed database columns produce unavailable or partial results, not a clean bill of health. Logs are limited to up to 60 recent local JSON files; missing remote or expired logs cannot establish migration history.

Future dates are a possible symptom of clock differences, imported data or shifted local dates. This heuristic does not detect older shifts or shifts into the past. No matches does not prove correct date conversion. Check known historical event times and upgrade logs before considering any data correction.

## Scheduled publishing

Job timestamps describe bookkeeping, not successful publication: Umbraco updates them after failures and skipped/suspended publishing. GodMode therefore uses “Recently ran” and “Marked running”, displays publishing suspension, and flags pending overdue schedules independently. Old timestamps warrant investigation; they do not by themselves prove that a job has stopped.

Automatic election normally touches and deactivates server registrations. Fixed/custom role accessors bypass that mechanism, so existing rows may be historical and cleanup is not assured. GodMode suppresses server-down findings for these accessors and labels old check-ins as historical. Missing database evidence is shown as unknown.

## Verification

Regression tests cover provider-specific zone validity, different historical rules with equal winter offsets, the migration zone's DST warning on a UTC server, failed/partial evidence, shared caching, retrospective settings, fixed server roles, publishing suspension and UTC serialization. Validation of these fixes: both client type-checks and the solution build pass, with 57 automated tests. SQL Server LocalDB checks confirm time-zone names, schedule query shapes, job-period ticks and parameterized future-date counts using temporary tables. Nine browser fixture checks cover job labels, suspension, historical registrations, optional AI layouts, reactive drawer dates, UTC tooltips and the isolated live clock. These checks do not replace a full SQL Server Umbraco host or authenticated backoffice acceptance; the configured local login did not succeed.
