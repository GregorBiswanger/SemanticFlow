using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SemanticFlow.Extensions;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;
using SemanticFlow.Tests.Activities;

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
        currentActivity.Should().NotBeNull();
        currentActivity.Should().BeOfType<CustomerIdentificationActivity>();
        workflowService.WorkflowState.DataFrom(sessionId).CurrentActivityIndex.Should().Be(0);
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
        firstActivity.Should().BeOfType<CustomerIdentificationActivity>();
        nextActivity.Should().NotBeNull();
        nextActivity.Should().BeOfType<DeliveryTimeEstimationActivity>();

        var finalActivity = workflowService.CompleteActivity(sessionId, kernel);
        finalActivity.Should().BeNull();
    }

    [Fact]
    public void WorkflowService_ShouldMaintainStateConsistency_AfterMultipleCompleteActivityCalls()
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
        firstActivity.Should().BeOfType<CustomerIdentificationActivity>();
        secondActivity.Should().BeOfType<DeliveryTimeEstimationActivity>();
        thirdActivity.Should().BeNull();

        var state = workflowService.WorkflowState.DataFrom(sessionId);
        state.CurrentActivityIndex.Should().Be(2);
        state.CollectedData.Should().Contain("First Data").And.Contain("Second Data");
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
        currentActivity.Should().BeNull();
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
        currentActivity.Should().NotBeNull();
        kernel.Plugins.Should().ContainSingle(plugin => plugin.Name == "CustomerIdentificationActivity");
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
        currentActivity.Should().NotBeNull();
        workflowService.WorkflowState.DataFrom(secondSessionId).Should().NotBeNull();
        workflowService.WorkflowState.DataFrom(secondSessionId).CurrentActivityIndex.Should().Be(0);
    }

    [Fact]
    public void WorkflowService_ShouldStoreDataInCollectedData()
    {
        // Arrange
        var workflowService = _serviceProvider.GetRequiredService<WorkflowService>();
        var kernel = _serviceProvider.GetRequiredService<Kernel>();
        var sessionId = "data-storage";

        workflowService.WorkflowState.UpdateDataContext(sessionId, state => { state.CurrentActivityIndex = 0; });

        // Act
        workflowService.CompleteActivity(sessionId, "Test Data 1", kernel);
        workflowService.CompleteActivity(sessionId, "Test Data 2", kernel);

        // Assert
        var state = workflowService.WorkflowState.DataFrom(sessionId);
        state.CollectedData.Should().Contain("Test Data 1").And.Contain("Test Data 2");
        state.CollectedData.Should().HaveCount(2);
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
        firstActivity.Should().BeOfType<CustomerIdentificationActivity>();
        secondActivity.Should().BeOfType<DeliveryTimeEstimationActivity>();
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
        initialActivity.Should().NotBeNull();
        currentActivity.Should().NotBeNull();
        currentActivity.Should().BeOfType<DeliveryTimeEstimationActivity>();
    }
}