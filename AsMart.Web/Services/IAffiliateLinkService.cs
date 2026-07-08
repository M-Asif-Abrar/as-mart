// Services/IAffiliateLinkService.cs
namespace AsMart.Web.Services
{
    public interface IAffiliateLinkService
    {
        string BuildProductUrl(string? asin);
    }
}
