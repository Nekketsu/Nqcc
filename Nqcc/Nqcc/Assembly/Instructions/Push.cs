namespace Nqcc.Assembly.Instructions;

public class Push(Operand operand) : Instruction
{
    public Operand Operand { get; } = operand;
}
