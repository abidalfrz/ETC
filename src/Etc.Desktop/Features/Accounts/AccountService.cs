using Etc.Desktop.Infrastructure;
using Etc.Desktop.Shared;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
namespace Etc.Desktop.Features.Accounts;
public interface IAccountService
{
    Task<StudentIdentity> RegisterAsync(string name, string email, string password, CancellationToken ct = default);
    Task<StudentIdentity> LoginAsync(string email, string password, CancellationToken ct = default);
}
public sealed class AccountService(SqlConnectionFactory connections, SessionState session) : IAccountService
{
    private readonly PasswordHasher<StudentIdentity> hasher=new();
    private static string Email(string value)
    {
        var email=Guard.Text(value,"Email",254);
        if(!MailAddress.TryCreate(email,out var parsed)||parsed.Address!=email||!email.Contains('@'))
            throw new DomainException("Validation","Masukkan alamat email yang valid.");
        return email;
    }
    public async Task<StudentIdentity> RegisterAsync(string name, string email, string password, CancellationToken ct = default)
    {
        name=Guard.Text(name,"Nama lengkap",120); email=Email(email);
        if(password.Length is <12 or >128)throw new DomainException("Validation","Password harus 12–128 karakter.");
        var identity=new StudentIdentity(0,name,email);
        var hash=await Task.Run(()=>hasher.HashPassword(identity,password),ct);
        await using var c=await connections.OpenAsync(ct);
        await using var cmd=Db.Command(c,null,"INSERT Students(FullName,Email,NormalizedEmail,PasswordHash) OUTPUT INSERTED.Id VALUES(@name,@email,@normalized,@hash)",("name",name),("email",email),("normalized",email.ToUpperInvariant()),("hash",hash));
        try{return identity with{Id=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))};}
        catch(SqlException e)when(e.Number is 2601 or 2627){throw new DomainException("Conflict","Email sudah terdaftar.");}
    }
    public async Task<StudentIdentity> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var revision=session.Revision;email=Email(email);
        if(password.Length>128)throw new DomainException("Validation","Password terlalu panjang.");
        StudentIdentity? identity=null;string? hash=null;
        await using(var c=await connections.OpenAsync(ct))
        await using(var cmd=Db.Command(c,null,"SELECT Id,FullName,Email,PasswordHash FROM Students WHERE NormalizedEmail=@email",("email",email.ToUpperInvariant())))
        await using(var r=await cmd.ExecuteReaderAsync(ct))
            if(await r.ReadAsync(ct)){identity=new(r.GetInt32(0),r.GetString(1),r.GetString(2));hash=r.GetString(3);}
        if(identity is null||await Task.Run(()=>hasher.VerifyHashedPassword(identity,hash!,password),ct)==PasswordVerificationResult.Failed)
            throw new DomainException("Validation","Email atau password tidak sesuai.");
        ct.ThrowIfCancellationRequested();
        if(!session.TrySignIn(identity,revision))throw new DomainException("Unauthorized","Sesi sudah berubah. Silakan masuk kembali.");
        return identity;
    }
}
