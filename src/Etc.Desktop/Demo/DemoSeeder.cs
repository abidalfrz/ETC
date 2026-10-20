using Etc.Desktop.Infrastructure;
using Etc.Desktop.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
namespace Etc.Desktop.Demo;
public sealed class DemoSeeder(SqlConnectionFactory connections,TimeProvider clock)
{
    public const string DefaultPassword="Etc-Demo!2026";
    public static void EnsureDemoTarget(string databaseName)
    {
        if(databaseName!="ETC_Demo")throw new DomainException("Validation","Seed CLI hanya diizinkan pada database ETC_Demo.");
    }
    public async Task SeedAsync(string? demoPassword=null,CancellationToken ct=default)
    {
        demoPassword??=DefaultPassword;
        if(demoPassword.Length is <12 or >128)throw new DomainException("Validation","ETC_DEMO_PASSWORD harus 12–128 karakter.");
        var names=new[]{"Raka Pratama","Nadia Putri","Dimas Saputra"};var emails=new[]{"raka.demo@example.test","nadia.demo@example.test","dimas.demo@example.test"};
        var hasher=new PasswordHasher<StudentIdentity>();var hashes=await Task.Run(()=>names.Select((n,i)=>hasher.HashPassword(new(0,n,emails[i]),demoPassword)).ToArray(),ct);
        await using var c=await connections.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
        await using(var cmd=Db.Command(c,tx,"EXEC sp_getapplock @Resource='ETC_demo_seed',@LockMode='Exclusive',@LockOwner='Transaction';"))await cmd.ExecuteNonQueryAsync(ct);
        var students=new int[3];
        for(var i=0;i<names.Length;i++)
        {
            await using var lookup=Db.Command(c,tx,"SELECT Id,IsDemo FROM Students WHERE NormalizedEmail=@email",("email",emails[i].ToUpperInvariant()));
            bool exists;await using(var r=await lookup.ExecuteReaderAsync(ct)){exists=await r.ReadAsync(ct);if(exists){students[i]=r.GetInt32(0);if(!r.GetBoolean(1))throw new DomainException("Conflict","Email akun demo telah dipakai akun lain; tidak ada data ditimpa.");}}
            if(exists)continue;
            await using(var cmd=Db.Command(c,tx,"INSERT Students(FullName,Email,NormalizedEmail,PasswordHash,Cohort,Bio,Experience,IsDemo) OUTPUT INSERTED.Id VALUES(@name,@email,@normalized,@hash,2024,N'Data demonstrasi: mahasiswa yang mencari kolaborasi.',N'Data demonstrasi: pengembangan proyek kuliah dan prototipe tim.',1)",("name",names[i]),("email",emails[i]),("normalized",emails[i].ToUpperInvariant()),("hash",hashes[i])))students[i]=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            await using(var cmd=Db.Command(c,tx,"INSERT StudentSkills SELECT @id,Id FROM Skills WHERE Name IN(N'C#',N'SQL Server',N'UI design')",("id",students[i])))await cmd.ExecuteNonQueryAsync(ct);
        }
        var templates=new (string Title,int Owner,int Category,int Status,int Days,int Capacity,string Position, string[] Skills)[]
        {
            ("Hology 9.0 (Holomine)",0,0,1,14,2,"ML/AI",new[]{"Python"}),
            ("Aplikasi inventaris laboratorium",1,1,1,10,2,"UI/UX",new[]{"CSS", "HTML"}),
            ("Portal dokumentasi kegiatan",2,1,2,-3,1,"Frontend",new[]{"CSS", "HTML", "JavaScript"}),
            ("JOINTS UGM",0,1,0,20,2,"ML/AI",new[]{"Python"}),
            ("TC Quadrathlon (CTF)",1,0,2,-2,2,"Member",new[]{"SQL Server"}),
            ("Aplikasi katalog publik",2,1,1,-1,1,"Frontend",new[]{"CSS", "HTML", "JavaScript"}),
            ("NPC Data Science",1,0,1,12,1,"ML/AI",new[]{"Python"}),
            ("Proyek NLP Multi Label Emosi",2,1,1,18,2,"Backend",new[]{"Python", "JavaScript"})
        };
        var recs=new int[templates.Length];var positions=new int[templates.Length];var created=new HashSet<int>();var now=clock.GetUtcNow();
        for(var i=0;i<templates.Length;i++)
        {
            var t=templates[i];
            await using(var cmd=Db.Command(c,tx,"SELECT Id,IsDemo FROM Recruitments WHERE OwnerId=@owner AND Title=@title",("owner",students[t.Owner]),("title",t.Title)))
            await using(var r=await cmd.ExecuteReaderAsync(ct))if(await r.ReadAsync(ct)){recs[i]=r.GetInt32(0);if(!r.GetBoolean(1))throw new DomainException("Conflict","Judul data demo sudah dipakai posting lain; data tidak ditimpa.");}
            if(recs[i]!=0)continue;
            await using(var cmd=Db.Command(c,tx,"INSERT Recruitments(OwnerId,Title,Category,ActivityName,Description,DeadlineUtc,Mode,FeeKind,FeeAmount,ContactName,ContactChannel,Status,PublishedAtUtc,UpdatedAtUtc,IsDemo) OUTPUT INSERTED.Id VALUES(@owner,@title,@category,@title,N'Data demonstrasi. Tim mencari mahasiswa untuk berkolaborasi mengembangkan prototipe, mendokumentasikan proses, dan menyiapkan presentasi hasil. Peran dipilih sesuai skill yang dibutuhkan.',@deadline,0,1,0,@contact,N'Kontak simulasi; tidak untuk dihubungi',@status,@published,@now,1)",("owner",students[t.Owner]),("title",t.Title),("category",t.Category),("deadline",now.AddDays(t.Days)),("contact",names[t.Owner]),("status",t.Status),("published",t.Status==0?null:now.AddDays(-i-1)),("now",now)))recs[i]=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            await using(var cmd=Db.Command(c,tx,"INSERT Positions(RecruitmentId,Name,Capacity) OUTPUT INSERTED.Id VALUES(@rec,@name,@capacity)",("rec",recs[i]),("name",t.Position),("capacity",t.Capacity)))positions[i]=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            foreach(var skill in t.Skills)
            {
                await using var cmd=Db.Command(c,tx,"INSERT PositionSkills(PositionId,SkillId) SELECT @position,Id FROM Skills WHERE Name=@skill",("position",positions[i]),("skill",skill));
                if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new DomainException("Validation",$"Skill seed tidak ditemukan di katalog: {skill}.");
            }
            created.Add(i);
        }
        foreach(var a in new (int Student,int Recruitment,int Status)[]{(1,0,0),(2,0,1),(0,1,0),(0,2,1),(2,4,2),(2,6,1),(0,7,3)})
        {
            if(!created.Contains(a.Recruitment))continue;
            await using var cmd=Db.Command(c,tx,"INSERT Applications(StudentId,RecruitmentId,PositionId,Message,Status,SubmittedAtUtc,DecidedAtUtc) VALUES(@student,@rec,@position,N'Data demonstrasi: saya ingin berkontribusi sesuai skill dan bersedia berdiskusi mengenai kebutuhan tim.',@status,@now,@decision)",("student",students[a.Student]),("rec",recs[a.Recruitment]),("position",positions[a.Recruitment]),("status",a.Status),("now",now.AddDays(-1)),("decision",a.Status==0?null:now));await cmd.ExecuteNonQueryAsync(ct);
        }
        foreach(var i in new[]{1,5})if(created.Contains(i)){await using var cmd=Db.Command(c,tx,"INSERT Bookmarks VALUES(@student,@rec)",("student",students[0]),("rec",recs[i]));await cmd.ExecuteNonQueryAsync(ct);}
        await MentorSeedData.SeedAsync(c,tx,ct);
        await tx.CommitAsync(ct);
    }
}
