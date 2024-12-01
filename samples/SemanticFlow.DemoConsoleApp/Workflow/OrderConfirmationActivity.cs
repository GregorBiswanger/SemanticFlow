using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SemanticFlow.DemoConsoleApp.Services;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;

namespace SemanticFlow.DemoConsoleApp.Workflow;

public class OrderConfirmationActivity(WorkflowService workflowService,
    SessionService sessionService,
    Kernel kernel) : IActivity
{
    public string SystemPrompt { get; set; } =
        File.ReadAllText("./Workflow/OrderConfirmationActivity.SystemPrompt.txt");

    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new AzureOpenAIPromptExecutionSettings
    {
        ModelId = "gpt-35-turbo",
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        Temperature = 0.1,
        MaxTokens = 256
    };

    [KernelFunction, Description("Finalizes the order by confirming all details and sending the complete order for processing.")]
    [return: Description("A confirmation message indicating that the customer's order has been approved and is being processed.")]
    public string OrderApproved(
        [Description("The complete order details as a JSON string, including the customer's name, address, phone number, order items, payment method, and total cost.")] string completeOrder)
    {
        var id = sessionService.GetId();
        workflowService.CompleteActivity(id, completeOrder, kernel);

        Console.WriteLine("Incoming Order:" + workflowService.WorkflowState.DataFrom(id).ToPromptString());

        return $"Vielen Dank für Ihre Bestellung bei La Bella Pizza! Wir kümmern uns darum, dass Ihre Pizza schnell bei Ihnen ist.";
    }

}