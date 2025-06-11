### 🍕 Demo: Pizza & Support Workflow with Semantic Routing

This demo showcases the capabilities of **Semantic Flow** for orchestrating generative AI workflows using **Semantic Routing**. The project extends the classic pizza ordering workflow by introducing dynamic intent routing via a **RouterActivity**.

Built with **ASP.NET Core 8**, **Docker**, and the **Microsoft Semantic Kernel**, this demo dynamically chooses between multiple workflows: *pizza ordering* or *customer support*.

## 🛠 Prerequisites

Make sure you have the following tools installed:

* **ASP.NET Core 8 SDK** ([Download](https://dotnet.microsoft.com/download))
* **Docker** ([Download](https://www.docker.com/))
* Access to **Azure AI / Azure OpenAI** or **OpenAI API**

## 🧑‍💻 Installation and Setup

### 1️. Running Open WebUI

Use the following command to run Open WebUI with Docker:

```bash
docker run -d -p 3000:8080 --add-host=host.docker.internal:host-gateway --name open-webui ghcr.io/open-webui/open-webui:main
```

For metadata support, install the custom **Ollama API Facade Metadata** function via the Admin Panel, as described in the [official guide](https://openwebui.com/f/gregorbiswanger/ollama_api_facade_metadata/).

### 2️. ASP.NET Core Web API as Ollama API Server

This project uses [**OllamaApiFacade**](https://github.com/GregorBiswanger/OllamaApiFacade) to handle Open WebUI requests in .NET.

### 3. Kernel Configuration

Set up the Semantic Kernel with Azure OpenAI:

```csharp
builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion("gpt-4o-mini", endpoint, credentials, modelId: "gpt-4o-mini")
    .AddAzureOpenAIChatCompletion("gpt-35-turbo", endpoint, credentials, modelId: "gpt-35-turbo");
```

## 🧭 Semantic Routing with RouterActivity

### 🔁 What is Semantic Routing?

Semantic Routing enables the system to understand natural language input and decide whether the user wants to *order a pizza* or *check on an existing order*. It removes the need for keyword-based routing or hardcoded logic.

### 📦 RouterActivity Example

```csharp
public class RouterActivity(
    IHttpContextAccessor httpContextAccessor,
    WorkflowService workflowService,
    Kernel kernel) : IActivity
{
    public string SystemPrompt { get; set; } = File.ReadAllText("./Workflows/RouterActivity.SystemPrompt.txt");

    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new AzureOpenAIPromptExecutionSettings
    {
        ModelId = "gpt-4o-mini",
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        Temperature = 0.5,
        MaxTokens = 256
    };

    [KernelFunction]
    [Description("Starts the order process if the user wants to place a pizza order.")]
    public string RouteToPizzaOrder()
    {
        var chatId = httpContextAccessor.HttpContext?.GetChatRequest().ChatId;
        var next = workflowService.UseWorkflow(chatId, "pizzaOrder", kernel);

        return @$"{next.SystemPrompt} ### {workflowService.WorkflowState.DataFrom(chatId).ToPromptString()}";
    }

    [KernelFunction]
    [Description("Starts the support process if the user has an issue or question about a previous order.")]
    public string RouteToSupport()
    {
        var chatId = httpContextAccessor.HttpContext?.GetChatRequest().ChatId;
        var next = workflowService.UseWorkflow(chatId, "support", kernel);

        return @$"{next.SystemPrompt} ### {workflowService.WorkflowState.DataFrom(chatId).ToPromptString()}";
    }
}
```

A pre-trained model classifies the user intent using a `SystemPrompt` and activates the relevant sub-workflow accordingly.

### 🧱 Defined Workflows

```csharp
builder.Services.AddSemanticRouter<RouterActivity>();

builder.Services.AddKernelWorkflow("pizzaOrder")
    .StartWith<CustomerIdentificationActivity>()
    .Then<MenuSelectionActivity>()
    .Then<PaymentProcessingActivity>()
    .EndsWith<OrderConfirmationActivity>();

builder.Services.AddKernelWorkflow("support")
    .StartWith<IssueClassificationActivity>()
    .EndsWith<CheckOrderStatusActivity>();
```

### 🧩 Visual Overview

![Semantic Routing Workflow](https://github.com/GregorBiswanger/SemanticFlow/blob/main/assets/semantic-flow-semantic-routing-support.png?raw=true)

## 🚀 Starting the Project

* Access Open WebUI at `http://localhost:3000`
* Use natural language like:

  * "I want to order a pizza"
  * "Where is my order?"
* The system will semantically route your request and proceed with the appropriate workflow.

## ▶️ Pizza Order Workflow Details

Includes: `CustomerIdentificationActivity`, `MenuSelectionActivity`, `PaymentProcessingActivity`, and `OrderConfirmationActivity`.

## 🛠 Support Workflow Details

Includes: `IssueClassificationActivity` and `CheckOrderStatusActivity`, supported by sample order data and context preservation.

## 💬 Contribution and Contact

This project is a demonstration of **Semantic Flow**. For questions or issues, please use the **GitHub Issues** section.

## 📜 License

This project is licensed under the [**Apache License 2.0**](https://raw.githubusercontent.com/GregorBiswanger/SemanticFlow/refs/heads/main/LICENSE.txt).
