using Nqcc.Symbols.IdentifierAttributes;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Nqcc.Symbols;

public class SymbolTable : IEnumerable<Symbol>
{
    private readonly Dictionary<string, Symbol> symbols = [];

    public Function GetFunction(string name) => symbols[name] switch
    {
        Function function => function,
        Variable => throw new Exception("Tried to use variable as a function name"),
        _ => throw new NotImplementedException()
    };

    public Variable GetVariable(string name) => symbols[name] switch
    {
        Variable variable => variable,
        Function => throw new Exception("Tried to use function name as variable"),
        _ => throw new NotImplementedException()
    };

    public void Add(Symbol symbol)
    {
        symbols.Add(symbol.Name, symbol);
    }

    public void AddOrReplace(Symbol symbol)
    {
        symbols[symbol.Name] = symbol;
    }

    public bool TryGetValue(string name, [MaybeNullWhen(false)] out Symbol symbol) => symbols.TryGetValue(name, out symbol);

    public bool IsStaticVariable(string name)
    {
        if (!TryGetValue(name, out var symbol))
        {
            return false;
        }

        if (symbol is not Variable variable)
        {
            throw new Exception("Internal error: functions don't have storage duration");
        }

        return variable.Attributes switch
        {
            LocalAttributes => false,
            StaticAttributes => true,
            _ => throw new NotImplementedException()
        };
    }

    public IEnumerator<Symbol> GetEnumerator() => symbols.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
