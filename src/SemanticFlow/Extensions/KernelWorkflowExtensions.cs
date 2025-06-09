using Microsoft.Extensions.DependencyInjection;
using SemanticFlow.Builders;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;

namespace SemanticFlow.Extensions;

/// <summary>
/// Extension methods to register Semantic Flow components into ASP.NET Core's dependency injection system.
/// </summary>
public static class KernelWorkflowExtensions
{
    /// <summary>
    /// The service key used to register and identify the semantic router activity within the DI container.
    /// </summary>
    public const string SEMANTIC_ROUTER_KEY = "semantic_router_7a6508ce";

    /// <summary>
    /// Registers a semantic router and the required workflow services.
    /// Only one semantic router can be registered.
    /// </summary>
    /// <typeparam name="TActivity">The type of the router activity implementing <see cref="IActivity"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <exception cref="InvalidOperationException">Thrown if a semantic router is already registered.</exception>
    public static void AddSemanticRouter<TActivity>(this IServiceCollection services)
        where TActivity : class, IActivity
    {

        if (IsSemanticRouterAlreadyRegistered(services))
        {
            const string message = "Only one semantic router can be registered.";
            throw new InvalidOperationException(message);
        }

        services.AddKeyedTransient<IActivity, TActivity>(SEMANTIC_ROUTER_KEY);

        RegisterWorkflowServices(services);
    }

    private static bool IsSemanticRouterAlreadyRegistered(IEnumerable<ServiceDescriptor> descriptors)
    {
        return descriptors.Any(sd => sd.ServiceKey is string key && key == SEMANTIC_ROUTER_KEY);
    }

    /// <summary>
    /// Configures the workflow infrastructure and provides a fluent API to define the workflow structure.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <returns>An instance of <see cref="KernelWorkflowBuilderStart"/> to begin building the workflow.</returns>
    public static KernelWorkflowBuilderStart AddKernelWorkflow(this IServiceCollection services)
    {
        RegisterWorkflowServices(services);

        return new KernelWorkflowBuilderStart(services, string.Empty);
    }

    private static void RegisterWorkflowServices(IServiceCollection services)
    {
        var descriptors = services.ToList();

        if (IsServiceNotRegistered<WorkflowStateService>(descriptors))
        {
            services.AddSingleton<WorkflowStateService>();
        }

        if (IsServiceNotRegistered<WorkflowService>(descriptors))
        {
            services.AddSingleton<WorkflowService>();
        }
    }

    private static bool IsServiceNotRegistered<T>(IEnumerable<ServiceDescriptor> descriptors)
    {
        return descriptors.All(sd => sd.ServiceType != typeof(T));
    }

    /// <summary>
    /// Configures a named workflow infrastructure and provides a fluent API to define its structure.
    /// Named workflows require a registered semantic router and use keyed service registration internally.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <param name="name">
    /// The name of the workflow.
    /// </param>
    /// <returns>
    /// An instance of <see cref="KernelWorkflowBuilderStart"/> to begin building the named workflow.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no semantic router is registered prior to invoking this method.
    /// Named workflows require semantic routing for proper service resolution.
    /// </exception>
    public static KernelWorkflowBuilderStart AddKernelWorkflow(this IServiceCollection services, string name)
    {
        if (IsSemanticRouterAlreadyRegistered(services))
        {
            RegisterWorkflowServices(services);

            return new KernelWorkflowBuilderStart(services, name);
        }

        throw new InvalidOperationException("Named workflows work only with registered semantic router.");
    }
}
