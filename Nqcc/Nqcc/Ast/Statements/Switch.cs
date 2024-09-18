using System.Collections.Immutable;

namespace Nqcc.Ast.Statements;

public class Switch(Ast.Expression condition, Statement body, string label = "", ImmutableArray<Case>? cases = null, Default? @default = null) : Statement
{
    public Ast.Expression Condition { get; } = condition;
    public Statement Body { get; } = body;
    public string Label { get; } = label;
    public ImmutableArray<Case> Cases { get; } = cases ?? [];
    public Default? Default { get; } = @default;
}
