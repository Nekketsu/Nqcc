using Nqcc.Ast;

namespace Nqcc.Compiling.SemanticAnalysis;

public class SemanticAnalyzer(Program ast)
{
    public Program Analyze()
    {
        var variableResolver = new VariableResolver(ast);
        var resolvedAst = variableResolver.Resolve();

        var loopAndSwitchLabeler = new LoopAndSwitchLabeler(resolvedAst);
        var labeledAst = loopAndSwitchLabeler.Label();

        var switchResolver = new SwitchResolver(labeledAst);
        var switchResolved = switchResolver.Resolve();

        var labelAnalyzer = new LabelAnalyzer(switchResolved);
        labelAnalyzer.Analyze();

        return switchResolved;
    }
}
