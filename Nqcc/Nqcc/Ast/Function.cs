namespace Nqcc.Ast;

public class Function(string name, Statement body) : SyntaxNode
{
    public string Name { get; } = name;
    public Statement Body { get; } = body;
}