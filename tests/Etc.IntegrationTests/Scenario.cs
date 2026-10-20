using Etc.Desktop.Features.Accounts;
using Etc.Desktop.Features.Profiles;
using Etc.Desktop.Features.Recruitments;
using Etc.Desktop.Shared;
namespace Etc.IntegrationTests;
public sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now {get;set;}=new(2026,10,19,2,0,0,TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow()=>Now;
}
public sealed class Scenario(DatabaseFixture db)
{
    public SessionState Session {get;}=new();
    public TestClock Clock {get;}=new();
    public StudentIdentity A {get;private set;}=null!;
    public StudentIdentity B {get;private set;}=null!;
    public StudentIdentity C {get;private set;}=null!;
    public int SkillId {get;private set;}
    public RecruitmentService Recruitments=>new(db.Factory,Session,Clock);
    public async Task InitializeAsync()
    {
        await db.ResetAsync();var accounts=new AccountService(db.Factory,Session);
        A=await accounts.RegisterAsync("Raka","a@example.test","Etc-Test-Pass!2026");
        B=await accounts.RegisterAsync("Nadia","b@example.test","Etc-Test-Pass!2026");
        C=await accounts.RegisterAsync("Dimas","c@example.test","Etc-Test-Pass!2026");
        SkillId=Convert.ToInt32(await db.ScalarAsync("SELECT MIN(Id) FROM Skills"));
        foreach(var student in new[]{A,B,C})
        {
            Session.SignIn(student);
            await new ProfileService(db.Factory,Session,Clock).SaveMineAsync(new(student.FullName,2024,"","",null,null,[SkillId],null));
        }
        Session.SignIn(A);
    }
    public RecruitmentInput Input(string title="Proyek demonstrasi",int capacity=1)=>new(title,RecruitmentCategory.Project,title,"Deskripsi kegiatan demonstrasi",Clock.Now.AddDays(10),null,null,ActivityMode.Online,null,FeeKind.Free,0,null,null,"Raka","Kontak demonstrasi",[new(null,"Backend",capacity,[SkillId])]);
    public async Task<int> OpenAsync(int capacity=1)
    {
        var id=await Recruitments.SaveDraftAsync(null,Input(capacity:capacity));await Recruitments.PublishAsync(id);return id;
    }
}
