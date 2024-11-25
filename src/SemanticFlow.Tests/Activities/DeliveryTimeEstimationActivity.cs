using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SemanticFlow.Interfaces;

namespace SemanticFlow.Tests.Activities;

public class DeliveryTimeEstimationActivity : IActivity
{
    public string SystemPrompt { get; set; } = "Delivery Time Estimation Prompt";

    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new AzureOpenAIPromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };

    [KernelFunction]
    [Description("Estimates the delivery time for a given input.")]
    public string EstimateDeliveryTime(string input)
    {
        return $"Estimated delivery time for: {input}";
    }

    [KernelFunction]
    [Description("Ends the delivery time estimation process.")]
    public string CompleteDeliveryTimeEstimation(string input)
    {
        return $"Delivery time estimation completed for: {input}";
    }
}