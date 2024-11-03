namespace Nqcc.Tacky.TopLevels;

public class StaticVariable(string name, bool global, int initValue) : TopLevel
{
    public string Name { get; } = name;
    public bool Global { get; } = global;
    public int InitialValue { get; } = initValue;
}
