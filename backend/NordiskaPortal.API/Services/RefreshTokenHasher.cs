using System.Text;
using System.Security.Cryptography;

namespace NordiskaPortal.API.Services;

public static class RefreshTokenHasher
{
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}