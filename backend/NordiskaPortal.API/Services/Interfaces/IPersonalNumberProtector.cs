namespace NordiskaPortal.API.Services.Interfaces;

public interface IPersonalNumberProtector
{
    bool IsKeyAvailable { get; }
    void GenerateNewKey();
    string Protect(string personalNumber);
    string Unprotect(string stored);
    string ComputeHash(string personalNumber);
    bool IsProtected(string stored);
}