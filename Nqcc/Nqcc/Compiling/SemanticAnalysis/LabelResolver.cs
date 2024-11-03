using Nqcc.Ast;
using Nqcc.Ast.Declarations;
using Nqcc.Ast.Statements;
using System.Collections.Immutable;

namespace Nqcc.Compiling.SemanticAnalysis;

public class LabelResolver(Program ast)
{
    private readonly HashSet<string> labels = [];
    private readonly HashSet<string> gotos = [];

    private readonly Stack<string> functions = new();
    private string CurrentFunction => functions.Peek();

    public Program Resolve() => ResolveProgram(ast);

    private Program ResolveProgram(Program program)
    {
        var builder = ImmutableArray.CreateBuilder<Declaration>();

        foreach (var declaration in program.Declarations)
        {
            switch (declaration)
            {
                case FunctionDeclaration functionDeclaration:
                    functions.Push(functionDeclaration.Name);
                    builder.Add(ResolveFunctionDeclaration(functionDeclaration));
                    functions.Pop();
                    break;
                case VariableDeclaration:
                    builder.Add(declaration);
                    break;
            }
        }

        foreach (var @goto in gotos)
        {
            if (!labels.Contains(@goto))
            {
                throw new Exception($"Undeclared label {@goto}!");
            }
        }

        return new Program(builder.ToImmutable());
    }

    private FunctionDeclaration ResolveFunctionDeclaration(FunctionDeclaration functionDeclaration)
    {
        var body = functionDeclaration.Body is null ? null : ResolveBlock(functionDeclaration.Body);

        return new FunctionDeclaration(functionDeclaration.Name, functionDeclaration.Parameters, body, functionDeclaration.StorageClass);
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
        _ => blockItem
    };

    private Statement ResolveStatement(Statement statement) => statement switch
    {
        Label label => ResolveLabel(label),
        Goto @goto => ResolveGoto(@goto),
        If @if => ResolveIf(@if),
        Compound compound => ResolveCompound(compound),
        While @while => ResolveWhile(@while),
        DoWhile doWhile => ResolveDoWhile(doWhile),
        For @for => ResolveFor(@for),
        Switch @switch => ResolveSwitch(@switch),
        Case @case => ResolveCase(@case),
        Default @default => ResolveDefault(@default),
        _ => statement
    };

    private Label ResolveLabel(Label label)
    {
        var name = ResolveLabelName(label.Name);

        if (!labels.Add(name))
        {
            throw new Exception($"Duplicate label {name} declaration!");
        }

        var statement = ResolveStatement(label.Statement);

        return new Label(name, statement);
    }

    private Goto ResolveGoto(Goto @goto)
    {
        var target = ResolveLabelName(@goto.Target);

        gotos.Add(target);

        return new Goto(target);
    }

    private If ResolveIf(If @if)
    {
        var then = ResolveStatement(@if.Then);
        var @else = @if.Else is null ? null : ResolveStatement(@if.Else);

        return new If(@if.Condition, then, @else);
    }

    private Compound ResolveCompound(Compound compound)
    {
        var block = ResolveBlock(compound.Block);

        return new Compound(block);
    }

    private While ResolveWhile(While @while)
    {
        var body = ResolveStatement(@while.Body);

        return new While(@while.Condition, body);
    }

    private DoWhile ResolveDoWhile(DoWhile doWhile)
    {
        var body = ResolveStatement(doWhile.Body);

        return new DoWhile(body, doWhile.Condition);
    }

    private For ResolveFor(For @for)
    {
        var body = ResolveStatement(@for.Body);

        return new For(@for.Init, @for.Condition, @for.Post, body);
    }

    private Switch ResolveSwitch(Switch @switch)
    {
        var body = ResolveStatement(@switch.Body);

        return new Switch(@switch.Condition, body);
    }

    private Case ResolveCase(Case @case)
    {
        var statement = ResolveStatement(@case.Statement);

        return new Case(@case.Condition, statement);
    }

    private Default ResolveDefault(Default @default)
    {
        var statement = ResolveStatement(@default.Statement);

        return new Default(statement);
    }

    private string ResolveLabelName(string name) => $"{CurrentFunction}.{name}";
}
