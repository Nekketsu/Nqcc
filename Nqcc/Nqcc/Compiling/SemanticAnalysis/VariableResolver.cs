using Nqcc.Ast;
using Nqcc.Ast.Expressions;
using Nqcc.Ast.ForInits;
using System.Collections.Immutable;

namespace Nqcc.Compiling.SemanticAnalysis;

public class VariableResolver(Program ast)
{
    private readonly VariableMap variableMap = new();

    public Program Resolve() => ResolveProgram(ast);

    private Program ResolveProgram(Program program)
    {
        var functionDefinition = ResolveFunction(program.FunctionDefinition);

        return new Program(functionDefinition);
    }

    private Function ResolveFunction(Function function)
    {
        var body = ResolveBlock(function.Body);

        return new Function(function.Name, body);
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
        Ast.BlockItems.Declaration declaration => new Ast.BlockItems.Declaration(ResolveDeclaration(declaration.InnerDeclaration)),
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
        variableMap.Push();
        var block = ResolveBlock(compound.Block);
        variableMap.Pop();

        return new Ast.Statements.Compound(block);
    }

    private Ast.Statements.For ResolveFor(Ast.Statements.For @for)
    {
        variableMap.Push();
        var init = ResolveForInit(@for.Init);
        var condition = ResolveOptionalExpression(@for.Condition);
        var post = ResolveOptionalExpression(@for.Post);
        var body = ResolveStatement(@for.Body);
        variableMap.Pop();

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
        InitDeclaration initDeclaration => new InitDeclaration(ResolveDeclaration(initDeclaration.Declaration)),
        _ => throw new NotImplementedException()
    };

    private Expression? ResolveOptionalExpression(Expression? expression) => expression switch
    {
        Expression => ResolveExpression(expression),
        _ => null
    };

    private Declaration ResolveDeclaration(Declaration declaration)
    {
        if (variableMap.Contains(declaration.Name))
        {
            throw new Exception("Duplicate variable declaration!");
        }

        var uniqueName = UniqueId.MakeTemporary();
        variableMap[declaration.Name] = uniqueName;
        var initializer = declaration.Initializer is null
            ? null
            : ResolveExpression(declaration.Initializer);

        return new Declaration(uniqueName, initializer);
    }

    private Expression ResolveExpression(Expression expression) => expression switch
    {
        Assignment { Left: Variable } assignment => new Assignment(ResolveExpression(assignment.Left), ResolveExpression(assignment.Right)),
        Assignment => throw new Exception("Invalid lvalue!"),

        Variable variable when variableMap.TryGetValue(variable.Name, out var resolvedVariableName) => new Variable(resolvedVariableName),
        Variable => throw new Exception("Undeclared variable!"),

        Unary unary => new Unary(unary.Operator, ResolveExpression(unary.Expression)),
        Binary binary => new Binary(ResolveExpression(binary.Left), binary.Operator, ResolveExpression(binary.Right)),
        
        Compound { Left: Variable } compound => new Compound(ResolveExpression(compound.Left), compound.Operator, ResolveExpression(compound.Right)),
        Compound => throw new Exception("Invalid lvalue!"),

        Prefix { Expression: Variable} prefix => new Prefix(prefix.Operator, ResolveExpression(prefix.Expression)),
        Prefix => throw new Exception("Invalid lvalue!"),

        Postfix { Expression: Variable } postfix => new Postfix(ResolveExpression(postfix.Expression), postfix.Operator),
        Postfix => throw new Exception("Invalid lvalue!"),

        Conditional conditional => new Conditional(ResolveExpression(conditional.Condition), ResolveExpression(conditional.Then), ResolveExpression(conditional.Else)),

        _ => expression
    };
}
