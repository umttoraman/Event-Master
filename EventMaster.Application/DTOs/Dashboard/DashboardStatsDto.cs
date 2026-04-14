namespace EventMaster.Application.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalEvents { get; set; }
    public int ActiveRooms { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingApprovals { get; set; }
}
