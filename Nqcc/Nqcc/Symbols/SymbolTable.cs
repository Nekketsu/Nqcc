using System.Diagnostics.CodeAnalysis;

namespace Nqcc.Symbols;

public class SymbolTable
{
    private readonly Dictionary<string, Symbol> symbols = [];

    public Symbol this[string name]
    {
        get => symbols[name];
    }

    public void Add(Symbol symbol)
    {
        symbols.Add(symbol.Name, symbol);
    }

    public void AddOrReplace(Symbol symbol)
    {
        symbols[symbol.Name] = symbol;
    }

    public bool TryGetValue(string name, [MaybeNullWhen(false)] out Symbol symbol) => symbols.TryGetValue(name, out symbol);
}
