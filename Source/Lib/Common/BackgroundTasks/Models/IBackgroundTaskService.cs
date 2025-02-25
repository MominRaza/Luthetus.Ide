﻿using Luthetus.Common.RazorLib.Keys.Models;
using System.Collections.Immutable;

namespace Luthetus.Common.RazorLib.BackgroundTasks.Models;

public interface IBackgroundTaskService
{
    public ImmutableArray<BackgroundTaskQueue> Queues { get; }

    public Task EnqueueAsync(IBackgroundTask backgroundTask);
    public Task EnqueueAsync(Key<BackgroundTask> taskKey, Key<BackgroundTaskQueue> queueKey, string name, Func<Task> runFunc);
    public void RegisterQueue(BackgroundTaskQueue queue);

    public Task<IBackgroundTask> DequeueAsync(
        Key<BackgroundTaskQueue> queueKey,
        CancellationToken cancellationToken);

    public void SetExecutingBackgroundTask(
        Key<BackgroundTaskQueue> queueKey,
        IBackgroundTask? backgroundTask);

    public BackgroundTaskQueue GetQueue(Key<BackgroundTaskQueue> queueKey);

    public Task StopAsync(CancellationToken cancellationToken);
}
