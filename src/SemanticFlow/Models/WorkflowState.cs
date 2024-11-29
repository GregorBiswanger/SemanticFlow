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
    /// Serializes the collected data into a JSON string format.
    /// This can be used as part of a system prompt or context for AI models to process the accumulated workflow data.
    /// </summary>
    /// <returns>A JSON string representation of the collected data.</returns>
    public string ToPromptString()
    {
        return JsonSerializer.Serialize(CollectedData) ?? "";
    }
}
