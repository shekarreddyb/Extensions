<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MassTransit" Version="8.*" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="7.*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="7.*" />
  </ItemGroup>

</Project>


using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ======================================================
// 1) Define your message types (events/commands)
// ======================================================
public record RedisProvisionEvent
{
    public string AppId { get; init; }
    public string DataCenter { get; init; }
    public DateTime RequestedAt { get; init; }
}

public record PaymentProcessedEvent
{
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public DateTime Timestamp { get; init; }
}

// ======================================================
// 2) Define Consumers that handle these messages
// ======================================================
public class RedisProvisionConsumer : IConsumer<RedisProvisionEvent>
{
    public async Task Consume(ConsumeContext<RedisProvisionEvent> context)
    {
        // Example logic for provisioning
        var message = context.Message;
        Console.WriteLine($"[RedisProvisionConsumer] Received request:");
        Console.WriteLine($"   AppId = {message.AppId}");
        Console.WriteLine($"   DataCenter = {message.DataCenter}");
        Console.WriteLine($"   RequestedAt = {message.RequestedAt}");

        // Simulate some async provisioning work
        await Task.Delay(500);

        Console.WriteLine($"[RedisProvisionConsumer] Provisioning complete for {message.AppId} at {message.DataCenter}");
    }
}

public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;
        Console.WriteLine($"[PaymentProcessedConsumer] Payment {message.PaymentId} processed:");
        Console.WriteLine($"   Amount = {message.Amount}");
        Console.WriteLine($"   Timestamp = {message.Timestamp}");

        // Simulate some async work, e.g. updating a DB
        await Task.Delay(300);

        Console.WriteLine($"[PaymentProcessedConsumer] Done handling payment {message.PaymentId}");
    }
}

// ======================================================
// 3) Program: set up Host + MassTransit
// ======================================================
public class Program
{
    public static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // MassTransit configuration
                services.AddMassTransit(x =>
                {
                    // Add our consumers
                    x.AddConsumer<RedisProvisionConsumer>();
                    x.AddConsumer<PaymentProcessedConsumer>();

                    // Option A: Single Endpoint for everything
                    x.UsingInMemory((context, cfg) =>
                    {
                        // "my-app-queue" is just an endpoint name (queue name)
                        cfg.ReceiveEndpoint("my-app-queue", e =>
                        {
                            // For each consumer:
                            e.ConfigureConsumer<RedisProvisionConsumer>(context);
                            e.ConfigureConsumer<PaymentProcessedConsumer>(context);
                        });
                    });

                    // -----------------------------------------------------------
                    // Option B (Alternative): Multiple Endpoints
                    //
                    // x.UsingInMemory((context, cfg) =>
                    // {
                    //     cfg.ReceiveEndpoint("redis-events-queue", e =>
                    //     {
                    //         e.ConfigureConsumer<RedisProvisionConsumer>(context);
                    //     });
                    //
                    //     cfg.ReceiveEndpoint("payment-events-queue", e =>
                    //     {
                    //         e.ConfigureConsumer<PaymentProcessedConsumer>(context);
                    //     });
                    // });
                    // -----------------------------------------------------------
                });
            })
            .Build();

        // Start the host, which starts MassTransit in the background
        await host.StartAsync();

        // 4) Publish some example messages
        var bus = host.Services.GetRequiredService<IBus>();

        // Publish a RedisProvisionEvent
        await bus.Publish(new RedisProvisionEvent
        {
            AppId = "MyCoolApp",
            DataCenter = "us-east-1",
            RequestedAt = DateTime.UtcNow
        });

        // Publish a PaymentProcessedEvent
        await bus.Publish(new PaymentProcessedEvent
        {
            PaymentId = Guid.NewGuid(),
            Amount = 123.45m,
            Timestamp = DateTime.UtcNow
        });

        Console.WriteLine("Messages published. Waiting a bit for consumers to process...");

        // Wait a moment so we can see the consumers run
        await Task.Delay(2000);

        // 5) Stop the host
        await host.StopAsync();
    }
}