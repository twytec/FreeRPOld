namespace FreeRP
{
    public partial class FrpUtcDateTime
    {
        public static FrpUtcDateTime FromDateTime(DateTime dt)
        {
            var dto = (DateTimeOffset)dt.ToUniversalTime();
            return new FrpUtcDateTime() { 
                Day = dto.Day,
                Hours = dto.Hour,
                Minutes = dto.Minute,
                Seconds = dto.Second,
                Month = dto.Month,
                Year = dto.Year,
                UnixTimeSeconds = dto.ToUnixTimeSeconds()
            };
        }

        public DateTime ToDateTime()
             => new(Year, Month, Day, Hours, Minutes, Seconds, DateTimeKind.Utc);
    }
}
