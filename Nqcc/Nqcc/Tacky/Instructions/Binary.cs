namespace Nqcc.Tacky.Instructions;

public class Binary(Operand left, BinaryOperator @operator, Operand right, Operand destination) : Instruction
{
    public Operand Left { get; } = left;
    public BinaryOperator Operator { get; } = @operator;
    public Operand Right { get; } = right;
    public Operand Destination { get; } = destination;
}
