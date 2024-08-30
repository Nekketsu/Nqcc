namespace Nqcc.Tacky.Instructions;

public class Return(Operand value) : Instruction
{
    public Operand Value { get; } = value;
}
