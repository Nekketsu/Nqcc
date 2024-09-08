namespace Nqcc.Compiling;

public static class UniqueId
{
    private static int temporaryCounter = 0;
    private static int labelCounter = 0;

    public static string MakeTemporary() => $"tmp.{temporaryCounter++}";

    public static string MakeLabel(string prefix) => $"{prefix}.{labelCounter++}";
}
