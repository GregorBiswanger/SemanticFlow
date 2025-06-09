using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OllamaApiFacade.Extensions;
using SemanticFlow.DemoWebApi.Workflow.DTOs;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;

namespace SemanticFlow.DemoWebApi.Workflow;

public class MenuSelectionActivity(IHttpContextAccessor httpContextAccessor,
    WorkflowService workflowService,
    Kernel kernel) : IActivity
{
    private static readonly List<MenuItem> MenuItems =
    [
        new() { Title = "Margherita", Ingredients = "Tomato, Mozzarella, Basil", Price = 8.50 },
        new() { Title = "Pepperoni", Ingredients = "Tomato, Mozzarella, Pepperoni", Price = 9.50 },
        new()
        {
            Title = "Quattro Stagioni", Ingredients = "Tomato, Mozzarella, Mushrooms, Ham, Artichokes, Olives",
            Price = 11.00
        },
        new() { Title = "Marinara", Ingredients = "Tomato, Garlic, Oregano", Price = 7.00 },
        new() { Title = "Carbonara", Ingredients = "Mozzarella, Eggs, Bacon, Black Pepper", Price = 10.00 }
    ];

    public string SystemPrompt { get; set; } = File.ReadAllText("./Workflow/MenuSelectionActivity.SystemPrompt.txt");
    public PromptExecutionSettings PromptExecutionSettings { get; set; } = new AzureOpenAIPromptExecutionSettings
    {
        ModelId = "gpt-4",
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        Temperature = 0.1,
        MaxTokens = 256
    };

    [KernelFunction, Description("Checks if a specific menu item is available in the restaurant's menu, including handling unknown words.")]
    [return: Description("Returns a confirmation message if the menu item is available, or a suggestion for alternatives if it is not.")]
    public string CheckMenuAvailability(
        [Description("The name of the menu item that the customer wants to order.")] string menu)
    {
        var menuItem = MenuItems.FirstOrDefault(item => item.Title.Contains(menu, StringComparison.OrdinalIgnoreCase) ||
                                                        menu.Contains(item.Title, StringComparison.OrdinalIgnoreCase));
        if (menuItem != null)
        {
            var menuItemJson = JsonSerializer.Serialize(menuItem);

            return menuItemJson;
        }

        return "The requested menu item is not available. Please choose from our available options.";
    }

    [KernelFunction, Description("Finalizes the customer's order by confirming their complete menu selection.")]
    [return: Description("A confirmation message indicating that the customer's menu selection has been approved.")]
    public string MenuSelectionApproved(
        [Description("The full menu selection as confirmed by the customer, including all items they want to order.")] string fullMenuSelection)
    {
        var chatId = httpContextAccessor.HttpContext?.GetChatRequest().ChatId;
        var nextActivity = workflowService.CompleteActivity(chatId, fullMenuSelection, kernel);

        return @$"{nextActivity.SystemPrompt} ###
                  {workflowService.WorkflowState.DataFrom(chatId).ToPromptString()}";
    }
}