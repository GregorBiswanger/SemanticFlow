using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OllamaApiFacade.Extensions;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;

namespace SemanticFlow.DemoWebApi.Workflow;

public class PaymentProcessingActivity(IHttpContextAccessor httpContextAccessor,
    WorkflowService workflowService,
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
        var chatId = httpContextAccessor.HttpContext?.GetChatRequest().ChatId;
        var nextActivity = workflowService.CompleteActivity(chatId, paymentMethod, kernel);

        return @$"{nextActivity.SystemPrompt} ###
                  {workflowService.WorkflowState.DataFrom(chatId).ToPromptString()}";
    }
}