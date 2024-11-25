[![Semantic Flow Logo](https://github.com/GregorBiswanger/SemanticFlow/raw/main/assets/semantic-flow-logo-transparent.png)](https://github.com/GregorBiswanger/SemanticFlow)

# Semantic Flow

**Semantic Flow** is a powerful state manager designed to orchestrate complex workflows using the **Microsoft Semantic Kernel**. With Semantic Flow, you can break down intricate AI-driven processes into manageable, self-contained steps, known as **Activities**, ensuring your AI solution operates efficiently and transparently.

## Why Semantic Flow? 🚀

When building AI-powered solutions with the **Microsoft Semantic Kernel**, managing multi-step processes can quickly become challenging. Each step may require:

- A specific **System Prompt** tailored to its purpose.
- Customized **model configurations** (e.g., model type, parameters like temperature and max tokens).
- Seamless transitions between steps while persisting data and state.

Semantic Flow streamlines this process by introducing the concept of **Activities**, self-contained units that handle individual steps of a workflow. Each activity:

- Operates independently with its own **System Prompt**.
- Configures the desired AI model and parameters.
- Optionally registers **Kernel Functions** for added flexibility, such as:
  - Receiving external data.
  - Persisting state with the State Manager.
  - Signaling the completion of the activity and transitioning to the next.

## Features ✨

- **Stateful Workflows**: Keep track of progress and collected data across multiple workflow sessions.
- **Atomic Activities**: Each activity is self-contained, ensuring a clear separation of concerns.
- **Seamless Integration**: Built to work directly with the Microsoft Semantic Kernel.
- **Customizable**: Define model settings, parameters, and prompts on a per-activity basis.
- **Flexible Extensions**: Add Kernel Functions for custom logic within activities.

## Installation 📦

Install Semantic Flow via NuGet:

```bash
dotnet add package SemanticFlow
```

## How It Works 🛠️

Semantic Flow allows you to define workflows in a structured, fluent API:

```csharp
services.AddKernelWorkflow()
    .StartWith<CustomerIdentificationActivity>()
    .Then<CustomerIdentificationActivity>()
    .EndsWith<DeliveryTimeEstimationActivity>();
```

Here’s what happens:

1. Each step (or **Activity**) is registered in the workflow in the specified order.
2. The **State Manager** keeps track of progress and transitions between steps automatically.
3. Activities execute with their own configurations, prompts, and functions.

## Step-by-Step Guide 🧭

### 1. Configure Your Services 🔧

Add the necessary services in your `IServiceCollection`:

```csharp
var services = new ServiceCollection();
services.AddKernelWorkflow()
    .StartWith<CustomerIdentificationActivity>()
    .Then<CustomerIdentificationActivity>()
    .EndsWith<DeliveryTimeEstimationActivity>();
```

Build the service provider:

```csharp
var serviceProvider = services.BuildServiceProvider();
```

### 2. Create Your Activities 🏗️

Implement custom activities by inheriting from `IActivity`. For example:

```csharp
public class CustomerIdentificationActivity : IActivity
{
    public string SystemPrompt { get; set; } = "Identify the customer based on their input.";
    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new PromptExecutionSettings
    {
        ModelId = "gpt-4",
        Temperature = 0.7
    };

    [KernelFunction]
    public string IdentifyCustomer(string input)
    {
        return $"Customer identified: {input}";
    }
}
```

Each activity:

- Has its own **System Prompt**.
- Configures the AI model and its parameters.
- Can optionally include **Kernel Functions**, like `IdentifyCustomer`.

### 3. Manage Your Workflow ⚙️

Use the `WorkflowService` to execute workflows:

```csharp
var workflowService = serviceProvider.GetRequiredService<WorkflowService>();
var kernel = serviceProvider.GetRequiredService<Kernel>();
var sessionId = "user-session";

// Get the current activity
var activity = workflowService.GetCurrentActivity(sessionId, kernel);

// Complete an activity and move to the next
workflowService.CompleteActivity(sessionId, "Customer data", kernel);

// Repeat until the workflow is completed
```

The `WorkflowService` handles:

- Retrieving the current activity based on the state.
- Recording data and transitioning to the next activity.
- Ensuring smooth execution across multiple sessions.

### 4. Access Workflow State 📋

The `WorkflowStateService` tracks progress and collected data:

```csharp
var stateService = serviceProvider.GetRequiredService<WorkflowStateService>();
var state = stateService.DataFrom("user-session");

Console.WriteLine($"Current Activity Index: {state.CurrentActivityIndex}");
Console.WriteLine($"Collected Data: {state.ToPromptString()}");
```

## Example Use Case: Customer Service Workflow 🛒

Imagine you're building a customer service AI solution. Here's how Semantic Flow can help:

1. **Identify the Customer**: Collect user details with `CustomerIdentificationActivity`.
2. **Process Customer Query**: Pass their query through a `QueryProcessingActivity`.
3. **Estimate Delivery Time**: Use `DeliveryTimeEstimationActivity` to provide precise information.

With Semantic Flow, each step operates independently, but transitions smoothly, ensuring your AI delivers consistent and reliable results.

## Contributing 🤝

Contributions are welcome! Feel free to open issues or submit pull requests on GitHub.

## License 📄

Semantic Flow is licensed under the Apache License 2.0. See the [LICENSE](https://raw.githubusercontent.com/GregorBiswanger/SemanticFlow/refs/heads/main/LICENSE.txt) file for details.

## Acknowledgments 🙌

Semantic Flow leverages the **Microsoft Semantic Kernel** to provide cutting-edge workflow orchestration for AI-driven solutions. Special thanks to the open-source community for their contributions and inspiration.
