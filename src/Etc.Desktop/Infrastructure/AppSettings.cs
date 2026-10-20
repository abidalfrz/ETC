namespace Etc.Desktop.Infrastructure;
public sealed record AppSettings(string ConnectionString,string? MentorFormUrl)
{
    public static AppSettings Load()=>new(Environment.GetEnvironmentVariable("ETC_CONNECTION_STRING")??"",Environment.GetEnvironmentVariable("ETC_MENTOR_FORM_URL")??Demo.MentorSeedData.FormUrl);
}
