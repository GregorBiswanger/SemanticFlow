using Microsoft.Extensions.DependencyInjection;
using SemanticFlow.Interfaces;

namespace SemanticFlow.Builders;

internal static class WorkflowRegistration
{
    public static void RegisterActivity<TActivity>(IServiceCollection services, string name)
        where TActivity : class, IActivity
    {
        if (name == string.Empty)
        {
            services.AddTransient<IActivity, TActivity>();
        }
        else
        {
            services.AddKeyedTransient<IActivity, TActivity>(name);
        }
    }
}