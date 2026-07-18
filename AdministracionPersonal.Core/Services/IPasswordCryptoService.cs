namespace AdministracionPersonal.Core.Services;

/// <summary>
/// Verificación de contraseñas replicando el esquema AES-GCM del Alcance 1.
/// Formato almacenado en <c>usuario.password_hash</c>: <c>AESGCM:iv:tag:cipherdata</c>
/// (cada parte en base64; iv de 12 bytes, tag de 16 bytes, AES-256).
/// Responsable: Kendall (Core4, Core5).
/// </summary>
public interface IPasswordCryptoService
{
    /// <summary>
    /// Indica si <paramref name="passwordPlano"/> corresponde al valor cifrado
    /// <paramref name="passwordHash"/> almacenado. Ante cualquier error retorna <c>false</c>.
    /// </summary>
    bool Verificar(string passwordPlano, string passwordHash);
}
