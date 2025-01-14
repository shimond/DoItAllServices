using Infra.Messaging.Models;
using Infra.Messaging;
using Microsoft.AspNetCore.SignalR;
using AlertingService.Hubs;

namespace AlertingService;

public class VitalsMonitorWorker : BackgroundService
{
    private readonly ILogger<VitalsMonitorWorker> _logger;
    private readonly IHubContext<VitalsHub> _hubContext;
    private readonly IEventBus _eventBus;

    public VitalsMonitorWorker(ILogger<VitalsMonitorWorker> logger, IHubContext<VitalsHub> hubContext, IEventBus eventBus)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventBus = eventBus;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to PatientVitalsUpdatedEvent
        _eventBus.Subscribe<PatientVitalsUpdatedEvent>(async (vitalsEvent) =>
        {
            if (vitalsEvent != null && !IsVitalsNormal(vitalsEvent.VitalsData))
            {
                // Notify clients subscribed to this patient about the critical condition
                await _hubContext.Clients.Group(vitalsEvent.PatientId.ToString())
                    .SendAsync("ReceiveAlert", $"Patient {vitalsEvent.PatientId} has a critical condition!", stoppingToken);
            }
        });

        return Task.CompletedTask;
    }

    private bool IsVitalsNormal(string vitalsData)
    {
        // Logic to determine if the patient's vitals are within normal limits
        // For simplicity, we assume vitals are critical if heart rate exceeds 100
        return !vitalsData.Contains("101");
    }
}

public class PatientVitalsUpdatedEvent : IntegrationEvent
{
    public int PatientId { get; set; }
    public string VitalsData { get; set; }
    public DateTime Timestamp { get; set; }
}