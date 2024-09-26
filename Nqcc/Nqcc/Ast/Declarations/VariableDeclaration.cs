namespace Nqcc.Ast.Declarations;

public class VariableDeclaration(string name, Expression? initializer) : Declaration
{
    public string Name { get; } = name;
    public Expression? Initializer { get; } = initializer;
}
