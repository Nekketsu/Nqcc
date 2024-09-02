namespace Nqcc.Tacky.Instructions;

public class Copy(Operand source, Operand destination) : Instruction
{
    public Operand Source { get; } = source;
    public Operand Destination { get; } = destination;
}
