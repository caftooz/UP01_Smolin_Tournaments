/*
  DB_Smolin_Tournaments — полный скрипт базы данных (УП.01.01, тема ПР2
  «Платформа организации киберспортивных турниров»).

  Один файл вместо трёх: schema.sql + 02_cascade.sql + 03_test_data.sql.
    1) удаляет базу DB_Smolin_Tournaments, если она уже есть (ВСЕ данные в ней пропадут!);
    2) создаёт 27 таблиц по ER-модели ПР2 с ключами, ограничениями и каскадным удалением;
    3) заполняет справочники и тестовые данные.

  Запуск: SSMS -> открыть файл -> F5. Перед запуском закройте WPF-приложение.
  Учётные записи: admin / Admin123 (Администратор); остальные тестовые — пароль Test123.
  Хэш пароля = SHA-256(PasswordSalt + пароль), как в PasswordHelper.GetHash.
*/
USE master;
GO
IF DB_ID(N'DB_Smolin_Tournaments') IS NOT NULL
BEGIN
    ALTER DATABASE DB_Smolin_Tournaments SET SINGLE_USER WITH ROLLBACK IMMEDIATE;  -- закрыть чужие подключения
    DROP DATABASE DB_Smolin_Tournaments;
END;
GO
CREATE DATABASE DB_Smolin_Tournaments;
GO
USE DB_Smolin_Tournaments;
GO
SET NOCOUNT ON;
GO

/* =====================================================================
   1. ТАБЛИЦЫ
   Правила удаления (требование задания №10 — каскадное удаление):
   - ON DELETE CASCADE  — удаление турнира удаляет заявки, участников, призы,
     спонсорские договоры, стадии -> группы, матчи -> карты матча -> статистику;
     удаление пользователя — его уведомления; удаление дисциплины — её карты;
   - ON DELETE SET NULL — нарушения и журнал остаются, ссылка обнуляется;
   - справочники (дисциплины, форматы, статусы, площадки, роли) — NO ACTION:
     их нельзя удалить, пока на них ссылаются турниры.
   К одной таблице не ведут два каскадных пути (иначе SQL Server выдаёт ошибку 1785),
   поэтому FK_TournamentParticipants_Groups оставлен NO ACTION.
   ===================================================================== */
CREATE TABLE dbo.Roles (
    RoleID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    Name nvarchar(50) NOT NULL CONSTRAINT UQ_Roles_Name UNIQUE,
    Description nvarchar(255) NULL,
    AccessLevel int NOT NULL CONSTRAINT DF_Roles_AccessLevel DEFAULT(1),
    CONSTRAINT CK_Roles_AccessLevel CHECK (AccessLevel BETWEEN 1 AND 6)
);
GO
CREATE TABLE dbo.Disciplines (
    DisciplineID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Disciplines PRIMARY KEY,
    Name nvarchar(100) NOT NULL CONSTRAINT UQ_Disciplines_Name UNIQUE,
    Platform nvarchar(50) NOT NULL,
    TeamSize int NOT NULL CONSTRAINT DF_Disciplines_TeamSize DEFAULT(5),
    AgeRating int NOT NULL CONSTRAINT DF_Disciplines_AgeRating DEFAULT(0),
    Description nvarchar(500) NULL,
    LogoPath nvarchar(255) NULL,
    CONSTRAINT CK_Disciplines_TeamSize CHECK (TeamSize BETWEEN 1 AND 100),
    CONSTRAINT CK_Disciplines_AgeRating CHECK (AgeRating BETWEEN 0 AND 21)
);
GO
CREATE TABLE dbo.Maps (
    MapID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Maps PRIMARY KEY,
    DisciplineID int NOT NULL,
    Name nvarchar(100) NOT NULL,
    MapType nvarchar(50) NULL,
    ImagePath nvarchar(255) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Maps_IsActive DEFAULT(1),
    CONSTRAINT FK_Maps_Disciplines FOREIGN KEY (DisciplineID) REFERENCES dbo.Disciplines(DisciplineID) ON DELETE CASCADE,
    CONSTRAINT UQ_Maps_Discipline_Name UNIQUE (DisciplineID, Name)
);
GO
CREATE TABLE dbo.Venues (
    VenueID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Venues PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    VenueType nvarchar(50) NOT NULL,
    Address nvarchar(255) NULL,
    SeatCount int NOT NULL CONSTRAINT DF_Venues_SeatCount DEFAULT(0),
    ContactPerson nvarchar(100) NULL,
    Phone nvarchar(20) NULL,
    CONSTRAINT CK_Venues_SeatCount CHECK (SeatCount >= 0)
);
GO
CREATE TABLE dbo.TournamentFormats (
    TournamentFormatID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TournamentFormats PRIMARY KEY,
    Name nvarchar(100) NOT NULL CONSTRAINT UQ_TournamentFormats_Name UNIQUE,
    Description nvarchar(500) NULL
);
GO
CREATE TABLE dbo.TournamentStatuses (
    TournamentStatusID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TournamentStatuses PRIMARY KEY,
    Name nvarchar(50) NOT NULL CONSTRAINT UQ_TournamentStatuses_Name UNIQUE
);
GO
CREATE TABLE dbo.MatchStatuses (
    MatchStatusID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MatchStatuses PRIMARY KEY,
    Name nvarchar(50) NOT NULL CONSTRAINT UQ_MatchStatuses_Name UNIQUE
);
GO
CREATE TABLE dbo.ViolationTypes (
    ViolationTypeID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ViolationTypes PRIMARY KEY,
    Name nvarchar(100) NOT NULL CONSTRAINT UQ_ViolationTypes_Name UNIQUE,
    DefaultSanction nvarchar(255) NULL,
    PenaltyPoints int NOT NULL CONSTRAINT DF_ViolationTypes_PenaltyPoints DEFAULT(0),
    CONSTRAINT CK_ViolationTypes_PenaltyPoints CHECK (PenaltyPoints >= 0)
);
GO
CREATE TABLE dbo.Users (
    UserID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Login nvarchar(50) NOT NULL CONSTRAINT UQ_Users_Login UNIQUE,
    PasswordHash nvarchar(255) NOT NULL,
    PasswordSalt nvarchar(64) NOT NULL,
    LastName nvarchar(50) NOT NULL,
    FirstName nvarchar(50) NOT NULL,
    MiddleName nvarchar(50) NULL,
    BirthDate date NOT NULL,
    Email nvarchar(100) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    Phone nvarchar(20) NULL,
    RoleID int NOT NULL,
    RegisteredAt datetime2 NOT NULL CONSTRAINT DF_Users_RegisteredAt DEFAULT(SYSDATETIME()),
    LastLoginAt datetime2 NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT(1),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID)
);
GO
CREATE TABLE dbo.Notifications (
    NotificationID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY,
    UserID int NOT NULL,
    Title nvarchar(150) NOT NULL,
    Body nvarchar(1000) NOT NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT(SYSDATETIME()),
    IsRead bit NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT(0),
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID) ON DELETE CASCADE
);
GO
CREATE TABLE dbo.AuditLog (
    LogID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLog PRIMARY KEY,
    UserID int NULL,
    Action nvarchar(50) NOT NULL,
    TableName nvarchar(100) NULL,
    RecordID int NULL,
    OccurredAt datetime2 NOT NULL CONSTRAINT DF_AuditLog_OccurredAt DEFAULT(SYSDATETIME()),
    IPAddress nvarchar(45) NULL,
    CONSTRAINT FK_AuditLog_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID) ON DELETE SET NULL
);
GO
CREATE TABLE dbo.Players (
    PlayerID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Players PRIMARY KEY,
    UserID int NOT NULL CONSTRAINT UQ_Players_UserID UNIQUE,
    Nickname nvarchar(50) NOT NULL,
    GameAccountID nvarchar(100) NULL,
    Rating int NOT NULL CONSTRAINT DF_Players_Rating DEFAULT(1000),
    MatchesPlayed int NOT NULL CONSTRAINT DF_Players_MatchesPlayed DEFAULT(0),
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Players_CreatedAt DEFAULT(SYSDATETIME()),
    CONSTRAINT FK_Players_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT CK_Players_Rating CHECK (Rating >= 0),
    CONSTRAINT CK_Players_MatchesPlayed CHECK (MatchesPlayed >= 0)
);
GO
CREATE TABLE dbo.Referees (
    RefereeID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Referees PRIMARY KEY,
    UserID int NOT NULL CONSTRAINT UQ_Referees_UserID UNIQUE,
    Qualification nvarchar(100) NULL,
    AppointedDate date NOT NULL,
    MatchesOfficiated int NOT NULL CONSTRAINT DF_Referees_MatchesOfficiated DEFAULT(0),
    CONSTRAINT FK_Referees_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT CK_Referees_MatchesOfficiated CHECK (MatchesOfficiated >= 0)
);
GO
CREATE TABLE dbo.Teams (
    TeamID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Teams PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Tag nvarchar(10) NOT NULL,
    DisciplineID int NOT NULL,
    CaptainID int NOT NULL,
    FoundedDate date NOT NULL,
    LogoPath nvarchar(255) NULL,
    Description nvarchar(500) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Teams_IsActive DEFAULT(1),
    CONSTRAINT FK_Teams_Disciplines FOREIGN KEY (DisciplineID) REFERENCES dbo.Disciplines(DisciplineID),
    CONSTRAINT FK_Teams_Captain FOREIGN KEY (CaptainID) REFERENCES dbo.Players(PlayerID),
    CONSTRAINT UQ_Teams_Name_Discipline UNIQUE (Name, DisciplineID),
    CONSTRAINT UQ_Teams_Tag_Discipline UNIQUE (Tag, DisciplineID)
);
GO
CREATE TABLE dbo.TeamMembers (
    TeamMemberID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeamMembers PRIMARY KEY,
    TeamID int NOT NULL,
    PlayerID int NOT NULL,
    TeamRole nvarchar(50) NULL,
    JoinedDate date NOT NULL,
    LeftDate date NULL,
    IsStarter bit NOT NULL CONSTRAINT DF_TeamMembers_IsStarter DEFAULT(1),
    CONSTRAINT FK_TeamMembers_Teams FOREIGN KEY (TeamID) REFERENCES dbo.Teams(TeamID),
    CONSTRAINT FK_TeamMembers_Players FOREIGN KEY (PlayerID) REFERENCES dbo.Players(PlayerID),
    CONSTRAINT CK_TeamMembers_Dates CHECK (LeftDate IS NULL OR LeftDate >= JoinedDate)
);
GO
CREATE TABLE dbo.Tournaments (
    TournamentID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tournaments PRIMARY KEY,
    Name nvarchar(150) NOT NULL,
    DisciplineID int NOT NULL,
    TournamentFormatID int NOT NULL,
    TournamentStatusID int NOT NULL,
    OrganizerID int NOT NULL,
    VenueID int NULL,
    IsOnline bit NOT NULL CONSTRAINT DF_Tournaments_IsOnline DEFAULT(0),
    RegistrationStart datetime2 NOT NULL,
    RegistrationEnd datetime2 NOT NULL,
    StartDate datetime2 NOT NULL,
    EndDate datetime2 NULL,
    MaxTeams int NOT NULL CONSTRAINT DF_Tournaments_MaxTeams DEFAULT(8),
    EntryFee decimal(10,2) NOT NULL CONSTRAINT DF_Tournaments_EntryFee DEFAULT(0),
    PrizePool decimal(10,2) NOT NULL CONSTRAINT DF_Tournaments_PrizePool DEFAULT(0),
    Rules nvarchar(max) NULL,
    Description nvarchar(1000) NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Tournaments_CreatedAt DEFAULT(SYSDATETIME()),
    CONSTRAINT FK_Tournaments_Disciplines FOREIGN KEY (DisciplineID) REFERENCES dbo.Disciplines(DisciplineID),
    CONSTRAINT FK_Tournaments_Formats FOREIGN KEY (TournamentFormatID) REFERENCES dbo.TournamentFormats(TournamentFormatID),
    CONSTRAINT FK_Tournaments_Statuses FOREIGN KEY (TournamentStatusID) REFERENCES dbo.TournamentStatuses(TournamentStatusID),
    CONSTRAINT FK_Tournaments_Organizer FOREIGN KEY (OrganizerID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Tournaments_Venues FOREIGN KEY (VenueID) REFERENCES dbo.Venues(VenueID),
    CONSTRAINT CK_Tournaments_Dates CHECK (RegistrationStart <= RegistrationEnd AND RegistrationEnd <= StartDate AND (EndDate IS NULL OR EndDate >= StartDate)),
    CONSTRAINT CK_Tournaments_MaxTeams CHECK (MaxTeams BETWEEN 2 AND 128),
    CONSTRAINT CK_Tournaments_EntryFee CHECK (EntryFee >= 0),
    CONSTRAINT CK_Tournaments_PrizePool CHECK (PrizePool >= 0)
);
GO
CREATE TABLE dbo.Applications (
    ApplicationID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Applications PRIMARY KEY,
    TournamentID int NOT NULL,
    TeamID int NOT NULL,
    SubmittedAt datetime2 NOT NULL CONSTRAINT DF_Applications_SubmittedAt DEFAULT(SYSDATETIME()),
    Status nvarchar(30) NOT NULL CONSTRAINT DF_Applications_Status DEFAULT(N'Подана'),
    ReviewedBy int NULL,
    ReviewedAt datetime2 NULL,
    Comment nvarchar(500) NULL,
    IsFeePaid bit NOT NULL CONSTRAINT DF_Applications_IsFeePaid DEFAULT(0),
    CONSTRAINT FK_Applications_Tournaments FOREIGN KEY (TournamentID) REFERENCES dbo.Tournaments(TournamentID) ON DELETE CASCADE,
    CONSTRAINT FK_Applications_Teams FOREIGN KEY (TeamID) REFERENCES dbo.Teams(TeamID),
    CONSTRAINT FK_Applications_ReviewedBy FOREIGN KEY (ReviewedBy) REFERENCES dbo.Users(UserID),
    CONSTRAINT UQ_Applications_Tournament_Team UNIQUE (TournamentID, TeamID),
    CONSTRAINT CK_Applications_Status CHECK (Status IN (N'Подана',N'Принята',N'Отклонена',N'Отозвана'))
);
GO
CREATE TABLE dbo.Stages (
    StageID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Stages PRIMARY KEY,
    TournamentID int NOT NULL,
    Name nvarchar(100) NOT NULL,
    StageType nvarchar(50) NOT NULL,
    StageOrder int NOT NULL,
    StartDate date NULL,
    EndDate date NULL,
    CONSTRAINT FK_Stages_Tournaments FOREIGN KEY (TournamentID) REFERENCES dbo.Tournaments(TournamentID) ON DELETE CASCADE,
    CONSTRAINT UQ_Stages_Tournament_Order UNIQUE (TournamentID, StageOrder),
    CONSTRAINT CK_Stages_Order CHECK (StageOrder >= 1),
    CONSTRAINT CK_Stages_Dates CHECK (StartDate IS NULL OR EndDate IS NULL OR EndDate >= StartDate)
);
GO
CREATE TABLE dbo.TournamentGroups (
    GroupID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TournamentGroups PRIMARY KEY,
    StageID int NOT NULL,
    Name nvarchar(50) NOT NULL,
    CONSTRAINT FK_TournamentGroups_Stages FOREIGN KEY (StageID) REFERENCES dbo.Stages(StageID) ON DELETE CASCADE,
    CONSTRAINT UQ_TournamentGroups_Stage_Name UNIQUE (StageID, Name)
);
GO
CREATE TABLE dbo.TournamentParticipants (
    ParticipantID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TournamentParticipants PRIMARY KEY,
    TournamentID int NOT NULL,
    TeamID int NOT NULL,
    GroupID int NULL,
    Seed int NULL,
    Points int NOT NULL CONSTRAINT DF_TournamentParticipants_Points DEFAULT(0),
    FinalPlace int NULL,
    IsDisqualified bit NOT NULL CONSTRAINT DF_TournamentParticipants_IsDisqualified DEFAULT(0),
    CONSTRAINT FK_TournamentParticipants_Tournaments FOREIGN KEY (TournamentID) REFERENCES dbo.Tournaments(TournamentID) ON DELETE CASCADE,
    CONSTRAINT FK_TournamentParticipants_Teams FOREIGN KEY (TeamID) REFERENCES dbo.Teams(TeamID),
    CONSTRAINT FK_TournamentParticipants_Groups FOREIGN KEY (GroupID) REFERENCES dbo.TournamentGroups(GroupID),
    CONSTRAINT UQ_TournamentParticipants_Tournament_Team UNIQUE (TournamentID, TeamID),
    CONSTRAINT CK_TournamentParticipants_Points CHECK (Points >= 0),
    CONSTRAINT CK_TournamentParticipants_Seed CHECK (Seed IS NULL OR Seed >= 1),
    CONSTRAINT CK_TournamentParticipants_FinalPlace CHECK (FinalPlace IS NULL OR FinalPlace >= 1)
);
GO
CREATE TABLE dbo.Matches (
    MatchID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Matches PRIMARY KEY,
    StageID int NOT NULL,
    RoundNumber int NOT NULL CONSTRAINT DF_Matches_RoundNumber DEFAULT(1),
    Team1ID int NOT NULL,
    Team2ID int NOT NULL,
    RefereeID int NULL,
    MatchStatusID int NOT NULL,
    ScheduledAt datetime2 NOT NULL,
    StartedAt datetime2 NULL,
    SeriesFormat nvarchar(10) NOT NULL CONSTRAINT DF_Matches_SeriesFormat DEFAULT(N'BO1'),
    Team1Score int NOT NULL CONSTRAINT DF_Matches_Team1Score DEFAULT(0),
    Team2Score int NOT NULL CONSTRAINT DF_Matches_Team2Score DEFAULT(0),
    WinnerTeamID int NULL,
    Comment nvarchar(500) NULL,
    CONSTRAINT FK_Matches_Stages FOREIGN KEY (StageID) REFERENCES dbo.Stages(StageID) ON DELETE CASCADE,
    CONSTRAINT FK_Matches_Team1 FOREIGN KEY (Team1ID) REFERENCES dbo.Teams(TeamID),
    CONSTRAINT FK_Matches_Team2 FOREIGN KEY (Team2ID) REFERENCES dbo.Teams(TeamID),
    CONSTRAINT FK_Matches_Referees FOREIGN KEY (RefereeID) REFERENCES dbo.Referees(RefereeID),
    CONSTRAINT FK_Matches_Statuses FOREIGN KEY (MatchStatusID) REFERENCES dbo.MatchStatuses(MatchStatusID),
    CONSTRAINT FK_Matches_Winner FOREIGN KEY (WinnerTeamID) REFERENCES dbo.Teams(TeamID),
    CONSTRAINT CK_Matches_Teams CHECK (Team1ID <> Team2ID),
    CONSTRAINT CK_Matches_Round CHECK (RoundNumber >= 1),
    CONSTRAINT CK_Matches_Scores CHECK (Team1Score >= 0 AND Team2Score >= 0),
    CONSTRAINT CK_Matches_SeriesFormat CHECK (SeriesFormat IN (N'BO1',N'BO3',N'BO5'))
);
GO
CREATE TABLE dbo.MatchMaps (
    MatchMapID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MatchMaps PRIMARY KEY,
    MatchID int NOT NULL,
    MapID int NOT NULL,
    MapOrder int NOT NULL,
    Team1Score int NOT NULL CONSTRAINT DF_MatchMaps_Team1Score DEFAULT(0),
    Team2Score int NOT NULL CONSTRAINT DF_MatchMaps_Team2Score DEFAULT(0),
    DurationMinutes int NULL,
    CONSTRAINT FK_MatchMaps_Matches FOREIGN KEY (MatchID) REFERENCES dbo.Matches(MatchID) ON DELETE CASCADE,
    CONSTRAINT FK_MatchMaps_Maps FOREIGN KEY (MapID) REFERENCES dbo.Maps(MapID),
    CONSTRAINT UQ_MatchMaps_Match_Order UNIQUE (MatchID, MapOrder),
    CONSTRAINT CK_MatchMaps_Order CHECK (MapOrder >= 1),
    CONSTRAINT CK_MatchMaps_Scores CHECK (Team1Score >= 0 AND Team2Score >= 0),
    CONSTRAINT CK_MatchMaps_Duration CHECK (DurationMinutes IS NULL OR DurationMinutes > 0)
);
GO
CREATE TABLE dbo.PlayerMatchStats (
    StatID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlayerMatchStats PRIMARY KEY,
    MatchMapID int NOT NULL,
    PlayerID int NOT NULL,
    Kills int NOT NULL CONSTRAINT DF_PlayerMatchStats_Kills DEFAULT(0),
    Deaths int NOT NULL CONSTRAINT DF_PlayerMatchStats_Deaths DEFAULT(0),
    Assists int NOT NULL CONSTRAINT DF_PlayerMatchStats_Assists DEFAULT(0),
    Score int NOT NULL CONSTRAINT DF_PlayerMatchStats_Score DEFAULT(0),
    IsMVP bit NOT NULL CONSTRAINT DF_PlayerMatchStats_IsMVP DEFAULT(0),
    CONSTRAINT FK_PlayerMatchStats_MatchMaps FOREIGN KEY (MatchMapID) REFERENCES dbo.MatchMaps(MatchMapID) ON DELETE CASCADE,
    CONSTRAINT FK_PlayerMatchStats_Players FOREIGN KEY (PlayerID) REFERENCES dbo.Players(PlayerID),
    CONSTRAINT UQ_PlayerMatchStats_Map_Player UNIQUE (MatchMapID, PlayerID),
    CONSTRAINT CK_PlayerMatchStats_Values CHECK (Kills >= 0 AND Deaths >= 0 AND Assists >= 0 AND Score >= 0)
);
GO
CREATE TABLE dbo.Violations (
    ViolationID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Violations PRIMARY KEY,
    ViolationTypeID int NOT NULL,
    MatchID int NULL,
    TeamID int NOT NULL,
    PlayerID int NULL,
    RefereeID int NOT NULL,
    RecordedAt datetime2 NOT NULL CONSTRAINT DF_Violations_RecordedAt DEFAULT(SYSDATETIME()),
    Description nvarchar(500) NULL,
    AppliedSanction nvarchar(255) NULL,
    CONSTRAINT FK_Violations_Types FOREIGN KEY (ViolationTypeID) REFERENCES dbo.ViolationTypes(ViolationTypeID),
    CONSTRAINT FK_Violations_Matches FOREIGN KEY (MatchID) REFERENCES dbo.Matches(MatchID) ON DELETE SET NULL,
    CONSTRAINT FK_Violations_Teams FOREIGN KEY (TeamID) REFERENCES dbo.Teams(TeamID),
    CONSTRAINT FK_Violations_Players FOREIGN KEY (PlayerID) REFERENCES dbo.Players(PlayerID),
    CONSTRAINT FK_Violations_Referees FOREIGN KEY (RefereeID) REFERENCES dbo.Referees(RefereeID)
);
GO
CREATE TABLE dbo.Prizes (
    PrizeID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Prizes PRIMARY KEY,
    TournamentID int NOT NULL,
    Place int NOT NULL,
    Amount decimal(10,2) NOT NULL CONSTRAINT DF_Prizes_Amount DEFAULT(0),
    NonCashPrize nvarchar(255) NULL,
    AwardedDate date NULL,
    CONSTRAINT FK_Prizes_Tournaments FOREIGN KEY (TournamentID) REFERENCES dbo.Tournaments(TournamentID) ON DELETE CASCADE,
    CONSTRAINT UQ_Prizes_Tournament_Place UNIQUE (TournamentID, Place),
    CONSTRAINT CK_Prizes_Place CHECK (Place >= 1),
    CONSTRAINT CK_Prizes_Amount CHECK (Amount >= 0)
);
GO
CREATE TABLE dbo.Sponsors (
    SponsorID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sponsors PRIMARY KEY,
    Name nvarchar(150) NOT NULL,
    ContactPerson nvarchar(100) NULL,
    Phone nvarchar(20) NULL,
    Email nvarchar(100) NULL,
    Website nvarchar(255) NULL,
    LogoPath nvarchar(255) NULL
);
GO
CREATE TABLE dbo.TournamentSponsors (
    TournamentSponsorID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TournamentSponsors PRIMARY KEY,
    TournamentID int NOT NULL,
    SponsorID int NOT NULL,
    ContributionAmount decimal(10,2) NOT NULL CONSTRAINT DF_TournamentSponsors_Contribution DEFAULT(0),
    SupportType nvarchar(100) NULL,
    ContractNumber nvarchar(50) NULL,
    CONSTRAINT FK_TournamentSponsors_Tournaments FOREIGN KEY (TournamentID) REFERENCES dbo.Tournaments(TournamentID) ON DELETE CASCADE,
    CONSTRAINT FK_TournamentSponsors_Sponsors FOREIGN KEY (SponsorID) REFERENCES dbo.Sponsors(SponsorID),
    CONSTRAINT UQ_TournamentSponsors_Tournament_Sponsor UNIQUE (TournamentID, SponsorID),
    CONSTRAINT CK_TournamentSponsors_Contribution CHECK (ContributionAmount >= 0)
);
GO

/* Индексы на часто используемые внешние ключи и даты */
CREATE INDEX IX_Tournaments_StartDate ON dbo.Tournaments(StartDate);
CREATE INDEX IX_Tournaments_DisciplineID ON dbo.Tournaments(DisciplineID);
CREATE INDEX IX_Tournaments_StatusID ON dbo.Tournaments(TournamentStatusID);
CREATE INDEX IX_Matches_ScheduledAt ON dbo.Matches(ScheduledAt);
CREATE INDEX IX_Matches_StageID ON dbo.Matches(StageID);
CREATE INDEX IX_Applications_TournamentID ON dbo.Applications(TournamentID);
CREATE INDEX IX_TeamMembers_PlayerID ON dbo.TeamMembers(PlayerID);
GO

/* Справочники — значения должны быть стабильны для тестирования */
INSERT INTO dbo.Roles (Name, Description, AccessLevel) VALUES
(N'Гость',N'Просмотр открытой информации',1),
(N'Игрок',N'Личный профиль и участие в соревнованиях',2),
(N'Капитан',N'Управление командой и заявками',3),
(N'Судья',N'Ведение матчей и результатов',4),
(N'Организатор',N'Создание и проведение турниров',5),
(N'Администратор',N'Управление пользователями и справочниками',6);
INSERT INTO dbo.Disciplines (Name, Platform, TeamSize, AgeRating, Description) VALUES
(N'Dota 2',N'PC',5,12,N'Командная дисциплина'),
(N'Counter-Strike 2',N'PC',5,18,N'Тактический командный шутер'),
(N'Яндекс Игры',N'Браузер',1,6,N'Браузерные игры с индивидуальным зачётом'),
(N'Rocket League',N'PC',3,3,N'Футбол на автомобилях');
INSERT INTO dbo.TournamentFormats (Name, Description) VALUES
(N'Single Elimination',N'Олимпийская система с выбыванием после поражения'),
(N'Double Elimination',N'Система с верхней и нижней сеткой'),
(N'Round Robin',N'Круговая система'),
(N'Swiss',N'Швейцарская система');
INSERT INTO dbo.TournamentStatuses (Name) VALUES
(N'Анонс'),(N'Регистрация'),(N'Идёт'),(N'Завершён'),(N'Отменён');
INSERT INTO dbo.MatchStatuses (Name) VALUES
(N'Запланирован'),(N'Идёт'),(N'Завершён'),(N'Тех. поражение'),(N'Перенесён'),(N'Отменён');
INSERT INTO dbo.ViolationTypes (Name, DefaultSanction, PenaltyPoints) VALUES
(N'Неявка',N'Техническое поражение',3),
(N'Опоздание',N'Предупреждение',1),
(N'Читерство',N'Дисквалификация',10),
(N'Подставной игрок',N'Дисквалификация',8),
(N'Неспортивное поведение',N'Предупреждение',2);
INSERT INTO dbo.Venues (Name, VenueType, Address, SeatCount, ContactPerson, Phone) VALUES
(N'Cyber Nexus',N'ПК-клуб',N'Учебный адрес, заменить на реальный при необходимости',40,NULL,NULL),
(N'Онлайн',N'Онлайн',NULL,0,NULL,NULL);

/* Учебная запись администратора. Пароль: Admin123. Соль тестовая; смените пароль после входа. */
INSERT INTO dbo.Users
(Login, PasswordHash, PasswordSalt, LastName, FirstName, MiddleName, BirthDate, Email, RoleID, IsActive)
VALUES
(N'admin',N'B0D96D46705CD30FE87066A47A31CA362EE811F87F7EFE071F3FB2A18436BD30',N'VGVzdFNhbHQxMjM0NTY3OA==',
 N'Тестовый',N'Администратор',NULL,'2000-01-01',N'admin@example.local',
 (SELECT RoleID FROM dbo.Roles WHERE Name=N'Администратор'),1);
GO

/* Контроль: ожидается 27 таблиц и непустые справочники. */
SELECT COUNT(*) AS TableCount FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo'); -- ожидается 27
SELECT N'Roles' AS TableName, COUNT(*) AS [RowsTotal] FROM dbo.Roles
UNION ALL SELECT N'Disciplines', COUNT(*) FROM dbo.Disciplines
UNION ALL SELECT N'TournamentFormats', COUNT(*) FROM dbo.TournamentFormats
UNION ALL SELECT N'TournamentStatuses', COUNT(*) FROM dbo.TournamentStatuses
UNION ALL SELECT N'MatchStatuses', COUNT(*) FROM dbo.MatchStatuses
UNION ALL SELECT N'Venues', COUNT(*) FROM dbo.Venues;
GO

/* =====================================================================
   2. ТЕСТОВЫЕ ДАННЫЕ (пароль у всех тестовых пользователей: Test123)
   ===================================================================== */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* ---------- Пользователи (пароль у всех: Test123) ---------- */
INSERT INTO dbo.Users (Login, PasswordHash, PasswordSalt, LastName, FirstName, MiddleName,
                       BirthDate, Email, Phone, RoleID, RegisteredAt, IsActive)
SELECT v.Login, v.Hash, v.Salt, v.LastName, v.FirstName, v.MiddleName,
       v.BirthDate, v.Email, v.Phone, r.RoleID, SYSDATETIME(), 1
FROM (VALUES
    (N'org_petrov', N'C6D24811034A29A0C74973101E63BF2B86CC157C23C612226B31E80809261FDA', N'NXo/wJV8b+s+Rot68PqlLrtLRItM0fWt', N'Петров', N'Пётр', N'Петрович', '1994-03-12', N'petrov@cybernexus.local', N'+7 (900) 111-22-33', N'Организатор'),
    (N'org_volkova', N'2AD06AC1A25C485061DE2160C5A70ECCA1CF4DA29593EC9CACA1658FF5DB839E', N'VAWSf/WPvdG3qTImLfz2PDCj4+E7Fu+w', N'Волкова', N'Мария', N'Андреевна', '1997-07-05', N'volkova@cybernexus.local', N'+7 (900) 222-33-44', N'Организатор'),
    (N'judge_smirnov', N'1C715E2C83A6CD6C4921890E11551BFE0769F1A56CC9D0FB7D68F3EEAAED73A0', N'2NwUsVMXt9hn//pmB2X97zhAJ5SFeEB2', N'Смирнов', N'Алексей', N'Игоревич', '1990-11-20', N'smirnov@cybernexus.local', NULL, N'Судья'),
    (N'pl_ivanov', N'EBBEF18FA3471F3535E4A5DC127AC49CF9C8223D24C7027E4CFC304E4BB7D1B5', N'/C8FzXryJUVFPdNooENkuI8zU4s5Itkn', N'Иванов', N'Кирилл', N'Сергеевич', '2006-02-14', N'ivanov@players.local', NULL, N'Капитан'),
    (N'pl_kuznetsov', N'B7C7CC2D3C0E7BC6BC22E536EE0A342E42DBA0E5696094AA7FEDFD551CE84B0F', N'A6chpCyQG4FVIRl3MuGeP8MYFYHhZr4U', N'Кузнецов', N'Денис', N'Олегович', '2007-05-30', N'kuznetsov@players.local', NULL, N'Игрок'),
    (N'pl_sokolov', N'9B3B4144AF7D9614D9BF38E9D7F167B6C613608E36FA25A27627EB954E533097', N'MUkDuVcl2MJe13ck0JpD1UfDBZYliR81', N'Соколов', N'Артём', N'Викторович', '2005-09-09', N'sokolov@players.local', NULL, N'Капитан'),
    (N'pl_popov', N'342742106FF27C77E77894384C9111495DCCBE7D73ECB5F0B29C5C7D4E387204', N'IslKdcQcgy8zEmr8BXk2i/CVMCCpLPQz', N'Попов', N'Никита', NULL, '2006-12-01', N'popov@players.local', NULL, N'Игрок'),
    (N'pl_lebedev', N'40F1AD265AEEE6034FA2A46884B23B765DCE679C35CECAEB1CEB277339DCFE7D', N'LLoVPPi2wkE1FZf8ltIQjyN1drjLrZAd', N'Лебедев', N'Иван', N'Павлович', '2004-04-18', N'lebedev@players.local', NULL, N'Капитан'),
    (N'pl_kozlov', N'CB670567EB9E0FE77B05244B4D1FCE6FAE1E79C2F9070EB020927B44B8586618', N'A+qHn/lgiO9XStlpaG1859yYaZnpUlaz', N'Козлов', N'Егор', N'Романович', '2005-08-23', N'kozlov@players.local', NULL, N'Игрок'),
    (N'pl_novikov', N'58B34E9148CA613B450E68B3C1778F71C5A0950BE1085E2781DE708DAEF07C5D', N'GlHAeZxR4yjKXeP1XLh0enZX7VWgidCr', N'Новиков', N'Максим', N'Ильич', '2006-01-11', N'novikov@players.local', NULL, N'Капитан'),
    (N'pl_morozova', N'123678B917BBDE82359B3CF91466FF25433F8C858BFCAE0D9D4F441D0CA6C7D1', N'pKLDO4jEK8z7eohQRcgau0nKgGFA9GMJ', N'Морозова', N'Анна', N'Дмитриевна', '2007-10-02', N'morozova@players.local', NULL, N'Игрок')
) AS v(Login, Hash, Salt, LastName, FirstName, MiddleName, BirthDate, Email, Phone, RoleName)
JOIN dbo.Roles r ON r.Name = v.RoleName;

/* ---------- Идентификаторы справочников и пользователей ---------- */
DECLARE @dDota int = (SELECT DisciplineID FROM dbo.Disciplines WHERE Name = N'Dota 2');
DECLARE @dCS   int = (SELECT DisciplineID FROM dbo.Disciplines WHERE Name = N'Counter-Strike 2');
DECLARE @dYa   int = (SELECT DisciplineID FROM dbo.Disciplines WHERE Name = N'Яндекс Игры');
DECLARE @dRL   int = (SELECT DisciplineID FROM dbo.Disciplines WHERE Name = N'Rocket League');

DECLARE @fSE int = (SELECT TournamentFormatID FROM dbo.TournamentFormats WHERE Name = N'Single Elimination');
DECLARE @fDE int = (SELECT TournamentFormatID FROM dbo.TournamentFormats WHERE Name = N'Double Elimination');
DECLARE @fRR int = (SELECT TournamentFormatID FROM dbo.TournamentFormats WHERE Name = N'Round Robin');
DECLARE @fSW int = (SELECT TournamentFormatID FROM dbo.TournamentFormats WHERE Name = N'Swiss');

DECLARE @sAnn  int = (SELECT TournamentStatusID FROM dbo.TournamentStatuses WHERE Name = N'Анонс');
DECLARE @sReg  int = (SELECT TournamentStatusID FROM dbo.TournamentStatuses WHERE Name = N'Регистрация');
DECLARE @sLive int = (SELECT TournamentStatusID FROM dbo.TournamentStatuses WHERE Name = N'Идёт');
DECLARE @sDone int = (SELECT TournamentStatusID FROM dbo.TournamentStatuses WHERE Name = N'Завершён');
DECLARE @sCanc int = (SELECT TournamentStatusID FROM dbo.TournamentStatuses WHERE Name = N'Отменён');

DECLARE @vNexus int = (SELECT VenueID FROM dbo.Venues WHERE Name = N'Cyber Nexus');

DECLARE @uAdmin  int = (SELECT UserID FROM dbo.Users WHERE Login = N'admin');
DECLARE @uPetrov int = (SELECT UserID FROM dbo.Users WHERE Login = N'org_petrov');
DECLARE @uVolk   int = (SELECT UserID FROM dbo.Users WHERE Login = N'org_volkova');
DECLARE @uJudge  int = (SELECT UserID FROM dbo.Users WHERE Login = N'judge_smirnov');

/* ---------- Профили игроков и судьи ---------- */
INSERT INTO dbo.Players (UserID, Nickname, GameAccountID, Rating, MatchesPlayed, CreatedAt)
SELECT u.UserID, v.Nick, v.Account, v.Rating, v.Played, SYSDATETIME()
FROM (VALUES
    (N'pl_ivanov',    N'Wolfie',   N'steam:1001', 1450, 24),
    (N'pl_kuznetsov', N'Denchik',  N'steam:1002', 1210, 15),
    (N'pl_sokolov',   N'Phoenix',  N'steam:1003', 1380, 30),
    (N'pl_popov',     N'Nikitos',  N'steam:1004', 1100,  9),
    (N'pl_lebedev',   N'NightIv',  N'steam:1005', 1520, 41),
    (N'pl_kozlov',    N'Egorka',   N'steam:1006', 1290, 18),
    (N'pl_novikov',   N'IronMax',  N'steam:1007', 1405, 27),
    (N'pl_morozova',  N'AnnaByte', N'steam:1008', 1330, 22)
) AS v(Login, Nick, Account, Rating, Played)
JOIN dbo.Users u ON u.Login = v.Login;

INSERT INTO dbo.Referees (UserID, Qualification, AppointedDate, MatchesOfficiated)
VALUES (@uJudge, N'Судья клубного уровня', '2025-09-01', 12);

DECLARE @pIvanov int = (SELECT p.PlayerID FROM dbo.Players p JOIN dbo.Users u ON u.UserID = p.UserID WHERE u.Login = N'pl_ivanov');
DECLARE @pKuzn   int = (SELECT p.PlayerID FROM dbo.Players p JOIN dbo.Users u ON u.UserID = p.UserID WHERE u.Login = N'pl_kuznetsov');
DECLARE @pSokol  int = (SELECT p.PlayerID FROM dbo.Players p JOIN dbo.Users u ON u.UserID = p.UserID WHERE u.Login = N'pl_sokolov');
DECLARE @pPopov  int = (SELECT p.PlayerID FROM dbo.Players p JOIN dbo.Users u ON u.UserID = p.UserID WHERE u.Login = N'pl_popov');
DECLARE @pLebed  int = (SELECT p.PlayerID FROM dbo.Players p JOIN dbo.Users u ON u.UserID = p.UserID WHERE u.Login = N'pl_lebedev');
DECLARE @pKozl   int = (SELECT p.PlayerID FROM dbo.Players p JOIN dbo.Users u ON u.UserID = p.UserID WHERE u.Login = N'pl_kozlov');
DECLARE @pNovik  int = (SELECT p.PlayerID FROM dbo.Players p JOIN dbo.Users u ON u.UserID = p.UserID WHERE u.Login = N'pl_novikov');
DECLARE @pMoroz  int = (SELECT p.PlayerID FROM dbo.Players p JOIN dbo.Users u ON u.UserID = p.UserID WHERE u.Login = N'pl_morozova');
DECLARE @rJudge  int = (SELECT RefereeID FROM dbo.Referees WHERE UserID = @uJudge);

/* ---------- Карты CS2 ---------- */
INSERT INTO dbo.Maps (DisciplineID, Name, MapType, IsActive) VALUES
(@dCS, N'Mirage',   N'Active Duty', 1),
(@dCS, N'Inferno',  N'Active Duty', 1),
(@dCS, N'Dust II',  N'Active Duty', 1),
(@dCS, N'Nuke',     N'Active Duty', 1);
DECLARE @mMirage int = (SELECT MapID FROM dbo.Maps WHERE DisciplineID = @dCS AND Name = N'Mirage');

/* ---------- Команды и составы ---------- */
INSERT INTO dbo.Teams (Name, Tag, DisciplineID, CaptainID, FoundedDate, Description, IsActive) VALUES
(N'Nexus Wolves', N'NXW', @dDota, @pIvanov, '2025-02-01', N'Команда клуба Cyber Nexus', 1),
(N'Red Phoenix',  N'RPX', @dDota, @pSokol,  '2025-04-15', N'Студенческая команда',      1),
(N'Night Owls',   N'NOW', @dCS,   @pLebed,  '2024-11-10', N'Вечерняя лига клуба',       1),
(N'Iron Bytes',   N'IRB', @dCS,   @pNovik,  '2025-01-20', N'Команда колледжа',          1);

DECLARE @tWolves  int = (SELECT TeamID FROM dbo.Teams WHERE Tag = N'NXW' AND DisciplineID = @dDota);
DECLARE @tPhoenix int = (SELECT TeamID FROM dbo.Teams WHERE Tag = N'RPX' AND DisciplineID = @dDota);
DECLARE @tOwls    int = (SELECT TeamID FROM dbo.Teams WHERE Tag = N'NOW' AND DisciplineID = @dCS);
DECLARE @tIron    int = (SELECT TeamID FROM dbo.Teams WHERE Tag = N'IRB' AND DisciplineID = @dCS);

INSERT INTO dbo.TeamMembers (TeamID, PlayerID, TeamRole, JoinedDate, IsStarter) VALUES
(@tWolves,  @pIvanov, N'Капитан', '2025-02-01', 1),
(@tWolves,  @pKuzn,   N'Игрок',   '2025-02-01', 1),
(@tPhoenix, @pSokol,  N'Капитан', '2025-04-15', 1),
(@tPhoenix, @pPopov,  N'Игрок',   '2025-04-20', 1),
(@tOwls,    @pLebed,  N'Капитан', '2024-11-10', 1),
(@tOwls,    @pKozl,   N'Игрок',   '2024-11-10', 1),
(@tIron,    @pNovik,  N'Капитан', '2025-01-20', 1),
(@tIron,    @pMoroz,  N'Игрок',   '2025-01-25', 1);

/* ---------- Турниры (главная таблица УП) ---------- */
INSERT INTO dbo.Tournaments (Name, DisciplineID, TournamentFormatID, TournamentStatusID, OrganizerID, VenueID, IsOnline,
                             RegistrationStart, RegistrationEnd, StartDate, EndDate, MaxTeams, EntryFee, PrizePool,
                             Rules, Description, CreatedAt) VALUES
(N'Кубок Cyber Nexus по Dota 2',       @dDota, @fSE, @sDone, @uPetrov, @vNexus, 0, '2026-09-01', '2026-09-20', '2026-09-27 12:00', '2026-09-28 20:00',  8,  500.00,  20000.00, N'Серии BO3, финал BO5', N'Осенний клубный кубок',        SYSDATETIME()),
(N'Студенческий кубок по CS2',         @dCS,   @fDE, @sLive, @uVolk,   @vNexus, 0, '2026-09-15', '2026-10-10', '2026-10-17 11:00', NULL,               16,  300.00,  30000.00, N'Маппул Active Duty',   N'Для студентов колледжей',      SYSDATETIME()),
(N'Быстрый турнир «Яндекс Игры»',      @dYa,   @fSW, @sReg,  @uPetrov, NULL,    1, '2026-10-01', '2026-10-30', '2026-11-01 18:00', NULL,               32,    0.00,   5000.00, NULL,                    N'Онлайн, индивидуальный зачёт', SYSDATETIME()),
(N'Зимняя лига Dota 2',                @dDota, @fRR, @sReg,  @uVolk,   @vNexus, 0, '2026-10-10', '2026-11-25', '2026-12-05 12:00', NULL,                8,  700.00,  40000.00, N'Круговая система',     N'Лига выходного дня',           SYSDATETIME()),
(N'Открытый чемпионат CS2',            @dCS,   @fSE, @sAnn,  @uPetrov, @vNexus, 0, '2026-11-01', '2026-12-01', '2026-12-12 10:00', NULL,               16, 1000.00, 100000.00, NULL,                    N'Крупнейший турнир сезона',     SYSDATETIME()),
(N'Rocket League Cup',                 @dRL,   @fDE, @sAnn,  @uAdmin,  NULL,    1, '2026-11-15', '2026-12-10', '2026-12-19 17:00', NULL,               12,  400.00,  15000.00, NULL,                    N'Онлайн-кубок 3x3',             SYSDATETIME()),
(N'Рождественский браузерный кубок',   @dYa,   @fSW, @sAnn,  @uVolk,   NULL,    1, '2026-12-01', '2027-01-10', '2027-01-16 16:00', NULL,               64,  150.00,  10000.00, NULL,                    N'Праздничный онлайн-турнир',    SYSDATETIME()),
(N'Весенний кубок Dota 2',             @dDota, @fSE, @sAnn,  @uPetrov, @vNexus, 0, '2027-02-01', '2027-03-10', '2027-03-20 12:00', NULL,                8,  600.00,  25000.00, NULL,                    NULL,                            SYSDATETIME()),
(N'Межфакультетская лига CS2',         @dCS,   @fRR, @sCanc, @uAdmin,  @vNexus, 0, '2026-08-01', '2026-08-20', '2026-08-29 10:00', NULL,                6,  350.00,  12000.00, NULL,                    N'Отменён из-за малого числа заявок', SYSDATETIME()),
(N'Контрольный турнир Rocket League',  @dRL,   @fSE, @sDone, @uVolk,   NULL,    1, '2026-07-01', '2026-07-20', '2026-07-25 15:00', '2026-07-25 20:00',  6,  250.00,   6000.00, NULL,                    NULL,                            SYSDATETIME());

DECLARE @T1 int = (SELECT TournamentID FROM dbo.Tournaments WHERE Name = N'Кубок Cyber Nexus по Dota 2');
DECLARE @T2 int = (SELECT TournamentID FROM dbo.Tournaments WHERE Name = N'Студенческий кубок по CS2');
DECLARE @T4 int = (SELECT TournamentID FROM dbo.Tournaments WHERE Name = N'Зимняя лига Dota 2');
DECLARE @T5 int = (SELECT TournamentID FROM dbo.Tournaments WHERE Name = N'Открытый чемпионат CS2');

/* ---------- Заявки и участники ---------- */
INSERT INTO dbo.Applications (TournamentID, TeamID, SubmittedAt, Status, ReviewedBy, ReviewedAt, Comment, IsFeePaid) VALUES
(@T1, @tWolves,  '2026-09-05', N'Принята', @uPetrov, '2026-09-06', NULL, 1),
(@T1, @tPhoenix, '2026-09-07', N'Принята', @uPetrov, '2026-09-08', NULL, 1),
(@T2, @tOwls,    '2026-09-20', N'Принята', @uVolk,   '2026-09-21', NULL, 1),
(@T2, @tIron,    '2026-09-22', N'Принята', @uVolk,   '2026-09-23', NULL, 1),
(@T4, @tWolves,  '2026-10-12', N'Подана',  NULL,     NULL,         NULL, 0),
(@T4, @tPhoenix, '2026-10-14', N'Подана',  NULL,     NULL,         N'Ожидает оплаты взноса', 0);

INSERT INTO dbo.TournamentParticipants (TournamentID, TeamID, Seed, Points, FinalPlace, IsDisqualified) VALUES
(@T1, @tWolves,  1, 6, 1, 0),
(@T1, @tPhoenix, 2, 3, 2, 0),
(@T2, @tOwls,    1, 0, NULL, 0),
(@T2, @tIron,    2, 0, NULL, 0);

/* ---------- Стадии и матчи ---------- */
INSERT INTO dbo.Stages (TournamentID, Name, StageType, StageOrder, StartDate, EndDate) VALUES
(@T1, N'Финал',    N'Плей-офф', 1, '2026-09-28', '2026-09-28'),
(@T2, N'Плей-офф', N'Плей-офф', 1, '2026-10-17', NULL);

DECLARE @stT1 int = (SELECT StageID FROM dbo.Stages WHERE TournamentID = @T1 AND StageOrder = 1);
DECLARE @stT2 int = (SELECT StageID FROM dbo.Stages WHERE TournamentID = @T2 AND StageOrder = 1);
DECLARE @msDone int = (SELECT MatchStatusID FROM dbo.MatchStatuses WHERE Name = N'Завершён');
DECLARE @msLive int = (SELECT MatchStatusID FROM dbo.MatchStatuses WHERE Name = N'Идёт');

INSERT INTO dbo.Matches (StageID, RoundNumber, Team1ID, Team2ID, RefereeID, MatchStatusID, ScheduledAt, StartedAt,
                         SeriesFormat, Team1Score, Team2Score, WinnerTeamID, Comment) VALUES
(@stT1, 1, @tWolves, @tPhoenix, @rJudge, @msDone, '2026-09-28 14:00', '2026-09-28 14:05', N'BO3', 2, 1, @tWolves, N'Финал'),
(@stT2, 1, @tOwls,   @tIron,    @rJudge, @msLive, '2026-10-17 12:00', '2026-10-17 12:10', N'BO1', 0, 0, NULL,     NULL);

DECLARE @matchT2 int = (SELECT MatchID FROM dbo.Matches WHERE StageID = @stT2 AND RoundNumber = 1);

INSERT INTO dbo.MatchMaps (MatchID, MapID, MapOrder, Team1Score, Team2Score, DurationMinutes)
VALUES (@matchT2, @mMirage, 1, 9, 7, NULL);
DECLARE @mmT2 int = (SELECT MatchMapID FROM dbo.MatchMaps WHERE MatchID = @matchT2 AND MapOrder = 1);

INSERT INTO dbo.PlayerMatchStats (MatchMapID, PlayerID, Kills, Deaths, Assists, Score, IsMVP) VALUES
(@mmT2, @pLebed, 18, 11, 4, 52, 1),
(@mmT2, @pKozl,  12, 14, 6, 38, 0),
(@mmT2, @pNovik, 15, 13, 3, 44, 0),
(@mmT2, @pMoroz, 10, 15, 7, 33, 0);

INSERT INTO dbo.Violations (ViolationTypeID, MatchID, TeamID, PlayerID, RefereeID, RecordedAt, Description, AppliedSanction)
SELECT vt.ViolationTypeID, @matchT2, @tIron, @pMoroz, @rJudge, '2026-10-17 12:05', N'Опоздание к началу матча на 5 минут', N'Предупреждение'
FROM dbo.ViolationTypes vt WHERE vt.Name = N'Опоздание';

/* ---------- Призы и спонсоры ---------- */
INSERT INTO dbo.Prizes (TournamentID, Place, Amount, NonCashPrize, AwardedDate) VALUES
(@T1, 1, 12000.00, N'Кубок и медали', '2026-09-28'),
(@T1, 2,  6000.00, NULL,              '2026-09-28'),
(@T1, 3,  2000.00, NULL,              '2026-09-28'),
(@T2, 1, 15000.00, NULL, NULL),
(@T2, 2, 10000.00, NULL, NULL),
(@T2, 3,  5000.00, NULL, NULL),
(@T5, 1, 50000.00, NULL, NULL),
(@T5, 2, 30000.00, NULL, NULL),
(@T5, 3, 20000.00, NULL, NULL);

INSERT INTO dbo.Sponsors (Name, ContactPerson, Phone, Email, Website) VALUES
(N'ООО «ГеймТех» (учебный пример)', N'Орлов Сергей', N'+7 (900) 555-01-01', N'partner@gametech.local', NULL),
(N'Кафе «Пиксель» (учебный пример)', N'Белова Ольга', N'+7 (900) 555-02-02', N'pixel@cafe.local', NULL);

DECLARE @spGame  int = (SELECT SponsorID FROM dbo.Sponsors WHERE Name LIKE N'ООО «ГеймТех»%');
DECLARE @spPixel int = (SELECT SponsorID FROM dbo.Sponsors WHERE Name LIKE N'Кафе «Пиксель»%');

INSERT INTO dbo.TournamentSponsors (TournamentID, SponsorID, ContributionAmount, SupportType, ContractNumber) VALUES
(@T1, @spGame,  10000.00, N'Призовой фонд', N'Д-2026/01'),
(@T2, @spGame,  15000.00, N'Призовой фонд', N'Д-2026/02'),
(@T2, @spPixel,  5000.00, N'Питание участников', N'Д-2026/03');

/* ---------- Уведомления ---------- */
INSERT INTO dbo.Notifications (UserID, Title, Body, CreatedAt, IsRead)
SELECT u.UserID, v.Title, v.Body, SYSDATETIME(), 0
FROM (VALUES
    (N'pl_ivanov',  N'Заявка принята', N'Заявка команды Nexus Wolves на «Кубок Cyber Nexus по Dota 2» принята.'),
    (N'pl_lebedev', N'Матч назначен',  N'Матч Night Owls — Iron Bytes назначен на 17.10.2026 12:00.'),
    (N'pl_novikov', N'Нарушение',      N'Игроку команды Iron Bytes вынесено предупреждение за опоздание.')
) AS v(Login, Title, Body)
JOIN dbo.Users u ON u.Login = v.Login;

COMMIT TRANSACTION;
GO

/* Контроль: число строк в основных таблицах */
SELECT N'Users' AS TableName, COUNT(*) AS RowsTotal FROM dbo.Users
UNION ALL SELECT N'Players', COUNT(*) FROM dbo.Players
UNION ALL SELECT N'Teams', COUNT(*) FROM dbo.Teams
UNION ALL SELECT N'Tournaments', COUNT(*) FROM dbo.Tournaments
UNION ALL SELECT N'Applications', COUNT(*) FROM dbo.Applications
UNION ALL SELECT N'Stages', COUNT(*) FROM dbo.Stages
UNION ALL SELECT N'Matches', COUNT(*) FROM dbo.Matches
UNION ALL SELECT N'Prizes', COUNT(*) FROM dbo.Prizes
UNION ALL SELECT N'TournamentSponsors', COUNT(*) FROM dbo.TournamentSponsors;
GO

/* Контроль каскадных ключей */
SELECT fk.name AS ForeignKey,
       OBJECT_NAME(fk.parent_object_id) AS ChildTable,
       OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
       fk.delete_referential_action_desc AS OnDelete
FROM sys.foreign_keys fk
WHERE fk.delete_referential_action <> 0
ORDER BY ParentTable, ChildTable;
GO
