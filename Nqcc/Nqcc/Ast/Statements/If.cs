namespace Nqcc.Ast.Statements;

public class If(Ast.Expression condition, Statement then, Statement? @else) : Statement
{
    public Ast.Expression Condition { get; } = condition;
    public Statement Then { get; } = then;
    public Statement? Else { get; } = @else;
}
