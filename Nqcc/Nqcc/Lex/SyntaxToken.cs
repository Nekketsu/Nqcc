namespace Nqcc.Lex;

public abstract class SyntaxToken(string text)
{
    public string Text { get; } = text;

    public override string ToString() => GetType().Name;
}
