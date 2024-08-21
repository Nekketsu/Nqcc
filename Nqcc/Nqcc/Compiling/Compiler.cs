using Nqcc.Backend;
using System.Collections.Immutable;

namespace Nqcc.Compiling;

public abstract class Compiler : ICompiler
{
    public async Task<string> CompileAsync(string preprocessedFile, Stage stage = Stage.Codegen)
    {
        var assemblyFile = GetAssemblyFile(preprocessedFile);

        if (stage < Stage.Lex) { return assemblyFile; }

        var code = await File.ReadAllTextAsync(preprocessedFile);
        var lexer = new Lexer(code);
        var tokens = lexer.ToImmutableArray();

        if (stage < Stage.Parse) { return assemblyFile; }

        var parser = new Parser(tokens);
        var ast = parser.Parse();

        if (stage < Stage.Codegen) { return assemblyFile; }

        var assemblyGenerator = new AssemblyGenerator(ast);
        var assembly = assemblyGenerator.Convert();

        if (stage < Stage.Assembly) { return assemblyFile; }

        using var streamWriter = new StreamWriter(assemblyFile);
        var emitter = GetCodeEmitter(streamWriter);
        emitter.Emit(assembly);

        return assemblyFile;
    }

    protected virtual string GetAssemblyFile(string preprocessedFile) => Path.ChangeExtension(preprocessedFile, ".s");

    protected abstract ICodeEmitter GetCodeEmitter(TextWriter writer);
}
