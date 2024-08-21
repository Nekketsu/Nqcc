using Nqcc.Platforms.Linux.Compiling;

namespace Nqcc.Platforms.Linux;

public class Driver(Settings settings) : Nqcc.Driver(settings)
{
    protected override Nqcc.Compiling.ICompiler GetCompiler() => new Compiler();
}
