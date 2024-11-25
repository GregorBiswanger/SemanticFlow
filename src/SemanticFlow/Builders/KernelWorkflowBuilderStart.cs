using Microsoft.Extensions.DependencyInjection;
using SemanticFlow.Interfaces;

namespace SemanticFlow.Builders;

public class KernelWorkflowBuilderStart(IServiceCollection services)
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
        services.AddTransient<TActivity>();
        services.AddTransient<IActivity, TActivity>();

        return new KernelWorkflowBuilderThen(services);
    }
}