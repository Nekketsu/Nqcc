namespace Nqcc.Ast.Expressions;

public class Unary(UnaryOperator @operator, Expression expression) : Expression
{
    public UnaryOperator Operator { get; } = @operator;
    public Expression Expression { get; } = expression;
}
