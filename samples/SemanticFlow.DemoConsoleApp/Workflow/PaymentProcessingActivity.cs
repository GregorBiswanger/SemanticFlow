using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SemanticFlow.DemoConsoleApp.Services;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;

namespace SemanticFlow.DemoConsoleApp.Workflow;

public class PaymentProcessingActivity(WorkflowService workflowService,
    SessionService sessionService,
    Kernel kernel) : IActivity
{
    public string SystemPrompt { get; set; } = File.ReadAllText("./Workflow/PaymentProcessingActivity.SystemPrompt.txt");

    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new AzureOpenAIPromptExecutionSettings
    {
        ModelId = "gpt-35-turbo",
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        Temperature = 0.1,
        MaxTokens = 256
    };

    [KernelFunction, Description("Finalizes the payment method selected by the customer for the delivery.")]
    [return: Description("A confirmation message indicating that the customer's payment method has been recorded.")]
    public string PaymentMethodSelected(
        [Description("The payment method selected by the customer. Options are: 'Bar', 'Kreditkarte', or 'PayPal'.")] string paymentMethod)
    {
        var id = sessionService.GetId();
        var nextActivity = workflowService.CompleteActivity(id, paymentMethod, kernel);

        return @$"{nextActivity.SystemPrompt} ###
                  {workflowService.WorkflowState.DataFrom(id).ToPromptString()}";
    }
}