using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticFlow.Extensions;
using SemanticFlow.Interfaces;
using SemanticFlow.Models;

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
    /// If no workflow is active, a routing activity is selected and registered.
    /// If the end of the workflow is reached, the workflow state is reset for the next invocation.
    /// The selected activity's methods are registered as functions in the Semantic Kernel.
    /// </summary>
    /// <param name="id">The unique identifier of the workflow session.</param>
    /// <param name="kernel">The Semantic Kernel instance used to register activity functions.</param>
    /// <returns>
    /// The activity to be executed. Returns a routing activity if no workflow has been started yet.
    /// Throws an <see cref="InvalidOperationException"/> if no activities are registered for the workflow.
    /// </returns>
    public IActivity? GetCurrentActivity(string id, Kernel kernel)
    {
        logger?.LogInformation("Fetching current activity for workflow session {WorkflowId}", id);

        var workflowState = WorkflowState.DataFrom(id);

        if (ShouldUseRoutingActivity(workflowState))
        {
            workflowState.CurrentWorkflowName = "semantic-router";

            var routingActivity = serviceProvider.GetKeyedService<IActivity>(KernelWorkflowExtensions.SEMANTIC_ROUTER_KEY);
            kernel.AddFromActivity(routingActivity);

            logger?.LogDebug("Routing activity {ActivityName} returned for workflow session {WorkflowId}", routingActivity.GetType().Name, id);

            return routingActivity;
        }

        var activities = GetActivitiesForWorkflow(workflowState.CurrentWorkflowName).ToList();

        if (activities.Count == 0)
        {
            logger?.LogWarning("No activities found for workflow '{Workflow}' in session {WorkflowId}", workflowState.CurrentWorkflowName, id);
            throw new InvalidOperationException($"No activities found for workflow '{workflowState.CurrentWorkflowName}' in session {id}");
        }

        if (IsReachedTheEndOfActivities(workflowState, activities))
        {
            logger?.LogInformation("Workflow {WorkflowId} is complete. Resetting workflow state.", id);
            workflowState.Reset();
        }

        var activity = activities[workflowState.CurrentActivityIndex];
        kernel.AddFromActivity(activity);

        logger?.LogDebug("Returning activity {ActivityName} for workflow session {WorkflowId}", activity.GetType().Name, id);

        return activity;
    }

    private bool ShouldUseRoutingActivity(WorkflowState workflowState)
    {
        var routingActivity = serviceProvider.GetKeyedService<IActivity>(KernelWorkflowExtensions.SEMANTIC_ROUTER_KEY);

        if (workflowState.CurrentWorkflowName == "semantic-router")
        {
            return true;
        }

        return workflowState.CurrentWorkflowName == string.Empty && routingActivity != null;
    }

    private IEnumerable<IActivity> GetActivitiesForWorkflow(string workflowKey)
    {
        return string.IsNullOrWhiteSpace(workflowKey) ? serviceProvider.GetServices<IActivity>() : serviceProvider.GetKeyedServices<IActivity>(workflowKey);
    }

    private bool IsReachedTheEndOfActivities(WorkflowState workflowState, IList<IActivity> activities)
    {
        return workflowState.CurrentActivityIndex >= activities.Count;
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
    public IActivity? CompleteActivity(string id, Kernel kernel) => CompleteActivity(id, null, kernel);

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
    public IActivity? CompleteActivity(string id, object? data, Kernel kernel)
    {
        logger?.LogInformation("Completing activity for workflow session {WorkflowId}", id);

        return UpdateWorkflowState(
            id,
            kernel,
            workflowState =>
            {
                if (data != null)
                {
                    workflowState.CollectedData.Add(data);
                }
                workflowState.CurrentActivityIndex++;
            },
            "An error occurred while completing the activity."
        );
    }

    /// <summary>
    /// Navigates to the activity of the specified type for the given workflow session.
    /// Updates the workflow state to point to the target activity and returns the current activity.
    /// </summary>
    /// <typeparam name="T">The type of the activity to navigate to.</typeparam>
    /// <param name="id">The unique identifier for the workflow session.</param>
    /// <param name="kernel">The Semantic Kernel instance to use for registering activity functions.</param>
    /// <returns>
    /// The current activity to be executed, or <c>null</c> if the workflow has been completed.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when no activity of the specified type is found.</exception>
    public IActivity? GoTo<T>(string id, Kernel kernel) where T : IActivity => GoTo<T>(id, null, kernel);

    /// <summary>
    /// Navigates to the activity of the specified type for the given workflow session, recording the provided data.
    /// Updates the workflow state to point to the target activity and returns the current activity.
    /// </summary>
    /// <typeparam name="T">The type of the activity to navigate to.</typeparam>
    /// <param name="id">The unique identifier for the workflow session.</param>
    /// <param name="data">The data to be recorded for the current activity.</param>
    /// <param name="kernel">The Semantic Kernel instance to use for registering activity functions.</param>
    /// <returns>
    /// The current activity to be executed, or <c>null</c> if the workflow has been completed.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when no activity of the specified type is found.</exception>
    public IActivity? GoTo<T>(string id, object? data, Kernel kernel) where T : IActivity
    {
        logger?.LogInformation("Navigating to activity of type {ActivityType} for workflow session {WorkflowId}", typeof(T).Name, id);

        var registeredActivities = serviceProvider.GetServices<IActivity>().ToList();
        var targetIndex = registeredActivities.FindIndex(activity => activity is T);

        if (targetIndex == -1)
        {
            logger?.LogWarning("No activity of type {ActivityType} found for workflow session {WorkflowId}", typeof(T).Name, id);
            throw new InvalidOperationException($"No activity of type {typeof(T).Name} found.");
        }

        return UpdateWorkflowState(
            id,
            kernel,
            workflowState =>
            {
                if (data != null)
                {
                    workflowState.CollectedData.Add(data);
                }
                workflowState.CurrentActivityIndex = targetIndex;
            },
            $"An error occurred while navigating to activity of type {typeof(T).Name}."
        );
    }

    /// <summary>
    /// Updates the workflow state based on the provided action and returns the current activity.
    /// </summary>
    /// <param name="id">The unique identifier for the workflow session.</param>
    /// <param name="kernel">The Semantic Kernel instance to use for registering activity functions.</param>
    /// <param name="updateAction">The action to update the workflow state.</param>
    /// <param name="errorMessage">The error message to log if an exception occurs.</param>
    /// <returns>
    /// The current activity to be executed, or <c>null</c> if the workflow has been completed.
    /// </returns>
    private IActivity? UpdateWorkflowState(string id, Kernel kernel, Action<WorkflowState> updateAction, string errorMessage)
    {
        try
        {
            WorkflowState.UpdateDataContext(id, updateAction);
            logger?.LogDebug("Workflow state updated successfully for session {WorkflowId}", id);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, errorMessage);
            throw;
        }

        return GetCurrentActivity(id, kernel);
    }

    /// <summary>
    /// Determines whether a workflow is currently active for the specified session ID.
    /// </summary>
    /// <param name="id">The unique identifier for the workflow session.</param>
    /// <returns><c>true</c> if the workflow is active for the session; otherwise, <c>false</c>.</returns>
    public bool IsWorkflowActiveFor(string id)
    {
        return WorkflowState.IsWorkflowActiveFor(id);
    }

    /// <summary>
    /// Determines whether a workflow is not active for the specified session ID.
    /// </summary>
    /// <param name="id">The unique identifier for the workflow session.</param>
    /// <returns><c>true</c> if the workflow is not active for the session; otherwise, <c>false</c>.</returns>
    public bool IsWorkflowNotActiveFor(string id)
    {
        return !IsWorkflowActiveFor(id);
    }

    /// <summary>
    /// Activates the specified workflow for the given session by resetting its state and setting the new workflow name.
    /// After the update, the current activity of the workflow is resolved and returned via <see cref="GetCurrentActivity"/>.
    /// </summary>
    /// <param name="id">The unique identifier of the workflow session.</param>
    /// <param name="workflowName">The name of the workflow to activate.</param>
    /// <param name="kernel">The Semantic Kernel instance used to register activity functions.</param>
    /// <returns>
    /// The first activity of the specified workflow, or <c>null</c> if the workflow has no registered activities.
    /// </returns>
    /// <exception cref="Exception">
    /// Propagates any exception that occurs during workflow state update or activity resolution.
    /// </exception>
    public IActivity UseWorkflow(string id, string workflowName, Kernel kernel)
    {
        logger?.LogInformation("Activating workflow '{WorkflowName}' for session {WorkflowId}", workflowName, id);

        var activity = UpdateWorkflowState(
            id,
            kernel,
            workflowState =>
            {
                workflowState.Reset();
                workflowState.CurrentWorkflowName = workflowName;
            },
            $"Failed to activate workflow '{workflowName}' for session '{id}'."
        );

        return activity;
    }

    public IActivity GoToRouting(string id, Kernel kernel)
    {
        logger?.LogInformation("Activating Semantic Routing for session {id}", id);

        var routingActivity = serviceProvider.GetKeyedService<IActivity>(KernelWorkflowExtensions.SEMANTIC_ROUTER_KEY);

        if (routingActivity == null)
        {
            throw new InvalidOperationException("No Semantic Route is configuraed. Plase add an Activity as Semantic Router with services.AddSemanticRouter<RouterActivity>();");
        }

        var activity = UpdateWorkflowState(
            id,
            kernel,
            workflowState =>
            {
                workflowState.Reset();
                workflowState.CurrentWorkflowName = string.Empty;
            },
            $"Failed to activate Semantic Routing for session '{id}'."
        );

        return activity;
    }
}