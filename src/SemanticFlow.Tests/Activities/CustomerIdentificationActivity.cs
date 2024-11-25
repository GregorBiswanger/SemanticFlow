using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SemanticFlow.Interfaces;

namespace SemanticFlow.Tests.Activities;

public class CustomerIdentificationActivity : IActivity
{
    public string SystemPrompt { get; set; } = "Customer Identification Prompt";

    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new AzureOpenAIPromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };

    [KernelFunction]
    [Description("Identifies the customer based on their input.")]
    public string IdentifyCustomer(string input)
    {
        return $"Identified customer: {input}";
    }

    [KernelFunction]
    [Description("Ends the customer identification process.")]
    public string CompleteCustomerIdentification(string input)
    {
        return $"Customer identification completed for: {input}";
    }

    [KernelFunction]
    [Description("Fix bug with KernelFunction without parameters")]
    public string Done()
    {
        return string.Empty;
    }
}