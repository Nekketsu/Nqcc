namespace Nqcc.Ast;

public class Program(Function functionDefinition) : SyntaxNode
{
    public Function FunctionDefinition { get; } = functionDefinition;
}