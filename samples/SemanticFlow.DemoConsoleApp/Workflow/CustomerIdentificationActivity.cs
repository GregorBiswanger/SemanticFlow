using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SemanticFlow.DemoConsoleApp.Services;
using SemanticFlow.DemoConsoleApp.Workflow.DTOs;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;

namespace SemanticFlow.DemoConsoleApp.Workflow;

public class CustomerIdentificationActivity(WorkflowService workflowService,
    SessionService sessionService,
    Kernel kernel) : IActivity
{
    public string SystemPrompt { get; set; } = File.ReadAllText("./Workflow/CustomerIdentificationActivity.SystemPrompt.txt");

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
        var id = sessionService.GetId();
        var nextActivity = workflowService.CompleteActivity(id, customer, kernel);

        return @$"{nextActivity.SystemPrompt} ###
                      {workflowService.WorkflowState.DataFrom(id).ToPromptString()}";
    }
}