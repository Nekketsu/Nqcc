namespace Nqcc.Tacky;

public class Program(Function functionDefinition) : TackyNode
{
    public Function FunctionDefinition { get; } = functionDefinition;
}
