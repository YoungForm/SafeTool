using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SafeTool.Application.Services;

/// <summary>
/// 电子签名服务（策略模式）
/// </summary>
public class ElectronicSignatureService
{
    private readonly string _signatureDir;
    private readonly AuditSignatureService _auditSignature;

    public ElectronicSignatureService(string dataDir, AuditSignatureService auditSignature)
    {
        _signatureDir = Path.Combine(dataDir, "ElectronicSignatures");
        Directory.CreateDirectory(_signatureDir);
        _auditSignature = auditSignature;
    }

    /// <summary>
    /// 创建电子签名
    /// </summary>
    public ElectronicSignature CreateSignature(
        string documentId,
        string documentType,
        string signer,
        string signerRole,
        string? comment = null)
    {
        var signature = new ElectronicSignature
        {
            Id = Guid.NewGuid().ToString("N"),
            DocumentId = documentId,
            DocumentType = documentType,
            Signer = signer,
            SignerRole = signerRole,
            Comment = comment,
            SignedAt = DateTime.UtcNow,
            SignatureData = GenerateSignatureData(documentId, documentType, signer, signerRole, comment)
        };

        // 使用审计签名服务对签名进行数字签名
        var auditEvent = new AuditEvent
        {
            Timestamp = signature.SignedAt,
            User = signer,
            Action = "sign",
            Resource = documentType,
            Detail = $"签署文档 {documentId}，角色：{signerRole}"
        };
        signature.DigitalSignature = _auditSignature.SignAuditEvent(auditEvent);

        // 保存签名
        SaveSignature(signature);

        return signature;
    }

    /// <summary>
    /// 验证电子签名
    /// </summary>
    public SignatureVerificationResult VerifySignature(string signatureId)
    {
        var signature = GetSignature(signatureId);
        if (signature == null)
        {
            return new SignatureVerificationResult
            {
                IsValid = false,
                Errors = new List<string> { "签名不存在" }
            };
        }

        var result = new SignatureVerificationResult
        {
            SignatureId = signatureId,
            Signer = signature.Signer,
            SignedAt = signature.SignedAt,
            IsValid = true,
            Checks = new List<SignatureCheck>()
        };

        // 验证数字签名
        var auditEvent = new AuditEvent
        {
            Timestamp = signature.SignedAt,
            User = signature.Signer,
            Action = "sign",
            Resource = signature.DocumentType,
            Detail = $"签署文档 {signature.DocumentId}，角色：{signature.SignerRole}"
        };

        var isSignatureValid = _auditSignature.VerifyAuditEvent(auditEvent, signature.DigitalSignature ?? "");
        result.Checks.Add(new SignatureCheck
        {
            CheckType = "DigitalSignature",
            Passed = isSignatureValid,
            Message = isSignatureValid ? "数字签名验证通过" : "数字签名验证失败"
        });

        // 验证签名数据完整性
        var expectedData = GenerateSignatureData(signature.DocumentId, signature.DocumentType, 
            signature.Signer, signature.SignerRole, signature.Comment);
        var isDataValid = signature.SignatureData == expectedData;
        result.Checks.Add(new SignatureCheck
        {
            CheckType = "DataIntegrity",
            Passed = isDataValid,
            Message = isDataValid ? "签名数据完整性验证通过" : "签名数据已被篡改"
        });

        result.IsValid = result.Checks.All(c => c.Passed);
        result.Errors = result.Checks.Where(c => !c.Passed).Select(c => c.Message).ToList();

        return result;
    }

    /// <summary>
    /// 获取文档的所有签名
    /// </summary>
    public IEnumerable<ElectronicSignature> GetDocumentSignatures(string documentId)
    {
        var filePath = Path.Combine(_signatureDir, $"{documentId}_signatures.json");
        if (!File.Exists(filePath))
            return Enumerable.Empty<ElectronicSignature>();

        try
        {
            var json = File.ReadAllText(filePath);
            var signatures = JsonSerializer.Deserialize<List<ElectronicSignature>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return signatures ?? Enumerable.Empty<ElectronicSignature>();
        }
        catch
        {
            return Enumerable.Empty<ElectronicSignature>();
        }
    }

    private string GenerateSignatureData(string documentId, string documentType, string signer, string signerRole, string? comment)
    {
        var data = new
        {
            documentId,
            documentType,
            signer,
            signerRole,
            comment,
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private void SaveSignature(ElectronicSignature signature)
    {
        var filePath = Path.Combine(_signatureDir, $"{signature.DocumentId}_signatures.json");
        var existing = GetDocumentSignatures(signature.DocumentId).ToList();
        existing.Add(signature);
        
        var json = JsonSerializer.Serialize(existing, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    private ElectronicSignature? GetSignature(string signatureId)
    {
        var files = Directory.GetFiles(_signatureDir, "*_signatures.json");
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var signatures = JsonSerializer.Deserialize<List<ElectronicSignature>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var signature = signatures?.FirstOrDefault(s => s.Id == signatureId);
                if (signature != null)
                    return signature;
            }
            catch { }
        }
        return null;
    }
}

public class ElectronicSignature
{
    public string Id { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty; // SRS/ChangeRequest/Report
    public string Signer { get; set; } = string.Empty;
    public string SignerRole { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime SignedAt { get; set; }
    public string SignatureData { get; set; } = string.Empty; // 签名数据哈希
    public string? DigitalSignature { get; set; } // RSA数字签名
}

public class SignatureVerificationResult
{
    public string? SignatureId { get; set; }
    public string? Signer { get; set; }
    public DateTime? SignedAt { get; set; }
    public bool IsValid { get; set; }
    public List<SignatureCheck> Checks { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class SignatureCheck
{
    public string CheckType { get; set; } = string.Empty; // DigitalSignature/DataIntegrity
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
}

