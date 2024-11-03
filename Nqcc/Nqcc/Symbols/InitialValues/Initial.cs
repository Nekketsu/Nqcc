namespace Nqcc.Symbols.InitialValues;

public class Initial(int value) : InitialValue
{
    public int Value { get; } = value;
}
