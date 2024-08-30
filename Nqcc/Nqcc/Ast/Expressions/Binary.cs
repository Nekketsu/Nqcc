namespace Nqcc.Ast.Expressions;

public class Binary(Expression left, BinaryOperator @operator, Expression right) : Expression
{
    public Expression Left { get; } = left;
    public BinaryOperator Operator { get; } = @operator;
    public Expression Right { get; } = right;
}
