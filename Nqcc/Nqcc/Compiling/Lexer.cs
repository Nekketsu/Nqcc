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
                if (Current == '-')
                {
                    position++;
                    return new MinusMinus(CurrentTokenText);
                }
                return new Minus(CurrentTokenText);
            case '+':
                position++;
                return new Plus(CurrentTokenText);
            case '*':
                position++;
                return new Star(CurrentTokenText);
            case '/':
                position++;
                return new Slash(CurrentTokenText);
            case '%':
                position++;
                return new Percent(CurrentTokenText);
            case '&':
                position++;
                if (Current == '&')
                {
                    position++;
                    return new AmpersandAmpersand(CurrentTokenText);
                }
                return new Ampersand(CurrentTokenText);
            case '|':
                position++;
                if (Current == '|')
                {
                    position++;
                    return new PipePipe(CurrentTokenText);
                }
                return new Pipe(CurrentTokenText);
            case '^':
                position++;
                return new Hat(CurrentTokenText);
            case '<':
                position++;
                switch (Current)
                {
                    case '<':
                        position++;
                        return new LessLess(CurrentTokenText);
                    case '=':
                        position++;
                        return new LessOrEquals(CurrentTokenText);
                    default:
                        return new Less(CurrentTokenText);
                }
            case '>':
                position++;
                switch (Current)
                {
                    case '>':
                        position++;
                        return new GreaterGreater(CurrentTokenText);
                    case '=':
                        position++;
                        return new GreaterOrEquals(CurrentTokenText);
                    default:
                        return new Greater(CurrentTokenText);
                }
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
                break;
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
