using Microsoft.Extensions.DependencyInjection;
using SemanticFlow.Extensions;
using SemanticFlow.Interfaces;
using SemanticFlow.Tests.Activities;
using Shouldly;

namespace SemanticFlow.Tests;

public class KernelWorkflowExtensionsTests
{
    [Fact]
    public void AddKernelWorkflow_ShouldRegisterActivitiesInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKernelWorkflow()
            .StartWith<CustomerIdentificationActivity>()
            .Then<CustomerIdentificationActivity>()
            .EndsWith<DeliveryTimeEstimationActivity>();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var activities = serviceProvider.GetServices<IActivity>().ToList();
        activities.Count.ShouldBe(3);
        activities[0].ShouldBeOfType<CustomerIdentificationActivity>();
        activities[1].ShouldBeOfType<CustomerIdentificationActivity>();
        activities[2].ShouldBeOfType<DeliveryTimeEstimationActivity>();
    }
}