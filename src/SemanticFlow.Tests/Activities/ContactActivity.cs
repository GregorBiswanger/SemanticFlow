using Microsoft.SemanticKernel;
using SemanticFlow.Interfaces;

namespace SemanticFlow.Tests.Activities;

public class ContactActivity : IActivity
{
    public string SystemPrompt { get; set; }
    public PromptExecutionSettings PromptExecutionSettings { get; set; }
}