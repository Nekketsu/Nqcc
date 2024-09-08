namespace Nqcc.Ast.Statements;

public class Expression(Ast.Expression innerExpression) : Statement
{
    public Ast.Expression InnerExpression { get; } = innerExpression;
}
