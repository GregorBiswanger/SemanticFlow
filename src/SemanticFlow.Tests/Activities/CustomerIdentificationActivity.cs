using System.ComponentModel;
using Microsoft.SemanticKernel;
using SemanticFlow.Interfaces;

namespace SemanticFlow.Tests.Activities;

public class CustomerIdentificationActivity : IActivity
{
    public string SystemPrompt { get; set; } = "Customer Identification Prompt";
    public PromptExecutionSettings PromptExecutionSettings { get; set; }

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
}