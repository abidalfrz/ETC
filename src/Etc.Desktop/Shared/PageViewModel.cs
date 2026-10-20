using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace Etc.Desktop.Shared;
public abstract partial class PageViewModel(SessionState session,string title) : ObservableObject
{
    public string Title {get;}=title;
    protected SessionState Session=>session;
    [ObservableProperty]private Avalonia.Vector scrollOffset;
    [ObservableProperty]private bool isBusy;
    [ObservableProperty,NotifyPropertyChangedFor(nameof(HasLoadedData)),NotifyPropertyChangedFor(nameof(LastLoadedText))]private DateTimeOffset? lastLoadedAtUtc;
    public bool HasLoadedData=>LastLoadedAtUtc.HasValue;
    public string LastLoadedText=>LastLoadedAtUtc is{} loaded?"Terakhir dimuat: "+loaded.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy, HH.mm.ss 'WIB'"):"";
    [ObservableProperty,NotifyPropertyChangedFor(nameof(HasError))]private string errorMessage="";
    [ObservableProperty,NotifyPropertyChangedFor(nameof(HasInfo))]private string infoMessage="";
    public bool HasError=>!string.IsNullOrEmpty(ErrorMessage);
    public bool HasInfo=>!string.IsNullOrEmpty(InfoMessage);
    public bool IsActive {get;private set;}=true;
    private CancellationTokenSource lifetime=new();
    private long revision=session.Revision;
    private long readVersion;
    private CancellationTokenSource? pendingRead;
    public IAsyncRelayCommand RefreshCommand=>refresh??=new AsyncRelayCommand(()=>LoadAsync());
    private IAsyncRelayCommand? refresh;
    private Func<CancellationToken,Task>? retryAction;
    private IAsyncRelayCommand? retry;
    public IAsyncRelayCommand RetryCommand=>retry??=new AsyncRelayCommand(()=>retryAction is{} work?RunAsync(work):LoadAsync());
    public virtual Task LoadAsync(CancellationToken ct=default)=>Task.CompletedTask;
    public void Activate(){lifetime.Dispose();lifetime=new();revision=session.Revision;IsActive=true;}
    public virtual void Deactivate(){IsActive=false;lifetime.Cancel();pendingRead?.Cancel();readVersion++;IsBusy=false;ErrorMessage="";InfoMessage="";}
    protected internal bool IsCurrent=>IsActive&&session.Revision==revision;
    [RelayCommand]private void DismissInfo()=>InfoMessage="";
    protected async Task RunAsync(Func<CancellationToken,Task> work)
    {
        if(IsBusy||!IsCurrent)return;var token=lifetime.Token;retryAction=work;IsBusy=true;ErrorMessage="";InfoMessage="";
        try{await work(token);}
        catch(OperationCanceledException){}
        catch(Exception e){if(IsCurrent&&!token.IsCancellationRequested)ErrorMessage=SafeMessage(e);}
        finally{if(IsCurrent&&!token.IsCancellationRequested)IsBusy=false;}
    }
    protected async Task ReadAsync<T>(Func<CancellationToken,Task<T>> read,Action<T> apply,CancellationToken ct=default)
    {
        if(ct.IsCancellationRequested||!IsCurrent)return;
        var version=++readVersion;pendingRead?.Cancel();pendingRead?.Dispose();
        pendingRead=CancellationTokenSource.CreateLinkedTokenSource(ct,lifetime.Token);var token=pendingRead.Token;
        IsBusy=true;ErrorMessage="";
        try{var result=await read(token);if(version==readVersion&&IsCurrent&&!token.IsCancellationRequested){apply(result);LastLoadedAtUtc=DateTimeOffset.UtcNow;}}
        catch(OperationCanceledException){}
        catch(Exception e){if(version==readVersion&&IsCurrent){retryAction=retryToken=>ReadAsync(read,apply,retryToken);ErrorMessage=SafeMessage(e,reading:true);}}
        finally{if(version==readVersion&&IsActive)IsBusy=false;}
    }
    protected void Apply(CancellationToken ct,Action action){if(IsCurrent&&!ct.IsCancellationRequested)action();}
    private static string SafeMessage(Exception e,bool reading=false)=>e is DomainException?e.Message:reading?"Data belum dapat dimuat. Periksa koneksi dan coba lagi.":"Operasi belum berhasil. Periksa koneksi dan coba lagi. Isian Anda tetap tersedia.";
}
