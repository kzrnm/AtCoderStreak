namespace AtCoderStreak.Model.Entities
{
#nullable disable
    public class Setting
    {
        public int Id { get; set; }
        public string Data { get; set; }

        public const int SessionId = 1;
        public static Setting Session(string cookie) => new Setting() { Id = 1, Data = cookie };
    }
#nullable restore
}
