namespace Nqcc.Tacky.Operands;

public class Constant(int value) : Operand
{
    public int Value { get; } = value;
}
