namespace Nqcc.Assembly.Instructions;

public class Unary(UnaryOperator @operator, Operand destination) : Instruction
{
    public UnaryOperator Operator { get; } = @operator;
    public Operand Destination { get; } = destination;
}
