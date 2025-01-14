using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace AlertingService.Hubs;

public class VitalsHub : Hub
{
    public async Task SubscribeToPatient(string patientId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, patientId);
    }

    public async Task UnsubscribeFromPatient(string patientId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, patientId);
    }
}
