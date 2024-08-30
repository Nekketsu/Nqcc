namespace Nqcc.Assembly.Instructions;

public class Idiv(Operand operand) : Instruction
{
    public Operand Operand { get; } = operand;
}
