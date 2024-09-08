namespace Nqcc.Ast.Expressions;

public class Postfix(Expression expression, PostfixOperator @operator) : Expression
{
    public Expression Expression { get; } = expression;
    public PostfixOperator Operator { get; } = @operator;
}
