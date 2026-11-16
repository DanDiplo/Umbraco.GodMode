import assert from 'node:assert/strict';
import { test } from 'node:test';
import { buildSchedulingHealthSubject } from '../Diplo.GodMode/Client/src/shared/scheduling-ai-context.ts';

const schedule = (id, changes = {}) => ({
    id: String(id), nodeId: id, nodeKey: `key-${id}`, name: `Page ${id}`, contentTypeAlias: 'article',
    icon: 'icon-document', culture: '', action: 'Release', date: '2026-11-15T12:00:00Z',
    published: false, trashed: false, isOverdue: true, ...changes
});
const job = name => ({ name, lastRun: '2026-11-16T11:59:00Z', lastAttemptedRun: '2026-11-16T11:59:00Z',
    periodSeconds: 60, isRunning: false, isStale: false });
const server = (id, changes = {}) => ({ id, computerName: 'PRIVATE-MACHINE', address: 'https://private-server.example',
    registeredDate: '2026-01-01T00:00:00Z', lastNotifiedDate: '2026-11-16T11:59:00Z',
    isActive: false, isSchedulingPublisher: false, isStale: true, ...changes });
const overview = (changes = {}) => ({ serverUtcNow: '2026-11-16T12:00:00Z', overdueAfterMinutes: 5,
    items: [], jobs: [job('ScheduledPublishingJob')], servers: [], currentServerRole: 'Single',
    automaticServerRegistration: true, scheduledPublishingSuspended: false, jobsCheckSucceeded: true,
    serversCheckSucceeded: true, ...changes });

test('counts use the full snapshot and exclude recycle-bin schedules from overdue/live counts', () => {
    const input = overview({ items: [schedule(1), schedule(2, { trashed: true }), schedule(3, { isOverdue: false, action: 'Expire' })],
        scheduledPublishingSuspended: true, servers: [server(1, { isActive: true, isSchedulingPublisher: true }), server(2)] });
    const before = JSON.stringify(input);
    const subject = buildSchedulingHealthSubject(input);
    assert.equal(subject.data.scheduledPublishingSuspended, true);
    assert.deepEqual(subject.data.schedules, { total: 3, pendingPublish: 1, pendingUnpublish: 1, overdue: 1, inRecycleBin: 1 });
    assert.equal(subject.context.overdueScheduleSamples.length, 1);
    assert.equal(subject.data.servers.recordedActivePublishers, 1);
    assert.equal(JSON.stringify(input), before, 'building AI evidence must not reorder/mutate displayed data');
});

test('failed evidence checks stay unknown and do not leak stale cached job/server rows', () => {
    const subject = buildSchedulingHealthSubject(overview({ jobsCheckSucceeded: false, serversCheckSucceeded: false,
        jobs: [job('ScheduledPublishingJob')], servers: [server(1, { isActive: true })] }));
    assert.equal(subject.data.registeredJobs, null);
    assert.equal(subject.data.servers, null);
    assert.equal(subject.context.publishingJob, null);
    assert.deepEqual(subject.context.otherJobs, []);
    assert.deepEqual(subject.context.serverSamples, []);
});

test('large snapshots retain full counts, bound samples and prioritize publisher/active evidence', () => {
    const subject = buildSchedulingHealthSubject(overview({
        items: [...Array.from({length: 300}, (_, i) => schedule(i)), ...Array.from({length: 100}, (_, i) => schedule(300 + i, { isOverdue: false }))],
        jobs: [...Array.from({length: 30}, (_, i) => job(`Job ${i}`)), job('ScheduledPublishingJob')],
        servers: [...Array.from({length: 100}, (_, i) => server(i)), server(101, { isActive: true, isSchedulingPublisher: true })]
    }));
    assert.equal(subject.data.schedules.total, 400);
    assert.equal(subject.context.overdueScheduleSamples.length, 20);
    assert.equal(subject.context.pendingScheduleSamples.length, 10);
    assert.equal(subject.context.otherJobs.length, 10);
    assert.equal(subject.context.serverSamples.length, 20);
    assert.equal(subject.context.serverSamples[0].recordedActive, true);
    assert.equal(subject.context.publishingJob.name, 'ScheduledPublishingJob');
    assert.deepEqual(subject.context.omitted, { overdueSchedules: 280, pendingSchedules: 90, servers: 81, otherJobs: 20 });
});

test('historical/disabled election state is explicit and unnecessary private fields are omitted', () => {
    const subject = buildSchedulingHealthSubject(overview({ automaticServerRegistration: false, currentServerRole: 'Subscriber',
        servers: [server(1)], items: [schedule(1, { name: 'x'.repeat(2000), body: 'PRIVATE-CONTENT-BODY', secret: 'PRIVATE-SECRET' })] }));
    assert.equal(subject.data.automaticServerRegistration, false);
    assert.equal(subject.data.currentServerRole, 'Subscriber');
    assert.equal(subject.context.overdueScheduleSamples[0].name.length, 160);
    const json = JSON.stringify(subject);
    for (const secret of ['PRIVATE-MACHINE', 'private-server.example', 'PRIVATE-CONTENT-BODY', 'PRIVATE-SECRET'])
        assert.ok(!json.includes(secret), `${secret} must not be in scheduling evidence`);
    assert.ok(json.length < 20000);
});
