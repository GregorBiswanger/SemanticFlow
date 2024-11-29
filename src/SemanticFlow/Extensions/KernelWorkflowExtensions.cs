using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        var logger = services.BuildServiceProvider().GetService<ILogger<KernelWorkflowBuilderStart>>();
        logger?.LogTrace("Adding kernel workflow services to the service collection.");

        try
        {
            // Register workflow-related services
            services.AddSingleton<WorkflowStateService>();
            logger?.LogDebug("Registered WorkflowStateService as a singleton.");

            services.AddSingleton<WorkflowService>();
            logger?.LogDebug("Registered WorkflowService as a singleton.");

            logger?.LogTrace("Kernel workflow services added successfully.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while adding kernel workflow services.");
            throw;
        }

        return new KernelWorkflowBuilderStart(services, services.BuildServiceProvider().GetService<ILogger<KernelWorkflowBuilderStart>>());
    }
}