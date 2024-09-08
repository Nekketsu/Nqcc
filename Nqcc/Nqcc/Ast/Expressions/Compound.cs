namespace Nqcc.Ast.Expressions;

public class Compound(Expression left, CompoundOperator @operator, Expression right) : Expression
{
    public Expression Left { get; } = left;
    public CompoundOperator Operator { get; } = @operator;
    public Expression Right { get; } = right;
}
