using Nqcc.Backend;
using Nqcc.Tacky;

namespace Nqcc.Platforms.Windows.Compiling;

public class Compiler : Nqcc.Compiling.Compiler
{
    protected override string GetAssemblyFile(string preprocessedFile) => Path.ChangeExtension(preprocessedFile, ".asm");

    protected override Nqcc.Compiling.ICodeEmitter GetCodeEmitter(TextWriter writer) => new CodeEmitter(writer);

    protected override AssemblyGenerator GetAssemblyGenerator(Program tacky) => new Backend.AssemblyGenerator(symbols, tacky);
}
