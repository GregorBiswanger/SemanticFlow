using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OllamaApiFacade.Extensions;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;

namespace SemanticFlow.DemoSemanticRouting.Workflows.Support;

public class IssueClassificationActivity(
    IHttpContextAccessor httpContextAccessor,
    WorkflowService workflowService,
    Kernel kernel) : IActivity
{
    public string SystemPrompt { get; set; } =
        File.ReadAllText("./Workflows/Support/IssueClassificationActivity.SystemPrompt.txt");

    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new AzureOpenAIPromptExecutionSettings
    {
        ModelId = "gpt-4o-mini",
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        Temperature = 0.5,
        MaxTokens = 256
    };

    [KernelFunction]
    [Description("Classifies the user's issue and proceeds with the support process.")]
    public string ClassifyIssue(
        [Description("Free-text input from the user describing their problem.")] string input,
        [Description("The issue category like 'Late delivery', 'Wrong item', etc.")] string issueCategory)
    {
        var chatId = httpContextAccessor.HttpContext?.GetChatRequest().ChatId;
        var nextActivity = workflowService.CompleteActivity(chatId, new { input, issueCategory }, kernel);

        if (issueCategory == "TrackOrder")
        {
            return "Have it confirmed whether the order is from today.";
        }

        return @$"{nextActivity.SystemPrompt} ###
                  {workflowService.WorkflowState.DataFrom(chatId).ToPromptString()}";
    }
}