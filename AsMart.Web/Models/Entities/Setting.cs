// Models/Entities/Setting.cs
namespace AsMart.Web.Models.Entities
{
    public class Setting
    {
        public int Id { get; set; }

        public string Key { get; set; } = null!;
        public string? Value { get; set; }
    }
}
