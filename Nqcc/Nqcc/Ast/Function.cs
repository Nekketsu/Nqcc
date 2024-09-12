namespace Nqcc.Ast;

public class Function(string name, Block body) : SyntaxNode
{
    public string Name { get; } = name;
    public Block Body { get; } = body;
}