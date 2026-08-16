namespace TrayShot.Models;

public record Screenshot(string Path, DateTime Created, long Size)
{
    public string Name => System.IO.Path.GetFileName(Path);
    public Uri Uri => new(Path);
}
