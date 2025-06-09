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
    
    [Fact]
    public void AddKernelWorkflow_WithName_ThrowsIfSemanticRouterIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action action = () =>
        {
            services.AddKernelWorkflow("support")
                .StartWith<CustomerIdentificationActivity>()
                .Then<CustomerIdentificationActivity>()
                .EndsWith<DeliveryTimeEstimationActivity>();

            services.BuildServiceProvider();
        };

        // Assert
        var exception = Should.Throw<InvalidOperationException>(action);
        exception.Message.ShouldBe("Named workflows work only with registered semantic router.");
    }
    
    [Fact]
    public void AddKernelWorkflow_WithNamedWorkflows_RegistersKeyedActivitiesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSemanticRouter<RouterActivity>();

        services.AddKernelWorkflow("sell")
            .StartWith<CustomerIdentificationActivity>()
            .EndsWith<DeliveryTimeEstimationActivity>();        
        
        services.AddKernelWorkflow("support")
            .StartWith<CustomerIdentificationActivity>()
            .Then<CustomerIdentificationActivity>()
            .EndsWith<DeliveryTimeEstimationActivity>();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var activities = serviceProvider.GetServices<IActivity>().ToList();
        activities.Count.ShouldBe(0);

        var sellActivities = serviceProvider.GetKeyedServices<IActivity>("sell").ToList();
        sellActivities.Count.ShouldBe(2);
        sellActivities[0].ShouldBeOfType<CustomerIdentificationActivity>();
        sellActivities[1].ShouldBeOfType<DeliveryTimeEstimationActivity>();
        
        var supportActivities = serviceProvider.GetKeyedServices<IActivity>("support").ToList();
        supportActivities.Count.ShouldBe(3);
        supportActivities[0].ShouldBeOfType<CustomerIdentificationActivity>();
        supportActivities[1].ShouldBeOfType<CustomerIdentificationActivity>();
        supportActivities[2].ShouldBeOfType<DeliveryTimeEstimationActivity>();
    }
}