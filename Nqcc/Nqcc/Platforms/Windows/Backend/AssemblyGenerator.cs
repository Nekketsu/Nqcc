using Nqcc.Assembly.Operands.Registers;
using Nqcc.Symbols;
using Nqcc.Tacky;

namespace Nqcc.Platforms.Windows.Backend;

public class AssemblyGenerator(SymbolTable symbols, Program tacky) : Nqcc.Backend.AssemblyGenerator(symbols, tacky)
{
    protected override Nqcc.Backend.TackyToAssemblyConverter GetTackyToAssemblyConverter(Program tacky)
    {
        return new Nqcc.Backend.TackyToAssemblyConverter(tacky, [new CX(), new DX(), new R8(), new R9()]);
    }
}
