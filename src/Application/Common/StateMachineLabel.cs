namespace Tarik.Application.Common;

public enum StateMachineLabel
{
    Init,
    // auto-code:awaiting-plan-approval (awaiting plan approval by manager)
    AutoCodeAwaitingPlanApproval,
    // auto-code:awaiting-implementation (awaiting implementation by Tarik)
    AutoCodeAwaitingImplementation,
    // auto-code:awaiting-code-review (awaiting code review by manager)
    AutoCodeAwaitingCodeReview
}

public static class StateMachineLabelExtensions
{
    public static string ToLabelString(this StateMachineLabel stateMachineLabel)
    {
        return stateMachineLabel switch
        {
            StateMachineLabel.AutoCodeAwaitingPlanApproval => "auto-code:awaiting-plan-approval",
            StateMachineLabel.AutoCodeAwaitingImplementation => "auto-code:awaiting-implementation",
            StateMachineLabel.AutoCodeAwaitingCodeReview => "auto-code:awaiting-code-review",
            _ => throw new ArgumentOutOfRangeException(nameof(stateMachineLabel), stateMachineLabel, null)
        };
    }

    public static StateMachineLabel? FromLabelString(this string labelString)
    {
        return labelString switch
        {
            "auto-code:awaiting-plan-approval" => StateMachineLabel.AutoCodeAwaitingPlanApproval,
            "auto-code:awaiting-implementation" => StateMachineLabel.AutoCodeAwaitingImplementation,
            "auto-code:awaiting-code-review" => StateMachineLabel.AutoCodeAwaitingCodeReview,
            _ => null
        };
    }
}