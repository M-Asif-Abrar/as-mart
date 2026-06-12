namespace AsMart.Web.Services.Email
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;     // SMTP host
        public int Port { get; set; }                        // e.g. 587
        public bool EnableSsl { get; set; }                  // true/false
        public string UserName { get; set; } = string.Empty; // SMTP username
        public string Password { get; set; } = string.Empty; // SMTP/app password
        public string From { get; set; } = string.Empty;     // no-reply@as-mart.com
        public string FromName { get; set; } = "as-mart";    // display name
    }
}
