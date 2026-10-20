IF COL_LENGTH('dbo.Mentors','StudentNumber') IS NULL
    ALTER TABLE Mentors ADD StudentNumber nvarchar(20) NULL;
IF COL_LENGTH('dbo.Mentors','Cohort') IS NULL
    ALTER TABLE Mentors ADD Cohort int NULL CHECK(Cohort BETWEEN 1000 AND 9999);
IF COL_LENGTH('dbo.Mentors','Focus') IS NULL
    ALTER TABLE Mentors ADD Focus nvarchar(100) NOT NULL CONSTRAINT DF_Mentors_Focus DEFAULT '';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Mentors') AND name='UX_Mentors_StudentNumber')
    EXEC(N'CREATE UNIQUE INDEX UX_Mentors_StudentNumber ON Mentors(StudentNumber) WHERE StudentNumber IS NOT NULL');
IF NOT EXISTS(SELECT 1 FROM SchemaVersion WHERE Version=3)
    INSERT SchemaVersion VALUES(3);
