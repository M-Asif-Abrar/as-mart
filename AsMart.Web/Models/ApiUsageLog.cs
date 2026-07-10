using AsMart.Web.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace AsMart.Web.Models
{
    public class ApiUsageLog
    {
        public long Id { get; set; }

        public int? ApiClientId { get; set; }
        public ApiClient? ApiClient { get; set; }

        [MaxLength(128)]
        public string? UserId { get; set; }

        [MaxLength(20)]
        public string HttpMethod { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Endpoint { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? QueryString { get; set; }

        public int StatusCode { get; set; }

        public long ResponseTimeMs { get; set; }

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}