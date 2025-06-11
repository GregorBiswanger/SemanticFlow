namespace SemanticFlow.DemoSemanticRouting.Workflows.Support.DTOs;

public record OrderStatusInfo
{
    public string OrderId { get; init; }
    public string CustomerName { get; set; }
    public DateTime OrderedAt { get; init; }
    public DateTime EstimatedDelivery { get; init; }
    public string Status { get; init; } // e.g. InKitchen, OutForDelivery, Delivered
    public string? DriverName { get; init; }
    public string? LastKnownLocation { get; init; }
}
