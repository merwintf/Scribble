using System.IO;
using System.Reflection;
using System.Runtime.Loader;

public sealed class IsolatedLoadContext : AssemblyLoadContext
{
    private readonly string _baseDir;

    public IsolatedLoadContext(string baseDir)
        : base(isCollectible: true) => _baseDir = baseDir;

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var candidate = Path.Combine(_baseDir, assemblyName.Name + ".dll");
        return File.Exists(candidate) ? LoadFromAssemblyPath(candidate) : null;
    }
}
