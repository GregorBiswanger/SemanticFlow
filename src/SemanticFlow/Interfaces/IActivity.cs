using Microsoft.SemanticKernel;

namespace SemanticFlow.Interfaces;

/// <summary>
/// Defines a contract for creating a custom activity that represents a single step in a workflow process.
/// This interface allows the implementation of classes with customizable system prompts, 
/// model configuration, and optional kernel functions.
/// </summary>
public interface IActivity
{
    /// <summary>
    /// Gets or sets the system prompt associated with this activity.
    /// The system prompt defines the instructions or context provided to the AI model for this specific activity.
    /// </summary>
    string SystemPrompt { get; set; }

    /// <summary>
    /// Gets or sets the desired model execution settings for this activity.
    /// These settings allow for customization of the AI model, including selecting the model type
    /// and configuring parameters such as temperature or max tokens.
    /// </summary>
    PromptExecutionSettings PromptExecutionSettings { get; set; }
}
