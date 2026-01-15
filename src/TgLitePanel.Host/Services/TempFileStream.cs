namespace TgLitePanel.Host.Services;

internal sealed class TempFileStream : FileStream
{
    private readonly string _path;

    public TempFileStream(string path)
        : base(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose)
    {
        _path = path;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
        catch
        {
        }
    }
}

