using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace NordiskaPortal.API.Services.BankId;

public static class BankIdQrCode
{
    public static string Create(string qrStartToken, string qrStartSecret, int secondsSinceStart)
    {
        var time = secondsSinceStart.ToString(CultureInfo.InvariantCulture);
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(qrStartSecret), Encoding.UTF8.GetBytes(time));

        return $"bankid.{qrStartToken}.{time}.{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}