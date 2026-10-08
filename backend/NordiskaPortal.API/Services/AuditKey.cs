namespace NordiskaPortal.API.Services
{
    // The secret that signs the audit chain. It is kept outside the database (configuration or /secrets/audit.key), so someone who can only edit the database cannot compute valid hashes.
    public sealed class AuditKey
    {
        public AuditKey(byte[] bytes)
        {
            if (bytes.Length < 32)
            {
                throw new InvalidOperationException("The audit key must be at least 32 bytes (base64).");
            }

            Bytes = bytes;
        }

        public byte[] Bytes { get; }
    }
}
