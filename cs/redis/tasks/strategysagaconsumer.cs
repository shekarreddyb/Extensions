public class OperationTaskSaga : MassTransitStateMachine<OperationTaskState>
{
    public State Polling { get; private set; }
    public Event<StartProvisioningTask> StartTask { get; private set; }
    public Event<PollProvisioningTask> Poll { get; private set; }
    private const int MaxPollingAttempts = 12;

    public OperationTaskSaga(ITaskExecutionFactory factory)
    {
        InstanceState(x => x.CurrentState);
        Event(() => StartTask, x => x.CorrelateById(m => m.OperationTaskId));
        Event(() => Poll, x => x.CorrelateById(m => m.OperationTaskId));

        Initially(
            When(StartTask)
                .ThenAsync(async ctx =>
                {
                    var s = ctx.Instance;
                    var msg = ctx.Data;
                    s.TransactionId = msg.TransactionId;
                    s.DatabaseId = msg.DatabaseId;
                    s.ClusterId = msg.ClusterId;
                    s.Operation = msg.Operation;
                    s.PollingAttempt = 0;
                    s.CurrentState = "Polling";

                    var db = ctx.GetPayload<IServiceProvider>().GetRequiredService<IProvisioningDbContext>();
                    var task = await db.OperationTasks.FindAsync(msg.OperationTaskId);
                    var strategy = factory.GetStrategy(msg.Operation);
                    await strategy.ExecuteStartAsync(task);
                })
                .TransitionTo(Polling)
                .Schedule(Poll, ctx => new PollProvisioningTask { OperationTaskId = ctx.Instance.CorrelationId }, TimeSpan.FromSeconds(10))
        );

        During(Polling,
            When(Poll)
                .ThenAsync(async ctx =>
                {
                    var s = ctx.Instance;
                    s.PollingAttempt++;
                    var db = ctx.GetPayload<IServiceProvider>().GetRequiredService<IProvisioningDbContext>();
                    var task = await db.OperationTasks.FindAsync(s.CorrelationId);
                    if (task == null) return;

                    var strategy = factory.GetStrategy(s.Operation);
                    var result = await strategy.PollStatusAsync(task);

                    if (result.Status == "Active" || result.Status == "Success")
                    {
                        await ctx.Publish(new TaskSucceeded
                        {
                            OperationTaskId = s.CorrelationId,
                            TransactionId = s.TransactionId,
                            DatabaseId = s.DatabaseId,
                            ClusterId = s.ClusterId
                        });
                        s.CurrentState = "Completed";
                    }
                    else if (result.Status == "Failed" || s.PollingAttempt >= MaxPollingAttempts)
                    {
                        await ctx.Publish(new TaskFailed
                        {
                            OperationTaskId = s.CorrelationId,
                            TransactionId = s.TransactionId,
                            DatabaseId = s.DatabaseId,
                            ClusterId = s.ClusterId,
                            Message = "Failed or timed out"
                        });
                        s.CurrentState = "Failed";
                    }
                    else
                    {
                        await ctx.Schedule(Poll, TimeSpan.FromSeconds(10), new PollProvisioningTask { OperationTaskId = s.CorrelationId });
                    }
                })
        );

        SetCompletedWhenFinalized();
    }
}




public class TaskSucceededConsumer : IConsumer<TaskSucceeded>
{
    private readonly IProvisioningDbContext _db;

    public TaskSucceededConsumer(IProvisioningDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<TaskSucceeded> context)
    {
        var msg = context.Message;
        var now = DateTime.UtcNow;

        var task = await _db.OperationTasks.FindAsync(msg.OperationTaskId);
        if (task == null) return;

        task.Status = "Completed";
        task.CompletedAt = now;

        var detail = await _db.TransactionDetails.FirstOrDefaultAsync(x => x.TransactionId == msg.TransactionId && x.ClusterId == msg.ClusterId);
        if (detail != null)
        {
            detail.Status = "Completed";
            detail.EndTime = now;
        }

        var dbDetail = await _db.DatabaseDetails.FirstOrDefaultAsync(x => x.DatabaseId == msg.DatabaseId && x.ClusterId == msg.ClusterId);
        if (dbDetail != null)
        {
            dbDetail.Status = "Active";
        }

        if (task.OperationData.TryGetValue(task.Operation, out var dataNode))
        {
            _db.AuditLogs.Add(new AuditLog
            {
                OperationTaskId = task.Id,
                Operation = task.Operation,
                Action = task.Operation,
                EntityId = task.DatabaseId,
                EntityType = "Database",
                Timestamp = now,
                OldValuesJson = dataNode.TryGetProperty("Old", out var old) ? old.ToString() : null,
                NewValuesJson = dataNode.TryGetProperty("New", out var @new) ? @new.ToString() : null
            });
        }

        await _db.SaveChangesAsync();
    }
}



public class TaskFailedConsumer : IConsumer<TaskFailed>
{
    private readonly IProvisioningDbContext _db;

    public TaskFailedConsumer(IProvisioningDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<TaskFailed> context)
    {
        var msg = context.Message;
        var now = DateTime.UtcNow;

        var task = await _db.OperationTasks.FindAsync(msg.OperationTaskId);
        if (task == null) return;

        task.Status = "Failed";
        task.Message = msg.Message;
        task.CompletedAt = now;

        var detail = await _db.TransactionDetails.FirstOrDefaultAsync(x => x.TransactionId == msg.TransactionId && x.ClusterId == msg.ClusterId);
        if (detail != null)
        {
            detail.Status = "Failed";
            detail.EndTime = now;
            detail.Message = msg.Message;
        }

        var dbDetail = await _db.DatabaseDetails.FirstOrDefaultAsync(x => x.DatabaseId == msg.DatabaseId && x.ClusterId == msg.ClusterId);
        if (dbDetail != null)
        {
            dbDetail.Status = "Failed";
        }

        if (task.OperationData.TryGetValue(task.Operation, out var dataNode))
        {
            _db.AuditLogs.Add(new AuditLog
            {
                OperationTaskId = task.Id,
                Operation = task.Operation,
                Action = task.Operation,
                EntityId = task.DatabaseId,
                EntityType = "Database",
                Timestamp = now,
                OldValuesJson = dataNode.TryGetProperty("Old", out var old) ? old.ToString() : null,
                NewValuesJson = dataNode.TryGetProperty("New", out var @new) ? @new.ToString() : null
            });
        }

        await _db.SaveChangesAsync();
    }
}


public interface ITaskExecutionStrategy
{
    Task ExecuteStartAsync(OperationTask task);
    Task<TaskStatusResult> PollStatusAsync(OperationTask task);
}

public interface ITaskExecutionFactory
{
    ITaskExecutionStrategy GetStrategy(string operation);
}


public class UpdateMemoryTaskStrategy : ITaskExecutionStrategy
{
    private readonly IRedisApiService _redis;

    public UpdateMemoryTaskStrategy(IRedisApiService redis)
    {
        _redis = redis;
    }

    public async Task ExecuteStartAsync(OperationTask task)
    {
        var data = task.GetData<UpdateMemoryData>();
        await _redis.UpdateMemoryAsync(task.ClusterId, task.DatabaseId, data.New);
    }

    public async Task<TaskStatusResult> PollStatusAsync(OperationTask task)
    {
        return await _redis.GetMemoryUpdateStatusAsync(task.ClusterId, task.DatabaseId);
    }
}

public class UpdateMemoryData
{
    public long Old { get; set; }
    public long New { get; set; }
}


