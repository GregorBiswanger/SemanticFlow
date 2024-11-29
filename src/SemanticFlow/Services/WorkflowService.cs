using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticFlow.Extensions;
using SemanticFlow.Interfaces;
using System;

namespace SemanticFlow.Services;

/// <summary>
/// Provides core functionality to manage and execute workflows in a stateful and structured manner.
/// The <see cref="WorkflowService"/> acts as the central coordinator for workflow state transitions,
/// activity execution, and integration with the Semantic Kernel.
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve registered activities.</param>
/// <param name="workflowStateService">The service responsible for managing the workflow state.</param>
public class WorkflowService(IServiceProvider serviceProvider, WorkflowStateService workflowStateService, ILogger<WorkflowService>? logger)
{
    /// <summary>
    /// Gets the service that manages the workflow state, allowing access to workflow progress and collected data.
    /// </summary>
    public WorkflowStateService WorkflowState { get; } = workflowStateService;

    /// <summary>
    /// Retrieves the current activity for the specified workflow session and prepares it for execution.
    /// This includes registering the activity's methods as functions in the Semantic Kernel.
    /// </summary>
    /// <param name="id">The unique identifier for the workflow session.</param>
    /// <param name="kernel">The Semantic Kernel instance to use for registering activity functions.</param>
    /// <returns>
    /// The current activity to be executed, or <c>null</c> if the workflow has been completed.
    /// </returns>
    public IActivity GetCurrentActivity(string id, Kernel kernel)
    {
        logger?.LogInformation("Fetching current activity for workflow session {WorkflowId}", id);

        var state = workflowStateService.DataFrom(id);
        var registeredActivities = serviceProvider.GetServices<IActivity>().ToList();

        if (state.CurrentActivityIndex >= registeredActivities.Count)
        {
            logger?.LogWarning("Workflow {WorkflowId} is complete. No more activities.", id);
            return null; // Workflow done - Todo: Still need an idea what should happen here
        }

        var activity = registeredActivities[state.CurrentActivityIndex];
        kernel.AddFromActivity(activity);

        logger?.LogDebug("Returning activity {ActivityName} for workflow session {WorkflowId}", activity.GetType().Name, id);

        return activity;
    }

    /// <summary>
    /// Completes the current activity for the specified workflow session by recording the provided data
    /// and transitioning to the next activity.
    /// </summary>
    /// <param name="id">The unique identifier for the workflow session.</param>
    /// <param name="data">The data collected from the completed activity.</param>
    /// <param name="kernel">The Semantic Kernel instance to use for the next activity.</param>
    /// <returns>
    /// The next activity to be executed, or <c>null</c> if the workflow has been completed.
    /// </returns>
    public IActivity CompleteActivity(string id, object data, Kernel kernel)
    {
        logger?.LogInformation("Completing activity for workflow session {WorkflowId} with data: {ActivityData}", id, data);

        try
        {
            workflowStateService.UpdateDataContext(id, workflowState =>
            {
                workflowState.CollectedData.Add(data);
                workflowState.CurrentActivityIndex++;
            });

            logger?.LogDebug("Activity completed successfully for workflow session {WorkflowId}. Transitioning to the next activity.", id);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while completing the activity for workflow session {WorkflowId}", id);
            throw;
        }

        return GetCurrentActivity(id, kernel);
    }

    /// <summary>
    /// Completes the current activity for the specified workflow session without providing additional data
    /// and transitions to the next activity.
    /// </summary>
    /// <param name="id">The unique identifier for the workflow session.</param>
    /// <param name="kernel">The Semantic Kernel instance to use for the next activity.</param>
    /// <returns>
    /// The next activity to be executed, or <c>null</c> if the workflow has been completed.
    /// </returns>
    public IActivity CompleteActivity(string id, Kernel kernel)
    {
        logger?.LogInformation("Completing activity without additional data for workflow session {WorkflowId}", id);

        try
        {
            workflowStateService.UpdateDataContext(id, workflowState =>
            {
                workflowState.CurrentActivityIndex++;
            });

            logger?.LogDebug("Activity completed successfully for workflow session {WorkflowId}. Transitioning to the next activity.", id);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while completing the activity without additional data for workflow session {WorkflowId}", id);
            throw;
        }

        return GetCurrentActivity(id, kernel);
    }
}