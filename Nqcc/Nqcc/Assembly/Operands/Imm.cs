namespace Nqcc.Assembly.Operands;

public class Imm(int value) : Operand
{
    public int Value { get; } = value;
}
