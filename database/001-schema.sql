IF OBJECT_ID('dbo.SchemaVersion') IS NULL
BEGIN
CREATE TABLE Students (
 Id int IDENTITY PRIMARY KEY, FullName nvarchar(120) NOT NULL,
 Email nvarchar(254) NOT NULL, NormalizedEmail nvarchar(254) NOT NULL UNIQUE,
 PasswordHash nvarchar(512) NOT NULL, Cohort int NULL CHECK(Cohort BETWEEN 1000 AND 9999),
 Bio nvarchar(2000) NOT NULL DEFAULT '', Experience nvarchar(max) NOT NULL DEFAULT '',
 PortfolioUrl nvarchar(2048) NULL, LinkedInUrl nvarchar(2048) NULL, Photo varbinary(max) NULL,
 IsDemo bit NOT NULL DEFAULT 0,
 CHECK(DATALENGTH(Photo) <= 2097152), CHECK(LEN(Experience)<=10000)
);
CREATE TABLE Skills (Id int IDENTITY PRIMARY KEY, Name nvarchar(80) NOT NULL UNIQUE);
CREATE TABLE StudentSkills (StudentId int NOT NULL REFERENCES Students(Id) ON DELETE CASCADE,
 SkillId int NOT NULL REFERENCES Skills(Id), PRIMARY KEY(StudentId,SkillId));
CREATE TABLE Recruitments (
 Id int IDENTITY PRIMARY KEY, OwnerId int NOT NULL REFERENCES Students(Id),
 Title nvarchar(200) NOT NULL, Category tinyint NOT NULL CHECK(Category IN (0,1)),
 ActivityName nvarchar(200) NOT NULL, Description nvarchar(max) NOT NULL CHECK(LEN(Description)<=10000),
 DeadlineUtc datetimeoffset NULL, StartsOn date NULL, EndsOn date NULL,
 Mode tinyint NULL CHECK(Mode IN (0,1,2)), Location nvarchar(500) NULL,
 FeeKind tinyint NOT NULL CHECK(FeeKind IN (0,1,2)), FeeAmount decimal(18,2) NULL CHECK(FeeAmount>=0),
 FeeNote nvarchar(2000) NULL, InfoUrl nvarchar(2048) NULL,
 ContactName nvarchar(120) NULL, ContactChannel nvarchar(500) NULL,
 Status tinyint NOT NULL DEFAULT 0 CHECK(Status IN (0,1,2)),
 PublishedAtUtc datetimeoffset NULL, UpdatedAtUtc datetimeoffset NOT NULL, IsDemo bit NOT NULL DEFAULT 0,
 CHECK(StartsOn IS NULL OR EndsOn IS NULL OR EndsOn>=StartsOn)
);
CREATE TABLE Positions (
 Id int IDENTITY PRIMARY KEY, RecruitmentId int NOT NULL REFERENCES Recruitments(Id),
 Name nvarchar(120) NOT NULL, Capacity int NOT NULL CHECK(Capacity>0), UNIQUE(Id,RecruitmentId)
);
CREATE TABLE PositionSkills (PositionId int NOT NULL REFERENCES Positions(Id),
 SkillId int NOT NULL REFERENCES Skills(Id), PRIMARY KEY(PositionId,SkillId));
CREATE TABLE Applications (
 Id int IDENTITY PRIMARY KEY, StudentId int NOT NULL REFERENCES Students(Id),
 RecruitmentId int NOT NULL REFERENCES Recruitments(Id), PositionId int NOT NULL,
 Message nvarchar(2000) NOT NULL, PortfolioUrl nvarchar(2048) NULL,
 Status tinyint NOT NULL DEFAULT 0 CHECK(Status IN(0,1,2,3)),
 SubmittedAtUtc datetimeoffset NOT NULL, DecidedAtUtc datetimeoffset NULL,
 UNIQUE(StudentId,RecruitmentId),
 FOREIGN KEY(PositionId,RecruitmentId) REFERENCES Positions(Id,RecruitmentId)
);
CREATE TABLE Bookmarks (
 StudentId int NOT NULL REFERENCES Students(Id), RecruitmentId int NOT NULL REFERENCES Recruitments(Id),
 PRIMARY KEY(StudentId,RecruitmentId)
);
CREATE TABLE Mentors (
 Id int IDENTITY PRIMARY KEY, Name nvarchar(120) NOT NULL, Photo varbinary(max) NULL,
 Experience nvarchar(max) NOT NULL, Achievements nvarchar(max) NOT NULL, IsDemo bit NOT NULL DEFAULT 0,
 CHECK(DATALENGTH(Photo)<=2097152)
);
CREATE TABLE MentorSkills (MentorId int NOT NULL REFERENCES Mentors(Id),
 SkillId int NOT NULL REFERENCES Skills(Id), PRIMARY KEY(MentorId,SkillId));
CREATE INDEX IX_Recruitments_Public ON Recruitments(Status,PublishedAtUtc DESC,Id DESC);
CREATE INDEX IX_Recruitments_Owner ON Recruitments(OwnerId,Status);
CREATE INDEX IX_Applications_Position ON Applications(PositionId,Status);
CREATE TABLE SchemaVersion (Version int PRIMARY KEY);
INSERT INTO SchemaVersion VALUES(1);
END;
