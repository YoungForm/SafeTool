using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 审计事件签名服务（防篡改）
/// </summary>
public class AuditSignatureService
{
    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;
    private readonly object _lock = new();

    public AuditSignatureService(string dataDir)
    {
        var dir = Path.Combine(dataDir, "AuditKeys");
        Directory.CreateDirectory(dir);
        _privateKeyPath = Path.Combine(dir, "private.key");
        _publicKeyPath = Path.Combine(dir, "public.key");
        EnsureKeysExist();
    }

    private void EnsureKeysExist()
    {
        if (!File.Exists(_privateKeyPath) || !File.Exists(_publicKeyPath))
        {
            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportRSAPrivateKey();
            var publicKey = rsa.ExportRSAPublicKey();
            
            lock (_lock)
            {
                File.WriteAllBytes(_privateKeyPath, privateKey);
                File.WriteAllBytes(_publicKeyPath, publicKey);
            }
        }
    }

    /// <summary>
    /// 对审计事件进行签名
    /// </summary>
    public string SignAuditEvent(AuditEvent auditEvent)
    {
        var json = JsonSerializer.Serialize(auditEvent, new JsonSerializerOptions { WriteIndented = false });
        var data = Encoding.UTF8.GetBytes(json);
        
        using var rsa = RSA.Create();
        var privateKey = File.ReadAllBytes(_privateKeyPath);
        rsa.ImportRSAPrivateKey(privateKey, out _);
        
        var signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    /// <summary>
    /// 验证审计事件签名
    /// </summary>
    public bool VerifyAuditEvent(AuditEvent auditEvent, string signature)
    {
        try
        {
            var json = JsonSerializer.Serialize(auditEvent, new JsonSerializerOptions { WriteIndented = false });
            var data = Encoding.UTF8.GetBytes(json);
            var signatureBytes = Convert.FromBase64String(signature);
            
            using var rsa = RSA.Create();
            var publicKey = File.ReadAllBytes(_publicKeyPath);
            rsa.ImportRSAPublicKey(publicKey, out _);
            
            return rsa.VerifyData(data, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取公钥（用于验证）
    /// </summary>
    public string GetPublicKey()
    {
        var publicKey = File.ReadAllBytes(_publicKeyPath);
        return Convert.ToBase64String(publicKey);
    }
}

public class AuditEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Timestamp { get; set; }
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string? Signature { get; set; }
}

