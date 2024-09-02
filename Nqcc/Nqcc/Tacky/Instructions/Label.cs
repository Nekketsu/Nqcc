namespace Nqcc.Tacky.Instructions;

public class Label(string identifier) : Instruction
{
    public string Identifier { get; } = identifier;
}
