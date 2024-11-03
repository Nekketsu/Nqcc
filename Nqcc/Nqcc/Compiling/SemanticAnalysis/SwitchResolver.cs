using Nqcc.Ast;
using Nqcc.Ast.Declarations;
using Nqcc.Ast.Statements;
using System.Collections.Immutable;

namespace Nqcc.Compiling.SemanticAnalysis;

public class SwitchResolver(Program ast)
{
    private readonly Stack<Switch> switches = new();
    private readonly Stack<ImmutableArray<Case>.Builder> cases = new();
    private readonly Stack<Default?> defaults = new();

    private Switch? CurrentSwitch => switches.TryPeek(out var currentSwitch) ? currentSwitch : null;
    private ImmutableArray<Case>.Builder CurrentCases => cases.Peek();
    private Default? CurrentDefault
    {
        get => defaults.Peek();
        set
        {
            defaults.Pop();
            defaults.Push(value);
        }
    }

    public Program Resolve() => ResolveProgram(ast);

    private Program ResolveProgram(Program program)
    {
        var builder = ImmutableArray.CreateBuilder<Declaration>();

        foreach (var declaration in program.Declarations)
        {
            switch (declaration)
            {
                case FunctionDeclaration functionDeclaration:
                    builder.Add(ResolveFunctionDeclaration(functionDeclaration));
                    break;
                case VariableDeclaration variableDeclaration:
                    builder.Add(variableDeclaration);
                    break;
            }
        }

        return new Program(builder.ToImmutable());
    }

    private FunctionDeclaration ResolveFunctionDeclaration(FunctionDeclaration functionDeclaration)
    {
        var block = functionDeclaration.Body is null ? null : ResolveBlock(functionDeclaration.Body);

        return new FunctionDeclaration(functionDeclaration.Name, functionDeclaration.Parameters, block, functionDeclaration.StorageClass);
    }

    private Block ResolveBlock(Block block)
    {
        var builder = ImmutableArray.CreateBuilder<BlockItem>();

        foreach (var blockItem in block.BlockItems)
        {
            builder.Add(ResolveBlockItem(blockItem));
        }

        var blockItems = builder.ToImmutable();

        return new Block(blockItems);
    }

    private BlockItem ResolveBlockItem(BlockItem blockItem) => blockItem switch
    {
        Ast.BlockItems.Statement statement => new Ast.BlockItems.Statement(ResolveStatement(statement.InnerStatement)),
        _ => blockItem
    };

    private Statement ResolveStatement(Statement statement) => statement switch
    {
        While @while => ResolveWhile(@while),
        DoWhile doWhile => ResolveDoWhile(doWhile),
        For @for => ResolveFor(@for),
        Compound compound => ResolveCompound(compound),
        If @if => ResolveIf(@if),
        Switch @switch => ResolveSwitch(@switch),
        Case @case => ResolveCase(@case),
        Default @default => ResolveDefault(@default),
        Label label => ResolveLabel(label),
        _ => statement
    };

    private While ResolveWhile(While @while)
    {
        var body = ResolveStatement(@while.Body);

        return new While(@while.Condition, body, @while.Label);
    }

    private DoWhile ResolveDoWhile(DoWhile doWhile)
    {
        var body = ResolveStatement(doWhile.Body);

        return new DoWhile(body, doWhile.Condition, doWhile.Label);
    }

    private For ResolveFor(For @for)
    {
        var body = ResolveStatement(@for.Body);

        return new For(@for.Init, @for.Condition, @for.Post, body, @for.Label);
    }

    private Compound ResolveCompound(Compound compound)
    {
        var block = ResolveBlock(compound.Block);

        return new Compound(block);
    }

    private If ResolveIf(If @if)
    {
        var then = ResolveStatement(@if.Then);
        var @else = @if.Else is not null ? ResolveStatement(@if.Else) : null;

        return new If(@if.Condition, then, @else);
    }

    private Switch ResolveSwitch(Switch @switch)
    {
        switches.Push(@switch);
        cases.Push(ImmutableArray.CreateBuilder<Case>());
        defaults.Push(null);

        var body = ResolveStatement(@switch.Body);
        var resolvedSwitch = new Switch(@switch.Condition, body, @switch.Label, CurrentCases.ToImmutable(), CurrentDefault);

        defaults.Pop();
        cases.Pop();
        switches.Pop();

        return resolvedSwitch;
    }

    private Case ResolveCase(Case @case)
    {
        if (CurrentSwitch is null)
        {
            throw new Exception("A case must be inside a switch");
        }
        if (@case.Condition is not Ast.Expressions.Constant constant)
        {
            throw new Exception("Case statement values must be constant");
        }
        foreach (var currentCase in CurrentCases)
        {
            var currentConstant = (Ast.Expressions.Constant)currentCase.Condition;
            if (currentConstant.Value == constant.Value)
            {
                throw new Exception($"Duplicate of previous 'case {constant.Value}'");
            }
        }

        var statement = ResolveStatement(@case.Statement);
        var resolvedCase = new Case(@case.Condition, statement, @case.Label);


        foreach (var currentCase in CurrentCases)
        {
            var currentConstant = (Ast.Expressions.Constant)currentCase.Condition;
            if (currentConstant.Value == constant.Value)
            {
                throw new Exception($"Duplicate of previous 'case {constant.Value}'");
            }
        }

        CurrentCases.Add(resolvedCase);

        return resolvedCase;
    }

    private Default ResolveDefault(Default @default)
    {
        if (CurrentSwitch is null)
        {
            throw new Exception("A default must be inside a switch");
        }

        var statement = ResolveStatement(@default.Statement);
        var resolvedDefault = new Default(statement, @default.Label);

        if (CurrentDefault is not null)
        {
            throw new Exception("There cannot be more than one default inside a switch");
        }

        CurrentDefault = resolvedDefault;

        return @default;
    }

    private Label ResolveLabel(Label label)
    {
        var statement = ResolveStatement(label.Statement);

        return new Label(label.Name, statement);
    }
}
