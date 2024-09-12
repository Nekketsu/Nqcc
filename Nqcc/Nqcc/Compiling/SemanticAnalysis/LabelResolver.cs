using Nqcc.Ast;
using Nqcc.Ast.Statements;

namespace Nqcc.Compiling.SemanticAnalysis;

public class LabelAnalyzer(Program ast)
{
    private readonly HashSet<string> labels = [];
    private readonly HashSet<string> gotos = [];

    public void Analyze() => AnalyzeProgram(ast);

    private void AnalyzeProgram(Program program)
    {
        AnalyzeFunction(program.FunctionDefinition);

        foreach (var @goto in gotos)
        {
            if (!labels.Contains(@goto))
            {
                throw new Exception($"Undeclared label {@goto}!");
            }
        }
    }

    private void AnalyzeFunction(Function function)
    {
        AnalyzeBlock(function.Body);
        
    }

    private void AnalyzeBlock(Block block)
    {
        foreach (var blockItem in block.BlockItems)
        {
            AnalyzeBlockItem(blockItem);
        }
    }

    private void AnalyzeBlockItem(BlockItem blockItem)
    {
        if (blockItem is Ast.BlockItems.Statement statement)
        {
            AnalyzeStatement(statement.InnerStatement);
        }
    }

    private void AnalyzeStatement(Statement statement)
    {
        switch (statement)
        {
            case Label label:
                AnalyzeLabel(label);
                break;
            case Goto @goto:
                AnalyzeGoto(@goto);
                break;
            case If @if:
                AnalyzeIf(@if);
                break;
            case Compound compound:
                AnalyzeCompound(compound);
                break;
        }
    }

    private void AnalyzeLabel(Label label)
    {
        if (!labels.Add(label.Name))
        {
            throw new Exception($"Duplicate label {label.Name} declaration!");
        }

        AnalyzeStatement(label.Statement);
    }

    private void AnalyzeGoto(Goto @goto)
    {
        gotos.Add(@goto.Target);
    }

    private void AnalyzeIf(If @if)
    {
        AnalyzeStatement(@if.Then);
        if (@if.Else is not null)
        {
            AnalyzeStatement(@if.Else);
        }
    }

    private void AnalyzeCompound(Compound compound)
    {
        AnalyzeBlock(compound.Block);
    }
}
