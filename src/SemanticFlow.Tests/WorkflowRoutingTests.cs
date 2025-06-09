using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SemanticFlow.Extensions;
using SemanticFlow.Services;
using SemanticFlow.Tests.Activities;
using Shouldly;

namespace SemanticFlow.Tests;

public class WorkflowRoutingTests
{
    private readonly IServiceProvider _provider;

    public WorkflowRoutingTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKernel();

        services.AddSemanticRouter<RouterActivity>();
        services.AddKernelWorkflow("sales")
            .StartWith<CustomerIdentificationActivity>()
            .EndsWith<DeliveryTimeEstimationActivity>();

        services.AddKernelWorkflow("support")
            .StartWith<CustomerIdentificationActivity>()
            .EndsWith<ContactActivity>();

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void ShouldReturnRoutingActivity_IfNoWorkflowSetAndRoutingExists()
    {
        var workflowService = _provider.GetRequiredService<WorkflowService>();
        var kernel = _provider.GetRequiredService<Kernel>();
        var sessionId = "routing-user";

        var activity = workflowService.GetCurrentActivity(sessionId, kernel);

        activity.ShouldNotBeNull();
        activity.ShouldBeOfType<RouterActivity>();
    }

    [Fact]
    public void ShouldSwitchToSalesWorkflow_WhenUseWorkflowIsCalled()
    {
        var workflowService = _provider.GetRequiredService<WorkflowService>();
        var kernel = _provider.GetRequiredService<Kernel>();
        var sessionId = "user-sales";

        var nextActivity = workflowService.UseWorkflow(sessionId, "sales", kernel);

        nextActivity.ShouldNotBeNull();
        nextActivity.ShouldBeOfType<CustomerIdentificationActivity>();
    }

    [Fact]
    public void ShouldSwitchToSupportWorkflow_WhenUseWorkflowIsCalled()
    {
        var workflowService = _provider.GetRequiredService<WorkflowService>();
        var kernel = _provider.GetRequiredService<Kernel>();
        var sessionId = "user-support";

        var nextActivity = workflowService.UseWorkflow(sessionId, "support", kernel);

        nextActivity.ShouldNotBeNull();
        nextActivity.ShouldBeOfType<CustomerIdentificationActivity>();
    }    
    
    [Fact]
    public void GetCurrentActivity_ShouldReturnContactActivity_AfterCompletingFirstSupportActivity()
    {
        var workflowService = _provider.GetRequiredService<WorkflowService>();
        var kernel = _provider.GetRequiredService<Kernel>();
        var sessionId = "user-support";

        workflowService.UseWorkflow(sessionId, "support", kernel);
        workflowService.CompleteActivity(sessionId, kernel);

        var currentActivity = workflowService.GetCurrentActivity(sessionId, kernel);

        currentActivity.ShouldNotBeNull();
        currentActivity.ShouldBeOfType<ContactActivity>();
    }
    
    [Fact]
    public void GetCurrentActivity_ShouldReturnDeliveryTimeEstimationActivity_AfterCompletingFirstSalesActivity()
    {
        var workflowService = _provider.GetRequiredService<WorkflowService>();
        var kernel = _provider.GetRequiredService<Kernel>();
        var sessionId = "user-support";

        workflowService.UseWorkflow(sessionId, "sales", kernel);
        workflowService.CompleteActivity(sessionId, kernel);

        var currentActivity = workflowService.GetCurrentActivity(sessionId, kernel);

        currentActivity.ShouldNotBeNull();
        currentActivity.ShouldBeOfType<DeliveryTimeEstimationActivity>();
    }

    [Fact]
    public void ShouldReturnWorkflowName_WhenWorkflowIsSet()
    {
        var workflowService = _provider.GetRequiredService<WorkflowService>();
        var kernel = _provider.GetRequiredService<Kernel>();
        var sessionId = "named-workflow";

        workflowService.UseWorkflow(sessionId, "support", kernel);
        var workflowState = workflowService.WorkflowState.DataFrom(sessionId);

        workflowState.CurrentWorkflowName.ShouldBe("support");
    }

    [Fact]
    public void ShouldNotReturnRoutingActivity_IfWorkflowAlreadySet()
    {
        var service = _provider.GetRequiredService<WorkflowService>();
        var kernel = _provider.GetRequiredService<Kernel>();
        var sessionId = "routing-vs-active";

        service.UseWorkflow(sessionId, "sales", kernel);
        var activity = service.GetCurrentActivity(sessionId, kernel);

        activity.ShouldNotBeOfType<RouterActivity>();
        activity.ShouldBeOfType<CustomerIdentificationActivity>();
    }

    [Fact]
    public void GetCurrentActivity_ShouldReturnRouterActivity_AfterExplicitRoutingSwitch()
    {
        var workflowService = _provider.GetRequiredService<WorkflowService>();
        var kernel = _provider.GetRequiredService<Kernel>();
        var sessionId = "routing-vs-active";

        workflowService.UseWorkflow(sessionId, "sales", kernel);
        var activity = workflowService.GetCurrentActivity(sessionId, kernel);
        activity.ShouldNotBeOfType<RouterActivity>();
        activity.ShouldBeOfType<CustomerIdentificationActivity>();

        workflowService.GoToRouting(sessionId, kernel);

        var lastActivity = workflowService.GetCurrentActivity(sessionId, kernel);
        lastActivity.ShouldNotBeOfType<CustomerIdentificationActivity>();
        lastActivity.ShouldBeOfType<RouterActivity>();
    }
}
