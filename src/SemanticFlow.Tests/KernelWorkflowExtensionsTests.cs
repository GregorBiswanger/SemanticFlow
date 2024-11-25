using Microsoft.Extensions.DependencyInjection;
using SemanticFlow.Tests.Activities;
using FluentAssertions;
using SemanticFlow.Extensions;
using SemanticFlow.Interfaces;

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
        activities.Should().HaveCount(3);
        activities[0].Should().BeOfType<CustomerIdentificationActivity>();
        activities[1].Should().BeOfType<CustomerIdentificationActivity>();
        activities[2].Should().BeOfType<DeliveryTimeEstimationActivity>();
    }
}