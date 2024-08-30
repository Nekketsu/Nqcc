using Nqcc.Ast;
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

    private Expression ParseExpression() => Current switch
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
        var expression = ParseExpression();

        return new Unary(unaryOperator, expression);
    }

    private Expression ParseParenthesizedExpression()
    {
        Expect<OpenParenthesis>();
        var expression = ParseExpression();
        Expect<CloseParenthesis>();

        return expression;
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
}
