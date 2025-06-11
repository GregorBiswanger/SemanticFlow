using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OllamaApiFacade.Extensions;
using SemanticFlow.DemoWebApi.Workflow.DTOs;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;

namespace SemanticFlow.DemoSemanticRouting.Workflows.PizzaOrder;

public class CustomerIdentificationActivity(IHttpContextAccessor httpContextAccessor,
    WorkflowService workflowService,
    Kernel kernel) : IActivity
{
    public string SystemPrompt { get; set; } = File.ReadAllText("./Workflows/PizzaOrder/CustomerIdentificationActivity.SystemPrompt.txt");

    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new AzureOpenAIPromptExecutionSettings
    {
        ModelId = "gpt-35-turbo",
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        Temperature = 0.7,
        MaxTokens = 256
    };

    [KernelFunction]
    [Description("Confirms the customer's data for the pizza delivery.")]
    public string CustomerDataApproved(Customer customer)
    {
        var chatId = httpContextAccessor.HttpContext?.GetChatRequest().ChatId;
        var nextActivity = workflowService.CompleteActivity(chatId, customer, kernel);

        return @$"{nextActivity.SystemPrompt} ###
                      {workflowService.WorkflowState.DataFrom(chatId).ToPromptString()}";
    }
}