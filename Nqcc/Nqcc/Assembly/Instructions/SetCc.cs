namespace Nqcc.Assembly.Instructions;

public class SetCc(ConditionCode conditionCode, Operand operand) : Instruction
{
    public ConditionCode ConditionCode { get; } = conditionCode;
    public Operand Operand { get; } = operand;
}
