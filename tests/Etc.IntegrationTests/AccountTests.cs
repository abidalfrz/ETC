using Etc.Desktop.Features.Accounts;
using Etc.Desktop.Shared;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class AccountTests(DatabaseFixture db)
{
    [Fact]public async Task LogoutWhileLoginIsBlockedCannotRestoreSession()
    {
        await db.ResetAsync();var session=new SessionState();var service=new AccountService(db.Factory,session);
        var student=await service.RegisterAsync("Raka","raka@example.test","Etc-Test-Pass!2026");
        await using var c=await db.Factory.OpenAsync();await using var tx=(Microsoft.Data.SqlClient.SqlTransaction)await c.BeginTransactionAsync();
        await using(var cmd=new Microsoft.Data.SqlClient.SqlCommand("UPDATE Students SET FullName=FullName WHERE Id=@id",c,tx)){cmd.Parameters.AddWithValue("@id",student.Id);await cmd.ExecuteNonQueryAsync();}
        var login=service.LoginAsync(student.Email,"Etc-Test-Pass!2026");session.SignOut();await tx.CommitAsync();
        Assert.Equal("Unauthorized",(await Assert.ThrowsAsync<DomainException>(()=>login)).Code);Assert.Null(session.Current);
    }
    [Fact] public async Task RegisterNormalizesEmailAndRejectsDuplicate()
    {
        await db.ResetAsync();
        var session=new SessionState(); var service=new AccountService(db.Factory,session);
        var first=await service.RegisterAsync("Raka"," RAKA@example.test ","Etc-Test-Pass!2026");
        Assert.Equal("RAKA@example.test",first.Email);
        var e=await Assert.ThrowsAsync<DomainException>(()=>service.RegisterAsync("Nadia","raka@example.test","Etc-Test-Pass!2026"));
        Assert.Equal("Conflict",e.Code);
        Assert.NotEqual("Etc-Test-Pass!2026",await db.ScalarAsync("SELECT TOP 1 PasswordHash FROM Students"));
    }
    [Fact] public async Task WrongPasswordDoesNotCreateSession()
    {
        await db.ResetAsync();var session=new SessionState();var service=new AccountService(db.Factory,session);
        var student=await service.RegisterAsync("Nadia","nadia@example.test","Etc-Test-Pass!2026");
        await Assert.ThrowsAsync<DomainException>(()=>service.LoginAsync(student.Email,"wrong-password"));
        Assert.Null(session.Current);
        Assert.Equal(student.Id,(await service.LoginAsync(student.Email,"Etc-Test-Pass!2026")).Id);
        Assert.Equal(student.Id,session.RequireStudentId());
    }
    [Theory][InlineData("bad","Etc-Test-Pass!2026")][InlineData("a@example.test","short")]
    public async Task InvalidCredentialsRejectedBeforeDatabase(string email,string pass)
    {
        var e=await Assert.ThrowsAsync<DomainException>(()=>new AccountService(db.Factory,new()).RegisterAsync("Raka",email,pass));
        Assert.Equal("Validation",e.Code);
    }
}
