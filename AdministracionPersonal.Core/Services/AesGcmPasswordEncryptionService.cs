using System.Security.Cryptography;
using System.Text;

namespace AdministracionPersonal.Core.Services;

/// <summary>
/// Implementación AES-256-GCM del esquema de cifrado de contraseñas del Alcance 1.
/// Formato: "AESGCM:iv:tag:cipherdata", cada parte en Base64 y separadas por ':'.
///
/// IMPORTANTE: para poder verificar los usuarios que ya existen en la base de datos
/// (creados por el Alcance 1), la clave configurada en "Auth:PasswordEncryptionKey"
/// debe ser EXACTAMENTE la misma que usó el Alcance 1 al cifrar esas contraseñas.
/// Sin esa clave compartida no es posible descifrar/verificar los hashes existentes;
/// solo se podrán verificar contraseñas cifradas por esta misma instancia del servicio.
/// </summary>
public class AesGcmPasswordEncryptionService : IPasswordEncryptionService
{
    private const string Prefijo = "AESGCM";
    private const int TamanoNonce = 12;
    private const int TamanoTag = 16;

    private readonly byte[] _clave;

    public AesGcmPasswordEncryptionService(IConfiguration configuration)
    {
        var claveConfigurada = configuration["Auth:PasswordEncryptionKey"]
            ?? throw new InvalidOperationException("Auth:PasswordEncryptionKey no configurado en appsettings.");

        // Se normaliza a 32 bytes (AES-256) vía SHA-256, sin importar la longitud del texto configurado.
        _clave = SHA256.HashData(Encoding.UTF8.GetBytes(claveConfigurada));
    }

    public string Encrypt(string plainText)
    {
        var nonce = RandomNumberGenerator.GetBytes(TamanoNonce);
        var textoPlano = Encoding.UTF8.GetBytes(plainText);
        var textoCifrado = new byte[textoPlano.Length];
        var tag = new byte[TamanoTag];

        using var aesGcm = new AesGcm(_clave, TamanoTag);
        aesGcm.Encrypt(nonce, textoPlano, textoCifrado, tag);

        return $"{Prefijo}:{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(textoCifrado)}";
    }

    public bool Verify(string plainText, string storedHash)
    {
        try
        {
            var partes = storedHash.Split(':');
            if (partes.Length != 4 || partes[0] != Prefijo)
            {
                return false;
            }

            var nonce = Convert.FromBase64String(partes[1]);
            var tag = Convert.FromBase64String(partes[2]);
            var textoCifrado = Convert.FromBase64String(partes[3]);
            var textoPlano = new byte[textoCifrado.Length];

            using var aesGcm = new AesGcm(_clave, TamanoTag);
            aesGcm.Decrypt(nonce, textoCifrado, tag, textoPlano);

            var textoDescifrado = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(textoPlano));
            var textoIngresado = Encoding.UTF8.GetBytes(plainText);

            return CryptographicOperations.FixedTimeEquals(textoDescifrado, textoIngresado);
        }
        catch (CryptographicException)
        {
            // El tag de autenticación no coincide: contraseña incorrecta o hash corrupto.
            return false;
        }
        catch (FormatException)
        {
            // El hash almacenado no respeta el formato "AESGCM:iv:tag:cipherdata".
            return false;
        }
    }
}
