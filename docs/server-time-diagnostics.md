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

Registrations appear with recorded-active entries first, then newest last
check-in first. Registration date and ID break ties.

### Cleaning up old server registrations

Administrators can select **Review old registrations** in Scheduled Publishing's
Servers section. Review does not change the database. Eligible rows must be
inactive, not marked as scheduling publishers, and have both a registration date
and last check-in more than 30 days old. The current instance, unknown identities
and invalid dates are excluded. The ten-minute stale diagnostic is not the
cleanup cutoff.

Select individual reviewed rows, or select all, then choose **Delete selected
registrations**. The confirmation defaults focus to Cancel and lists the selected
count. Each batch is limited to 100 registrations; review again for additional
rows. Deletion is permanent. A returning instance can register again when
automatic registration/check-ins are enabled.

Both preview and deletion endpoints require Umbraco administrator authorization
as well as Settings access. Deletion acquires Umbraco's server-registration lock,
refreshes the repository cache and rechecks current eligibility in one transaction.
Changed or missing rows are skipped, and only the submitted IDs can be removed.
The operation logs the actor, deleted IDs and skipped IDs. No schedules or content
are deleted, and there is no background or automatic cleanup.

### Optional AI overview

With `Diplo.GodMode.AI` installed and an Umbraco.AI profile/provider configured,
**Explain scheduling health** appears below the summary cards. It uses the loaded
workspace snapshot only when clicked; normal rendering makes no AI requests.
Without the companion package the button and its surrounding layout are absent.

The explanation combines schedule counts, overdue and pending samples, publishing
suspension, the publishing job's timestamps, check availability, and recorded
server roles/check-ins. Samples are bounded (20 overdue schedules, 10 other pending
schedules, 20 registrations and 10 other jobs), with full counts and omission
counts retained. Content bodies, server addresses and machine identities are not
included in this overview's evidence.

The AI is instructed to distinguish historical registrations from outages,
unknown checks from healthy results, and job bookkeeping from successful
publication. Its response is advisory and cannot publish content, change
configuration or clean up registrations. Reload the workspace when a fresh
snapshot is needed. Umbraco.AI alone does not supply this GodMode UI; the optional
GodMode AI companion is required.
