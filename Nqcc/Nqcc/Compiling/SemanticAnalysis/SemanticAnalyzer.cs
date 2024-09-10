using Nqcc.Ast;

namespace Nqcc.Compiling.SemanticAnalysis;

public class SemanticAnalyzer(Program ast)
{
    public Program Analyze()
    {
        var variableResolver = new VariableResolver(ast);
        var resolvedProgram = variableResolver.Resolve();

        var labelAnalyzer = new LabelAnalyzer(ast);
        labelAnalyzer.Analyze();

        return resolvedProgram;
    }
}
