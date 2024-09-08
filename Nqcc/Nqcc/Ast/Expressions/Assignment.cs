namespace Nqcc.Ast.Expressions;

public class Assignment(Expression left, Expression right) : Expression
{
    public Expression Left { get; } = left;
    public Expression Right { get; } = right;
}
