using Nqcc.Ast;
using Nqcc.Ast.BinaryOperators;
using Nqcc.Ast.Expressions;
using Nqcc.Ast.UnaryOperators;
using Nqcc.Lex;
using Nqcc.Lex.Keywords;
using System.Collections.Immutable;

namespace Nqcc.Compiling;

public class Parser(ImmutableArray<SyntaxToken> tokens)
{
    private int position = 0;

    private SyntaxToken Peek(int offset)
    {
        var index = position + offset;

        return tokens[index];
    }

    private SyntaxToken Current => Peek(0);

    private SyntaxToken TakeToken()
    {
        var token = Current;
        position++;

        return token;
    }

    private T Expect<T>() where T : SyntaxToken
    {
        var actual = TakeToken();
        if (actual is not T token)
        {
            throw new Exception($"Expected \"{typeof(T).Name}\" but found \"{actual}\"");
        }

        return token;
    }

    public Program Parse()
    {
        var program = ParseProgram();

        if (position < tokens.Length)
        {
            throw new Exception($"Expected end of file but got {Current}");
        }

        return program;
    }

    private Program ParseProgram()
    {
        var functionSyntax = ParseFunction();

        return new Program(functionSyntax);
    }

    private Function ParseFunction()
    {
        Expect<Int>();
        var name = Expect<Identifier>();
        Expect<OpenParenthesis>();
        Expect<Lex.Keywords.Void>();
        Expect<CloseParenthesis>();
        Expect<OpenBrace>();
        var body = ParseStatement();
        Expect<CloseBrace>();

        return new Function(name.Text, body);
    }

    private Ast.Statements.Return ParseStatement()
    {
        return ParseReturnStatement();
    }

    private Ast.Statements.Return ParseReturnStatement()
    {
        Expect<Return>();
        var expression = ParseExpression();
        Expect<Semicolon>();

        return new Ast.Statements.Return(expression);
    }

    private Expression ParseExpression(int minimumPrecedence = 0)
    {
        var left = ParseFactor();

        var precedence = GetPrecedence(Current);
        while (precedence >= 0 && precedence >= minimumPrecedence)
        {
            var binaryOperator = ParseBinaryOperator();
            var right = ParseExpression(precedence + 1);
            left = new Binary(left, binaryOperator, right);

            precedence = GetPrecedence(Current);
        }

        return left;
    }

    private Expression ParseFactor() => Current switch
    {
        Lex.Constant => ParseConstantExpression(),
        Tilde or Minus => ParseUnaryExpression(),
        OpenParenthesis => ParseParenthesizedExpression(),
        _ => throw new Exception($"Expected an expression but found \"{Current}\""),
    };

    private Ast.Expressions.Constant ParseConstantExpression()
    {
        var constant = Expect<Lex.Constant>();

        return new Ast.Expressions.Constant(constant.Value);
    }

    private Unary ParseUnaryExpression()
    {
        var unaryOperator = ParseUnaryOperator();
        var expression = ParseFactor();

        return new Unary(unaryOperator, expression);
    }

    private Expression ParseParenthesizedExpression()
    {
        Expect<OpenParenthesis>();
        var expression = ParseExpression();
        Expect<CloseParenthesis>();

        return expression;
    }

    private BinaryOperator ParseBinaryOperator()
    {
        var token = TakeToken();

        BinaryOperator binaryOperator = token switch
        {
            Plus => new Add(),
            Minus => new Subtract(),
            Star => new Multiply(),
            Slash => new Divide(),
            Percent => new Modulo(),
            Ampersand => new BitwiseAnd(),
            Pipe => new BitwiseOr(),
            Hat => new BitwiseXor(),
            LessLess => new LeftShift(),
            GreaterGreater => new RightShift(),
            _ => throw new Exception($"Expected a binary operator but found a {token}")
        };

        return binaryOperator;
    }

    private UnaryOperator ParseUnaryOperator()
    {
        var token = TakeToken();

        UnaryOperator unaryOperator = token switch
        {
            Tilde => new Complement(),
            Minus => new Negate(),
            _ => throw new Exception($"Expected a unary operator but found a {token}")
        };

        return unaryOperator;
    }

    private static int GetPrecedence(SyntaxToken current) => current switch
    {
        Star or Slash or Percent => 50,
        Plus or Minus => 45,
        LessLess or GreaterGreater => 40,
        Ampersand => 25,
        Hat => 20,
        Pipe => 15,
        _ => -1
    };
}
