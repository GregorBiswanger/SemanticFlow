using Microsoft.Extensions.DependencyInjection;
using SemanticFlow.Builders;
using SemanticFlow.Services;

namespace SemanticFlow.Extensions;

public static class KernelWorkflowExtensions
{
    /// <summary>
    /// Configures the workflow state machine and provides a fluent API to define the workflow structure.
    /// This method serves as the entry point for building a workflow by chaining activities in the desired execution order.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the workflow services will be added.</param>
    /// <returns>An instance of <see cref="KernelWorkflowBuilderStart"/> to begin defining the workflow with activities.</returns>
    public static KernelWorkflowBuilderStart AddKernelWorkflow(this IServiceCollection services)
    {
        services.AddSingleton<WorkflowStateService>();
        services.AddSingleton<WorkflowService>();

        return new KernelWorkflowBuilderStart(services);
    }
}