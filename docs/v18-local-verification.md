# Umbraco 18 local package verification

16 November 2026. Experimental local packages, not a public support declaration.

## Shared source and build

Normal source defaults remain on Umbraco 17, with core version 17.4.1 and AI
version 1.0.0 unchanged. `scripts/build-v18-local.ps1` stages the same source,
builds against CMS/backoffice 18.2.0 and Umbraco.AI 18.3.5, and produces core/AI
prereleases with CMS dependency range `[18.2.0,19.0.0)`.

The compatibility change uses the tree API's `paging` request property (already
available in v17) and supplies `totalBefore`/`totalAfter`. GodMode returns its
small static catalogue in full, so both unloaded sibling counts are zero.
Browser testing also found that the Property Search submit button did not invoke
the search, although Enter worked. It now calls the shared submission handler
explicitly, retaining submit semantics, preventing duplicate/default submission,
and guarding IME composition and in-flight requests. The overview no longer
checks a hard-coded `index2.js`: Vite's hashed chunks make that check incorrect;
the package manifest and entry point remain checked.
The backend initially compiled without functional changes. Existing template master/layout
aliases produce obsolete warnings on v18 and remain compatible through v18.

## Verified

- v17: core and AI TypeScript checks passed, AI/backend build passed, all 99
  existing backend tests passed.
- v18: core and AI TypeScript checks and production client builds passed; all 99
  existing backend tests passed.
- Both NuGets were packed and inspected for the expected plugin manifest/entry
  JS, manifest version, CMS range and AI-to-core/Umbraco.AI dependencies.
- The v18 host restored and built with the selected core and AI local NuGets.
  Deployed DLL SHA-256 values matched the selected packages in the NuGet cache.
- The host started and Umbraco.AI database migrations completed successfully.
- `/umbraco/`, both `/App_Plugins/DiploGodMode/` and
  `/App_Plugins/DiploGodModeAI/` manifests, and both JS entry points returned 200.
- Anonymous `GET /umbraco/management/api/v1/godmode/data-types` returned 401.
- Authenticated Settings navigation loaded the GodMode tree and folder collection.
- Overview returned real system, version, cache/database and Delivery API data.
- Content Browser listed 40 items; the Home details panel loaded metadata,
  publishing state and audit entries.
- Property Search for `Umbraco` returned 15 indexed results using Enter and,
  after the final rebuild, by clicking Search. The obsolete asset warning is
  absent from the final overview.
- Dictionary Browser loaded 24 items across three languages. Opening Article.By
  displayed the native dictionary editing side panel and existing values; no
  values were changed or saved.
- DataType Browser loaded 64 data types and editor/usage filters.
- The initial AI request failed authentication. After the user updated the
  connection, a response arrived as incomplete JSON. With a local test-site
  `Diplo:GodMode:AI:MaxOutputTokens` allowance of 4096, the Article explanation
  completed and displayed summary, observed details, risk and next steps. This
  confirms one real provider round trip; the package's own defaults are unchanged.
- Template Browser loaded 11 templates and their partial/master filters.
- Media Browser loaded media rows with types, sizes and timestamps.
- Database Browser loaded 108 SQLite tables with zero table-inspection warnings;
  the umbracoLog detail modal loaded its eight columns and foreign-key map.
- Element Type Usage loaded 19 element types, including Library/Element Picker
  columns. Code Snippet Row showed six uses, with stored block evidence in its
  detail modal. This database has no populated Library items to verify.
- The browser's captured console errors were SignalR disconnects at the two
  intentional host restarts, not GodMode rendering/API failures.

Final local package version used: `18.0.0-local.20261116115521570` for both packages.
The first browser checks used `18.0.0-local.20261116114645107`. Final packages
passed both client checks/builds and all 99 backend tests, were reinstalled, and
their deployed DLL hashes were checked again before the final runtime checks.
Build manifest, package hashes and TRX evidence are under ignored
`artifacts/v18-build/<version>/`; packages are in `artifacts/v18-feed/`.
The host's SQLite files were backed up before runtime checks under ignored
`artifacts/v18-package-test-backup-*`.

## Remaining acceptance

SQL Server, a core-only installation, broader AI profile/model coverage,
content editing and saved dictionary edits,
and populated Library/Element Picker usage still require runtime acceptance.
These unit tests and static asset checks do not establish complete v18 support.

Restore reported moderate/high transitive NuGet vulnerability advisories on both
the v17 defaults and the pinned v18 stack. The logs retain the exact packages and
advisories. Dependency remediation and selection of release support floors must
be addressed before preparing public packages; this work does not update the
existing host CMS/AI floors or suppress audit warnings.

## Scheduled Publishing ordering follow-up

Server registrations now sort by recorded active status first, then last check-in
descending, registration date descending and ID descending for deterministic ties.
This uses the existing recorded status; it does not infer whether historical
registrations are alive when automatic election/check-ins are disabled.

The new regression test exercises the public schedule overview with mixed active
and inactive registrations and tied check-in dates. All 100 tests passed on v17
and v18. Core and AI were rebuilt as `18.0.0-local.20261116122909638`, selected in
the v18 host, and deployed DLL hashes matched their NuGet cache copies. The host
was restarted. The subsequent browser check was blocked by an open Chrome
extension UI, so the new order has test coverage but was not visually rechecked.

The public README and AI NuGet readme now explain the 4096 example output
allowance, incomplete structured JSON, optional model-supported Temperature and
credential checks after cloning/upgrading. At this stage server-registration
deletion had not been implemented; the cleanup work below adds that capability.

## Conservative server registration cleanup

Added an administrator-only preview and selected-row cleanup in Scheduled
Publishing. Both the registration and last check-in must be more than 30 days
old. Active rows, scheduling publishers, the current instance, unknown identities
and invalid dates are protected. Review is read-only, selection defaults empty,
and at most 100 rows can be submitted. The native confirmation focuses Cancel.

Deletion uses the server-registration repository inside a core scope, obtains
Umbraco's Servers write lock, clears the cache and rechecks eligibility before
deleting only the selected IDs. Changed/missing entries are skipped. Cache cleanup
also runs on failure, the transaction is completed only after all deletes succeed,
and the committed result is logged with the acting user and affected IDs.

All 107 tests passed against v17 and v18. The added tests cover eligibility,
boundaries, current-instance protection, batch limits, invalid/unconfirmed
requests, state changes after preview, lock/read ordering, rollback completion
behaviour and administrator policy attributes on both endpoints. TypeScript checks
passed on both majors; both v18 clients built and both packages passed inspection.
Current local packages: `18.0.0-local.20261116125304601`. The host was rebuilt,
restarted, and deployed DLL hashes matched the selected NuGets.

Browser/SQLite acceptance:

- The active publisher appeared first, with inactive entries ordered newest
  check-in first. Cleanup preview excluded the active publisher.
- A synthetic inactive registration was created for the deletion test. No row was
  selected by default; selecting only that row enabled the delete action.
- The confirmation showed one selected registration and focused Cancel. Cancelling
  left the test row in SQLite.
- After explicit user approval, the test row was deleted through the browser.
  The UI reported one deletion and zero skips; the database retained all five
  original registrations and the audit log recorded the acting administrator.
- Anonymous GET preview and POST cleanup both returned 401. Non-administrator
  policy enforcement has attribute/unit coverage; no separate non-admin account
  was created for a live 403 check.

Screenshots, TRX results, logs and test-row bookkeeping remain under ignored
`artifacts/`. The host remains running on port 44358. No real site registrations
were removed during testing, and no public package was published.

## Scheduled publishing AI overview

The optional companion now adds **Explain scheduling health** below the summary.
The core view hides its entire action container when the companion is absent.
Evidence is built only on activation from the loaded snapshot, with full counts
and bounded samples. Content bodies and server addresses/machine identities are
excluded. The prompt distinguishes failed checks, historical registrations and
job bookkeeping from proof of a publishing failure or success. It offers advisory
read-only checks and cannot perform cleanup or change publishing settings.

Local packages `18.0.0-local.20261116162753955` passed both v18 client checks/builds,
all 107 .NET tests and NuGet inspection. Both v17 client type checks and the v17 AI
project build passed. Four focused Node tests cover counts, recycle-bin exclusion,
unknown evidence, bounded large snapshots, historical registration and payload
field exclusion (`node --test scripts/test-scheduling-ai-context.mjs`). Existing
transitive dependency advisories remain outside this feature's scope.

Browser acceptance completed against the v18 SQLite host: the labelled action
appeared below the summary and the provider returned a populated scheduling-health
modal. Its observed counts matched the workspace (zero pending/overdue schedules,
ten jobs, three registrations including one active publisher). It explicitly
qualified the snapshot as non-live evidence and did not diagnose an outage from
the inactive historical registrations. Screenshot: ignored
`artifacts/scheduling-ai-overview.jpg`. The temporary host was stopped after this
check. No public package was published.
