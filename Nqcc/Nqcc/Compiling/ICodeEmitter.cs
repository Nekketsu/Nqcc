using Nqcc.Assembly;

namespace Nqcc.Compiling;

public interface ICodeEmitter
{
    void Emit(Program assembly);
}
