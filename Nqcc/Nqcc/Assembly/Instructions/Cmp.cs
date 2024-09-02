namespace Nqcc.Assembly.Instructions;

public class Cmp(Operand source, Operand destination) : Instruction
{
    public Operand Source { get; } = source;
    public Operand Destination { get; } = destination;
}
