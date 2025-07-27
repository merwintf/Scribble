using Wolverine;
using Wolverine.RabbitMQ;
using LibraryDemo.Infrastructure.Persistence;

namespace LibraryDemo.Infrastructure.Messaging;

public static class WolverineConfig
{
    public static void Configure(WolverineOptions options)
    {
        // Automatic transactions + EF Outbox
        options.Policies.AutoApplyTransactions();
        options.Policies.UseEntityFrameworkOutbox<LibraryDbContext>();

        options.UseRabbitMq((bus, rmq) =>
        {
            rmq.HostName = "rabbitmq";
            rmq.Username = "guest";
            rmq.Password = "guest";

            rmq.DeclareExchange(RabbitMqConfig.OutboundExchange);
            rmq.DeclareExchange(RabbitMqConfig.InboundExchange);

            bus.PublishAllMessages().ToRabbitExchange(RabbitMqConfig.OutboundExchange);

            bus.ListenToRabbitQueue(RabbitMqConfig.InboundQueue)
               .BindToExchange(RabbitMqConfig.InboundExchange);
        });
    }
}
