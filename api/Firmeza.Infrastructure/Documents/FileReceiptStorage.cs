using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Microsoft.Extensions.Configuration;
namespace Firmeza.Infrastructure.Documents;

public sealed class FileReceiptStorage(IConfiguration config) : IReceiptStorage
{
    private string DirectoryPath => Path.GetFullPath(config["Receipts:Directory"] ?? "storage/receipts");
    public async Task<string> SaveAsync(Guid saleId, byte[] contents, CancellationToken ct)
    {
        var directory = DirectoryPath;
        Directory.CreateDirectory(directory);
        var key = saleId.ToString("N") + ".pdf";
        var target = Path.Combine(directory, key);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, contents, ct);
            File.Move(temporary, target, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return key;
    }
    public Task<Stream> OpenReadAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (key.Length != 36 || !key.EndsWith(".pdf", StringComparison.Ordinal) || !Guid.TryParseExact(key[..32], "N", out _))
            throw RequestException.NotFound();
        var file = Path.Combine(DirectoryPath, key);
        if (!File.Exists(file))
            throw RequestException.NotFound();
        return Task.FromResult<Stream>(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read));
    }
}
