using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OllamaApiFacade.Extensions;
using SemanticFlow.DemoConsoleApp;
using SemanticFlow.DemoConsoleApp.Services;
using SemanticFlow.DemoConsoleApp.Workflow;
using SemanticFlow.Extensions;
using SemanticFlow.Services;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

const string keyVaultUrl = "https://ai-rag-workshop.vault.azure.net/";
var azureKeyVaultHelper = new AzureKeyVaultHelper(keyVaultUrl);
var azureOpenAiApiKey = await azureKeyVaultHelper.GetSecretAsync("AZURE-OPENAI-API-KEY");
var azureOpenAiEndpoint = "https://ai-rag-workshop.openai.azure.com";
var azureOpenAiDeploymentNameGpt4 = "gpt-4";
var azureOpenAiDeploymentNameGpt35 = "gpt-35-turbo";

var builder = Kernel.CreateBuilder();

builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt4, azureOpenAiEndpoint, azureOpenAiApiKey, modelId: "gpt-4")
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt35, azureOpenAiEndpoint, azureOpenAiApiKey, modelId: "gpt-35-turbo");

builder.Services.AddLogging();
builder.Services.AddTransient<SessionService>();

builder.Services.AddKernelWorkflow()
    .StartWith<CustomerIdentificationActivity>()
    .Then<MenuSelectionActivity>()
    .Then<PaymentProcessingActivity>()
    .EndsWith<OrderConfirmationActivity>();

builder.Services.AddProxyForDebug();

var kernel = builder.Build();

var sessionService = kernel.GetRequiredService<SessionService>();
var id = sessionService.GetId();

var workflowService = kernel.GetRequiredService<WorkflowService>();
var chatHistory = new ChatHistory("Blank System Prompt");

while (true)
{
    var currentActivity = workflowService.GetCurrentActivity(id, kernel);

    var systemPrompt = currentActivity.SystemPrompt + " ### " +
                       workflowService.WorkflowState.DataFrom(id).ToPromptString();

    var systemChatMessage = new ChatMessageContent(AuthorRole.System, systemPrompt);
    chatHistory[0] = systemChatMessage;

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Du: ");
    string userInput = Console.ReadLine();
    chatHistory.AddUserMessage(userInput);
    Console.ResetColor();

    if (userInput?.ToLower() == "exit")
        break;

    var chatCompletions = await kernel.GetChatCompletionForActivity(currentActivity)
        .GetChatMessageContentsAsync(chatHistory, currentActivity.PromptExecutionSettings, kernel);

    var chatResponse = chatCompletions.First().ToChatResponse();
    chatHistory.AddAssistantMessage(chatResponse.Message.Content);
    
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"KI: {chatResponse.Message.Content}");
    Console.ResetColor();
}
