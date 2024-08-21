namespace Nqcc.Lex;

public class Constant(string text, int value) : SyntaxToken(text)
{
    public int Value { get; } = value;
}
