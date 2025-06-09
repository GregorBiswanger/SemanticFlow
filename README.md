[![Semantic Flow Logo](https://github.com/GregorBiswanger/SemanticFlow/raw/main/assets/semantic-flow-logo-transparent.png)](https://github.com/GregorBiswanger/SemanticFlow)

# Semantic Flow

**Semantic Flow** is a powerful state manager and a game-changer in the orchestration of generative AI workflows. Designed to simplify complex processes, reduce costs, and increase efficiency, it enables you to break down intricate AI-driven workflows into manageable, self-contained steps - known as **Activities** - through a modular architecture perfectly aligned with the **Microsoft Semantic Kernel**.

## 🚀 Why Semantic Flow?

**Traditional AI workflow orchestration is inefficient. Semantic Flow changes that.**

1. **Overwhelming System Prompts**  
   - Long prompts lead to increased costs due to excessive token usage.  
   - They overload the model with irrelevant information, which decreases efficiency and accuracy.  
   - **Semantic Flow solves this by dividing large prompts into smaller, specialized system prompts for each activity, optimizing both performance and cost.**

2. **One Model for Everything**  
   - A single model is often inefficient and expensive for diverse tasks.  
   - **With Semantic Flow, each activity can use the most suitable model - whether it`s GPT-3.5 Turbo for simpler tasks or GPT-4 for more complex ones.**

3. **Unnecessary Registered Plugins / Kernel Functions**  
   - In traditional approaches, all available functions are often registered, regardless of whether they are needed.  
   - This increases latency, costs, and complexity.  
   - **Semantic Flow registers only the functions that are truly relevant to the current activity.**

4. **Complex Software Architecture**  
   - Without a clear structure, the code quickly becomes convoluted and hard to maintain.  
   - **Semantic Flow brings order with a clean, modular architecture.**

5. **Manual State Tracking**  
   - Developers often need to manually track the conversation context.  
   - **Semantic Flow uses the WorkflowState service to automatically save progress and make it accessible.**

## 🔍 How Does Semantic Flow Work?

**Imagine building a complex AI-driven process** - such as an order system, customer interaction, or data analysis. Traditional approaches often struggle with scalability and efficiency. Semantic Flow revolutionizes this by breaking the process into **manageable steps**, called **Activities**.

### What is an Activity?

Activities are the **atomic building blocks** of Semantic Flow. Each activity is:

- 🧩 **Independent:** Focused on a single, well-defined task.  
- ⚡ **Customizable:** Designed to include only what`s needed for the current task, reducing overhead and cost:  
  1. **Own System Prompt:** Contains only the relevant information for this task.  
  2. **Own Model:** Each activity can use a different model (e.g., GPT-4, GPT-3.5 Turbo, Llama).  
  3. **Own Settings:** Model parameters like temperature, token limit, etc., can be configured individually.  
  4. **Own Kernel Functions:** Only the relevant functions for the current step are registered.

### Example: Pizza Order Workflow

![Pizza Order Workflow](https://github.com/GregorBiswanger/SemanticFlow/raw/main/assets/semantic-flow-workflow-sample.png)

In a pizza ordering workflow, the activities might look like this:

1. **Customer Identification Activity**: Identifies the customer.  
2. **Menu Selection Activity**: The customer selects a pizza.  
3. **Payment Processing Activity**: Payment is processed.  
4. **Order Confirmation Activity**: The order is confirmed.

Each activity works with its **own prompts, models, and functions**, ensuring the entire process is optimized and executed precisely.

## ⚙️ Installation

### Prerequisites

- .NET 8.0 or later  
- Microsoft Semantic Kernel  

Semantic Flow is available via [![NuGet](https://img.shields.io/nuget/v/SemanticFlow?style=flat-square)](https://www.nuget.org/packages/SemanticFlow/)

```bash
dotnet add package SemanticFlow
```

## 🛠 Getting Started

### 1. Configure the Workflow

Set up the workflow by defining a sequence of activities:

```csharp
// You need Semantic Kernel
builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey, modelId: "gpt-4")
    .AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey, modelId: "gpt-35-turbo");

// Setup from Semantic Flow
services.AddKernelWorkflow()
    .StartWith<CustomerIdentificationActivity>()
    .Then<MenuSelectionActivity>()
    .Then<PaymentProcessingActivity>()
    .EndsWith<OrderConfirmationActivity>();
```

### 2. Create an Activity

Define a custom activity, including its prompt, model, and logic:

```csharp
public class CustomerIdentificationActivity : IActivity
{
    public string SystemPrompt { get; set; } = "Please identify the customer based on their input.";
    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new PromptExecutionSettings
    {
        ModelId = "gpt-35-turbo",
        Temperature = 0.7
    };

    [KernelFunction]
    public string IdentifyCustomer(string input)
    {
        return $"Customer identified: {input}";
    }
}
```

### 3. Start the Workflow

Start the workflow and retrieve the current activity:

```csharp
var workflowService = kernel.GetRequiredService<WorkflowService>();
var currentActivity = workflowService.GetCurrentActivity("session-id", kernel);
```

### 4. Retrieve Workflow State

Access the workflow state at any time:

```csharp
var stateService = serviceProvider.GetRequiredService<WorkflowStateService>();
var state = stateService.DataFrom("session-id");
```

### 5. Complete Activity

Complete an activity and transition to the next step:

```csharp
var workflowService = serviceProvider.GetRequiredService<WorkflowService>();
var nextActivity = workflowService.CompleteActivity("session-id", dataFromUser, kernel);
```

## 🍕 Demo: Pizza Order Workflow

Try out Semantic Flow with our demo app:  
**[SemanticFlow.DemoWebApi](https://github.com/GregorBiswanger/SemanticFlow/tree/main/samples/SemanticFlow.DemoWebApi)**

Explore how Semantic Flow simplifies real-world AI tasks with a Pizza Order Workflow demo - from customer identification to order confirmation, every step is streamlined.

## ✨ Key Features

- 🧠 **State Management:** Automatically tracks progress and data.  
- 🔍 **Modular Architecture:** Activities are independent and easy to maintain.  
- ⚡ **Flexible Model Management:** Different models and settings for each activity.  
- 💰 **Cost Efficiency:** Reduces token consumption and optimizes API costs.  
- 🤝 **Easy Integration:** Fully compatible with the Microsoft Semantic Kernel.  

## 👨‍💻 Author

**[Gregor Biswanger](https://github.com/GregorBiswanger)** - is a leading expert in generative AI, a Microsoft MVP for Azure AI and Web App Development. As an independent consultant, he works closely with the Microsoft product team for GitHub Copilot and supports companies in implementing modern AI solutions.

 As a freelance consultant, trainer, and author, he shares his expertise in software architecture and cloud technologies and is a sought-after speaker at international conferences. For several years, he has been live-streaming every Friday evening on [Twitch](https://twitch.tv/GregorBiswanger) with [My Coding Zone](https://www.my-coding-zone.de) in german and is an active [YouTuber](https://youtube.com/GregorBiswanger).

Reach out to Gregor if you need support in the form of consulting, training, or implementing AI solutions using .NET or Node.js. [LinkedIn](https://www.linkedin.com/in/gregor-biswanger-51342011/) or Twitter [@BFreakout](https://www.twitter.com/BFreakout)  

See also the list of [contributors](https://github.com/GregorBiswanger/SemanticFlow/graphs/contributors) who participated in this project.

## 🙋‍♀️🙋‍♂ Contributing

Feel free to submit a pull request if you find any bugs (to see a list of active issues, visit the [Issues section](https://github.com/GregorBiswanger/SemanticFlow/issues).
Please make sure all commits are properly documented.

The best thing would be to write about what you plan to do in the issue beforehand. Then there will be no disappointment if we cannot accept your pull request.

## 🙏 Donate

I work on this open-source project in my free time alongside a full-time job and raising three kids. If you`d like to support my work and help me dedicate more time to this project, consider sponsoring me on GitHub:  

- [Gregor Biswanger](https://github.com/sponsors/GregorBiswanger)  

Your sponsorship allows me to invest more time in improving the project and prioritizing important issues or features. Any support is greatly appreciated - thank you! 🍻  

## 📜 License

This project is licensed under the [**Apache License 2.0**](https://raw.githubusercontent.com/GregorBiswanger/SemanticFlow/refs/heads/main/LICENSE.txt) - © Gregor Biswanger 2024

## 🌟 Get Started Now

Optimize, scale, and modularize your generative AI workflows with **Semantic Flow**.
