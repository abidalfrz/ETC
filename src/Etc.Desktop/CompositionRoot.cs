using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Etc.Desktop.Infrastructure;
using Etc.Desktop.Shared;
using Etc.Desktop.Features.Accounts;
using Etc.Desktop.Features.Profiles;
using Etc.Desktop.Features.Recruitments;
using Etc.Desktop.Features.Applications;
using Etc.Desktop.Features.Bookmarks;
using Etc.Desktop.Features.Dashboard;
using Etc.Desktop.Features.Mentors;
namespace Etc.Desktop;
public sealed class CompositionRoot
{
    public SessionState Session{get;}=new();
    public ShellViewModel Shell{get;}
    public AccountService Accounts{get;}
    private readonly MainWindow window;
    private readonly AppSettings settings;
    private readonly ProfileService profiles;
    private readonly RecruitmentService recruitments;
    private readonly RecruitmentQueries queries;
    private readonly ApplicationService applications;
    private readonly BookmarkService bookmarks;
    private readonly DashboardService dashboard;
    private readonly MentorService mentors;
    private readonly ExternalLinkService links;
    public CompositionRoot(MainWindow window,AppSettings? settings=null)
    {
        this.window=window;this.settings=settings??AppSettings.Load();
        if(Application.Current is{} app)app.RequestedThemeVariant=Avalonia.Styling.ThemeVariant.Light;
        var connections=new SqlConnectionFactory(this.settings.ConnectionString);var clock=TimeProvider.System;
        Accounts=new(connections,Session);profiles=new(connections,Session,clock);recruitments=new(connections,Session,clock);queries=new(connections,Session,clock);applications=new(connections,Session,clock);bookmarks=new(connections,Session,clock);dashboard=new(connections,Session,clock);mentors=new(connections,Session);
        links=new(uri=>window.Launcher.LaunchUriAsync(uri));
        Shell=new(Session,Create,ConfirmAsync,connections.DatabaseName=="ETC_Demo");
    }
    public void Start()=>Shell.Go("onboarding");
    private PageViewModel Create(string route,int id)
    {
        Action<string,int> go=(r,i)=>Shell.Go(r,i);
        return route switch
        {
            "login"=>new LoginViewModel(Accounts,Session,go),"register"=>new RegisterViewModel(Accounts,Session,go),
            "onboarding"=>new OnboardingViewModel(Session,go),"dashboard"=>new DashboardViewModel(dashboard,Session,go),
            "search"=>new SearchViewModel(queries,profiles,bookmarks,Session,go),"mine"=>new MyRecruitmentsViewModel(recruitments,Session,go,ConfirmAsync),
            "recruitment-form"=>new RecruitmentFormViewModel(recruitments,profiles,Session,id,go),
            "detail"=>new RecruitmentDetailViewModel(recruitments,profiles,bookmarks,applications,Session,id,go,links),
            "applications"=>new MyApplicationsViewModel(applications,Session,go,ConfirmAsync),"applicants"=>new ApplicantsViewModel(applications,recruitments,Session,id,go,ConfirmAsync,links),
            "bookmarks"=>new BookmarksViewModel(bookmarks,Session,go),"profile" or "profile-public"=>new ProfileViewModel(profiles,Session,id,PickPhotoAsync,links),
            "mentors"=>new MentorsViewModel(mentors,Session,go),"mentor-detail"=>new MentorDetailViewModel(mentors,Session,id,links,settings.MentorFormUrl),
            _=>throw new InvalidOperationException("Tujuan navigasi tidak dikenal.")
        };
    }
    private async Task<byte[]?> PickPhotoAsync()
    {
        var files=await window.StorageProvider.OpenFilePickerAsync(new(){Title="Pilih foto PNG/JPEG",AllowMultiple=false,FileTypeFilter=[new FilePickerFileType("Foto PNG/JPEG"){Patterns=["*.png","*.jpg","*.jpeg"]}]});
        if(files.Count==0)return null;
        await using var input=await files[0].OpenReadAsync();using var output=new MemoryStream();var buffer=new byte[81920];int n;
        while((n=await input.ReadAsync(buffer))>0){if(output.Length+n>2097152)throw new DomainException("Validation","Foto maksimal 2 MiB.");output.Write(buffer,0,n);}return output.ToArray();
    }
    private async Task<bool> ConfirmAsync(string title,string message)
    {
        var dialog=new Window{Title=title,Width=460,MaxWidth=Math.Max(320,window.Bounds.Width-48),SizeToContent=SizeToContent.Height,CanResize=false,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var action=title.StartsWith("Keluar")?"Keluar":title.StartsWith("Terima")?"Terima pelamar":title.StartsWith("Tolak")?"Tolak pelamar":title.StartsWith("Tutup")?"Tutup rekrutmen":title.StartsWith("Hapus")?"Hapus draft":title.StartsWith("Batalkan")?"Batalkan lamaran":"Publikasikan";
        var cancel=new Button{Content="Batal"};var accept=new Button{Content=action};accept.Classes.Add("primary");
        cancel.Click+=(_,_)=>dialog.Close(false);accept.Click+=(_,_)=>dialog.Close(true);
        dialog.Content=new StackPanel{Margin=new Thickness(24),Spacing=24,Children={new TextBlock{Text=title,FontSize=18,FontWeight=Avalonia.Media.FontWeight.SemiBold},new TextBlock{Text=message,TextWrapping=Avalonia.Media.TextWrapping.Wrap},new StackPanel{Orientation=Orientation.Horizontal,Spacing=12,HorizontalAlignment=HorizontalAlignment.Right,Children={cancel,accept}}}};
        dialog.Opened+=(_,_)=>{cancel.Focus();MotionSettings.Fade((Control)dialog.Content!,120);};dialog.KeyDown+=(_,e)=>{if(e.Key==Key.Escape){dialog.Close(false);e.Handled=true;}};
        return await dialog.ShowDialog<bool>(window);
    }
}
