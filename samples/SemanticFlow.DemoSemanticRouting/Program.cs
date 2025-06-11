using Azure.Identity;
using Microsoft.SemanticKernel;
using OllamaApiFacade.Extensions;
using SemanticFlow.DemoSemanticRouting.Workflows;
using SemanticFlow.DemoSemanticRouting.Workflows.PizzaOrder;
using SemanticFlow.DemoSemanticRouting.Workflows.Support;
using SemanticFlow.Extensions;
using SemanticFlow.Services;

var builder = WebApplication.CreateBuilder(args)
    .ConfigureAsLocalOllamaApi();

var configuration = builder.Configuration;
var azureOpenAiEndpoint = configuration["AzureOpenAI:Endpoint"];
var azureOpenAiDeploymentNameGpt40Mini = configuration["AzureOpenAI:DeploymentNameGpt4oMini"];
var azureOpenAiDeploymentNameGpt35 = configuration["AzureOpenAI:DeploymentNameGpt35"];

builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt40Mini, azureOpenAiEndpoint, new DefaultAzureCredential(), modelId: "gpt-4o-mini")
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt35, azureOpenAiEndpoint, new DefaultAzureCredential(), modelId: "gpt-35-turbo");

builder.Services.AddSemanticRouter<RouterActivity>();

// Avoid magic strings in production – use constants like WorkflowNames.PizzaOrder instead
//builder.Services.AddKernelWorkflow(WorkflowNames.PizzaOrder)
builder.Services.AddKernelWorkflow("pizzaOrder")
    .StartWith<CustomerIdentificationActivity>()
    .Then<MenuSelectionActivity>()
    .Then<PaymentProcessingActivity>()
    .EndsWith<OrderConfirmationActivity>();

builder.Services.AddKernelWorkflow("support")
    .StartWith<IssueClassificationActivity>()
    .EndsWith<CheckOrderStatusActivity>();

// Analyze with Burp Suite the Semantic Kernel backend communication
//builder.Services.AddProxyForDebug();

var app = builder.Build();

app.MapPostApiChat(async (chatRequest, chatCompletionService, httpContext, kernel) =>
{
    string chatId = chatRequest.ChatId ?? string.Empty;

    var workflowService = kernel.GetRequiredService<WorkflowService>();
    var currentActivity = workflowService.GetCurrentActivity(chatId, kernel);

    var systemPrompt = currentActivity.SystemPrompt + " ### " +
                       workflowService.WorkflowState.DataFrom(chatId).ToPromptString();

    var chatHistory = chatRequest.ToChatHistory(systemPrompt);

    var chatCompletion = kernel.GetChatCompletionForActivity(currentActivity);
    await chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, currentActivity.PromptExecutionSettings, kernel)
        .StreamToResponseAsync(httpContext.Response);
});

app.Run();
