﻿using Nqcc.Ast;
using Nqcc.Ast.BinaryOperators;
using Nqcc.Ast.CompoundOperators;
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
        var body = ImmutableArray.CreateBuilder<BlockItem>();
        while (Current is not CloseBrace)
        {
            var blockItem = ParseBlockItem();
            body.Add(blockItem);
        }
        Expect<CloseBrace>();

        return new Function(name.Text, body.ToImmutable());
    }

    private BlockItem ParseBlockItem() => Current switch
    {
        Int => new Ast.BlockItems.Declaration(ParseDeclaration()),
        _ => new Ast.BlockItems.Statement(ParseStatement())
    };

    private Declaration ParseDeclaration()
    {
        Expect<Int>();
        var name = Expect<Identifier>();
        var token = TakeToken();
        var initializer = token switch
        {
            Semicolon => null,
            Lex.Equals => ParseInitializer(),
            _ => throw new Exception($"Expected an initializer or semicolon but found \"{Current}\"")
        };

        return new Declaration(name.Text, initializer);
    }

    private Expression ParseInitializer()
    {
        var initializer = ParseExpression();
        Expect<Semicolon>();

        return initializer;
    }

    private Statement ParseStatement() => Current switch
    {
        Return => ParseReturnStatement(),
        Semicolon => ParseNullStatement(),
        _ => ParseExpressionStatement()
    };

    private Ast.Statements.Return ParseReturnStatement()
    {
        Expect<Return>();
        var expression = ParseExpression();
        Expect<Semicolon>();

        return new Ast.Statements.Return(expression);
    }

    private Ast.Statements.Null ParseNullStatement()
    {
        Expect<Semicolon>();

        return new Ast.Statements.Null();
    }

    private Ast.Statements.Expression ParseExpressionStatement()
    {
        var expression = ParseExpression();
        Expect<Semicolon>();

        return new Ast.Statements.Expression(expression);
    }

    private Expression ParseExpression(int minimumPrecedence = 0)
    {
        var left = ParseFactor();

        var precedence = GetPrecedence(Current);
        while (precedence >= 0 && precedence >= minimumPrecedence)
        {
            if (Current is Lex.Equals)
            {
                Expect<Lex.Equals>();
                var right = ParseExpression(GetPrecedence(Current));
                left = new Assignment(left, right);

            }
            else if (IsCompoundAssignmentOperator(Current))
            {
                var compoundAssignmentOperator = ParseCompoundAssignmentOperator();
                var right = ParseExpression(GetPrecedence(Current));
                left = new Compound(left, compoundAssignmentOperator, right);
            }
            else
            {
                var binaryOperator = ParseBinaryOperator();
                var right = ParseExpression(precedence + 1);
                left = new Binary(left, binaryOperator, right);
            }

            precedence = GetPrecedence(Current);
        }

        return left;
    }

    private Expression ParseFactor()
    {
        var expression = Current switch
        {
            Lex.Constant => ParseConstantExpression(),
            Identifier => ParseVariableExpression(),
            Tilde or Minus or Bang => ParseUnaryExpression(),
            PlusPlus or MinusMinus => ParsePrefixExpression(),
            OpenParenthesis => ParseParenthesizedExpression(),
            _ => throw new Exception($"Expected an expression but found \"{Current}\""),
        };

        if (IsPostfixOperator(Current))
        {
            var @operator = ParsePostfixOperator();
            expression = new Postfix(expression, @operator);
        }

        return expression;
    }

    private Ast.Expressions.Constant ParseConstantExpression()
    {
        var constant = Expect<Lex.Constant>();

        return new Ast.Expressions.Constant(constant.Value);
    }

    private Variable ParseVariableExpression()
    {
        var identifier = Expect<Identifier>();

        return new Variable(identifier.Text);
    }

    private Unary ParseUnaryExpression()
    {
        var unaryOperator = ParseUnaryOperator();
        var expression = ParseFactor();

        return new Unary(unaryOperator, expression);
    }

    private Prefix ParsePrefixExpression()
    {
        var prefixOperator = ParsePrefixOperator();
        var expression = ParseFactor();

        return new Prefix(prefixOperator, expression);
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
            AmpersandAmpersand => new And(),
            PipePipe => new Or(),
            EqualsEquals => new Ast.BinaryOperators.Equals(),
            BangEquals => new NotEquals(),
            Less => new LessThan(),
            Lex.LessOrEquals => new Ast.BinaryOperators.LessOrEquals(),
            Greater => new GreaterThan(),
            Lex.GreaterOrEquals => new Ast.BinaryOperators.GreaterOrEquals(),
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
            Bang => new Not(),
            _ => throw new Exception($"Expected a unary operator but found a {token}")
        };

        return unaryOperator;
    }

    private PrefixOperator ParsePrefixOperator()
    {
        var token = TakeToken();

        PrefixOperator prefixOperator = token switch
        {
            PlusPlus => new Ast.PrefixOperators.Increment(),
            MinusMinus => new Ast.PrefixOperators.Decrement(),
            _ => throw new NotImplementedException()
        };

        return prefixOperator;
    }

    private static int GetPrecedence(SyntaxToken token) => token switch
    {
        PlusPlus or MinusMinus => 60,
        Star or Slash or Percent => 50,
        Plus or Minus => 45,
        LessLess or GreaterGreater => 40,
        Less or Lex.LessOrEquals or Greater or Lex.GreaterOrEquals => 35,
        EqualsEquals or BangEquals => 30,
        Ampersand => 25,
        Hat => 20,
        Pipe => 15,
        AmpersandAmpersand => 10,
        PipePipe => 5,
        Lex.Equals or PlusEquals or MinusEquals or StarEquals or SlashEquals or PercentEquals or AmpersandEquals or PipeEquals or HatEquals or LessLessEquals or GreaterGreaterEquals => 1,
        _ => -1
    };

    private static bool IsCompoundAssignmentOperator(SyntaxToken token) => token
        is PlusEquals or MinusEquals or StarEquals or SlashEquals or PercentEquals
        or AmpersandEquals or PipeEquals or HatEquals or LessLessEquals or GreaterGreaterEquals;

    private CompoundOperator ParseCompoundAssignmentOperator() => TakeToken() switch
    {
        PlusEquals => new AddAssignment(),
        MinusEquals => new SubtractAssignment(),
        StarEquals => new MultiplyAssignment(),
        SlashEquals => new DivideAssignment(),
        PercentEquals => new ModuloAssignment(),
        AmpersandEquals => new BitwiseAndAssignment(),
        PipeEquals => new BitwiseOrAssignment(),
        HatEquals => new BitwiseXorAssignment(),
        LessLessEquals => new LeftShiftAssignment(),
        GreaterGreaterEquals => new RightShiftAssignment(),
        _ => throw new NotImplementedException()
    };

    private static bool IsPostfixOperator(SyntaxToken current) => current is PlusPlus or MinusMinus;

    private PostfixOperator ParsePostfixOperator() => TakeToken() switch
    {
        PlusPlus => new Ast.PostfixOperators.Increment(),
        MinusMinus => new Ast.PostfixOperators.Decrement(),
        _ => throw new NotImplementedException()
    };
}
