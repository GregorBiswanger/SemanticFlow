using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Services;
using SemanticFlow.Extensions;
using SemanticFlow.Tests.Activities;

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
        kernel.Plugins.Should().ContainSingle(plugin => plugin.Name == "CustomerIdentificationActivity");
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
        chatCompletionService.GetModelId().Should().Be("gpt-4");
    }
}
