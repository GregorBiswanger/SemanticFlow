using Microsoft.SemanticKernel;
using OllamaApiFacade.Extensions;
using SemanticFlow.DemoWebApi;
using SemanticFlow.DemoWebApi.Workflow;
using SemanticFlow.Extensions;
using SemanticFlow.Services;

var builder = WebApplication.CreateBuilder(args)
    .ConfigureAsLocalOllamaApi();

var configuration = builder.Configuration;
var keyVaultName = configuration["KeyVault:Name"];
var keyVaultUrl = $"https://{keyVaultName}.vault.azure.net";
var azureKeyVaultHelper = new AzureKeyVaultHelper(keyVaultUrl);
var azureOpenAiApiKey = await azureKeyVaultHelper.GetSecretAsync("AZURE-OPENAI-API-KEY");
var azureOpenAiEndpoint = configuration["AzureOpenAI:Endpoint"];
var azureOpenAiDeploymentNameGpt4 = configuration["AzureOpenAI:DeploymentNameGpt4"];
var azureOpenAiDeploymentNameGpt35 = configuration["AzureOpenAI:DeploymentNameGpt35"];

builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt4, azureOpenAiEndpoint, azureOpenAiApiKey, modelId: "gpt-4")
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt35, azureOpenAiEndpoint, azureOpenAiApiKey, modelId: "gpt-35-turbo");

builder.Services.AddKernelWorkflow()
    .StartWith<CustomerIdentificationActivity>()
    .Then<MenuSelectionActivity>()
    .Then<PaymentProcessingActivity>()
    .EndsWith<OrderConfirmationActivity>();

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
