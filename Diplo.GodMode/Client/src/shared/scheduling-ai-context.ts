import type { ContentScheduleOverview, ContentScheduleItem, DistributedJobInfo } from "./types";
import type { GodModeAiExplainSubject } from "./godmode-ai-explain-host.element";

const PUBLISHING_JOB = "ScheduledPublishingJob";
const OVERDUE_LIMIT = 20;
const PENDING_LIMIT = 10;
const SERVER_LIMIT = 20;
const OTHER_JOB_LIMIT = 10;

/** Build only when Explain is opened: full counts, bounded and whitelisted evidence. */
export function buildSchedulingHealthSubject(overview: ContentScheduleOverview): GodModeAiExplainSubject {
    const live = overview.items.filter(x => !x.trashed);
    const byDate = (a: ContentScheduleItem, b: ContentScheduleItem) => Date.parse(a.date) - Date.parse(b.date);
    const overdue = live.filter(x => x.isOverdue).sort(byDate);
    const pending = live.filter(x => !x.isOverdue).sort(byDate);
    const jobs = overview.jobsCheckSucceeded ? overview.jobs : [];
    const servers = overview.serversCheckSucceeded ? overview.servers : [];
    const active = servers.filter(x => x.isActive);
    const otherJobs = jobs.filter(x => x.name !== PUBLISHING_JOB);
    const jobEvidence = (job: DistributedJobInfo) => ({
        name: job.name.slice(0, 120), lastRunUtc: job.lastRun, lastAttemptedRunUtc: job.lastAttemptedRun,
        periodSeconds: job.periodSeconds, markedRunning: job.isRunning, isStale: job.isStale
    });
    const scheduleEvidence = (item: ContentScheduleItem) => ({
        name: (item.name ?? "").slice(0, 160), contentTypeAlias: (item.contentTypeAlias ?? "").slice(0, 100),
        action: item.action, culture: (item.culture ?? "").slice(0, 30) || "invariant", scheduledForUtc: item.date,
        currentlyPublished: item.published, isOverdue: item.isOverdue
    });
    const publishingJob = jobs.find(x => x.name === PUBLISHING_JOB);
    const sampledServers = [...servers].sort((a, b) => Number(b.isActive) - Number(a.isActive)
        || Date.parse(b.lastNotifiedDate) - Date.parse(a.lastNotifiedDate)).slice(0, SERVER_LIMIT);

    return {
        subjectType: "Umbraco scheduled publishing health",
        title: "Scheduled publishing health",
        data: {
            capturedAtUtc: overview.serverUtcNow,
            overdueAfterMinutes: overview.overdueAfterMinutes,
            scheduledPublishingSuspended: overview.scheduledPublishingSuspended,
            currentServerRole: overview.currentServerRole,
            automaticServerRegistration: overview.automaticServerRegistration,
            schedules: {
                total: overview.items.length, pendingPublish: live.filter(x => x.action === "Release").length,
                pendingUnpublish: live.filter(x => x.action === "Expire").length,
                overdue: overdue.length, inRecycleBin: overview.items.length - live.length
            },
            jobsCheckSucceeded: overview.jobsCheckSucceeded,
            registeredJobs: overview.jobsCheckSucceeded ? jobs.length : null,
            serversCheckSucceeded: overview.serversCheckSucceeded,
            servers: overview.serversCheckSucceeded ? {
                total: servers.length, recordedActive: active.length,
                recordedActivePublishers: active.filter(x => x.isSchedulingPublisher).length,
                recordedActiveWithStaleCheckIn: active.filter(x => x.isStale).length,
                recordedInactive: servers.length - active.length
            } : null
        },
        context: {
            publishingJob: publishingJob ? jobEvidence(publishingJob) : null,
            otherJobs: otherJobs.slice(0, OTHER_JOB_LIMIT).map(jobEvidence),
            overdueScheduleSamples: overdue.slice(0, OVERDUE_LIMIT).map(scheduleEvidence),
            pendingScheduleSamples: pending.slice(0, PENDING_LIMIT).map(scheduleEvidence),
            // No server addresses, machine identities, content bodies or cleanup requests.
            serverSamples: sampledServers.map(x => ({
                id: x.id, registeredUtc: x.registeredDate, lastCheckInUtc: x.lastNotifiedDate,
                recordedActive: x.isActive, recordedPublisher: x.isSchedulingPublisher, staleCheckIn: x.isStale
            })),
            omitted: {
                overdueSchedules: Math.max(0, overdue.length - OVERDUE_LIMIT),
                pendingSchedules: Math.max(0, pending.length - PENDING_LIMIT),
                servers: Math.max(0, servers.length - SERVER_LIMIT), otherJobs: Math.max(0, otherJobs.length - OTHER_JOB_LIMIT)
            },
            interpretation: "This is the loaded workspace snapshot, not a live publishing test. Job timestamps and running flags are bookkeeping, not proof of successful publishing. When automatic registration is disabled, old registrations may be historical. Failed checks mean unknown evidence. Samples are bounded; use the full counts and disclose omissions."
        }
    };
}
