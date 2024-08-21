using System.Reflection;

namespace Nqcc.Ast;

public abstract class SyntaxNode()
{
    public void WriteTo(TextWriter writer)
    {
        PrettyPrint(writer, this);
    }

    private IEnumerable<KeyValuePair<string, object>> Children
    {
        get
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var child = property.GetValue(this);

                if (child is not null)
                {
                    if (child is IEnumerable<SyntaxNode> collection)
                    {
                        foreach (var item in collection)
                        {
                            if (item is not null)
                            {
                                yield return KeyValuePair.Create(item.GetType().Name, (object)item);
                            }
                        }
                    }
                    else
                    {
                        yield return KeyValuePair.Create(property.Name, child);
                    }
                }
            }
        }
    }

    private static void PrettyPrint(TextWriter writer, object node, string indent = "", bool isLast = true, string? name = null)
    {
        name ??= node.GetType().Name;

        var marker = isLast ? "└──" : "├──";

        writer.Write(indent);
        writer.Write(marker);
        writer.Write(name);

        if (node is not SyntaxNode syntaxNode)
        {
            writer.Write(" ");
            writer.Write(node);
            writer.WriteLine();
            return;
        }

        writer.WriteLine();

        indent += isLast ? "   " : "│  ";

        var children = syntaxNode.Children.ToArray();
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var isLastChild = i == children.Length - 1;

            if (child.Value is SyntaxNode childNode)
            {
                PrettyPrint(writer, childNode, indent, isLastChild);
            }
            else
            {
                PrettyPrint(writer, child.Value, indent, isLastChild, child.Key);
            }
        }
    }

    public override string ToString()
    {
        using var writer = new StringWriter();
        WriteTo(writer);

        return writer.ToString();
    }
}
