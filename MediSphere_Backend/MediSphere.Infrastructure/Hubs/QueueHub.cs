using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace MediSphere.Infrastructure.Hubs;

public class QueueHub : Hub
{
    public async Task JoinDepartmentQueueRoom(string departmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dept_{departmentId}");
    }

    public async Task LeaveDepartmentQueueRoom(string departmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dept_{departmentId}");
    }

    public async Task JoinDoctorQueueRoom(string doctorId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"doc_{doctorId}");
    }

    public async Task LeaveDoctorQueueRoom(string doctorId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"doc_{doctorId}");
    }
}
