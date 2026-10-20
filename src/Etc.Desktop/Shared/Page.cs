namespace Etc.Desktop.Shared;
public sealed record Page<T>(IReadOnlyList<T> Items, int Total, int PageNumber, int PageSize = 12)
{
    public int Pages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}
