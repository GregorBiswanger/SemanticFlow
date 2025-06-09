using Microsoft.Extensions.DependencyInjection;
using SemanticFlow.Extensions;
using SemanticFlow.Interfaces;
using SemanticFlow.Services;
using SemanticFlow.Tests.Activities;
using Shouldly;

namespace SemanticFlow.Tests;

public class SemanticRouterTests
{
    [Fact]
    public void AddSemanticRouter_RegistersRouterAndWorkflowServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSemanticRouter<RouterActivity>();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var activity = serviceProvider.GetKeyedService<IActivity>(KernelWorkflowExtensions.SEMANTIC_ROUTER_KEY);
        activity.ShouldNotBeNull();
        activity.ShouldBeOfType<RouterActivity>();

        var workflowService = serviceProvider.GetService<WorkflowService>();
        workflowService.ShouldNotBeNull();

        var workflowStateService = serviceProvider.GetService<WorkflowStateService>();
        workflowStateService.ShouldNotBeNull();
    }

    [Fact]
    public void AddSemanticRouter_WithKernelWorkflow_RegistersAllActivitiesAndServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSemanticRouter<RouterActivity>();
        services.AddKernelWorkflow()
            .StartWith<CustomerIdentificationActivity>()
            .EndsWith<DeliveryTimeEstimationActivity>();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var activity = serviceProvider.GetKeyedService<IActivity>(KernelWorkflowExtensions.SEMANTIC_ROUTER_KEY);
        activity.ShouldNotBeNull();
        activity.ShouldBeOfType<RouterActivity>();

        var activities = serviceProvider.GetServices<IActivity>().ToList();
        activities.Count.ShouldBe(2);

        var workflowServices = serviceProvider.GetServices<WorkflowService>().ToList();
        workflowServices.Count.ShouldBe(1);

        var workflowStateServices = serviceProvider.GetServices<WorkflowStateService>().ToList();
        workflowStateServices.Count.ShouldBe(1);
    }

    [Fact]
    public void AddSemanticRouter_MultipleRegistrations_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        Action action = () =>
        {
            services.AddSemanticRouter<RouterActivity>();
            services.AddSemanticRouter<RouterActivity>();

            services.BuildServiceProvider();
        };

        // Assert
        var exception = Should.Throw<InvalidOperationException>(action);
        exception.Message.ShouldBe("Only one semantic router can be registered.");
    }
}