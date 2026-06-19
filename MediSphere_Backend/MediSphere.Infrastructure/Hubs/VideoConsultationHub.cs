using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace MediSphere.Infrastructure.Hubs;

public class VideoConsultationHub : Hub
{
    public async Task JoinConsultationRoom(string appointmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{appointmentId}");
    }

    public async Task LeaveConsultationRoom(string appointmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{appointmentId}");
    }

    public async Task ToggleMediaState(string appointmentId, bool camActive, bool micActive)
    {
        await Clients.OthersInGroup($"room_{appointmentId}").SendAsync("MediaStateChanged", camActive, micActive);
    }

    public async Task SyncLivePrescription(string appointmentId, string prescriptionJson)
    {
        await Clients.OthersInGroup($"room_{appointmentId}").SendAsync("PrescriptionSynced", prescriptionJson);
    }
}
