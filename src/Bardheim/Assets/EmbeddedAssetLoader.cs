using System.IO;
using System.Reflection;

namespace Bardheim.Assets;

public static class EmbeddedAssetLoader
{
    public static byte[]? ReadResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
