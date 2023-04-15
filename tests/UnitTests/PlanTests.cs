using Shouldly;
using Tarik.Application.Common;
using Xunit;

namespace Tarik.UnitTests;

public class PlanTests
{
    [Fact]
    public void CalculatorPlan_ShouldBe_Parseable()
    {
        var plan = new Plan(ApprovedPlans.CalculatorPlan, "/path/to/local/directory");
        plan.CreateFileSteps.Count.ShouldBe(3);
        plan.EditFileSteps.Count.ShouldBe(4);

        plan.CreateFileSteps[0].PathTo.RelativePath.ShouldBe("/index.html");
        plan.CreateFileSteps[0].Reason.ShouldBe("To create the main HTML file for the calculator app");
        plan.CreateFileSteps[0].PathTo.AbsolutePath.ShouldBe("/path/to/local/directory/index.html");

        plan.EditFileSteps[0].PathTo.RelativePath.ShouldBe("/index.html");
        plan.EditFileSteps[0].Reason.ShouldBe("To design the user interface for the calculator app, including the display and on-screen numeric keypad");
        plan.EditFileSteps[0].PathTo.AbsolutePath.ShouldBe("/path/to/local/directory/index.html");
    }
}