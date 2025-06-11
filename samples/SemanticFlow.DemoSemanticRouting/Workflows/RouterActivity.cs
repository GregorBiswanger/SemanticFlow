using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;
using System.ComponentModel;
using OllamaApiFacade.Extensions;

namespace SemanticFlow.DemoSemanticRouting.Workflows;

public class RouterActivity(
    IHttpContextAccessor httpContextAccessor,
    WorkflowService workflowService,
    Kernel kernel) : IActivity
{
    public string SystemPrompt { get; set; } = File.ReadAllText("./Workflows/RouterActivity.SystemPrompt.txt");

    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new AzureOpenAIPromptExecutionSettings
    {
        ModelId = "gpt-4o-mini",
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        Temperature = 0.5,
        MaxTokens = 256
    };

    [KernelFunction]
    [Description("Starts the order process if the user wants to place a pizza order.")]
    public string RouteToPizzaOrder()
    {
        var chatId = httpContextAccessor.HttpContext?.GetChatRequest().ChatId;
        var next = workflowService.UseWorkflow(chatId, "pizzaOrder", kernel);

        return @$"{next.SystemPrompt} ### {workflowService.WorkflowState.DataFrom(chatId).ToPromptString()}";
    }

    [KernelFunction]
    [Description("Starts the support process if the user has an issue or question about a previous order.")]
    public string RouteToSupport()
    {
        var chatId = httpContextAccessor.HttpContext?.GetChatRequest().ChatId;
        var next = workflowService.UseWorkflow(chatId, "support", kernel);

        return @$"{next.SystemPrompt} ### {workflowService.WorkflowState.DataFrom(chatId).ToPromptString()}";
    }
}