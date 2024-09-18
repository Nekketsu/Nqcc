namespace Nqcc.Ast.Statements;

public class Default(Statement statement, string label = "") : Statement
{
    public Statement Statement { get; } = statement;
    public string Label { get; } = label;
}
