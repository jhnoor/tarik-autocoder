namespace Tarik.Application.Common;

public enum StateMachineLabel
{
    // init (initial state, will try to understand the codebase and then move to planning)
    Init,
    // auto-code:planning (planning work to be done)
    AutoCodePlanning,
    // auto-code:awaiting-plan-approval (awaiting plan approval by manager)
    AutoCodeAwaitingPlanApproval,
    // auto-code:awaiting-implementation (awaiting implementation by Tarik)
    AutoCodeAwaitingImplementation,
    // auto-code:fail-plan-not-parsable (plan not parsable by Tarik)
    AutoCodeFailPlanNotParsable,
    // auto-code:fail-execution (execution failed by Tarik)
    AutoCodeFailExecution,
    // auto-code:fail-understanding-repository (understanding repository failed by Tarik)
    AutoCodeFailUnderstandingRepository,
    // auto-code:awaiting-code-review (awaiting code review by manager)
    AutoCodeAwaitingCodeReview
}

public static class StateMachineLabelExtensions
{
    public static string ToLabelString(this StateMachineLabel stateMachineLabel)
    {
        return stateMachineLabel switch
        {
            StateMachineLabel.AutoCodePlanning => AutoCodePlanning,
            StateMachineLabel.AutoCodeAwaitingPlanApproval => AutoCodeAwaitingPlanApproval,
            StateMachineLabel.AutoCodeAwaitingImplementation => AutoCodeAwaitingImplementation,
            StateMachineLabel.AutoCodeAwaitingCodeReview => AutoCodeAwaitingCodeReview,
            StateMachineLabel.AutoCodeFailPlanNotParsable => AutoCodeFailPlanNotParsable,
            StateMachineLabel.AutoCodeFailExecution => AutoCodeFailExecution,
            StateMachineLabel.AutoCodeFailUnderstandingRepository => AutoCodeFailUnderstandingRepository,
            _ => throw new ArgumentOutOfRangeException(nameof(stateMachineLabel), stateMachineLabel, null)
        };
    }

    public static StateMachineLabel? FromLabelString(this string labelString)
    {
        return labelString switch
        {
            AutoCodePlanning => StateMachineLabel.AutoCodePlanning,
            AutoCodeAwaitingPlanApproval => StateMachineLabel.AutoCodeAwaitingPlanApproval,
            AutoCodeAwaitingImplementation => StateMachineLabel.AutoCodeAwaitingImplementation,
            AutoCodeAwaitingCodeReview => StateMachineLabel.AutoCodeAwaitingCodeReview,
            AutoCodeFailPlanNotParsable => StateMachineLabel.AutoCodeFailPlanNotParsable,
            AutoCodeFailExecution => StateMachineLabel.AutoCodeFailExecution,
            AutoCodeFailUnderstandingRepository => StateMachineLabel.AutoCodeFailUnderstandingRepository,
            _ => null
        };
    }

    private const string AutoCodePlanning = "auto-code:planning";
    private const string AutoCodeAwaitingPlanApproval = "auto-code:awaiting-plan-approval";
    private const string AutoCodeAwaitingImplementation = "auto-code:awaiting-implementation";
    private const string AutoCodeAwaitingCodeReview = "auto-code:awaiting-code-review";
    private const string AutoCodeFailPlanNotParsable = "auto-code:fail-plan-not-parsable";
    private const string AutoCodeFailExecution = "auto-code:fail-execution";
    private const string AutoCodeFailUnderstandingRepository = "auto-code:fail-understanding-repository";
}