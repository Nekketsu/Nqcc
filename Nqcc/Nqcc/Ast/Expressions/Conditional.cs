namespace Nqcc.Ast.Expressions;

public class Conditional(Expression condition, Expression then, Expression @else) : Expression
{
    public Expression Condition { get; } = condition;
    public Expression Then { get; } = then;
    public Expression Else { get; } = @else;
}
