using Nqcc.Lex;
using System.Collections;
using Keywords = Nqcc.Lex.Keywords;

namespace Nqcc.Compiling;

public class Lexer(string code) : IEnumerable<SyntaxToken>
{
    private readonly string code = code.Trim();
    private int start = 0;
    private int position = 0;

    private char Current => Peek(0);

    private char Peek(int offset)
    {
        var index = position + offset;

        if (index >= code.Length)
        {
            return '\0';
        }

        return code[index];
    }

    private string CurrentTokenText => code[start..position];

    public SyntaxToken Lex()
    {
        start = position;

        TrimStartWhitespaces();

        if (char.IsLetter(Current) || Current == '_')
        {
            return ReadIdentifierKeyword();
        }

        if (char.IsNumber(Current))
        {
            return ReadConstant();
        }

        switch (Current)
        {
            case '(':
                position++;
                return new OpenParenthesis(CurrentTokenText);
            case ')':
                position++;
                return new CloseParenthesis(CurrentTokenText);
            case '{':
                position++;
                return new OpenBrace(CurrentTokenText);
            case '}':
                position++;
                return new CloseBrace(CurrentTokenText);
            case ';':
                position++;
                return new Semicolon(CurrentTokenText);
            case '~':
                position++;
                return new Tilde(CurrentTokenText);
            case '-':
                position++;
                switch (Current)
                {
                    case '-':
                        position++;
                        return new MinusMinus(CurrentTokenText);
                    case '=':
                        position++;
                        return new MinusEquals(CurrentTokenText);
                }
                return new Minus(CurrentTokenText);
            case '+':
                position++;
                switch (Current)
                {
                    case '+':
                        position++;
                        return new PlusPlus(CurrentTokenText);
                    case '=':
                        position++;
                        return new PlusEquals(CurrentTokenText);
                }
                return new Plus(CurrentTokenText);
            case '*':
                position++;
                if (Current == '=')
                {
                    position++;
                    return new StarEquals(CurrentTokenText);
                }
                return new Star(CurrentTokenText);
            case '/':
                position++;
                if (Current == '=')
                {
                    position++;
                    return new SlashEquals(CurrentTokenText);
                }
                return new Slash(CurrentTokenText);
            case '%':
                position++;
                if (Current == '=')
                {
                    position++;
                    return new PercentEquals(CurrentTokenText);
                }
                return new Percent(CurrentTokenText);
            case '&':
                position++;
                switch (Current)
                {
                    case '&':
                        position++;
                        return new AmpersandAmpersand(CurrentTokenText);
                    case '=':
                        position++;
                        return new AmpersandEquals(CurrentTokenText);
                }
                return new Ampersand(CurrentTokenText);
            case '|':
                position++;
                switch (Current)
                {
                    case '|':
                        position++;
                        return new PipePipe(CurrentTokenText);
                    case '=':
                        position++;
                        return new PipeEquals(CurrentTokenText);
                }
                return new Pipe(CurrentTokenText);
            case '^':
                position++;
                if (Current == '=')
                {
                    position++;
                    return new HatEquals(CurrentTokenText);
                }
                return new Hat(CurrentTokenText);
            case '<':
                position++;
                switch (Current)
                {
                    case '<':
                        position++;
                        if (Current == '=')
                        {
                            position++;
                            return new LessLessEquals(CurrentTokenText);
                        }
                        return new LessLess(CurrentTokenText);
                    case '=':
                        position++;
                        return new LessOrEquals(CurrentTokenText);
                }
                return new Less(CurrentTokenText);
            case '>':
                position++;
                switch (Current)
                {
                    case '>':
                        position++;
                        if (Current == '=')
                        {
                            position++;
                            return new GreaterGreaterEquals(CurrentTokenText);
                        }
                        return new GreaterGreater(CurrentTokenText);
                    case '=':
                        position++;
                        return new GreaterOrEquals(CurrentTokenText);
                }
                return new Greater(CurrentTokenText);
            case '!':
                position++;
                if (Current == '=')
                {
                    position++;
                    return new BangEquals(CurrentTokenText);
                }
                return new Bang(CurrentTokenText);
            case '=':
                position++;
                if (Current == '=')
                {
                    position++;
                    return new EqualsEquals(CurrentTokenText);
                }
                return new Equals(CurrentTokenText);
            case '?':
                position++;
                return new Question(CurrentTokenText);
            case ':':
                position++;
                return new Colon(CurrentTokenText);
            case ',':
                position++;
                return new Comma(CurrentTokenText);
        }

        throw new Exception($"Lexer failure: bad character {Current}");
    }

    private SyntaxToken ReadIdentifierKeyword()
    {
        while (char.IsLetterOrDigit(Current) || Current == '_')
        {
            position++;
        }

        SyntaxToken token = CurrentTokenText switch
        {
            "int" => new Keywords.Int(CurrentTokenText),
            "void" => new Keywords.Void(CurrentTokenText),
            "return" => new Keywords.Return(CurrentTokenText),
            "if" => new Keywords.If(CurrentTokenText),
            "else" => new Keywords.Else(CurrentTokenText),
            "goto" => new Keywords.Goto(CurrentTokenText),
            "do" => new Keywords.Do(CurrentTokenText),
            "while" => new Keywords.While(CurrentTokenText),
            "for" => new Keywords.For(CurrentTokenText),
            "break" => new Keywords.Break(CurrentTokenText),
            "continue" => new Keywords.Continue(CurrentTokenText),
            "switch" => new Keywords.Switch(CurrentTokenText),
            "case" => new Keywords.Case(CurrentTokenText),
            "default" => new Keywords.Default(CurrentTokenText),
            _ => new Identifier(CurrentTokenText)
        };

        return token;
    }

    private Constant ReadConstant()
    {
        position++;
        while (char.IsLetterOrDigit(Current))
        {
            position++;
        }

        var text = CurrentTokenText;
        if (!int.TryParse(text, out var value))
        {
            throw new Exception($"Lexer failure: input starts with a digit but isn't a constant: {text}");
        }

        return new Constant(text, value);
    }

    private void TrimStartWhitespaces()
    {
        while (char.IsWhiteSpace(Current))
        {
            position++;
        }

        start = position;
    }

    public IEnumerator<SyntaxToken> GetEnumerator()
    {
        while (HasNext())
        {
            yield return Lex();
        }
    }

    private bool HasNext()
    {
        return Current != '\0';
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
