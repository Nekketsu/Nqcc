namespace Nqcc.Ast.ForInits;

public class InitExpression(Expression? expression) : ForInit
{
    public Expression? Expression { get; } = expression;
}
