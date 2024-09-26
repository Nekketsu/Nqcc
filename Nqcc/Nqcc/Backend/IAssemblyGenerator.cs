using Nqcc.Assembly;

namespace Nqcc.Backend;
public interface IAssemblyGenerator
{
    Program Generate();
}