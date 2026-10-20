using Etc.Desktop.Infrastructure;
using Etc.Desktop.Shared;
using Microsoft.Data.SqlClient;
using SkiaSharp;
namespace Etc.Desktop.Demo;

public static class MentorSeedData
{
    private sealed record Entry(string Name,int Cohort,string StudentNumber,string[] Skills,string Focus,string Experience,string PhotoFile);
    private static readonly Lazy<(Entry[] Mentors,string FormUrl)> directory=new(ReadDirectory);
    public static string FormUrl=>directory.Value.FormUrl;

    private static (Entry[],string) ReadDirectory()
    {
        using var stream=typeof(MentorSeedData).Assembly.GetManifestResourceStream("MentorDirectory.md")??throw new InvalidOperationException("Data mentor tidak ditemukan.");
        using var reader=new StreamReader(stream);var rows=new List<Entry>();string? form=null;
        while(reader.ReadLine() is{} line)
        {
            if(line.StartsWith("Link form:",StringComparison.Ordinal))form=line[10..].Trim();
            if(!line.StartsWith('|'))continue;
            var cells=line.Trim().Trim('|').Split('|',StringSplitOptions.TrimEntries);
            if(!int.TryParse(cells[0],out _))continue;
            if(cells.Length!=8)throw new InvalidOperationException("Kolom tabel mentor tidak lengkap.");
            rows.Add(new(cells[1],int.Parse(cells[2]),cells[3],cells[4].Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries),cells[5],cells[6],Path.GetFileName(cells[7])));
        }
        if(rows.Count==0||ExternalLinkService.Validate(form) is null)throw new InvalidOperationException("Data atau tautan formulir mentor belum lengkap.");
        return(rows.ToArray(),form!);
    }

    private static byte[] ReadPhoto(string file)
    {
        using var stream=typeof(MentorSeedData).Assembly.GetManifestResourceStream("MentorPhotos/"+file)??throw new InvalidOperationException($"Foto mentor tidak ditemukan: {file}");
        using var original=SKBitmap.Decode(stream)??throw new InvalidOperationException($"Foto mentor tidak dapat dibaca: {file}");
        var scale=Math.Min(1d,960d/Math.Max(original.Width,original.Height));
        using var bitmap=new SKBitmap(Math.Max(1,(int)(original.Width*scale)),Math.Max(1,(int)(original.Height*scale)));
        using(var canvas=new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);using var image=SKImage.FromBitmap(original);
            canvas.DrawImage(image,new SKRect(0,0,bitmap.Width,bitmap.Height),new SKSamplingOptions(SKFilterMode.Linear));
        }
        using var result=SKImage.FromBitmap(bitmap);using var encoded=result.Encode(SKEncodedImageFormat.Jpeg,88);
        var bytes=encoded.ToArray();Features.Profiles.PhotoValidator.Validate(bytes);return bytes;
    }

    internal static async Task SeedAsync(SqlConnection c,SqlTransaction tx,CancellationToken ct)
    {
        await using(var cleanup=Db.Command(c,tx,"DELETE ms FROM MentorSkills ms JOIN Mentors m ON m.Id=ms.MentorId WHERE m.IsDemo=1 AND m.StudentNumber IS NULL AND m.Name IN(N'Ayu (mentor demo)',N'Bima (mentor demo)'); DELETE Mentors WHERE IsDemo=1 AND StudentNumber IS NULL AND Name IN(N'Ayu (mentor demo)',N'Bima (mentor demo)');"))await cleanup.ExecuteNonQueryAsync(ct);
        foreach(var row in directory.Value.Mentors)
        {
            ct.ThrowIfCancellationRequested();var photo=ReadPhoto(row.PhotoFile);
            await using var upsert=Db.Command(c,tx,"DECLARE @id int=(SELECT Id FROM Mentors WHERE StudentNumber=@number); IF @id IS NULL BEGIN INSERT Mentors(Name,StudentNumber,Cohort,Focus,Photo,Experience,Achievements,IsDemo) VALUES(@name,@number,@cohort,@focus,@photo,@experience,N'',0); SET @id=CONVERT(int,SCOPE_IDENTITY()); END ELSE UPDATE Mentors SET Name=@name,Cohort=@cohort,Focus=@focus,Photo=@photo,Experience=@experience,IsDemo=0 WHERE Id=@id; DELETE MentorSkills WHERE MentorId=@id; SELECT @id;",("number",row.StudentNumber),("name",row.Name),("cohort",row.Cohort),("focus",row.Focus),("photo",photo),("experience",row.Experience));
            var id=Convert.ToInt32(await upsert.ExecuteScalarAsync(ct));
            foreach(var skill in row.Skills.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await using var insert=Db.Command(c,tx,"IF NOT EXISTS(SELECT 1 FROM Skills WHERE Name=@skill) INSERT Skills(Name) VALUES(@skill); INSERT MentorSkills SELECT @mentor,Id FROM Skills WHERE Name=@skill;",("skill",skill),("mentor",id));
                await insert.ExecuteNonQueryAsync(ct);
            }
        }
    }
}
