using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticFlow.Interfaces;

namespace SemanticFlow.Builders;

public class KernelWorkflowBuilderStart(IServiceCollection services, ILogger<KernelWorkflowBuilderStart>? logger)
{
    /// <summary>
    /// Registers the specified activity type as the starting point of the workflow state machine,
    /// establishing it as the first step in the defined workflow execution order.
    /// This activity will be made available via dependency injection, but it is recommended
    /// to access it through the <see cref="WorkflowService"/> to ensure proper state tracking
    /// and controlled workflow transitions.
    /// </summary>
    /// <typeparam name="TActivity">The type of the activity to register. Must implement <see cref="IActivity"/>.</typeparam>
    /// <returns>A new instance of <see cref="KernelWorkflowBuilderThen"/> to enable chaining of subsequent workflow steps.</returns>
    public KernelWorkflowBuilderThen StartWith<TActivity>() where TActivity : class, IActivity
    {
        logger?.LogTrace("Registering the activity {ActivityType} as the starting point of the workflow.", typeof(TActivity).Name);

        try
        {
            services.AddTransient<TActivity>();
            logger?.LogDebug("Registered {ActivityType} as a transient service.", typeof(TActivity).Name);

            services.AddTransient<IActivity, TActivity>();
            logger?.LogDebug("Registered {ActivityType} as a transient implementation of IActivity.", typeof(TActivity).Name);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while registering the starting activity {ActivityType}.", typeof(TActivity).Name);
            throw;
        }

        logger?.LogTrace("First Activity {ActivityType}", typeof(TActivity).Name);

        return new KernelWorkflowBuilderThen(services, services.BuildServiceProvider().GetService<ILogger<KernelWorkflowBuilderThen>>());
    }
}