using Nqcc.Platforms.OSX.Compiling;

namespace Nqcc.Platforms.OSX;

public class Driver(Settings settings) : Nqcc.Driver(settings)
{
    protected override Nqcc.Compiling.ICompiler GetCompiler() => new Compiler();
}
