namespace Nqcc.Assembly.TopLevels;

public class StaticVariable(string name, bool global, int initialValue) : TopLevel
{
    public string Name { get; } = name;
    public bool Global { get; } = global;
    public int InitialValue { get; } = initialValue;
}
