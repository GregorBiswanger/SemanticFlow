using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OllamaApiFacade.Extensions;
using SemanticFlow.DemoConsoleApp.Services;
using SemanticFlow.DemoConsoleApp.Workflow;
using SemanticFlow.Extensions;
using SemanticFlow.Services;

var azureOpenAiEndpoint = "https://ai-rag-workshop.openai.azure.com";
var azureOpenAiDeploymentNameGpt4oMini = "gpt-4o-mini";
var azureOpenAiDeploymentNameGpt35 = "gpt-35-turbo";

var builder = Kernel.CreateBuilder();

builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt4oMini, azureOpenAiEndpoint, new DefaultAzureCredential(), modelId: "gpt-4o-mini")
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt35, azureOpenAiEndpoint, new DefaultAzureCredential(), modelId: "gpt-35-turbo");

builder.Services.AddLogging();
builder.Services.AddTransient<SessionService>();

builder.Services.AddKernelWorkflow()
    .StartWith<CustomerIdentificationActivity>()
    .Then<MenuSelectionActivity>()
    .Then<PaymentProcessingActivity>()
    .EndsWith<OrderConfirmationActivity>();

// Analyze with Burp Suite the Semantic Kernel backend communication
//builder.Services.AddProxyForDebug();

var kernel = builder.Build();

var sessionService = kernel.GetRequiredService<SessionService>();
var id = sessionService.GetId();

var workflowService = kernel.GetRequiredService<WorkflowService>();
var chatHistory = new ChatHistory("Init system prompt");

while (true)
{
    // Necessary for this stateful example
    var kernelClone = kernel.Clone();

    var currentActivity = workflowService.GetCurrentActivity(id, kernelClone);

    var systemPrompt = currentActivity.SystemPrompt + " ### " +
                       workflowService.WorkflowState.DataFrom(id).ToPromptString();

    var systemChatMessage = new ChatMessageContent(AuthorRole.System, systemPrompt);
    chatHistory[0] = systemChatMessage;
    
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("You: ");
    string userInput = Console.ReadLine();
    chatHistory.AddUserMessage(userInput);
    Console.ResetColor();

    if (userInput?.ToLower() == "exit")
        break;

    var chatCompletions = await kernelClone.GetChatCompletionForActivity(currentActivity)
        .GetChatMessageContentsAsync(new ChatHistory(chatHistory), currentActivity.PromptExecutionSettings, kernelClone);

    var chatResponse = chatCompletions.First().ToChatResponse();
    chatHistory.AddAssistantMessage(chatResponse.Message.Content);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"AI Pizza dealer: {chatResponse.Message.Content}");
    Console.ResetColor();
}
