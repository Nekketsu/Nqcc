namespace Nqcc.Tacky.Instructions;

public class Unary(UnaryOperator @operator, Operand source, Operand destination) : Instruction
{
    public UnaryOperator Operator { get; } = @operator;
    public Operand Source { get; } = source;
    public Operand Destination { get; } = destination;
}
