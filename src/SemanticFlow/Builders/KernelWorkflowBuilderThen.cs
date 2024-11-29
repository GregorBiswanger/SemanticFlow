using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticFlow.Interfaces;

namespace SemanticFlow.Builders;

public class KernelWorkflowBuilderThen(IServiceCollection services, ILogger<KernelWorkflowBuilderThen>? logger)
{
    /// <summary>
    /// Registers the specified activity type as the next step in the workflow state machine,
    /// defining its position in the workflow execution order.
    /// This activity will be made available via dependency injection, but it is recommended
    /// to access it using the <see cref="WorkflowService"/> for better state management and control.
    /// </summary>
    /// <typeparam name="TActivity">The type of the activity to register. Must implement <see cref="IActivity"/>.</typeparam>
    /// <returns>The current <see cref="KernelWorkflowBuilderThen"/> instance to enable further chaining of workflow steps.</returns>
    public KernelWorkflowBuilderThen Then<TActivity>() where TActivity : class, IActivity
    {
        logger?.LogTrace("Registering the activity {ActivityType} as the next step in the workflow.", typeof(TActivity).Name);

        try
        {
            services.AddTransient<TActivity>();
            logger?.LogDebug("Registered {ActivityType} as a transient service.", typeof(TActivity).Name);

            services.AddTransient<IActivity, TActivity>();
            logger?.LogDebug("Registered {ActivityType} as a transient implementation of IActivity.", typeof(TActivity).Name);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while registering the next activity {ActivityType}.", typeof(TActivity).Name);
            throw;
        }

        return this;
    }

    /// <summary>
    /// Registers the specified activity type as the final step in the workflow state machine,
    /// marking it as the conclusion of the defined workflow sequence.
    /// Like other activities, this type will be available via dependency injection, but
    /// it is recommended to retrieve it using the <see cref="WorkflowService"/> to ensure
    /// correct state handling and execution order.
    /// </summary>
    /// <typeparam name="TActivity">The type of the activity to register. Must implement <see cref="IActivity"/>.</typeparam>
    public void EndsWith<TActivity>() where TActivity : class, IActivity
    {
        logger?.LogTrace("Registering the activity {ActivityType} as the final step in the workflow.", typeof(TActivity).Name);

        try
        {
            services.AddTransient<TActivity>();
            logger?.LogDebug("Registered {ActivityType} as a transient service.", typeof(TActivity).Name);

            services.AddTransient<IActivity, TActivity>();
            logger?.LogDebug("Registered {ActivityType} as a transient implementation of IActivity.", typeof(TActivity).Name);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while registering the final activity {ActivityType}.", typeof(TActivity).Name);
            throw;
        }

        var activities = services.BuildServiceProvider().GetServices<IActivity>();
        var registeredActivities = activities.Select(a => a.GetType().Name);

        logger?.LogInformation("Workflow registered: {Activities}", string.Join(" -> ", registeredActivities));
    }
}