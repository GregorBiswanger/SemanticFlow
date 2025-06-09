using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace SemanticFlow.Models;

/// <summary>
/// Represents the state of a workflow, tracking the current activity and the data collected throughout the workflow process.
/// This class is used to maintain and serialize the workflow's progress and context.
/// </summary>
public class WorkflowState
{
    /// <summary>
    /// Gets or sets the index of the current activity in the workflow.
    /// This index determines which activity is currently being executed within the defined workflow sequence.
    /// </summary>
    public int CurrentActivityIndex { get; set; } = 0;

    /// <summary>
    /// Gets or sets the list of data collected during the workflow execution.
    /// Each entry in the list represents a piece of data captured at a specific step in the workflow.
    /// </summary>
    public List<object> CollectedData { get; set; } = new();

    /// <summary>
    /// Gets or sets the chat history associated with the workflow.
    /// This property maintains a record of all chat interactions that have occurred during the workflow execution.
    /// </summary>
    public ChatHistory ChatHistory { get; set; } = new();

    /// <summary>
    /// Gets or sets the count of how many times the workflow has been completed for this session.
    /// This property is incremented each time the workflow completes all activities and starts over.
    /// </summary>
    public int WorkflowCompletionCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the identifier of the currently selected workflow.
    /// Used for routing keyed service resolution and tracking workflow execution.
    /// </summary>
    public string CurrentWorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// Serializes the collected data into a JSON string format.
    /// This can be used as part of a system prompt or context for AI models to process the accumulated workflow data.
    /// </summary>
    /// <returns>A JSON string representation of the collected data.</returns>
    public string ToPromptString()
    {
        return JsonSerializer.Serialize(CollectedData) ?? "";
    }

    /// <summary>
    /// Resets the workflow state, setting the current activity index back to the first activity
    /// and incrementing the workflow completion count. This method clears all collected data.
    /// </summary>
    /// <remarks>
    /// <b>Warning:</b> This method should be used with caution as it will reset all collected data.
    /// </remarks>
    public void Reset()
    {
        WorkflowCompletionCount++;
        CurrentActivityIndex = 0;
        CollectedData.Clear();
    }
}
