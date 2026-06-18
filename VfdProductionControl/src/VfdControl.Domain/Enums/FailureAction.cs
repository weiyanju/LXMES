namespace VfdControl.Domain.Enums;

public enum FailureAction
{
    ContinueAndMarkFail,
    ContinueAsWarning,
    StopSlotImmediately,
    PauseAllSlots,
    RetryThenStop,
    RequireOperatorConfirm
}
