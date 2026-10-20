using Etc.Desktop.Features.Applications;
using Etc.Desktop.Features.Recruitments;
namespace Etc.Desktop.Features.Dashboard;
public sealed record DashboardStats(int ActiveRecruitments,int PendingApplications,int AcceptedApplications,int Bookmarks);
public sealed record DashboardData(DashboardStats Stats,IReadOnlyList<RecruitmentSummary> MyRecentRecruitments,IReadOnlyList<MyApplicationRow> RecentApplications);
