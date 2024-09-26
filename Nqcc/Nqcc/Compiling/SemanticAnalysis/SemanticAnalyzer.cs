using Nqcc.Ast;
using Nqcc.Symbols;

namespace Nqcc.Compiling.SemanticAnalysis;

public class SemanticAnalyzer(SymbolTable symbols, Program ast)
{
    public Program Analyze()
    {
        var identifierResolver = new IdentifierResolver(ast);
        var resolvedAst = identifierResolver.Resolve();

        var typeChecker = new TypeChecker(symbols, resolvedAst);
        typeChecker.Check();

        var labelResolver = new LabelResolver(resolvedAst);
        var labeledResolved = labelResolver.Resolve();

        var loopAndSwitchLabeler = new LoopAndSwitchLabeler(labeledResolved);
        var labeledAst = loopAndSwitchLabeler.Label();

        var switchResolver = new SwitchResolver(labeledAst);
        var switchResolved = switchResolver.Resolve();

        return switchResolved;
    }
}
