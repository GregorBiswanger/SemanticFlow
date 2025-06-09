using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Services;
using SemanticFlow.Extensions;
using SemanticFlow.Tests.Activities;
using Shouldly;

namespace SemanticFlow.Tests;

public class KernelExtensionsTests
{
    [Fact]
    public void AddFromActivity_ShouldRegisterActivityPlugin_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKernel();
        var serviceProvider = services.BuildServiceProvider();
        var kernel = serviceProvider.GetService<Kernel>();

        var customerIdentificationActivity = new CustomerIdentificationActivity();

        // Act
        kernel.AddFromActivity(customerIdentificationActivity);

        // Assert
        kernel.Plugins.Count(plugin => plugin.Name == "CustomerIdentificationActivity").ShouldBe(1);
    }

    [Fact]
    public void GetChatCompletionForActivity_ShouldReturnCorrectChatCompletionService_WhenModelIdIsProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:11413");

#pragma warning disable SKEXP0070
        services.AddKernel()
            .AddOllamaChatCompletion("gpt-4", httpClient)
#pragma warning restore SKEXP0070
            .AddOllamaChatCompletion("gpt-35-turbo", httpClient);

        var serviceProvider = services.BuildServiceProvider();
        var kernel = serviceProvider.GetService<Kernel>();

        var customerIdentificationActivity = new CustomerIdentificationActivity
        {
            PromptExecutionSettings = new AzureOpenAIPromptExecutionSettings
            {
                ModelId = "gpt-4"
            }
        };

        // Act
        var chatCompletionService = kernel.GetChatCompletionForActivity(customerIdentificationActivity);

        // Assert
        chatCompletionService.GetModelId().ShouldBe("gpt-4");
    }

    [Fact]
    public void GetChatCompletionForActivity_ShouldReturnDefaultChatCompletionService_WhenModelIdIsNotProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:11413");

#pragma warning disable SKEXP0070
        services.AddKernel()
            .AddOllamaChatCompletion("gpt-4", httpClient)
#pragma warning restore SKEXP0070
            .AddOllamaChatCompletion("gpt-35-turbo", httpClient);

        var serviceProvider = services.BuildServiceProvider();
        var kernel = serviceProvider.GetService<Kernel>();

        var customerIdentificationActivity = new CustomerIdentificationActivity();

        // Act
        var chatCompletionService = kernel.GetChatCompletionForActivity(customerIdentificationActivity);

        // Assert
        chatCompletionService.GetModelId().ShouldBe("gpt-35-turbo");
    }

    [Fact]
    public void GetChatCompletionForActivity_ShouldThrowException_WhenModelIdIsIncorrect()
    {
        // Arrange
        var services = new ServiceCollection();
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:11413");

#pragma warning disable SKEXP0070
        services.AddKernel()
            .AddOllamaChatCompletion("gpt-4", httpClient)
#pragma warning restore SKEXP0070
            .AddOllamaChatCompletion("gpt-35-turbo", httpClient);

        var serviceProvider = services.BuildServiceProvider();
        var kernel = serviceProvider.GetService<Kernel>();

        var customerIdentificationActivity = new CustomerIdentificationActivity
        {
            PromptExecutionSettings = new AzureOpenAIPromptExecutionSettings
            {
                ModelId = "gpt-turbo"
            }
        };

        // Act
        Action act = () => kernel.GetChatCompletionForActivity(customerIdentificationActivity);

        // Assert
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldBe("No matching chat completion service found for the specified model ID.");
    }

}
