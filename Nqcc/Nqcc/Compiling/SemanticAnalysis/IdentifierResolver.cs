using Nqcc.Ast;
using Nqcc.Ast.Declarations;
using Nqcc.Ast.Expressions;
using Nqcc.Ast.ForInits;
using Nqcc.Ast.StorageClasses;
using System.Collections.Immutable;

namespace Nqcc.Compiling.SemanticAnalysis;

public class IdentifierResolver(Program ast)
{
    private readonly IdentifierMap identifierMap = new();

    public Program Resolve() => ResolveProgram(ast);

    private Program ResolveProgram(Program program)
    {
        var builder = ImmutableArray.CreateBuilder<Declaration>();

        foreach (var declaration in program.Declarations)
        {
            builder.Add(ResolveGlobalDeclaration(declaration));
        }

        return new Program(builder.ToImmutable());
    }

    private Declaration ResolveGlobalDeclaration(Declaration declaration) => declaration switch
    {
        FunctionDeclaration functionDeclaration => ResolveFunctionDeclaration(functionDeclaration),
        VariableDeclaration variableDeclaration => ResolveFileScopedVariableDeclaration(variableDeclaration),
        _ => throw new NotImplementedException()
    };

    private VariableDeclaration ResolveFileScopedVariableDeclaration(VariableDeclaration variableDeclaration)
    {
        identifierMap[variableDeclaration.Name] = new Identifier(variableDeclaration.Name, true);

        return variableDeclaration;
    }

    private Block ResolveBlock(Block block)
    {
        var builder = ImmutableArray.CreateBuilder<BlockItem>();

        foreach (var blockItem in block.BlockItems)
        {
            builder.Add(ResolveBlockItem(blockItem));
        }

        return new Block(builder.ToImmutable());
    }

    private BlockItem ResolveBlockItem(BlockItem blockItem) => blockItem switch
    {
        Ast.BlockItems.Statement statement => new Ast.BlockItems.Statement(ResolveStatement(statement.InnerStatement)),
        Ast.BlockItems.Declaration declaration => new Ast.BlockItems.Declaration(ResolveLocalDeclaration(declaration.InnerDeclaration)),
        _ => blockItem
    };

    private Statement ResolveStatement(Statement statement) => statement switch
    {
        Ast.Statements.Return @return => new Ast.Statements.Return(ResolveExpression(@return.Expression)),
        Ast.Statements.Expression expression => new Ast.Statements.Expression(ResolveExpression(expression.InnerExpression)),
        Ast.Statements.Compound compound => ResolveCompound(compound),
        Ast.Statements.If @if => new Ast.Statements.If(ResolveExpression(@if.Condition), ResolveStatement(@if.Then), @if.Else is null ? null : ResolveStatement(@if.Else)),
        Ast.Statements.Label label => new Ast.Statements.Label(label.Name, ResolveStatement(label.Statement)),
        Ast.Statements.While @while => new Ast.Statements.While(ResolveExpression(@while.Condition), ResolveStatement(@while.Body)),
        Ast.Statements.DoWhile doWhile => new Ast.Statements.DoWhile(ResolveStatement(doWhile.Body), ResolveExpression(doWhile.Condition)),
        Ast.Statements.For @for => ResolveFor(@for),
        Ast.Statements.Switch @switch => ResolveSwitch(@switch),
        Ast.Statements.Case @case => ResolveCase(@case),
        Ast.Statements.Default @default => ResolveDefault(@default),
        _ => statement
    };

    private Ast.Statements.Compound ResolveCompound(Ast.Statements.Compound compound)
    {
        identifierMap.Push();
        var block = ResolveBlock(compound.Block);
        identifierMap.Pop();

        return new Ast.Statements.Compound(block);
    }

    private Ast.Statements.For ResolveFor(Ast.Statements.For @for)
    {
        identifierMap.Push();
        var init = ResolveForInit(@for.Init);
        var condition = ResolveOptionalExpression(@for.Condition);
        var post = ResolveOptionalExpression(@for.Post);
        var body = ResolveStatement(@for.Body);
        identifierMap.Pop();

        return new Ast.Statements.For(init, condition, post, body);
    }

    private Ast.Statements.Switch ResolveSwitch(Ast.Statements.Switch @switch)
    {
        var condition = ResolveExpression(@switch.Condition);
        var body = ResolveStatement(@switch.Body);

        return new Ast.Statements.Switch(condition, body);
    }

    private Ast.Statements.Case ResolveCase(Ast.Statements.Case @case)
    {
        var condition = ResolveExpression(@case.Condition);
        var statement = ResolveStatement(@case.Statement);

        return new Ast.Statements.Case(condition, statement);
    }

    private Ast.Statements.Default ResolveDefault(Ast.Statements.Default @default)
    {
        var statement = ResolveStatement(@default.Statement);

        return new Ast.Statements.Default(statement);
    }

    private ForInit ResolveForInit(ForInit init) => init switch
    {
        InitExpression initExpression => new InitExpression(ResolveOptionalExpression(initExpression.Expression)),
        InitDeclaration initDeclaration => new InitDeclaration(ResolveLocalVariableDeclaration(initDeclaration.Declaration)),
        _ => throw new NotImplementedException()
    };

    private Expression? ResolveOptionalExpression(Expression? expression) => expression switch
    {
        Expression => ResolveExpression(expression),
        _ => null
    };

    private Declaration ResolveLocalDeclaration(Declaration declaration) => declaration switch
    {
        VariableDeclaration variableDeclaration => ResolveLocalVariableDeclaration(variableDeclaration),
        FunctionDeclaration { Body: not null } => throw new Exception("Nested function definitions are not allowed"),
        FunctionDeclaration { StorageClass: Static } => throw new Exception("Static keyword not allowed on local function delcarations"),
        FunctionDeclaration functionDeclaration => ResolveFunctionDeclaration(functionDeclaration),
        _ => throw new NotImplementedException()

    };

    private VariableDeclaration ResolveLocalVariableDeclaration(VariableDeclaration variableDeclaration)
    {
        if (identifierMap.TryGetValue(variableDeclaration.Name, out var identifer))
        {
            if (identifer.FromCurrentScope && !(identifer.HasLinkage && variableDeclaration.StorageClass is Extern))
            {
                throw new Exception("Duplicate variable declaration!");
            }
        }
        if (variableDeclaration.StorageClass is Extern)
        {
            identifierMap[variableDeclaration.Name] = new Identifier(variableDeclaration.Name, true);

            return variableDeclaration;
        }
        else
        {
            var uniqueName = UniqueId.MakeTemporary();
            identifierMap[variableDeclaration.Name] = new Identifier(uniqueName);
            var initializer = variableDeclaration.Initializer is null
                ? null
                : ResolveExpression(variableDeclaration.Initializer);

            return new VariableDeclaration(uniqueName, initializer, variableDeclaration.StorageClass);
        }
    }

    private FunctionDeclaration ResolveFunctionDeclaration(FunctionDeclaration functionDeclaration)
    {
        if (identifierMap.TryGetValue(functionDeclaration.Name, out var identifier))
        {
            if (identifier.FromCurrentScope && !identifier.HasLinkage)
            {
                throw new Exception("Duplicate declaration");
            }
        }
        identifierMap[functionDeclaration.Name] = new Identifier(functionDeclaration.Name, true);

        identifierMap.Push();
        var builder = ImmutableArray.CreateBuilder<string>();
        foreach (var parameter in functionDeclaration.Parameters)
        {
            builder.Add(ResolveParameter(parameter));
        }
        var body = functionDeclaration.Body is null ? null : ResolveBlock(functionDeclaration.Body);
        identifierMap.Pop();

        return new FunctionDeclaration(functionDeclaration.Name, builder.ToImmutable(), body, functionDeclaration.StorageClass);
    }

    private string ResolveParameter(string parameter)
    {
        if (identifierMap.ContainsInCurrentScope(parameter))
        {
            throw new Exception("Duplicate variable declaration!");
        }

        var uniqueName = UniqueId.MakeTemporary();
        identifierMap[parameter] = new Identifier(uniqueName);

        return uniqueName;
    }

    private Expression ResolveExpression(Expression expression) => expression switch
    {
        Assignment { Left: Variable } assignment => new Assignment(ResolveExpression(assignment.Left), ResolveExpression(assignment.Right)),
        Assignment => throw new Exception("Invalid lvalue!"),

        Variable variable when identifierMap.TryGetValue(variable.Name, out var identifier) => new Variable(identifier.Name),
        Variable => throw new Exception("Undeclared variable!"),

        Unary unary => new Unary(unary.Operator, ResolveExpression(unary.Expression)),
        Binary binary => new Binary(ResolveExpression(binary.Left), binary.Operator, ResolveExpression(binary.Right)),

        Compound { Left: Variable } compound => new Compound(ResolveExpression(compound.Left), compound.Operator, ResolveExpression(compound.Right)),
        Compound => throw new Exception("Invalid lvalue!"),

        Prefix { Expression: Variable } prefix => new Prefix(prefix.Operator, ResolveExpression(prefix.Expression)),
        Prefix => throw new Exception("Invalid lvalue!"),

        Postfix { Expression: Variable } postfix => new Postfix(ResolveExpression(postfix.Expression), postfix.Operator),
        Postfix => throw new Exception("Invalid lvalue!"),

        Conditional conditional => new Conditional(ResolveExpression(conditional.Condition), ResolveExpression(conditional.Then), ResolveExpression(conditional.Else)),

        FunctionCall functionCall => ResolveFunctionCall(functionCall),

        _ => expression
    };

    private FunctionCall ResolveFunctionCall(FunctionCall functionCall)
    {
        if (!identifierMap.TryGetValue(functionCall.Name, out var identifier))
        {
            throw new Exception("Undeclared function!");
        }

        var builder = ImmutableArray.CreateBuilder<Expression>();
        foreach (var argument in functionCall.Arguments)
        {
            builder.Add(ResolveExpression(argument));
        }

        return new FunctionCall(identifier.Name, builder.ToImmutable());
    }
}
