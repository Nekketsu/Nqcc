namespace Nqcc.Assembly.Instructions;

public class Binary(BinaryOperator @operator, Operand source, Operand destination) : Instruction
{
    public BinaryOperator Operator { get; } = @operator;
    public Operand Source { get; } = source;
    public Operand Destination { get; } = destination;
}
