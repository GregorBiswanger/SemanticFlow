using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using SemanticFlow.Interfaces;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SemanticFlow.Extensions;

public static class KernelExtensions
{
    /// <summary>
    /// Registers all methods marked with the <see cref="KernelFunctionAttribute"/> from the provided activity 
    /// as functions within the Kernel. These functions are grouped under a plugin, with the plugin's name 
    /// automatically derived from the activity's type name.
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> instance where the functions will be registered.</param>
    /// <param name="activity">The activity containing the methods to register as kernel functions.</param>
    /// <remarks>
    /// This method enables dynamic registration of methods as AI functions, allowing developers to modularize 
    /// functionality into activities that can be reused across workflows.
    /// </remarks>
    public static void AddFromActivity(this Kernel kernel, IActivity activity)
    {
        var pluginName = activity.GetType().Name;
        if (kernel.Plugins.Any(plugin => plugin.Name == pluginName)) return;

        var functions = activity.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(HasKernelFunctionAttribute)
            .Select(method => kernel.CreateFunctionFromMethod(CreateDelegateForMethod(activity, method)))
            .ToList();

        kernel.Plugins.AddFromFunctions(pluginName, functions);
    }

    /// <summary>
    /// Retrieves the appropriate <see cref="IChatCompletionService"/> for the specified activity,
    /// based on the model configuration defined in the activity's <see cref="PromptExecutionSettings"/>.
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> instance that manages the chat completion services.</param>
    /// <param name="activity">The activity for which the appropriate chat completion service is required.</param>
    /// <returns>
    /// The <see cref="IChatCompletionService"/> instance configured to match the model ID and execution 
    /// settings specified in the activity's <see cref="PromptExecutionSettings"/>.
    /// </returns>
    /// <remarks>
    /// If the activity does not specify a model ID in its <see cref="PromptExecutionSettings"/>, 
    /// the default chat completion service is returned. Otherwise, the method selects the service 
    /// that matches the specified model ID.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no matching chat completion service is found for the specified model ID.
    /// </exception>
    public static IChatCompletionService GetChatCompletionForActivity(this Kernel kernel, IActivity activity)
    {
        if (IsDefaultModel(activity))
            return kernel.GetRequiredService<IChatCompletionService>();

        var chatCompletionServices = kernel.Services.GetServices<IChatCompletionService>();
        var chatCompletionService = chatCompletionServices.FirstOrDefault(service => MatchesModelId(service, activity));

        if (chatCompletionService == null)
        {
            throw new InvalidOperationException("No matching chat completion service found for the specified model ID.");
        }

        return chatCompletionService;
    }

    private static bool HasKernelFunctionAttribute(MethodInfo method) =>
        method.GetCustomAttribute<KernelFunctionAttribute>() != null;

    private static Delegate CreateDelegateForMethod(IActivity activity, MethodInfo method)
    {
        var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
        var delegateType = parameterTypes.Any()
            ? Expression.GetFuncType(parameterTypes.Concat([method.ReturnType]).ToArray())
            : typeof(Func<>).MakeGenericType(method.ReturnType);

        return Delegate.CreateDelegate(delegateType, activity, method);
    }

    private static bool IsDefaultModel(IActivity activity) =>
        activity.PromptExecutionSettings?.ModelId == null;

    private static bool MatchesModelId(IChatCompletionService service, IActivity activity) =>
        service.Attributes.TryGetValue(nameof(activity.PromptExecutionSettings.ModelId), out var modelId) &&
        modelId == activity.PromptExecutionSettings.ModelId;
}