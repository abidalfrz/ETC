using CommunityToolkit.Mvvm.ComponentModel;
namespace Etc.Desktop.Shared;
public sealed partial class SessionState : ObservableObject
{
    private readonly object sync = new();
    private StudentIdentity? current;
    public StudentIdentity? Current {get=>current; private set=>SetProperty(ref current,value);}
    public long Revision { get; private set; }
    public int RequireStudentId() => Current?.Id ?? throw new DomainException("Unauthorized", "Silakan masuk terlebih dahulu.");
    public void SignIn(StudentIdentity identity) { lock(sync){Revision++;Current=identity;} }
    public void SignOut() { lock(sync){Revision++;Current=null;} }
    public void UpdateName(int studentId,string name) { lock(sync){if(Current?.Id==studentId)Current=Current with{FullName=name};} }
    public bool TrySignIn(StudentIdentity identity,long expectedRevision)
    {
        lock(sync){if(Revision!=expectedRevision)return false;SignIn(identity);return true;}
    }
}
