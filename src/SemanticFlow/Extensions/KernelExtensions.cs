using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticFlow.Interfaces;

namespace SemanticFlow.Extensions;

public static class KernelExtensions
{
    /// <summary>
    /// Registers all methods marked with the <see cref="KernelFunctionAttribute"/> from the provided instance 
    /// as functions within the Kernel. These functions are grouped under a plugin, with the plugin's name 
    /// automatically derived from the instance's type name.
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> instance where the functions will be registered.</param>
    /// <param name="instance">The instance containing the methods to register as kernel functions.</param>
    /// <remarks>
    /// This method enables dynamic registration of methods as AI functions, allowing developers to modularize 
    /// functionality into activities that can be reused across workflows.
    /// </remarks>
    public static void AddFromActivity(this Kernel kernel, object instance)
    {
        var pluginName = instance.GetType().Name;
        if (kernel.Plugins.Any(plugin => plugin.Name == pluginName))
        {
            return;
        }

        var methods = instance.GetType()
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.GetCustomAttribute<KernelFunctionAttribute>() != null);

        var functions = methods
            .Select(m =>
            {
                var delegateType = typeof(Func<,>).MakeGenericType(m.GetParameters()[0].ParameterType, m.ReturnType);
                var methodDelegate = Delegate.CreateDelegate(delegateType, instance, m);
                return kernel.CreateFunctionFromMethod(methodDelegate);
            })
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
        if (activity.PromptExecutionSettings == null || string.IsNullOrEmpty(activity.PromptExecutionSettings.ModelId))
        {
            return kernel.GetRequiredService<IChatCompletionService>();
        }

        string modelIdPropertyName = nameof(activity.PromptExecutionSettings.ModelId);

        var chatCompletionServices = kernel.Services.GetServices<IChatCompletionService>();
        var chatCompletion = chatCompletionServices
            .First(service => service.Attributes.TryGetValue(modelIdPropertyName, out var modelId)
                              && modelId == activity.PromptExecutionSettings.ModelId);

        return chatCompletion;
    }
}