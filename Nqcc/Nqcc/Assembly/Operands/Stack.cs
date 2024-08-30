namespace Nqcc.Assembly.Operands;

public class Stack(int offset) : Operand
{
    public int Offset { get; } = offset;
}
