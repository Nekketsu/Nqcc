namespace Nqcc.Ast.Statements;

public class While(Ast.Expression condition, Statement body, string label = "") : Statement
{
    public Ast.Expression Condition { get; } = condition;
    public Statement Body { get; } = body;
    public string Label { get; } = label;
}
