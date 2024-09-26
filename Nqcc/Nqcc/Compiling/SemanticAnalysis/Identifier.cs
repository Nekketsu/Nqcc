namespace Nqcc.Compiling.SemanticAnalysis;

public class Identifier(string name, bool hasLinkage = false, bool fromCurrentScope = true)
{
    public string Name { get; } = name;
    public bool HasLinkage { get; } = hasLinkage;
    public bool FromCurrentScope { get; } = fromCurrentScope;
}
