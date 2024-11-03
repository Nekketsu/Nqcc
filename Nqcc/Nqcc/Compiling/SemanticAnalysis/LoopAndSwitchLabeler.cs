using Nqcc.Ast;
using Nqcc.Ast.Declarations;
using Nqcc.Ast.Statements;
using System.Collections.Immutable;

namespace Nqcc.Compiling.SemanticAnalysis;

public class LoopAndSwitchLabeler(Program ast)
{
    private readonly Stack<string> labels = new();
    private string? CurrentLabel => labels.TryPeek(out var currentLabel) ? currentLabel : null;

    public Program Label() => LabelProgram(ast);

    private Program LabelProgram(Program program)
    {
        var builder = ImmutableArray.CreateBuilder<Declaration>();

        foreach (var declaration in program.Declarations)
        {
            switch (declaration)
            {
                case FunctionDeclaration functionDeclaration:
                    builder.Add(LabelFunctionDeclaration(functionDeclaration));
                    break;
                case VariableDeclaration variableDeclaration:
                    builder.Add(declaration);
                    break;
            }
        }

        return new Program(builder.ToImmutable());
    }

    private FunctionDeclaration LabelFunctionDeclaration(FunctionDeclaration functionDeclaration)
    {
        var body = functionDeclaration.Body is null ? null : LabelBlock(functionDeclaration.Body);

        return new FunctionDeclaration(functionDeclaration.Name, functionDeclaration.Parameters, body, functionDeclaration.StorageClass);
    }

    private Block LabelBlock(Block block)
    {
        var builder = ImmutableArray.CreateBuilder<BlockItem>();

        foreach (var blockItem in block.BlockItems)
        {
            builder.Add(LabelBlockItem(blockItem));
        }

        return new Block(builder.ToImmutable());
    }

    private BlockItem LabelBlockItem(BlockItem blockItem) => blockItem switch
    {
        Ast.BlockItems.Statement statement => new Ast.BlockItems.Statement(LabelStatement(statement.InnerStatement)),
        _ => blockItem
    };

    private Statement LabelStatement(Statement statement) => statement switch
    {
        Break => LabelBreak(),
        Continue => LabelContinue(),
        While @while => LabelWhile(@while),
        DoWhile doWhile => LabelDoWhile(doWhile),
        For @for => LabelFor(@for),
        Compound compound => LabelCompound(compound),
        If @if => LabelIf(@if),
        Switch @switch => LabelSwitch(@switch),
        Case @case => LabelCase(@case),
        Default @default => LabelDefault(@default),
        Label label => LabelLabel(label),
        _ => statement
    };

    private Break LabelBreak() => CurrentLabel switch
    {
        string => new Break(CurrentLabel),
        _ => throw new Exception("Break outside of loop")
    };

    private Continue LabelContinue()
    {
        switch (CurrentLabel)
        {
            case string:
                foreach (var label in labels)
                {
                    if (!label.StartsWith("switch"))
                    {
                        return new Continue(label);
                    }
                }
                throw new Exception("Continue cannot be inside of switch");
            default:
                throw new Exception("Continue outside of loop");
        }
    }

    private While LabelWhile(While @while)
    {
        var label = UniqueId.MakeLabel("while");

        labels.Push(label);
        var body = LabelStatement(@while.Body);
        labels.Pop();

        return new While(@while.Condition, body, label);
    }

    private DoWhile LabelDoWhile(DoWhile doWhile)
    {
        var label = UniqueId.MakeLabel("do_while");

        labels.Push(label);
        var body = LabelStatement(doWhile.Body);
        labels.Pop();

        return new DoWhile(body, doWhile.Condition, label);
    }

    private For LabelFor(For @for)
    {
        var label = UniqueId.MakeLabel("for");

        labels.Push(label);
        var body = LabelStatement(@for.Body);
        labels.Pop();

        return new For(@for.Init, @for.Condition, @for.Post, body, label);
    }

    private Compound LabelCompound(Compound compound)
    {
        var block = LabelBlock(compound.Block);

        return new Compound(block);
    }

    private If LabelIf(If @if)
    {
        var then = LabelStatement(@if.Then);
        var @else = @if.Else is not null ? LabelStatement(@if.Else) : null;

        return new If(@if.Condition, then, @else);
    }

    private Switch LabelSwitch(Switch @switch)
    {
        var label = UniqueId.MakeLabel("switch");

        labels.Push(label);
        var body = LabelStatement(@switch.Body);
        labels.Pop();

        return new Switch(@switch.Condition, body, label);
    }

    private Case LabelCase(Case @case)
    {
        var label = UniqueId.MakeLabel($"case.{CurrentLabel}");
        var statement = LabelStatement(@case.Statement);

        return new Case(@case.Condition, statement, label);
    }

    private Default LabelDefault(Default @default)
    {
        var label = UniqueId.MakeLabel($"default.{CurrentLabel}");
        var statement = LabelStatement(@default.Statement);

        return new Default(statement, label);
    }

    private Label LabelLabel(Label label)
    {
        var statement = LabelStatement(label.Statement);

        return new Label(label.Name, statement);
    }
}
