using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticFlow.Extensions;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;
using SemanticFlow.Tests.Activities;
using Shouldly;

namespace SemanticFlow.Tests;

public class WorkflowServiceTests
{
    private readonly IServiceProvider _serviceProvider;

    public WorkflowServiceTests()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddSingleton<WorkflowStateService>();
        services.AddTransient<IActivity, CustomerIdentificationActivity>();
        services.AddTransient<IActivity, DeliveryTimeEstimationActivity>();

        services.AddSingleton<WorkflowService>();
        services.AddSingleton<Kernel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void WorkflowService_ShouldInitializeState_WhenNoExistingState()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        var sessionId = "new-session";

        // Act
        var currentActivity = workflowService.GetCurrentActivity(sessionId, kernel);

        // Assert
        currentActivity.ShouldNotBeNull();
        currentActivity.ShouldBeOfType<CustomerIdentificationActivity>();
        workflowService.WorkflowState.DataFrom(sessionId).CurrentActivityIndex.ShouldBe(0);
    }

    [Fact]
    public void WorkflowService_ShouldExecuteWorkflowInExpectedOrder()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        var sessionId = "test-session";

        workflowService.WorkflowState.UpdateDataContext(sessionId, state => { state.CurrentActivityIndex = 0; });

        // Act
        var firstActivity = workflowService.GetCurrentActivity(sessionId, kernel);
        var nextActivity = workflowService.CompleteActivity(sessionId, "Some Data", kernel);

        // Assert
        firstActivity.ShouldBeOfType<CustomerIdentificationActivity>();
        nextActivity.ShouldNotBeNull();
        nextActivity.ShouldBeOfType<DeliveryTimeEstimationActivity>();

        var finalActivity = workflowService.CompleteActivity(sessionId, kernel);
        finalActivity.ShouldBeOfType<CustomerIdentificationActivity>();
        finalActivity.ShouldNotBeNull();
    }

    [Fact]
    public void WorkflowService_ShouldResetActivityIndexAndIncrementCompletionCount_AfterMultipleCompleteActivityCalls()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        var sessionId = "multi-complete";

        workflowService.WorkflowState.UpdateDataContext(sessionId, state => { state.CurrentActivityIndex = 0; });

        // Act
        var firstActivity = workflowService.GetCurrentActivity(sessionId, kernel);
        var secondActivity = workflowService.CompleteActivity(sessionId, "First Data", kernel);
        var thirdActivity = workflowService.CompleteActivity(sessionId, "Second Data", kernel);

        // Assert
        firstActivity.ShouldBeOfType<CustomerIdentificationActivity>();
        secondActivity.ShouldBeOfType<DeliveryTimeEstimationActivity>();
        thirdActivity.ShouldBeOfType<CustomerIdentificationActivity>();

        var state = workflowService.WorkflowState.DataFrom(sessionId);
        state.CurrentActivityIndex.ShouldBe(0);
        state.CollectedData.Count.ShouldBe(0);
        state.WorkflowCompletionCount.ShouldBe(1);
    }

    [Fact]
    public void WorkflowService_ShouldReturnNull_WhenNoActivitiesRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<WorkflowStateService>();
        services.AddSingleton<WorkflowService>();
        services.AddSingleton<Kernel>();
        var emptyProvider = services.BuildServiceProvider();

        var workflowService = emptyProvider.GetRequiredService<WorkflowService>();
        var kernel = emptyProvider.GetRequiredService<Kernel>();
        var sessionId = "empty-workflow";

        // Act
        var currentActivity = workflowService.GetCurrentActivity(sessionId, kernel);

        // Assert
        currentActivity.ShouldBeNull();
    }

    [Fact]
    public void WorkflowService_ShouldRegisterActivityAsPlugin()
    {
        // Arrange
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var sessionId = "plugin-registration";

        workflowService.WorkflowState.UpdateDataContext(sessionId, state => { state.CurrentActivityIndex = 0; });

        // Act
        var currentActivity = workflowService.GetCurrentActivity(sessionId, kernel);

        // Assert
        currentActivity.ShouldNotBeNull();
        kernel.Plugins.Count(plugin => plugin.Name == "CustomerIdentificationActivity").ShouldBe(1);
    }

    [Fact]
    public void WorkflowService_ShouldInitializeNewState_ForUnknownSessionId()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        var sessionId = "known-session";
        var secondSessionId = "unknown-session";

        workflowService.GetCurrentActivity(sessionId, kernel);
        workflowService.CompleteActivity(sessionId, kernel);

        // Act
        var currentActivity = workflowService.GetCurrentActivity(secondSessionId, kernel);

        // Assert
        currentActivity.ShouldNotBeNull();
        workflowService.WorkflowState.DataFrom(secondSessionId).ShouldNotBeNull();
        workflowService.WorkflowState.DataFrom(secondSessionId).CurrentActivityIndex.ShouldBe(0);
    }

    [Fact]
    public void WorkflowService_ShouldStoreDataInCollectedData()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<WorkflowStateService>();
        services.AddTransient<IActivity, CustomerIdentificationActivity>();
        services.AddTransient<IActivity, DeliveryTimeEstimationActivity>();
        services.AddTransient<IActivity, DeliveryTimeEstimationActivity>();
        services.AddSingleton<WorkflowService>();
        services.AddSingleton<Kernel>();
        var serviceProvider = services.BuildServiceProvider();
        var workflowService = serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = serviceProvider.GetRequiredService<Kernel>();
        var sessionId = "data-storage";

        workflowService.WorkflowState.UpdateDataContext(sessionId, state => { state.CurrentActivityIndex = 0; });

        // Act
        workflowService.CompleteActivity(sessionId, "Test Data 1", kernel);
        workflowService.CompleteActivity(sessionId, "Test Data 2", kernel);

        // Assert
        var state = workflowService.WorkflowState.DataFrom(sessionId);
        state.CollectedData.ShouldContain("Test Data 1");
        state.CollectedData.ShouldContain("Test Data 2");
        state.CollectedData.Count.ShouldBe(2);
    }

    [Fact]
    public void WorkflowService_ShouldLoadNextActivity_AfterCompleteActivity()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        var sessionId = "next-activity";

        workflowService.WorkflowState.UpdateDataContext(sessionId, state => { state.CurrentActivityIndex = 0; });

        // Act
        var firstActivity = workflowService.GetCurrentActivity(sessionId, kernel);
        var secondActivity = workflowService.CompleteActivity(sessionId, "Some Data", kernel);

        // Assert
        firstActivity.ShouldBeOfType<CustomerIdentificationActivity>();
        secondActivity.ShouldBeOfType<DeliveryTimeEstimationActivity>();
    }

    [Fact]
    public void WorkflowService_ShouldCompleteActivityWithoutException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKernel();
        services.AddLogging();
        services.AddKernelWorkflow()
            .StartWith<CustomerIdentificationActivity>()
            .EndsWith<DeliveryTimeEstimationActivity>();

        var serviceProvider = services.BuildServiceProvider();

        var workflowService = serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = serviceProvider.GetRequiredService<Kernel>();
        var sessionId = "gregor";

        // Acts
        var initialActivity = workflowService.GetCurrentActivity(sessionId, kernel);
        workflowService.CompleteActivity(sessionId, kernel);
        var currentActivity = workflowService.GetCurrentActivity(sessionId, kernel);

        // Assert
        initialActivity.ShouldNotBeNull();
        currentActivity.ShouldNotBeNull();
        currentActivity.ShouldBeOfType<DeliveryTimeEstimationActivity>();
    }

    [Fact]
    public void WorkflowService_ShouldGoToSpecificActivity_ByType()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        var sessionId = "goto-activity";

        // Act
        IActivity? activity = workflowService.GoTo<CustomerIdentificationActivity>(sessionId, kernel);

        // Assert
        activity.ShouldBeOfType<CustomerIdentificationActivity>();
        workflowService.WorkflowState.DataFrom(sessionId).CurrentActivityIndex.ShouldBe(0);
    }

    [Fact]
    public void WorkflowService_ShouldGoToSpecificActivity_ByType_WithData()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        var sessionId = "goto-activity-with-data";
        var data = "Test Data";

        // Act
        IActivity? activity = workflowService.GoTo<CustomerIdentificationActivity>(sessionId, data, kernel);

        // Assert
        activity.ShouldBeOfType<CustomerIdentificationActivity>();
        workflowService.WorkflowState.DataFrom(sessionId).CurrentActivityIndex.ShouldBe(0);
        workflowService.WorkflowState.DataFrom(sessionId).CollectedData.ShouldContain(data);
    }

    [Fact]
    public void IsWorkflowActiveFor_ShouldReturnFalse_WhenWorkflowIsNotRunningForSession()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var sessionId = "goto-activity-with-data";

        // Act
        bool isWorkflowActive = workflowService.IsWorkflowActiveFor(sessionId);

        // Assert
        isWorkflowActive.ShouldBeFalse();
    }

    [Fact]
    public void IsWorkflowActiveFor_ShouldReturnTrue_WhenWorkflowIsRunningForSession()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var sessionId = "goto-activity-with-data";
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        workflowService.GetCurrentActivity(sessionId, kernel);

        // Act
        bool isWorkflowActive = workflowService.IsWorkflowActiveFor(sessionId);

        // Assert
        isWorkflowActive.ShouldBeTrue();
    }    
    
    [Fact]
    public void IsWorkflowNotActiveFor_ShouldReturnTrue_WhenWorkflowIsNotRunningForSession()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var sessionId = "goto-activity-with-data";

        // Act
        bool isWorkflowActive = workflowService.IsWorkflowNotActiveFor(sessionId);

        // Assert
        isWorkflowActive.ShouldBeTrue();
    }    
    
    [Fact]
    public void IsWorkflowNotActiveFor_ShouldReturnFalse_WhenWorkflowIsRunningForSession()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var sessionId = "goto-activity-with-data";
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        workflowService.GetCurrentActivity(sessionId, kernel);

        // Act
        bool isWorkflowActive = workflowService.IsWorkflowNotActiveFor(sessionId);

        // Assert
        isWorkflowActive.ShouldBeFalse();
    }

    [Fact]
    public void WorkflowState_ShouldInitializeChatHistory_WhenNewSessionIsCreated()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        var sessionId = "data-storage";
        workflowService.GetCurrentActivity(sessionId, kernel);
        var state = workflowService.WorkflowState.DataFrom(sessionId);

        // Act
        ChatHistory chatHistory = state.ChatHistory;

        // Assert
        chatHistory.Count.ShouldBe(0);
    }

    [Fact]
    public void WorkflowState_ShouldMaintainSeparateChatHistories_ForDifferentSessions()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        var sessionId1 = "user-1";
        var sessionId2 = "user-2";

        workflowService.GetCurrentActivity(sessionId1, kernel);
        workflowService.GetCurrentActivity(sessionId2, kernel);

        // Act
        var state1 = workflowService.WorkflowState.DataFrom(sessionId1);
        state1.ChatHistory.AddUserMessage("Hello World!");

        var state2 = workflowService.WorkflowState.DataFrom(sessionId2);

        // Assert
        state1.ChatHistory.Count.ShouldBe(1);
        state1.ChatHistory[0].Items[0].ToString().ShouldBe("Hello World!");
        state2.ChatHistory.Count.ShouldBe(0);
    }
}