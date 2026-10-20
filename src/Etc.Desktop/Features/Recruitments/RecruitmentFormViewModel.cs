using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Features.Profiles;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Recruitments;
public sealed partial class PositionEditor:ObservableObject
{
    public int? Id{get;set;}
    [ObservableProperty]private string name="";
    [ObservableProperty]private string capacity="1";
    public IReadOnlyList<SkillChoice> Skills{get;init;}=[];
}
public sealed partial class RecruitmentFormViewModel(IRecruitmentService service,IProfileService profiles,SessionState session,int recruitmentId,Action<string,int> navigate):PageViewModel(session,"Form rekrutmen")
{
    private int? id=recruitmentId==0?null:recruitmentId;
    private IReadOnlyList<SkillOption> catalog=[];
    private DateTimeOffset? originalDeadline;
    [ObservableProperty]private string titleText="";
    [ObservableProperty]private string category="Lomba";
    [ObservableProperty]private string activityName="";
    [ObservableProperty]private string description="";
    [ObservableProperty]private DateTimeOffset? deadline;
    [ObservableProperty]private TimeSpan? deadlineTime=new(23,59,0);
    [ObservableProperty]private DateTimeOffset? startsOn;
    [ObservableProperty]private DateTimeOffset? endsOn;
    [ObservableProperty]private string mode="Online";
    [ObservableProperty]private string location="";
    [ObservableProperty]private string fee="Belum diketahui";
    [ObservableProperty]private string feeAmount="";
    [ObservableProperty]private string feeNote="";
    [ObservableProperty]private string infoUrl="";
    [ObservableProperty]private string contactName="";
    [ObservableProperty]private string contactChannel="";
    [ObservableProperty]private bool canEditPositions=true;
    [ObservableProperty]private bool canEditDeadline=true;
    [ObservableProperty]private bool isDraft=true;
    public ObservableCollection<PositionEditor> Positions{get;}=[];
    public IReadOnlyList<string> Categories{get;}=["Lomba","Proyek"];
    public IReadOnlyList<string> Modes{get;}=["Online","Offline","Hybrid"];
    public IReadOnlyList<string> Fees{get;}=["Belum diketahui","Gratis","Berbayar"];
    public bool IsPaid=>Fee=="Berbayar";
    public string SaveLabel=>IsDraft?"Simpan draft":"Simpan perubahan";
    partial void OnFeeChanged(string value)=>OnPropertyChanged(nameof(IsPaid));
    public override Task LoadAsync(CancellationToken ct=default)=>ReadAsync(async token=>(catalog:await profiles.ListSkillsAsync(token),detail:id.HasValue?await service.GetAsync(id.Value,token):null),x=>
    {
        catalog=x.catalog;Positions.Clear();
        if(x.detail is { } d)
        {
            if(d.OwnerId!=Session.RequireStudentId()){ErrorMessage="Anda tidak dapat mengedit rekrutmen ini.";CanEditPositions=false;return;}
            var v=d.Data;TitleText=v.Title;Category=v.Category==RecruitmentCategory.Competition?"Lomba":"Proyek";ActivityName=v.ActivityName;Description=v.Description;
            originalDeadline=v.DeadlineUtc;Deadline=v.DeadlineUtc?.ToOffset(TimeSpan.FromHours(7));DeadlineTime=Deadline?.TimeOfDay??new(23,59,0);StartsOn=AsDate(v.StartsOn);EndsOn=AsDate(v.EndsOn);Mode=v.Mode?.ToString()??"Online";Location=v.Location??"";
            Fee=v.FeeKind==FeeKind.Free?"Gratis":v.FeeKind==FeeKind.Paid?"Berbayar":"Belum diketahui";FeeAmount=v.FeeAmount?.ToString(CultureInfo.GetCultureInfo("id-ID"))??"";FeeNote=v.FeeNote??"";InfoUrl=v.InfoUrl??"";ContactName=v.ContactName??"";ContactChannel=v.ContactChannel??"";
            IsDraft=d.Status==RecruitmentStatus.Draft;CanEditPositions=!d.HasApplications;CanEditDeadline=IsDraft||d.IsAvailable;
            foreach(var p in v.Positions)Positions.Add(Editor(p));
        }
        else{ContactName=Session.Current?.FullName??"";Positions.Add(Editor(null));}
        OnPropertyChanged(nameof(SaveLabel));
    },ct);
    private static DateTimeOffset? AsDate(DateOnly? value)=>value.HasValue?new DateTimeOffset(value.Value.ToDateTime(TimeOnly.MinValue),TimeSpan.FromHours(7)):null;
    private PositionEditor Editor(PositionInput? p)=>new(){Id=p?.Id,Name=p?.Name??"",Capacity=(p?.Capacity??1).ToString(),Skills=catalog.Select(s=>new SkillChoice(s.Id,s.Name,p?.SkillIds.Contains(s.Id)==true)).ToArray()};
    [RelayCommand]private void AddPosition(){if(CanEditPositions)Positions.Add(Editor(null));}
    [RelayCommand]private void RemovePosition(PositionEditor row){if(CanEditPositions)Positions.Remove(row);}
    private RecruitmentInput Input()
    {
        var positions=new List<PositionInput>();foreach(var p in Positions)
        {
            if(string.IsNullOrWhiteSpace(p.Name)&&!p.Skills.Any(s=>s.IsSelected))continue;
            if(!int.TryParse(p.Capacity,out var capacity))throw new DomainException("Validation","Kuota harus berupa bilangan bulat positif.");
            positions.Add(new(p.Id,p.Name,capacity,p.Skills.Where(s=>s.IsSelected).Select(s=>s.Id).ToArray()));
        }
        decimal? amount=null;if(IsPaid){if(!decimal.TryParse(FeeAmount,NumberStyles.Number,CultureInfo.GetCultureInfo("id-ID"),out var value)||decimal.Round(value,2)!=value)throw new DomainException("Validation","Masukkan biaya dengan maksimal dua angka desimal.");amount=value;}
        DateTimeOffset? end=Deadline is { } date?new DateTimeOffset(date.Year,date.Month,date.Day,(DeadlineTime??new(23,59,0)).Hours,(DeadlineTime??new(23,59,0)).Minutes,0,TimeSpan.FromHours(7)):null;
        if(originalDeadline is{} original)
        {
            // Time controls show minutes; preserve seconds/fractions when that minute is unchanged.
            var minute=original.AddTicks(-(original.Ticks%TimeSpan.TicksPerMinute));if(end==minute)end=original;
        }
        return new(TitleText,Category=="Lomba"?RecruitmentCategory.Competition:RecruitmentCategory.Project,ActivityName,Description,end,StartsOn is {} start?DateOnly.FromDateTime(start.Date):null,EndsOn is {} finish?DateOnly.FromDateTime(finish.Date):null,Enum.Parse<ActivityMode>(Mode),Location,Fee=="Gratis"?FeeKind.Free:IsPaid?FeeKind.Paid:FeeKind.Unknown,amount,FeeNote,InfoUrl,ContactName,ContactChannel,positions);
    }
    [RelayCommand]public Task SaveAsync()=>Save(false);
    [RelayCommand]private Task PublishAsync()=>Save(true);
    private Task Save(bool publish)=>RunAsync(async ct=>
    {
        var input=Input();if(IsDraft){id=await service.SaveDraftAsync(id,input,ct);if(publish)await service.PublishAsync(id.Value,ct);}else await service.UpdatePublishedAsync(id!.Value,input,ct);
        Apply(ct,()=>{InfoMessage=publish?"Rekrutmen berhasil dipublikasikan.":IsDraft?"Draft rekrutmen berhasil disimpan.":"Perubahan rekrutmen berhasil disimpan.";navigate("detail",id!.Value);});
    });
}
