namespace Nqcc.Ast.Expressions;

public class Prefix(PrefixOperator @operator, Expression expression) : Expression
{
    public PrefixOperator Operator { get; } = @operator;
    public Expression Expression { get; } = expression;
}
