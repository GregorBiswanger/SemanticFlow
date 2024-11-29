using Microsoft.Extensions.Logging;
using SemanticFlow.Models;

namespace SemanticFlow.Services;

/// <summary>
/// Provides a service for managing workflow states, allowing the retrieval and modification
/// of state data for specific workflow sessions. This service is responsible for maintaining
/// the current state of workflows identified by unique session IDs.
/// </summary>
public class WorkflowStateService(ILogger<WorkflowStateService>? logger)
{
    private readonly Dictionary<string, WorkflowState> _states = new();

    /// <summary>
    /// Retrieves the <see cref="WorkflowState"/> associated with the specified session ID.
    /// If no state exists for the given ID, a new state is created and returned.
    /// </summary>
    /// <param name="id">The unique identifier for the workflow session.</param>
    /// <returns>The <see cref="WorkflowState"/> associated with the session ID.</returns>
    public WorkflowState DataFrom(string id)
    {
        logger?.LogTrace("Retrieving workflow state for session ID {SessionId}.", id);

        if (!_states.TryGetValue(id, out var state))
        {
            logger?.LogWarning("No workflow state found for session ID {SessionId}. Creating a new state.", id);
            state = new WorkflowState();
            _states[id] = state;
        }

        logger?.LogDebug("Returning workflow state for session ID {SessionId}.", id);
        return state;
    }

    /// <summary>
    /// Updates the <see cref="WorkflowState"/> associated with the specified session ID using the provided update action.
    /// If no state exists for the given ID, a new state is created before the update is applied.
    /// </summary>
    /// <param name="id">The unique identifier for the workflow session.</param>
    /// <param name="update">An action to modify the <see cref="WorkflowState"/> associated with the session ID.</param>
    public void UpdateDataContext(string id, Action<WorkflowState> update)
    {
        logger?.LogInformation("Updating workflow state for session ID {SessionId}.", id);

        try
        {
            if (!_states.ContainsKey(id))
            {
                logger?.LogTrace("No workflow state found for session ID {SessionId}. Creating a new state.", id);

                _states[id] = new WorkflowState();
            }
            update(_states[id]);

            logger?.LogDebug("Workflow state updated successfully for session ID {SessionId}.", id);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while updating the workflow state for session ID {SessionId}.", id);
            throw;
        }
    }
}
