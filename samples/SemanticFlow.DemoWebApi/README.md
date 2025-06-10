### 🍕 Demo: Pizza Order Workflow

This demo demonstrates the use of the **Semantic Flow** framework for orchestrating workflows using generative AI models. With **ASP.NET Core 8**, **Docker**, and the **Microsoft Semantic Kernel**, it showcases how a pizza ordering process can be automated as a workflow.

## 🛠 Prerequisites

Make sure you have the following tools installed:

- **ASP.NET Core 8 SDK** ([Download](https://dotnet.microsoft.com/download))
- **Docker** ([Download](https://www.docker.com/))
- Access to **Azure AI / Azure OpenAI** or **OpenAI API** for external generative model usage.

## 🧑‍💻 Installation and Setup

### 1️. Setting Up Open WebUI

**Open WebUI** is a user-friendly, self-hosted interface that operates fully offline. It supports various LLM runners, including Ollama and OpenAI-compatible APIs.

Run Open WebUI with Docker using the following command:

```bash
docker run -d -p 3000:8080 --add-host=host.docker.internal:host-gateway --name open-webui ghcr.io/open-webui/open-webui:main
```

This command will start Open WebUI and make it accessible locally at `http://localhost:3000`. The `--add-host=host.docker.internal:host-gateway` flag is used to allow communication between the Docker container and your host machine.

For more detailed information on setting up Open WebUI with Docker, including advanced configurations such as GPU support, refer to the official [Open WebUI GitHub repository](https://github.com/open-webui/open-webui).

### 🛠 Setting Up Open WebUI with Metadata Support

By default, Open WebUI's Ollama API Facade does not send `Id`, `ChatId`, and `SessionId` metadata in the `ChatRequest` object to the backend. To enable these critical metadata fields for the ASP.NET Core Web API backend, you need to install and configure a custom "Pipe Function" in Open WebUI.

### Steps to Enable Metadata Support

1. **Open the Admin Panel**  
   - Click on your username in the bottom-left corner of Open WebUI and select **Admin Panel** from the context menu.

   ![Admin Panel Navigation](https://github.com/GregorBiswanger/SemanticFlow/blob/main/assets/1_open-webui-admin-panel.png?raw=true)

2. **Add a New Pipe Function**  
   - Navigate to the **Functions** menu and click the **+** (Plus) icon.  

3. **Install the Pipe Function**  
   - Copy the **Ollama Api Facade Metadata** function from [this website](https://openwebui.com/f/gregorbiswanger/ollama_api_facade_metadata/) and paste it into the provided input field, then save it.

4. **Activate the Function**  
   - Ensure the Pipe Function is activated by toggling the switch next to its name.

   ![Activate Function](https://github.com/GregorBiswanger/SemanticFlow/blob/main/assets/2_open-webui-activate-pipe-function.png?raw=true)

5. **Disable Connections**  
   - Go to the **Settings** menu and under **Connections**, disable both the **OpenAI API** and **Ollama API**, then save the settings.

   ![Connections Settings](https://github.com/GregorBiswanger/SemanticFlow/blob/main/assets/3_open-webui-disable-apis.png?raw=true)

6. **Verify Metadata Transmission**  
   - From now on, Open WebUI will include the missing metadata (`Id`, `ChatId`, `SessionId`) in its requests to your ASP.NET Core Web API backend.

### 2️. ASP.NET Core Web API as Ollama API Server

In this demo, the **ASP.NET Core Web API** acts as an Ollama API Server using [**OllamaApiFacade**](https://github.com/GregorBiswanger/OllamaApiFacade). This enables us to intercept Open WebUI communications and handle them in C#. Start the ASP.NET Core Web API to allow interaction with Open WebUI.

### 3. Configuring the Semantic Kernel

Configure the Semantic Kernel in the `Program.cs` file by adding or adjusting the appropriate connectors. An example is provided in the [Program.cs](https://github.com/GregorBiswanger/SemanticFlow/blob/main/samples/SemanticFlow.DemoWebApi/Program.cs).

> **Important:** Ensure the `ModelID` is set correctly in the code and in the activities folder within the workflow.

### 🚀 Starting the Project

- Access Open WebUI at `http://localhost:3000`.
- The API enables simulation of pizza orders, showcasing how Semantic Flow processes and manages workflows.

## 📂 A Look Inside the Program.cs File

The `Program.cs` file in your ASP.NET Core 8 application sets up key services and configurations to orchestrate the **Pizza Order Workflow** using **Semantic Flow** and the **Microsoft Semantic Kernel**.

### ⚙️ Registering Semantic Kernel with Azure OpenAI Connectors

The Semantic Kernel is configured to interact with Azure OpenAI services. Keys and endpoints are retrieved from Azure Key Vault as follows:

```csharp
var azureOpenAiEndpoint = configuration["AzureOpenAI:Endpoint"];
var azureOpenAiDeploymentNameGpt4 = configuration["AzureOpenAI:DeploymentNameGpt4oMini"];
var azureOpenAiDeploymentNameGpt35 = configuration["AzureOpenAI:DeploymentNameGpt35"];
```

The Azure OpenAI Chat Completion services are then registered with the Semantic Kernel:

```csharp
builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt4, azureOpenAiEndpoint, new DefaultAzureCredential(), modelId: "gpt-4")
    .AddAzureOpenAIChatCompletion(azureOpenAiDeploymentNameGpt35, azureOpenAiEndpoint, new DefaultAzureCredential(), modelId: "gpt-35-turbo");
```

### 🛠 Registering the Pizza Order Workflow with Semantic Flow

The pizza order workflow is defined using **Semantic Flow**. Individual steps of the workflow are specified in the desired sequence:

```csharp
builder.Services.AddKernelWorkflow()
    .StartWith<CustomerIdentificationActivity>()
    .Then<MenuSelectionActivity>()
    .Then<PaymentProcessingActivity>()
    .EndsWith<OrderConfirmationActivity>();
```

Each `Activity` represents a specific step in the ordering process, from greeting the customer to confirming the order.

### 📨 Setting Up the Central Endpoint for User Messages

The `MapPostApiChat` method serves as the main entry point for messages entered by the user through Open WebUI. It manages the workflow state and loads the appropriate activity based on the `ChatId`:

```csharp
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
```

## 🔍 Activities in the Workflow

The `Workflow` directory contains the activities that control the pizza ordering process. One of these is the `CustomerIdentificationActivity`, responsible for identifying and confirming customer data.

### 📋 CustomerIdentificationActivity in Detail

#### Overview

- Each activity must implement the `IActivity` interface.
- Naming convention: Activities end with `Activity`.
- A **SystemPrompt** and settings for the model are predefined. If no settings are specified, **Semantic Kernel** uses the last registered connector by default.

#### Sample Code

```csharp
public class CustomerIdentificationActivity(IHttpContextAccessor httpContextAccessor, WorkflowService workflowService, Kernel kernel) : IActivity
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
    [Description("Confirms the customer's data for the pizza delivery.")]
    public string CustomerDataApproved(Customer customer)
    {
        var chatId = httpContextAccessor.HttpContext?.GetChatRequest().ChatId;
        var nextActivity = workflowService.CompleteActivity(chatId, customer, kernel);
        return @$"{nextActivity.SystemPrompt} ### {workflowService.WorkflowState.DataFrom(chatId).ToPromptString()}";
    }
}
```

#### Features and Workflow

- **SystemPrompt:** Defines the context for the AI model, loaded from an external file.
- **PromptExecutionSettings:** Specifies the model and its parameters.
- **KernelFunction:** Marks the activity as complete with additional data.
- **WorkflowService:** Manages workflow state and associates extra data with activities.

## ▶️ Usage

1. **Place a Pizza Order:**
   - Use Open WebUI to navigate through workflow steps.
   - Watch the workflow orchestrated by Semantic Flow.

2. **Workflow Visualization:**
   - Refer to the workflow diagram below for an overview:

![Pizza Order Workflow](https://github.com/GregorBiswanger/SemanticFlow/raw/main/assets/semantic-flow-workflow-sample.png)

## 💬 Contribution and Contact

This project is a demonstration of **Semantic Flow**. For questions or issues, please use the **GitHub Issues** feature in this repository.

## 📜 License

This project is licensed under the [**Apache License 2.0**](https://raw.githubusercontent.com/GregorBiswanger/SemanticFlow/refs/heads/main/LICENSE.txt).
