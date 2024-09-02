namespace Nqcc.Assembly.Instructions;

public class JmpCc(ConditionCode conditionCode, string target) : Instruction
{
    public ConditionCode ConditionCode { get; } = conditionCode;
    public string Target { get; } = target;
}
