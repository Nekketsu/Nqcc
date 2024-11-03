using Nqcc.Ast;
using Nqcc.Ast.BinaryOperators;
using Nqcc.Ast.CompoundOperators;
using Nqcc.Ast.Declarations;
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
    private SyntaxToken LookAhead => Peek(1);
    private bool IsEof => position >= tokens.Length;

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

        if (!IsEof)
        {
            throw new Exception($"Expected end of file but got {Current}");
        }

        return program;
    }

    private Program ParseProgram()
    {
        var builder = ImmutableArray.CreateBuilder<Declaration>();

        while (!IsEof)
        {
            builder.Add(ParseDeclaration());
        }

        return new Program(builder.ToImmutable());
    }

    private FunctionDeclaration ParseFunctionDeclaration(StorageClass? storageClass = null, Identifier? name = null)
    {
        if (name is null)
        {
            storageClass = ParseTypeAndStorage();
            name = Expect<Identifier>();
        }
        Expect<OpenParenthesis>();
        var parameters = ParseParameters();
        Expect<CloseParenthesis>();
        Block? body = null;
        if (Current is OpenBrace)
        {
            body = ParseBlock();
        }
        else
        {
            Expect<Semicolon>();
        }

        return new FunctionDeclaration(name.Text, parameters, body, storageClass);
    }

    private ImmutableArray<string> ParseParameters()
    {
        var builder = ImmutableArray.CreateBuilder<string>();

        switch (Current)
        {
            case Lex.Keywords.Void:
                Expect<Lex.Keywords.Void>();
                break;
            case not Lex.Keywords.Void:
                {
                    bool hasParameters = true;
                    do
                    {
                        Expect<Int>();
                        var name = Expect<Identifier>();
                        builder.Add(name.Text);

                        if (Current is Comma)
                        {
                            Expect<Comma>();
                        }
                        else
                        {
                            hasParameters = false;
                        }
                    } while (hasParameters);
                    break;
                }
        }

        return builder.ToImmutable();
    }

    private Block ParseBlock()
    {
        Expect<OpenBrace>();
        var blockItems = ImmutableArray.CreateBuilder<BlockItem>();
        while (Current is not CloseBrace)
        {
            var blockItem = ParseBlockItem();
            blockItems.Add(blockItem);
        }
        Expect<CloseBrace>();


        return new Block(blockItems.ToImmutable());
    }

    private BlockItem ParseBlockItem() => IsSpecifier(Current)
        ? new Ast.BlockItems.Declaration(ParseDeclaration())
        : new Ast.BlockItems.Statement(ParseStatement());

    private Declaration ParseDeclaration()
    {
        var storageClass = ParseTypeAndStorage();
        var name = Expect<Identifier>();

        if (Current is not OpenParenthesis)
        {
            return ParseVariableDeclaration(storageClass, name);
        }
        else
        {
            return ParseFunctionDeclaration(storageClass, name);
        }
    }

    private StorageClass? ParseTypeAndStorage()
    {
        var typeCount = 0;
        var storageClasses = new List<StorageClass>();

        do
        {
            switch (TakeToken())
            {
                case Int:
                    typeCount++;
                    break;
                case Static:
                    storageClasses.Add(new Ast.StorageClasses.Static());
                    break;
                case Extern:
                    storageClasses.Add(new Ast.StorageClasses.Extern());
                    break;
            }
        } while (IsSpecifier(Current));

        if (typeCount != 1)
        {
            throw new Exception("Invalid type specifier");
        }
        if (storageClasses.Count > 1)
        {
            throw new Exception("Invalid storage class");
        }

        return storageClasses.Count == 1
            ? storageClasses.Single()
            : null;
    }

    private VariableDeclaration ParseVariableDeclaration(StorageClass? storageClass = null, Identifier? name = null)
    {
        if (name is null)
        {
            storageClass = ParseTypeAndStorage();
            name = Expect<Identifier>();
        }

        var token = TakeToken();
        var initializer = token switch
        {
            Semicolon => null,
            Lex.Equals => ParseInitializer(),
            _ => throw new Exception($"Expected an initializer or semicolon but found \"{Current}\"")
        };

        return new VariableDeclaration(name.Text, initializer, storageClass);
    }

    private ForInit ParseForInit() => IsSpecifier(Current)
        ? new Ast.ForInits.InitDeclaration(ParseVariableDeclaration())
        : new Ast.ForInits.InitExpression(ParseOptionalExpression<Semicolon>());

    private Expression ParseInitializer()
    {
        var initializer = ParseExpression();
        Expect<Semicolon>();

        return initializer;
    }

    private Statement ParseStatement() => Current switch
    {
        Return => ParseReturnStatement(),
        If => ParseIfStatement(),
        Semicolon => ParseNullStatement(),
        Identifier when LookAhead is Colon => ParseLabelStatement(),
        Goto => ParseGotoStatement(),
        OpenBrace => ParseCompoundStatement(),
        Break => ParseBreakStatement(),
        Continue => ParseContinueStatement(),
        While => ParseWhileStatement(),
        Do => ParseDoWhileStatement(),
        For => ParseForStatement(),
        Switch => ParseSwitchStatement(),
        Case => ParseCaseStatement(),
        Default => ParseDefaultStatement(),
        _ => ParseExpressionStatement()
    };

    private Ast.Statements.Return ParseReturnStatement()
    {
        Expect<Return>();
        var expression = ParseExpression();
        Expect<Semicolon>();

        return new Ast.Statements.Return(expression);
    }

    private Ast.Statements.If ParseIfStatement()
    {
        Expect<If>();
        Expect<OpenParenthesis>();
        var condition = ParseExpression();
        Expect<CloseParenthesis>();
        var then = ParseStatement();
        Statement? @else = null;
        if (Current is Else)
        {
            Expect<Else>();
            @else = ParseStatement();
        }

        return new Ast.Statements.If(condition, then, @else);
    }

    private Ast.Statements.Null ParseNullStatement()
    {
        Expect<Semicolon>();

        return new Ast.Statements.Null();
    }

    private Ast.Statements.Label ParseLabelStatement()
    {
        var identifier = Expect<Identifier>();
        Expect<Colon>();
        var statement = ParseStatement();

        return new Ast.Statements.Label(identifier.Text, statement);
    }

    private Ast.Statements.Goto ParseGotoStatement()
    {
        Expect<Goto>();
        var identifier = Expect<Identifier>();
        Expect<Semicolon>();

        return new Ast.Statements.Goto(identifier.Text);
    }

    private Ast.Statements.Compound ParseCompoundStatement()
    {
        var block = ParseBlock();

        return new Ast.Statements.Compound(block);
    }

    private Ast.Statements.Break ParseBreakStatement()
    {
        Expect<Break>();
        Expect<Semicolon>();

        return new Ast.Statements.Break();
    }

    private Ast.Statements.Continue ParseContinueStatement()
    {
        Expect<Continue>();
        Expect<Semicolon>();

        return new Ast.Statements.Continue();
    }

    private Ast.Statements.While ParseWhileStatement()
    {
        Expect<While>();
        Expect<OpenParenthesis>();
        var condition = ParseExpression();
        Expect<CloseParenthesis>();
        var body = ParseStatement();

        return new Ast.Statements.While(condition, body);
    }

    private Ast.Statements.DoWhile ParseDoWhileStatement()
    {
        Expect<Do>();
        var body = ParseStatement();
        Expect<While>();
        Expect<OpenParenthesis>();
        var condition = ParseExpression();
        Expect<CloseParenthesis>();
        Expect<Semicolon>();

        return new Ast.Statements.DoWhile(body, condition);
    }

    private Ast.Statements.For ParseForStatement()
    {
        Expect<For>();
        Expect<OpenParenthesis>();
        var init = ParseForInit();
        var condition = ParseOptionalExpression<Semicolon>();
        var post = ParseOptionalExpression<CloseParenthesis>();
        var body = ParseStatement();

        return new Ast.Statements.For(init, condition, post, body);
    }

    private Expression? ParseOptionalExpression<T>() where T : SyntaxToken
    {
        var expression = Current is T
            ? null
            : ParseExpression();

        Expect<T>();

        return expression;
    }

    private Ast.Statements.Switch ParseSwitchStatement()
    {
        Expect<Switch>();
        Expect<OpenParenthesis>();
        var condition = ParseExpression();
        Expect<CloseParenthesis>();
        var body = ParseStatement();

        return new Ast.Statements.Switch(condition, body);
    }

    private Ast.Statements.Case ParseCaseStatement()
    {
        Expect<Case>();
        var condition = ParseExpression();
        Expect<Colon>();
        var statement = ParseStatement();

        return new Ast.Statements.Case(condition, statement);
    }

    private Ast.Statements.Default ParseDefaultStatement()
    {
        Expect<Default>();
        Expect<Colon>();
        var statement = ParseStatement();

        return new Ast.Statements.Default(statement);
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
            else if (Current is Question)
            {
                var currentPrecedence = GetPrecedence(Current);
                Expect<Question>();
                var then = ParseExpression();
                Expect<Colon>();
                var @else = ParseExpression(currentPrecedence);
                left = new Conditional(left, then, @else);
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
            Identifier when LookAhead is not OpenParenthesis => ParseVariableExpression(),
            Tilde or Minus or Bang => ParseUnaryExpression(),
            PlusPlus or MinusMinus => ParsePrefixExpression(),
            OpenParenthesis => ParseParenthesizedExpression(),
            Identifier when LookAhead is OpenParenthesis => ParseFunctionCall(),
            _ => throw new Exception($"Expected an expression but found \"{Current}\""),
        };

        while (IsPostfixOperator(Current))
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

    private FunctionCall ParseFunctionCall()
    {
        var name = Expect<Identifier>();
        Expect<OpenParenthesis>();
        var arguments = (Current is CloseParenthesis) ? [] : ParseArguments();
        Expect<CloseParenthesis>();

        return new FunctionCall(name.Text, arguments);
    }

    private ImmutableArray<Expression> ParseArguments()
    {
        var builder = ImmutableArray.CreateBuilder<Expression>();

        builder.Add(ParseExpression());
        while (Current is Comma)
        {
            Expect<Comma>();
            builder.Add(ParseExpression());
        }

        return builder.ToImmutable();
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
        Question => 3,
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

    private static bool IsSpecifier(SyntaxToken token) => token is Int or Static or Extern;
}
