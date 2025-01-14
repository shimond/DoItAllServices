namespace Infra.Messaging.Models;
public abstract class IntegrationEvent
{
    public DateTime CreationDate { get; private set; }

    protected IntegrationEvent()
    {
        CreationDate = DateTime.UtcNow;
    }
}