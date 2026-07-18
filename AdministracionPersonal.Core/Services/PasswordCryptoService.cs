using System.Security.Cryptography;
using System.Text;

namespace AdministracionPersonal.Core.Services;

public class PasswordCryptoService : IPasswordCryptoService
{
    private const string Prefijo = "AESGCM";
    private const int TamanoNonce = 12; // bytes
    private const int TamanoTag = 16;   // bytes

    private readonly byte[] _clave;

    public PasswordCryptoService(IConfiguration configuration)
    {
        var claveConfig = configuration["Security:AesKey"]
            ?? throw new InvalidOperationException(
                "Security:AesKey no configurado. Debe ser la misma clave que Encryption:Key del Alcance 1.");

        if (claveConfig.Length != 32)
            throw new InvalidOperationException(
                $"Security:AesKey mide {claveConfig.Length} caracteres; debe medir exactamente 32 " +
                "(la misma clave de 32 caracteres que Encryption:Key en el Alcance 1).");

        _clave = Encoding.UTF8.GetBytes(claveConfig);
    }

    public bool Verificar(string passwordPlano, string passwordHash)
    {
        if (string.IsNullOrEmpty(passwordPlano) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        try
        {
            var partes = passwordHash.Split(':');
            if (partes.Length != 4 || partes[0] != Prefijo)
                return false;

            var nonce = Convert.FromBase64String(partes[1]);
            var tag = Convert.FromBase64String(partes[2]);
            var cipher = Convert.FromBase64String(partes[3]);

            if (nonce.Length != TamanoNonce || tag.Length != TamanoTag)
                return false;

            var plano = new byte[cipher.Length];
            using var aes = new AesGcm(_clave, TamanoTag);
            aes.Decrypt(nonce, cipher, tag, plano);

            var esperado = Encoding.UTF8.GetBytes(passwordPlano);
            return CryptographicOperations.FixedTimeEquals(plano, esperado);
        }
        catch
        {
            return false;
        }
    }
}