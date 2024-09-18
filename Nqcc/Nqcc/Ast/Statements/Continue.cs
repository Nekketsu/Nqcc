namespace Nqcc.Ast.Statements;

public class Continue(string label = "") : Statement
{
    public string Label { get; } = label;
}
