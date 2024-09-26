using System.Collections.Immutable;

namespace Nqcc.Ast.Expressions;

public class FunctionCall(string name, ImmutableArray<Expression> arguments) : Expression
{
    public string Name { get; } = name;
    public ImmutableArray<Expression> Arguments { get; } = arguments;
}
