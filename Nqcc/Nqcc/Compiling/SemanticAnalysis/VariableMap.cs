using System.Diagnostics.CodeAnalysis;

namespace Nqcc.Compiling.SemanticAnalysis;

public class VariableMap
{
	private readonly Stack<Dictionary<string, (string, bool)>> variableMaps = [];
	private Dictionary<string, (string name, bool fromCurrentBlock)> CurrentVariableMap => variableMaps.Peek();

    public VariableMap()
    {
		variableMaps.Push([]);
    }

    public string this[string key]
    {
        set => CurrentVariableMap[key] = (value, true);
    }

	public void Push() => variableMaps.Push(CurrentVariableMap.ToDictionary(v => v.Key, v => (v.Value.name, false)));

	public void Pop() => variableMaps.Pop();

    public bool Contains(string key) => CurrentVariableMap.TryGetValue(key, out var value) && value.fromCurrentBlock;

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
    {
        if (!CurrentVariableMap.TryGetValue(key, out var innerValue))
        {
            value = null;
            return false;
        }

        value = innerValue.name;

        return true;
    }
}
