namespace AsMart.Web.Models.Security
{
    public sealed record ApiKeyMaterial(
        string RawKey,
        string Hash,
        string Prefix);
}