namespace AdministracionPersonal.Core.Services;

/// <summary>
/// Cifra y verifica contraseñas usando el mismo esquema AES-GCM que el Alcance 1
/// (formato de columna usuario.password_hash: "AESGCM:iv:tag:cipherdata").
/// </summary>
public interface IPasswordEncryptionService
{
    /// <summary>Cifra una contraseña en texto plano y devuelve el hash en formato "AESGCM:iv:tag:cipherdata".</summary>
    string Encrypt(string plainText);

    /// <summary>Verifica si una contraseña en texto plano corresponde al hash almacenado.</summary>
    bool Verify(string plainText, string storedHash);
}
