namespace LibraryDemo.Infrastructure.Messaging;

public static class RabbitMqConfig
{
    public const string OutboundExchange = "libraryExchange";
    public const string InboundExchange  = "externalExchange";
    public const string InboundQueue     = "libraryQueue";
}
