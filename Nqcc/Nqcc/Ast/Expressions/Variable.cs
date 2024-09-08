namespace Nqcc.Ast.Expressions;

public class Variable(string name) : Expression
{
    public string Name { get; } = name;
}
