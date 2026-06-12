namespace AsMart.Web.Models.Entities.Marketing
{
    public enum MarketingPlatform
    {
        Facebook = 1,
        Instagram = 2,
        Pinterest = 3,
        Telegram = 4
    }

    public enum MarketingTargetType
    {
        FacebookGroup = 1,
        FacebookPage = 2,
        InstagramBusiness = 3,
        PinterestBoard = 4,
        TelegramChannel = 5
    }

    public enum MarketingCampaignSourceType
    {
        Custom = 0,
        Product = 1,
        BlogPost = 2
    }

    public enum MarketingCampaignStatus
    {
        Draft = 0,
        Ready = 1,
        Scheduled = 2,
        Running = 3,
        Completed = 4,
        Paused = 5,
        Failed = 6
    }

    public enum MarketingQueueStatus
    {
        Pending = 0,
        Scheduled = 1,
        Processing = 2,
        Posted = 3,
        Failed = 4,
        Skipped = 5,
        Cancelled = 6
    }

    public enum MarketingPublishMode
    {
        Manual = 0,
        Api = 1,
        BrowserAutomation = 2
    }
}