using Microsoft.Extensions.DependencyInjection;
using SemanticFlow.Interfaces;

namespace SemanticFlow.Builders;

public class KernelWorkflowBuilderThen(IServiceCollection services, string name)
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
        WorkflowRegistration.RegisterActivity<TActivity>(services, name);

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

        WorkflowRegistration.RegisterActivity<TActivity>(services, name);
    }
}