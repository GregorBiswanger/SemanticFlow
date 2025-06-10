# 🍕 Semantic Flow - Stateful Workflow in .NET 8 Console App

This example demonstrates how you can orchestrate complex AI workflows in a .NET 8 console application using **Semantic Flow**, the **Microsoft Semantic Kernel**, and a **stateful architecture**. A pizza order is guided through multiple steps—fully controlled by generative AI and without a web interface.

## 🔍 What makes this example special?

Unlike typical stateless applications, this example shows how you can:

- **Combine stateful services with generative AI**
- Control a complete **workflow via the console**
- Manage the state of each session separately
- Use **Semantic Flow** flexibly in classic console apps

## 🛠 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Access to Azure OpenAI (alternatively OpenAI API, if adjusted)
- Optional: Burp Suite, if you want to debug the backend

## 🚀 Quickstart

1. Clone the repository  

   ```bash
   git clone https://github.com/GregorBiswanger/SemanticFlow.git
   cd SemanticFlow/samples/SemanticFlow.DemoConsoleApp
   ```

2. Restore dependencies

   ```bash
   dotnet restore
   ```

3. Start the console app

   ```bash
   dotnet run
   ```

4. Order a pizza 😉  
   Interact directly via the console and follow the dialog controlled by Semantic Flow.

## 🧠 Architecture Overview

### 🧩 Technologies Used

- **Microsoft Semantic Kernel**
- **Azure OpenAI with GPT-4o mini & GPT-3.5 turbo**
- **Semantic Flow** for stateful workflows
- **Console UI** as a simple yet effective user interaction

### ⚙️ Program Logic (`Program.cs`)

In the console application, the Semantic Kernel is configured with Azure OpenAI services:

```csharp
builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt4oMini, azureOpenAiEndpoint, new DefaultAzureCredential(), modelId: "gpt-4o-mini")
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt35, azureOpenAiEndpoint, new DefaultAzureCredential(), modelId: "gpt-35-turbo");
```

Next, the pizza order workflow is registered:

```csharp
builder.Services.AddKernelWorkflow()
    .StartWith<CustomerIdentificationActivity>()
    .Then<MenuSelectionActivity>()
    .Then<PaymentProcessingActivity>()
    .EndsWith<OrderConfirmationActivity>();
```

The `SessionService` instance generates a unique ID per user session to keep the workflow stateful.

### 🔄 Stateful Interaction in the Loop

The central loop runs until the user enters `exit`:

```csharp
while (true)
{
    var kernelClone = kernel.Clone();
    var currentActivity = workflowService.GetCurrentActivity(id, kernelClone);

    var systemPrompt = currentActivity.SystemPrompt + " ### " +
                       workflowService.WorkflowState.DataFrom(id).ToPromptString();

    var systemChatMessage = new ChatMessageContent(AuthorRole.System, systemPrompt);
    chatHistory[0] = systemChatMessage;

    Console.Write("You: ");
    string userInput = Console.ReadLine();
    chatHistory.AddUserMessage(userInput);

    if (userInput?.ToLower() == "exit")
        break;

    var chatCompletions = await kernelClone.GetChatCompletionForActivity(currentActivity)
        .GetChatMessageContentsAsync(new ChatHistory(chatHistory), currentActivity.PromptExecutionSettings, kernelClone);

    var chatResponse = chatCompletions.First().ToChatResponse();
    chatHistory.AddAssistantMessage(chatResponse.Message.Content);

    Console.WriteLine($"AI Pizza dealer: {chatResponse.Message.Content}");
}
```

### 📂 Activities in the Workflow

The individual steps of the pizza order can be found in the `Workflow` directory. A typical activity looks like this:

```csharp
public class CustomerIdentificationActivity : IActivity
{
    public string SystemPrompt { get; set; } = File.ReadAllText("./Workflow/CustomerIdentificationActivity.SystemPrompt.txt");

    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new AzureOpenAIPromptExecutionSettings
    {
        ModelId = "gpt-35-turbo",
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        Temperature = 0.7,
        MaxTokens = 256
    };

    [KernelFunction]
    public string CustomerDataApproved(Customer customer)
    {
        var chatId = httpContextAccessor.HttpContext?.GetChatRequest().ChatId;
        var nextActivity = workflowService.CompleteActivity(chatId, customer, kernel);
        return @$"{nextActivity.SystemPrompt} ### {workflowService.WorkflowState.DataFrom(chatId).ToPromptString()}";
    }
}
```

### 🧠 Workflow Overview

```plaintext
CustomerIdentificationActivity
     ↓
MenuSelectionActivity
     ↓
PaymentProcessingActivity
     ↓
OrderConfirmationActivity
```

Each activity manages its state independently and communicates with the `WorkflowService` to proceed to the next step.

## 🧪 Debugging

You can debug the network traffic of the Semantic Kernel via a proxy:

```csharp
builder.Services.AddProxyForDebug();
```

Use tools like **Burp Suite** for detailed insights.

## 📬 Feedback and Questions

If you have questions or find issues, feel free to open a [GitHub Issue](https://github.com/GregorBiswanger/SemanticFlow/issues).

## 📜 License

This project is licensed under the [Apache License 2.0](https://raw.githubusercontent.com/GregorBiswanger/SemanticFlow/main/LICENSE.txt).