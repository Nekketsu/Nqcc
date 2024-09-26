using System.Diagnostics.CodeAnalysis;

namespace Nqcc.Compiling.SemanticAnalysis;

public class IdentifierMap
{
    private readonly Stack<Dictionary<string, Identifier>> identifierMaps = [];
    private Dictionary<string, Identifier> CurrentIdentifierMap => identifierMaps.Peek();

    public IdentifierMap()
    {
        identifierMaps.Push([]);
    }

    public Identifier this[string key]
    {
        get => CurrentIdentifierMap[key];
        set => CurrentIdentifierMap[key] = value;
    }

    public void Add(string key, Identifier identifier)
    {
        CurrentIdentifierMap.Add(key, identifier);
    }

    public void Push() => identifierMaps.Push(CurrentIdentifierMap.ToDictionary(v => v.Key, v => new Identifier(v.Value.Name, v.Value.HasLinkage, false)));

    public void Pop() => identifierMaps.Pop();

    public bool ContainsInCurrentScope(string key) => CurrentIdentifierMap.TryGetValue(key, out var value) && value.FromCurrentScope;

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out Identifier value) => CurrentIdentifierMap.TryGetValue(key, out value);
}
