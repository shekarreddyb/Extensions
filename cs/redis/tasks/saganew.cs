public class OperationTaskSaga : MassTransitStateMachine<OperationTaskState>
{
    private readonly ITaskExecutionFactory _factory;

    public State Polling { get; private set; }
    public State Completed { get; private set; }
    public State Failed { get; private set; }

    public Event<StartProvisioningTask> StartTask { get; private set; }
    public Event<PollProvisioningTask> Poll { get; private set; }

    public Schedule<OperationTaskState, PollProvisioningTask> PollingSchedule { get; private set; }
    private const int MaxPollingAttempts = 12;

    public OperationTaskSaga(ITaskExecutionFactory factory)
    {
        _factory = factory;
        InstanceState(x => x.CurrentState);

        Event(() => StartTask, x => x.CorrelateById(context => context.Message.OperationTaskId));
        Event(() => Poll, x => x.CorrelateById(context => context.Message.OperationTaskId));

        Schedule(() => PollingSchedule, x => x.CorrelationId, x =>
        {
            x.Delay = TimeSpan.FromSeconds(10); // Initial delay
            x.Received = e => e.CorrelateById(context => context.Message.OperationTaskId);
        });

        Initially(
            When(StartTask)
                .ThenAsync(context => HandleStartTask(context))
                .Schedule(PollingSchedule, context => context.Init<PollProvisioningTask>(new
                {
                    OperationTaskId = context.Saga.CorrelationId
                }), context => TimeSpan.FromSeconds(10)) // Initial schedule
                .TransitionTo(Polling)
        );

        During(Polling,
            When(Poll)
                .ThenAsync(context => HandlePoll(context))
        );

        SetCompletedWhenFinalized();
    }

    private async Task HandleStartTask(BehaviorContext<OperationTaskState, StartProvisioningTask> context)
    {
        var state = context.Saga;
        var message = context.Message;

        state.TransactionId = message.TransactionId;
        state.DatabaseId = message.DatabaseId;
        state.ClusterId = message.ClusterId;
        state.Operation = message.Operation;
        state.PollingAttempt = 0;

        using var scope = context.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IProvisioningDbContext>();
        var task = await db.OperationTasks.FindAsync(message.OperationTaskId);
        var strategy = _factory.GetStrategy(message.Operation);
        await strategy.ExecuteStartAsync(task);
    }

    private async Task HandlePoll(BehaviorContext<OperationTaskState, PollProvisioningTask> context)
    {
        var state = context.Saga;
        state.PollingAttempt++;

        using var scope = context.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IProvisioningDbContext>();
        var task = await db.OperationTasks.FindAsync(state.CorrelationId);
        if (task == null)
        {
            await context.Unschedule(PollingSchedule);
            return;
        }

        var strategy = _factory.GetStrategy(state.Operation);
        var result = await strategy.PollStatusAsync(task);

        switch (result.Status)
        {
            case "Active":
            case "Success":
                await context.Publish(new TaskSucceeded
                {
                    OperationTaskId = state.CorrelationId,
                    TransactionId = state.TransactionId,
                    DatabaseId = state.DatabaseId,
                    ClusterId = state.ClusterId
                });
                state.CurrentState = "Completed";
                await context.Unschedule(PollingSchedule);
                break;

            case "Failed":
                await PublishFailure(context, "Task failed");
                await context.Unschedule(PollingSchedule);
                break;

            default:
                if (state.PollingAttempt >= MaxPollingAttempts)
                {
                    await PublishFailure(context, "Maximum polling attempts reached");
                    await context.Unschedule(PollingSchedule);
                }
                // No manual rescheduling - rely on Quartz recurring schedule
                break;
        }
    }

    private async Task PublishFailure(BehaviorContext<OperationTaskState, PollProvisioningTask> context, string message)
    {
        var state = context.Saga;
        await context.Publish(new TaskFailed
        {
            OperationTaskId = state.CorrelationId,
            TransactionId = state.TransactionId,
            DatabaseId = state.DatabaseId,
            ClusterId = state.ClusterId,
            Message = message
        });
        state.CurrentState = "Failed";
    }
}

// State and Event classes remain unchanged
public class OperationTaskState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }
    public string TransactionId { get; set; }
    public string DatabaseId { get; set; }
    public string ClusterId { get; set; }
    public string Operation { get; set; }
    public int PollingAttempt { get; set; }
}

public record StartProvisioningTask
{
    public Guid OperationTaskId { get; init; }
    public string TransactionId { get; init; }
    public string DatabaseId { get; init; }
    public string ClusterId { get; init; }
    public string Operation { get; init; }
}

public record PollProvisioningTask
{
    public Guid OperationTaskId { get; init; }
}

public record TaskSucceeded
{
    public Guid OperationTaskId { get; init; }
    public string TransactionId { get; init; }
    public string DatabaseId { get; init; }
    public string ClusterId { get; init; }
}

public record TaskFailed
{
    public Guid OperationTaskId { get; init; }
    public string TransactionId { get; init; }
    public string DatabaseId { get; init; }
    public string ClusterId { get; init; }
    public string Message { get; init; }
}