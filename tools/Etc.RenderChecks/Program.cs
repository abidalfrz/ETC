using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Automation;
using Avalonia.Logging;
using Avalonia.Media.Imaging;
using Etc.Desktop;
using Etc.Desktop.Demo;
using Etc.Desktop.Infrastructure;
using Microsoft.Data.SqlClient;

internal static class Program
{
    [STAThread]public static int Main(string[] args)
    {
        var output=args.FirstOrDefault()??"docs/qa/screenshots";Directory.CreateDirectory(output);
        var exit=0;using var stop=new CancellationTokenSource();
        Logger.Sink=new BindingLog();
        AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).SetupWithoutStarting();
        Dispatcher.UIThread.Post(async()=>
        {
            try{await RunAsync(output);}catch(Exception e){Console.Error.WriteLine(e);exit=1;}finally{stop.Cancel();}
        });
        Dispatcher.UIThread.MainLoop(stop.Token);return exit;
    }
    private static async Task RunAsync(string output)
    {
        var settings=AppSettings.Load();var password=Environment.GetEnvironmentVariable("ETC_DEMO_PASSWORD")??DemoSeeder.DefaultPassword;
        var builder=new SqlConnectionStringBuilder(settings.ConnectionString);DemoSeeder.EnsureDemoTarget(builder.InitialCatalog);builder.InitialCatalog="master";
        await using(var master=new SqlConnection(builder.ConnectionString))
        {await master.OpenAsync();await using var create=new SqlCommand("IF DB_ID('ETC_Demo') IS NULL CREATE DATABASE ETC_Demo",master);await create.ExecuteNonQueryAsync();}
        var factory=new SqlConnectionFactory(settings.ConnectionString);await new DatabaseInitializer(factory).InitializeAsync();await new DemoSeeder(factory,TimeProvider.System).SeedAsync(password);
        var window=new MainWindow();Application.Current!.RequestedThemeVariant=ThemeVariant.Dark;
        var root=new CompositionRoot(window,settings);window.DataContext=root.Shell;window.Show();root.Start();
        var regressions=new List<string>();
        Check(Application.Current.RequestedThemeVariant==ThemeVariant.Light,"Composition root must lock the app to light mode regardless of the system theme.");
        Check(window.GetVisualDescendants().OfType<Button>().All(b=>b.Content?.ToString() is not{} content||!content.StartsWith("Tema:",StringComparison.Ordinal)),"Shell must not expose a theme toggle.");
        var font=new Typeface("avares://Etc.Desktop/Assets/Fonts#DM Sans").GlyphTypeface.FamilyName;
        if(!font.Contains("DM Sans",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Typeface fallback: "+font);
        Console.WriteLine("Loaded font: "+font);
        await Capture("onboarding");root.Shell.Go("login");await Capture("login");root.Shell.Go("register");await Capture("register");
        root.Shell.Go("login");await WaitLoaded();Dispatcher.UIThread.RunJobs();
        var email=window.GetVisualDescendants().OfType<TextBox>().First(t=>AutomationProperties.GetName(t)=="Email");
        var pass=window.GetVisualDescendants().OfType<TextBox>().First(t=>t.PasswordChar!='\0');
        email.Focus();window.KeyTextInput("raka.demo@example.test");pass.Focus();window.KeyTextInput(password);
        await Click("Masuk");if(!root.Shell.IsAuthenticated)throw new InvalidOperationException("Login click did not authenticate.");
        window.Width=800;window.Height=560;Dispatcher.UIThread.RunJobs();await Task.Delay(20);await Click("Menu");
        if(!root.Shell.IsMenuOpen)throw new InvalidOperationException("Menu click failed.");
        if(window.FocusManager?.GetFocusedElement() is not Button{CommandParameter:"dashboard"})throw new InvalidOperationException("Compact menu must focus its first navigation action.");
        window.KeyPress(Key.Tab,RawInputModifiers.Shift,PhysicalKey.Tab,null);window.KeyRelease(Key.Tab,RawInputModifiers.Shift,PhysicalKey.Tab,null);
        if(window.FocusManager?.GetFocusedElement() is not Button{Content:"Tutup menu"})throw new InvalidOperationException("Menu Shift+Tab must stay inside the flyout.");
        window.KeyPress(Key.Tab,RawInputModifiers.None,PhysicalKey.Tab,null);window.KeyRelease(Key.Tab,RawInputModifiers.None,PhysicalKey.Tab,null);
        if(window.FocusManager?.GetFocusedElement() is not Button{CommandParameter:"dashboard"})throw new InvalidOperationException("Menu Tab must wrap to first action.");
        window.KeyPress(Key.Escape,RawInputModifiers.None,PhysicalKey.Escape,null);window.KeyRelease(Key.Escape,RawInputModifiers.None,PhysicalKey.Escape,null);if(root.Shell.IsMenuOpen)throw new InvalidOperationException("Escape menu failed.");
        window.Width=1440;window.Height=900;Dispatcher.UIThread.RunJobs();
        foreach(var label in new[]{"Cari rekrutmen","Rekrutmen saya","Lamaran saya","Bookmark","Mentor","Dashboard"})await Click(label);
        root.Shell.Go("mine");await WaitLoaded();await Click("Tutup rekrutmen",false);Dispatcher.UIThread.RunJobs();
        var dialog=window.OwnedWindows.Single();
        if(dialog.FocusManager?.GetFocusedElement() is not Button{Content:"Batal"})throw new InvalidOperationException("Dialog initial focus is not Batal.");
        dialog.KeyPress(Key.Escape,RawInputModifiers.None,PhysicalKey.Escape,null);window.KeyRelease(Key.Escape,RawInputModifiers.None,PhysicalKey.Escape,null);await WaitLoaded();
        if(window.OwnedWindows.Count>0)throw new InvalidOperationException("Dialog Escape did not close.");
        Console.WriteLine("Input checks: real keyboard login, sidebar navigation, compact menu click/Escape.");
        Console.WriteLine("Dialog checks: close recruitment cancelled with Escape; initial focus Batal.");
        var rec=await ScalarId("SELECT MIN(Id) FROM Recruitments WHERE Title=N'Hology 9.0 (Holomine)'");
        var publicRec=await ScalarId("SELECT MIN(Id) FROM Recruitments WHERE Title=N'Aplikasi inventaris laboratorium'");
        var mentor=await ScalarId("SELECT MIN(Id) FROM Mentors");
        await VerifyMentorDirectory();
        var routes=new(string Route,int Id)[]{("dashboard",0),("search",0),("mine",0),("applications",0),("bookmarks",0),("profile",0),("detail",publicRec),("applicants",rec),("recruitment-form",0),("mentors",0),("mentor-detail",mentor)};
        await VerifyInteractionPolish();
        foreach(var (route,id) in routes)
        {
            root.Shell.Go(route,id);await WaitLoaded();
            foreach(var size in new[]{new Size(1440,900),new Size(1024,768),new Size(800,560)})
            {
                window.Width=size.Width;window.Height=size.Height;Dispatcher.UIThread.RunJobs();await Task.Delay(20);Dispatcher.UIThread.RunJobs();
                if(route=="mentors")VerifyMentorLayout();
                using var frame=window.CaptureRenderedFrame()??throw new InvalidOperationException("Tidak ada frame.");
                frame.Save(Path.Combine(output,$"{route}-light-{size.Width}x{size.Height}.png"),PngBitmapEncoderOptions.Default);
            }
            Console.WriteLine($"Rendered {route} (light)");
        }
        root.Shell.Go("dashboard");await WaitLoaded();
        Check(window.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text?.StartsWith("Terakhir dimuat: ")==true&&t.IsEffectivelyVisible),"Refresh must disclose when displayed data was loaded.");
        root.Shell.CurrentPage!.IsBusy=true;Dispatcher.UIThread.RunJobs();
        Check(window.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="Sedang memuat atau menyimpan…"&&t.IsEffectivelyVisible),"Busy state must retain visible text without animation.");root.Shell.CurrentPage.IsBusy=false;
        root.Shell.Go("applications");await WaitLoaded();
        var history=(Etc.Desktop.Features.Applications.MyApplicationsViewModel)root.Shell.CurrentPage!;
        history.Items=[history.Items[0] with{SubmittedAtUtc=new DateTimeOffset(2026,10,19,18,30,0,TimeSpan.Zero)}];Dispatcher.UIThread.RunJobs();
        Check(window.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="Diajukan: 20/10/2026, 01.30 WIB"),"Submission timestamp must display WIB across midnight.");
        root.Shell.Go("detail",publicRec);await WaitLoaded();
        Check(window.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text?.StartsWith("Dipublikasikan: ")==true&&t.Text.Contains("WIB")),"Recruitment detail must show publication time.");
        Check(window.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text?.StartsWith("Diperbarui: ")==true&&t.Text.Contains("WIB")),"Recruitment detail must show update time.");
        root.Shell.Go("applicants",rec);window.Width=800;window.Height=560;await WaitLoaded();
        var review=window.GetVisualDescendants().OfType<Button>().FirstOrDefault(b=>Equals(b.Content,"Tinjau pelamar"));
        Check(review is not null,"Compact applicants must offer a list-to-review action.");
        if(review is not null)
        {
            var applicants=(Etc.Desktop.Features.Applications.ApplicantsViewModel)root.Shell.CurrentPage!;var position=applicants.SelectedPosition;var page=applicants.PageNumber;
            await Click("Tinjau pelamar");
            using(var frame=window.CaptureRenderedFrame()!)frame.Save(Path.Combine(output,"applicants-review-light-800x560.png"),PngBitmapEncoderOptions.Default);
            Check(window.GetVisualDescendants().OfType<Button>().Any(b=>Equals(b.Content,"Kembali ke daftar pelamar")&&b.IsEffectivelyVisible),"Compact review must offer return to list.");
            Check(!window.GetVisualDescendants().OfType<ListBox>().Any(b=>b.IsEffectivelyVisible),"Compact review must hide the list while reviewing.");
            var selected=applicants.SelectedApplicant?.Id;var offset=applicants.ListScrollOffset;
            await Click("Kembali ke daftar pelamar");Check(window.GetVisualDescendants().OfType<ListBox>().Any(b=>b.IsEffectivelyVisible),"Return must restore applicant list.");
            Check(applicants.SelectedPosition==position&&applicants.PageNumber==page&&applicants.SelectedApplicant?.Id==selected,"Return must preserve filter, page, and selection.");
            Check(Math.Abs(applicants.ScrollOffset.Y-offset.Y)<1,"Return must restore list scroll position.");
        }
        root.Shell.Go("mentor-detail",mentor);await WaitLoaded();Dispatcher.UIThread.RunJobs();
        if(!((Etc.Desktop.Features.Mentors.MentorDetailViewModel)root.Shell.CurrentPage!).CanRequestMentor)throw new InvalidOperationException("Supplied Google Form must enable mentoring requests by default.");
        root.Shell.Navigate(new Etc.Desktop.Features.Mentors.MentorDetailViewModel(new Etc.Desktop.Features.Mentors.MentorService(factory,root.Session),root.Session,mentor,new Etc.Desktop.Shared.ExternalLinkService(_=>Task.FromResult(true)),null));await WaitLoaded();
        var disabled=window.GetVisualDescendants().OfType<Button>().Single(b=>b.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="Butuh mentor?"));
        var disabledLabel=disabled.GetVisualDescendants().OfType<TextBlock>().First();
        if(disabledLabel.Foreground is ISolidColorBrush ink&&disabled.Background is ISolidColorBrush ground){Console.WriteLine($"Disabled primary contrast: {Contrast(ink.Color,ground.Color):F2}:1.");Check(Contrast(ink.Color,ground.Color)>=4.5,$"Disabled primary label contrast is {Contrast(ink.Color,ground.Color):F2}:1; expected at least 4.5:1.");}
        else Check(false,"Disabled primary contrast colors unavailable.");
        if(regressions.Count>0)throw new InvalidOperationException(string.Join("\n",regressions));
        Console.WriteLine("Review regression input/render checks PASS: WIB, detail metadata, compact review, disabled contrast.");
        root.Shell.Go("profile");await WaitLoaded();
        var profile=(Etc.Desktop.Features.Profiles.ProfileViewModel)root.Shell.CurrentPage!;
        profile.FullName=string.Concat(Enumerable.Repeat("Mahasiswa 日本語 ",8));profile.Photo=[1,2,3,4];
        window.Width=800;window.Height=560;Dispatcher.UIThread.RunJobs();await Task.Delay(20);
        using(var frame=window.CaptureRenderedFrame()!)frame.Save(Path.Combine(output,"profile-long-corrupt-photo.png"),PngBitmapEncoderOptions.Default);
        window.Width=1280;Dispatcher.UIThread.RunJobs();await Task.Delay(20);await Click("Keluar",false);
        var logoutDialog=window.OwnedWindows.Single();
        if(!logoutDialog.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="Keluar dari akun?")||!logoutDialog.GetVisualDescendants().OfType<Button>().Any(b=>Equals(b.Content,"Keluar")))throw new InvalidOperationException("Logout confirmation content is missing.");
        logoutDialog.KeyPress(Key.Escape,RawInputModifiers.None,PhysicalKey.Escape,null);window.KeyRelease(Key.Escape,RawInputModifiers.None,PhysicalKey.Escape,null);await WaitLoaded();
        if(root.Session.Current is null||!root.Shell.IsAuthenticated)throw new InvalidOperationException("Cancelling logout confirmation must keep the session active.");
        await Click("Keluar",false);logoutDialog=window.OwnedWindows.Single();
        logoutDialog.KeyPress(Key.Tab,RawInputModifiers.None,PhysicalKey.Tab,null);logoutDialog.KeyRelease(Key.Tab,RawInputModifiers.None,PhysicalKey.Tab,null);
        logoutDialog.KeyPress(Key.Enter,RawInputModifiers.None,PhysicalKey.Enter,null);window.KeyRelease(Key.Enter,RawInputModifiers.None,PhysicalKey.Enter,null);await WaitLoaded();
        if(root.Session.Current is not null||root.Shell.IsAuthenticated)throw new InvalidOperationException("Confirming logout must clear the session.");
        window.Close();Console.WriteLine($"Binding diagnostics: {BindingLog.Count} unexpected; {BindingLog.ExpectedNullCount} expected unloaded/empty nullable paths.");
        if(BindingLog.Count>0)throw new InvalidOperationException("Avalonia binding diagnostics detected.");
        Console.WriteLine("Render checks complete; full native Ubuntu/Windows verification remains separate.");
        void VerifyMentorLayout()
        {
            var input=window.GetVisualDescendants().OfType<TextBox>().Single(t=>AutomationProperties.GetName(t)=="Cari mentor");
            if(input.PlaceholderForeground is not ISolidColorBrush hint||hint.Color.A!=255||input.Background is not ISolidColorBrush inputGround||Contrast(hint.Color,inputGround.Color)<4.5)throw new InvalidOperationException("Mentor search placeholder must use an opaque color meeting 4.5:1 contrast.");
            var heading=window.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Text=="Pilih mentor");
            var count=window.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Text?.EndsWith(" mentor ditemukan")==true);
            if(heading.TranslatePoint(default,window)!.Value.X+heading.Bounds.Width>count.TranslatePoint(default,window)!.Value.X)throw new InvalidOperationException("Mentor heading and result count must not overlap.");
            var cards=window.GetVisualDescendants().OfType<Border>().Where(b=>b.Classes.Contains("mentor-card")).ToArray();
            if(cards.Length>0&&cards.Any(card=>Math.Abs(card.Bounds.Width-cards[0].Bounds.Width)>.5||Math.Abs(card.Bounds.Height-cards[0].Bounds.Height)>.5))throw new InvalidOperationException("Every mentor card must have the same width and height at the current window size.");
            foreach(var card in cards)
            foreach(var label in card.GetVisualDescendants().OfType<TextBlock>().Where(t=>t.IsEffectivelyVisible))
            {
                var point=label.TranslatePoint(default,card)!.Value;
                if(point.X<0||point.X+label.Bounds.Width>card.Bounds.Width+.5)throw new InvalidOperationException("Mentor card text escapes its surface: "+label.Text);
            }
        }
        async Task VerifyMentorDirectory()
        {
            window.Width=1440;window.Height=900;root.Shell.Go("mentors");await WaitLoaded();
            var vm=(Etc.Desktop.Features.Mentors.MentorsViewModel)root.Shell.CurrentPage!;
            var searchInput=window.GetVisualDescendants().OfType<TextBox>().Single(t=>AutomationProperties.GetName(t)=="Cari mentor");
            if(searchInput.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Name=="PART_Placeholder").Opacity!=1)throw new InvalidOperationException("Mentor search template must not halve the accessible placeholder ink opacity.");
            var cards=window.GetVisualDescendants().OfType<Border>().Where(b=>b.Classes.Contains("mentor-card")).ToArray();
            if(cards.Length!=12||vm.Total!=15)throw new InvalidOperationException("Mentor directory must render 12 photo cards on page 1 of 15 supplied mentors.");
            VerifyMentorLayout();var firstPageCardSize=cards[0].Bounds.Size;
            foreach(var card in cards)if(!card.GetVisualDescendants().OfType<Image>().Any(i=>i.Source is not null))throw new InvalidOperationException("Every mentor card must show a readable photo.");
            var skill=window.GetVisualDescendants().OfType<Button>().First(b=>Equals(b.Content,"Competitive Programming"));
            skill.BringIntoView();Dispatcher.UIThread.RunJobs();skill.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Enter,RawInputModifiers.None,PhysicalKey.Enter,null);window.KeyRelease(Key.Enter,RawInputModifiers.None,PhysicalKey.Enter,null);await WaitLoaded();
            if(vm.Total!=3||vm.SearchText!="Competitive Programming")throw new InvalidOperationException("Keyboard skill action must search mentors with that skill.");
            await Click("Hapus pencarian");await Click("Berikutnya");
            if(vm.PageNumber!=2||vm.Items.Count!=3||vm.Items.Any(x=>x.Photo is null))throw new InvalidOperationException("Mentor pagination must show the remaining three profiles and their photos.");
            if(window.GetVisualDescendants().OfType<Border>().Where(b=>b.Classes.Contains("mentor-card")).Any(b=>!b.GetVisualDescendants().OfType<Image>().Any(i=>i.Source is not null)))throw new InvalidOperationException("Every page 2 mentor card must render its photo.");
            await Click("Lihat profil mentor");await Click("Kembali");
            if(vm.PageNumber!=2||vm.Items.Count!=3)throw new InvalidOperationException("Returning from a mentor profile must preserve directory page 2.");
            VerifyMentorLayout();
            var secondPageCard=window.GetVisualDescendants().OfType<Border>().First(b=>b.Classes.Contains("mentor-card"));
            if(Math.Abs(secondPageCard.Bounds.Height-firstPageCardSize.Height)>.5)throw new InvalidOperationException("Mentor card height must stay consistent between directory pages.");
            window.GetVisualDescendants().OfType<ScrollViewer>().First(s=>s.Content is StackPanel).Offset=default;Dispatcher.UIThread.RunJobs();
            using(var frame=window.CaptureRenderedFrame()!)frame.Save(Path.Combine(output,"mentors-page2-light-1440x900.png"),PngBitmapEncoderOptions.Default);
            await Click("Sebelumnya");vm.SearchText="___missing_mentor___";await Click("Cari mentor");
            if(vm.HasItems)throw new InvalidOperationException("Unmatched mentor search must show an empty state.");
            await Click("Hapus pencarian");await Click("Lihat profil mentor");
            if(root.Shell.CurrentPage is not Etc.Desktop.Features.Mentors.MentorDetailViewModel detail||detail.Detail?.Summary.Photo is null)throw new InvalidOperationException("Mentor card must open the full profile with photo.");
            await Click("Kembali");
            Console.WriteLine("Mentor directory PASS: 15 supplied profiles, visible photos, keyboard skill search, reset/empty, pagination, profile and return navigation.");
        }
        async Task VerifyInteractionPolish()
        {
            window.Width=1440;window.Height=900;
            root.Shell.Go("dashboard");await WaitLoaded();
                var primary=window.GetVisualDescendants().OfType<Button>().Single(b=>b.Classes.Contains("primary")&&b.IsEffectivelyVisible);
                primary.BringIntoView();Dispatcher.UIThread.RunJobs();window.MouseMove(new Point(1,1));primary.Focus(NavigationMethod.Tab);
                window.KeyPress(Key.Space,RawInputModifiers.None,PhysicalKey.Space,null);await Task.Delay(120);Dispatcher.UIThread.RunJobs();
                var presenter=primary.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().Single(p=>p.Name=="PART_ContentPresenter");
                var label=primary.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Text=="Cari rekrutmen");var icon=primary.GetVisualDescendants().OfType<PathIcon>().Single();
                if(presenter.Background is not ISolidColorBrush bg||label.Foreground is not ISolidColorBrush ink||icon.Foreground is not ISolidColorBrush iconInk)throw new InvalidOperationException("Pressed primary colors unavailable.");
                Console.WriteLine($"Primary keyboard-pressed (light): {ink.Color} on {bg.Color}; text {Contrast(ink.Color,bg.Color):F2}:1, icon {Contrast(iconInk.Color,bg.Color):F2}:1.");
                if(bg.Color.A!=255||Contrast(ink.Color,bg.Color)<4.5||Contrast(iconInk.Color,bg.Color)<3)throw new InvalidOperationException("Keyboard-pressed primary text/icon contrast fails.");
                using(var frame=window.CaptureRenderedFrame()!)frame.Save(Path.Combine(output,"primary-pressed-light.png"),PngBitmapEncoderOptions.Default);
                window.KeyRelease(Key.Space,RawInputModifiers.None,PhysicalKey.Space,null);await WaitLoaded();
            foreach(var (name,title) in new[]{("Buka rekrutmen saya","Rekrutmen saya"),("Lihat riwayat lamaran; 1 lamaran menunggu","Lamaran saya"),("Lihat riwayat lamaran; 1 lamaran diterima","Lamaran saya"),("Buka bookmark","Bookmark")})
            {
                root.Shell.Go("dashboard");await WaitLoaded();
                var shortcut=window.GetVisualDescendants().OfType<Button>().SingleOrDefault(b=>AutomationProperties.GetName(b)==name);
                if(shortcut is null)throw new InvalidOperationException("Dashboard shortcut missing: "+name);
                shortcut.BringIntoView();Dispatcher.UIThread.RunJobs();shortcut.Focus(NavigationMethod.Tab);
                if(!shortcut.IsFocused)throw new InvalidOperationException("Dashboard shortcut must be keyboard focusable: "+name);
                window.KeyPress(Key.Enter,RawInputModifiers.None,PhysicalKey.Enter,null);window.KeyRelease(Key.Enter,RawInputModifiers.None,PhysicalKey.Enter,null);await WaitLoaded();
                if(root.Shell.CurrentPage?.Title!=title)throw new InvalidOperationException("Dashboard shortcut destination: "+name);
            }
            foreach(var route in new[]{"search","mine","applications","bookmarks"})
                {
                    root.Shell.Go(route);await WaitLoaded();
                    var card=window.GetVisualDescendants().OfType<Border>().FirstOrDefault(b=>b.Classes.Contains("list-card"));
                    if(card is null)throw new InvalidOperationException("Opaque list card missing: "+route);
                    if(route=="search"&&card.TranslatePoint(default,window)!.Value.Y>window.Height*.6)
                        throw new InvalidOperationException("Search should expose its first result in the first 60% of a wide window; expanded filters push results below the fold.");
                    if(card.Background is not ISolidColorBrush surface||surface.Color.A!=255||window.Background is not ISolidColorBrush ground||surface.Color==ground.Color)
                        throw new InvalidOperationException("List surface must be opaque and distinct from ground: "+route);
                    if(!card.GetVisualDescendants().OfType<PathIcon>().Any())throw new InvalidOperationException("Labeled list action icons missing: "+route);
                    card.BringIntoView();Dispatcher.UIThread.RunJobs();window.MouseMove(new Point(1,1));Dispatcher.UIThread.RunJobs();
                    var edge=(card.BorderBrush as ISolidColorBrush)?.Color;
                    var point=card.TranslatePoint(new Point(4,4),window)!.Value;window.MouseMove(point);Dispatcher.UIThread.RunJobs();
                    if((card.BorderBrush as ISolidColorBrush)?.Color==edge)throw new InvalidOperationException("Card hover must give feedback: "+route);
            }
            Console.WriteLine("Interaction polish PASS: four keyboard dashboard shortcuts, four opaque list surfaces, labeled icons and hover outlines in light mode.");
            Console.WriteLine("Platform motion enabled: "+Etc.Desktop.Shared.MotionSettings.Enabled);
            root.Shell.Go("dashboard");await WaitLoaded();
            var metric=window.GetVisualDescendants().OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="Buka bookmark");
            metric.BringIntoView();Dispatcher.UIThread.RunJobs();
            var pressPoint=metric.TranslatePoint(new Point(metric.Bounds.Width/2,metric.Bounds.Height/2),window)!.Value;
            window.MouseMove(pressPoint);window.MouseDown(pressPoint,MouseButton.Left);await Task.Delay(120);Dispatcher.UIThread.RunJobs();
            using(var frame=window.CaptureRenderedFrame()!)frame.Save(Path.Combine(output,"dashboard-pressed.png"),PngBitmapEncoderOptions.Default);
            if(metric.RenderTransform?.Value.M11 is not <.999)throw new InvalidOperationException("Pressed dashboard shortcut must give scale feedback.");
            window.MouseUp(pressPoint,MouseButton.Left);await WaitLoaded();
            if(root.Shell.CurrentPage?.Title!="Bookmark")throw new InvalidOperationException("Dashboard mouse shortcut did not navigate.");
            root.Shell.Go("search");await WaitLoaded();
            await Click("Filter kategori, skill, dan posisi");
            var filter=window.GetVisualDescendants().OfType<Expander>().Single();
            if(!filter.IsExpanded)throw new InvalidOperationException("Filter must open on click.");
            window.Width=800;Dispatcher.UIThread.RunJobs();await Task.Delay(20);
            if(!filter.IsExpanded)throw new InvalidOperationException("Resize must preserve a user's expanded filter.");
            using(var frame=window.CaptureRenderedFrame()!)frame.Save(Path.Combine(output,"search-filters-800x560.png"),PngBitmapEncoderOptions.Default);
            await Click("Filter kategori, skill, dan posisi");
            var search=(Etc.Desktop.Features.Recruitments.SearchViewModel)root.Shell.CurrentPage!;
            search.SearchText="___ETC_no_matching_recruitment___";await Click("Terapkan pencarian");
            if(search.HasItems||!window.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.IsEffectivelyVisible&&t.Text=="Tidak ada rekrutmen yang cocok."))throw new InvalidOperationException("Search must show a framed empty state.");
            using(var frame=window.CaptureRenderedFrame()!)frame.Save(Path.Combine(output,"search-empty-800x560.png"),PngBitmapEncoderOptions.Default);
            await Click("Hapus filter");
            var unsaved=window.GetVisualDescendants().OfType<Button>().First(b=>b.IsEffectivelyVisible&&b.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="Simpan bookmark"));
            var item=(Etc.Desktop.Features.Recruitments.RecruitmentSummary)unsaved.CommandParameter!;
            await Click("Simpan bookmark");
            if(!search.Items.Single(x=>x.Id==item.Id).IsBookmarked)throw new InvalidOperationException("Bookmark click must update saved state.");
            var saved=window.GetVisualDescendants().OfType<Button>().Single(b=>b.IsEffectivelyVisible&&b.CommandParameter is Etc.Desktop.Features.Recruitments.RecruitmentSummary r&&r.Id==item.Id&&b.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="Hapus bookmark"));
            saved.BringIntoView();Dispatcher.UIThread.RunJobs();saved.Focus();window.KeyPress(Key.Enter,RawInputModifiers.None,PhysicalKey.Enter,null);window.KeyRelease(Key.Enter,RawInputModifiers.None,PhysicalKey.Enter,null);await WaitLoaded();
            if(search.Items.Single(x=>x.Id==item.Id).IsBookmarked)throw new InvalidOperationException("Bookmark removal must restore original unsaved state.");
            foreach(var (route,actionLabel) in new[]{("search","Lihat posisi"),("mine","Detail"),("applications","Lihat rekrutmen"),("bookmarks","Lihat posisi")})
            {root.Shell.Go(route);await WaitLoaded();await Click(actionLabel);if(root.Shell.CurrentPage?.Title!="Detail rekrutmen")throw new InvalidOperationException("Labeled detail action failed: "+route);}
            Console.WriteLine("Interaction checks PASS: pressed feedback, mouse navigation, filter toggle/resize, empty/reset, bookmark mouse-save/keyboard-remove round trip, all four detail actions.");
            const string longTitle="Tim kolaborasi mahasiswa untuk pengembangan sistem pemantauan lingkungan dan dokumentasi kegiatan lintas program studi dengan dukungan aksesibilitas dan pengujian bersama";
            foreach(var route in new[]{"search","mine","applications","bookmarks"})
                {
                    root.Shell.Go(route);await WaitLoaded();window.Width=800;window.Height=560;
                    switch(root.Shell.CurrentPage)
                    {
                        case Etc.Desktop.Features.Recruitments.SearchViewModel vm:vm.Items=[vm.Items[0] with{Title=longTitle}];break;
                        case Etc.Desktop.Features.Recruitments.MyRecruitmentsViewModel vm:vm.Items=[vm.Items[0] with{Title=longTitle}];break;
                        case Etc.Desktop.Features.Bookmarks.BookmarksViewModel vm:vm.Items=[vm.Items[0] with{Title=longTitle}];break;
                        case Etc.Desktop.Features.Applications.MyApplicationsViewModel vm:vm.Items=[vm.Items[0] with{RecruitmentTitle=longTitle,PositionName="Pengembang antarmuka dan dokumentasi aksesibilitas lintas program studi"}];break;
                    }
                    Dispatcher.UIThread.RunJobs();await Task.Delay(20);
                    var card=window.GetVisualDescendants().OfType<Border>().First(b=>b.Classes.Contains("list-card"));card.BringIntoView();Dispatcher.UIThread.RunJobs();
                    foreach(var text in card.GetVisualDescendants().OfType<TextBlock>().Where(t=>t.IsEffectivelyVisible))
                    {
                        var point=text.TranslatePoint(default,card)!.Value;
                        if(point.X<0||point.X+text.Bounds.Width>card.Bounds.Width+.5||text.DesiredSize.Height>text.Bounds.Height+.5)throw new InvalidOperationException("Compact long-text clipping: "+route+" / "+text.Text);
                    }
                    using(var frame=window.CaptureRenderedFrame()!)frame.Save(Path.Combine(output,$"{route}-long-light-800x560.png"),PngBitmapEncoderOptions.Default);
                }
            Console.WriteLine("Compact long recruitment/position titles PASS: four lists in light mode, no horizontal escape or clipped text.");
        }
        async Task<int> ScalarId(string sql){await using var c=await factory.OpenAsync();await using var cmd=new SqlCommand(sql,c);return Convert.ToInt32(await cmd.ExecuteScalarAsync());}
        async Task WaitLoaded()
        {
            var timeout=DateTime.UtcNow.AddSeconds(30);while(root.Shell.CurrentPage?.IsBusy==true&&DateTime.UtcNow<timeout)await Task.Delay(10);
            if(root.Shell.CurrentPage?.IsBusy==true||root.Shell.CurrentPage?.HasError==true)throw new InvalidOperationException(root.Shell.CurrentPage.ErrorMessage);
            Dispatcher.UIThread.RunJobs();
            var missing=window.GetVisualDescendants().OfType<Button>().Where(b=>b.GetType()==typeof(Button)&&b.IsEffectivelyVisible&&b.Name is null&&b.Content is string&&b.Command is null).Select(b=>b.Content).ToArray();
            if(missing.Length>0)throw new InvalidOperationException("Missing commands: "+string.Join(", ",missing));
        }
        async Task Capture(string name)
        {
            await WaitLoaded();window.Width=1024;window.Height=768;Dispatcher.UIThread.RunJobs();
            if(!root.Shell.IsAuthenticated&&window.GetVisualDescendants().OfType<Button>().Any(b=>b.IsEffectivelyVisible&&Equals(b.Content,"Menu")))throw new InvalidOperationException("Unauthenticated entry screens must not expose a dead account menu.");
            using var frame=window.CaptureRenderedFrame()!;frame.Save(Path.Combine(output,name+".png"),PngBitmapEncoderOptions.Default);
        }
        async Task Click(string label,bool wait=true)
        {
            Dispatcher.UIThread.RunJobs();var button=window.GetVisualDescendants().OfType<Button>().First(b=>b.IsEffectivelyVisible&&(Equals(b.Content,label)||b.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text==label)));
            button.BringIntoView();Dispatcher.UIThread.RunJobs();var point=button.TranslatePoint(new Point(button.Bounds.Width/2,button.Bounds.Height/2),window)!.Value;
            window.MouseMove(point);window.MouseDown(point,MouseButton.Left);window.MouseUp(point,MouseButton.Left);await Task.Delay(20);if(wait)await WaitLoaded();
        }
        void Check(bool condition,string failure){if(!condition)regressions.Add(failure);}
    }
    private static double Contrast(Color a,Color b)
    {
        static double Linear(byte channel){var v=channel/255d;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}
        static double L(Color c)=>.2126*Linear(c.R)+.7152*Linear(c.G)+.0722*Linear(c.B);
        return(Math.Max(L(a),L(b))+.05)/(Math.Min(L(a),L(b))+.05);
    }
    private sealed class BindingLog:ILogSink
    {
        public static int Count;
        public static int ExpectedNullCount;
        public bool IsEnabled(LogEventLevel level,string area)=>area==LogArea.Binding&&level>=LogEventLevel.Warning;
        public void Log(LogEventLevel level,string area,object? source,string template){if(IsEnabled(level,area)){Count++;Console.WriteLine("Binding diagnostic: "+template);}}
        public void Log(LogEventLevel level,string area,object? source,string template,params object?[] values)
        {
            if(!IsEnabled(level,area))return;
            if(values.Length>=4&&values[3]?.ToString()=="Value is null."&&values[1]?.ToString() is{} expression&&new[]{"CurrentPage.","Detail.","SelectedApplicant.","$parent[UserControl].DataContext."}.Any(expression.TrimStart('!').StartsWith)){ExpectedNullCount++;return;}
            Count++;
            var details=string.Join(" | ",values.Select(v=>v?.ToString()));
            foreach(var key in new[]{"ETC_CONNECTION_STRING","ETC_DEMO_PASSWORD","ETC_TEST_CONNECTION_STRING"})if(Environment.GetEnvironmentVariable(key) is{Length:>0} secret)details=details.Replace(secret,"[redacted]");
            Console.WriteLine("Binding diagnostic: "+details);
        }
    }
}
