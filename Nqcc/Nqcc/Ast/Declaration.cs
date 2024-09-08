namespace Nqcc.Ast;

public class Declaration(string name, Expression? initializer) : SyntaxNode
{
    public string Name { get; } = name;
    public Expression? Initializer { get; } = initializer;
}
