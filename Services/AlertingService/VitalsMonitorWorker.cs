using Infra.Messaging.Models;
using Infra.Messaging;
using Microsoft.AspNetCore.SignalR;
using AlertingService.Hubs;
using AlertingService.Models;

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to PatientVitalsUpdatedEvent
        await _eventBus.SubscribeAsync<PatientVitalsUpdatedEvent>(async (vitalsEvent) =>
        {
            if (vitalsEvent != null && !IsVitalsNormal(vitalsEvent.VitalsData))
            {
                // Notify clients subscribed to this patient about the critical condition
                await _hubContext.Clients.Group(vitalsEvent.PatientId.ToString())
                    .SendAsync("ReceiveAlert", $"Patient {vitalsEvent.PatientId} has a critical condition!", stoppingToken);
            }
        });
    }

    private bool IsVitalsNormal(VitalsData vitalsData)
    {
        if (vitalsData == null)
            return true;

        // Check various vital signs for abnormalities
        if (vitalsData.HeartRate > 100) return false;  // High heart rate
        if (vitalsData.Temperature > 38.0) return false; // Fever
        if (vitalsData.BloodPressure?.Systolic > 140) return false; // High blood pressure
        if (vitalsData.BloodPressure?.Diastolic > 90) return false; // High blood pressure
        if (vitalsData.OxygenSaturation < 95) return false; // Low oxygen saturation
        if (vitalsData.RespiratoryRate > 20) return false; // High respiratory rate

        return true;
    }
}

public class PatientVitalsUpdatedEvent : IntegrationEvent
{
    public int PatientId { get; set; }
    public VitalsData VitalsData { get; set; }
    public DateTime Timestamp { get; set; }
}