using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OllamaApiFacade.Extensions;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;
using System.ComponentModel;
using System.Text.Json;
using SemanticFlow.DemoSemanticRouting.Workflows.Support.DTOs;

namespace SemanticFlow.DemoSemanticRouting.Workflows.Support;

public class CheckOrderStatusActivity(
    IHttpContextAccessor httpContextAccessor,
    WorkflowService workflowService,
    Kernel kernel) : IActivity
{
    private static readonly List<OrderStatusInfo> DummyOrders =
    [
        new()
        {
            OrderId = "A123",
            CustomerName = "Gregor Biswanger",
            OrderedAt = DateTime.UtcNow.AddMinutes(-35),
            EstimatedDelivery = DateTime.UtcNow.AddMinutes(10),
            Status = "OutForDelivery",
            DriverName = "Luigi",
            LastKnownLocation = "3 blocks from destination"
        },
        new()
        {
            OrderId = "B456",
            CustomerName = "Jane Doe",
            OrderedAt = DateTime.UtcNow.AddMinutes(-20),
            EstimatedDelivery = DateTime.UtcNow.AddMinutes(25),
            Status = "InKitchen",
            DriverName = null,
            LastKnownLocation = null
        }
    ];

    public string SystemPrompt { get; set; } =
        File.ReadAllText("./Workflows/Support/CheckOrderStatusActivity.SystemPrompt.txt");

    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new AzureOpenAIPromptExecutionSettings
    {
        ModelId = "gpt-35-turbo",
        Temperature = 0.3,
        MaxTokens = 300,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };

    [KernelFunction]
    [Description("Provides a natural language summary of the customer's order status using name or order number.")]
    public string GetOrderStatus(
        [Description("The full name of the customer. Can be null if order number is provided.")] string? customerName = null,
        [Description("The order number of the customer's order. Can be null if name is provided.")] string? orderNumber = null)
    {
        var chatId = httpContextAccessor.HttpContext?.GetChatRequest().ChatId;

        OrderStatusInfo? order = null;

        if (!string.IsNullOrWhiteSpace(orderNumber))
        {
            order = DummyOrders.FirstOrDefault(o =>
                o.OrderId.Equals(orderNumber, StringComparison.OrdinalIgnoreCase));
        }

        if (order == null && !string.IsNullOrWhiteSpace(customerName))
        {
            order = DummyOrders.FirstOrDefault(o =>
                o.CustomerName.Equals(customerName, StringComparison.OrdinalIgnoreCase));
        }

        if (order == null)
        {
            return "I'm sorry, I couldn't find an order with that information. Could you please double-check your name or order number?";
        }

        workflowService.CompleteActivity(chatId, order, kernel);

        var json = JsonSerializer.Serialize(order, new JsonSerializerOptions { WriteIndented = true });

        return @$"Please summarize the following order status in a friendly and helpful tone:
###
{json}";
    }
}
