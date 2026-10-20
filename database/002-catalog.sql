INSERT INTO Skills(Name)
SELECT v.Name FROM (VALUES(N'C#'),(N'SQL Server'),(N'Avalonia'),(N'UI design'),
 (N'Prototyping'),(N'HTML'),(N'CSS'),(N'JavaScript'),(N'Python'),(N'Project management')) v(Name)
WHERE NOT EXISTS(SELECT 1 FROM Skills s WHERE s.Name=v.Name);
