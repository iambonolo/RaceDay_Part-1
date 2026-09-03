/* ============================================================
   RaceDay System Database
   Part 1 – Schema creation and sample data seeding
   Run this script in SQL Server Management Studio (SSMS)
   ============================================================ */

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'RaceDayDB')
BEGIN
    ALTER DATABASE [RaceDayDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [RaceDayDB];
END
GO

CREATE DATABASE [RaceDayDB];
GO

USE [RaceDayDB];
GO

/* ------------------------------------------------------------
   1. Roles  – lookup table distinguishing Organiser/Participant
   ------------------------------------------------------------ */
CREATE TABLE [dbo].[Roles]
(
    [RoleId]     INT IDENTITY(1,1) NOT NULL,
    [RoleName]   VARCHAR(30)       NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC),
    CONSTRAINT [UQ_Roles_RoleName] UNIQUE ([RoleName])
);
GO

/* ------------------------------------------------------------
   2. Users  – Organisers and Participants share this table
   ------------------------------------------------------------ */
CREATE TABLE [dbo].[Users]
(
    [UserId]         INT IDENTITY(1,1)   NOT NULL,
    [FullName]       VARCHAR(120)        NOT NULL,
    [Email]          VARCHAR(150)        NOT NULL,
    [PasswordHash]   VARCHAR(255)        NOT NULL,
    [RoleId]         INT                 NOT NULL,
    [DateJoined]     DATETIME            NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email]),
    CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId])
        REFERENCES [dbo].[Roles] ([RoleId])
);
GO

/* ------------------------------------------------------------
   3. Events  – created and owned by an Organiser
   ------------------------------------------------------------ */
CREATE TABLE [dbo].[Events]
(
    [EventId]        INT IDENTITY(1,1)   NOT NULL,
    [EventName]      VARCHAR(150)        NOT NULL,
    [EventDetails]   VARCHAR(MAX)        NULL,
    [EventDate]      DATE                NOT NULL,
    [Venue]          VARCHAR(150)        NOT NULL,
    [DistanceKm]     DECIMAL(6,2)        NOT NULL,
    [EventType]      VARCHAR(10)         NOT NULL,
    [OrganiserId]    INT                 NOT NULL,
    [DateCreated]    DATETIME            NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED ([EventId] ASC),
    CONSTRAINT [CK_Events_EventType] CHECK ([EventType] IN ('Run','Walk','Cycle')),
    CONSTRAINT [FK_Events_Users] FOREIGN KEY ([OrganiserId])
        REFERENCES [dbo].[Users] ([UserId])
);
GO

/* ------------------------------------------------------------
   4. Categories  – age/distance groupings within an Event
   ------------------------------------------------------------ */
CREATE TABLE [dbo].[Categories]
(
    [CategoryId]     INT IDENTITY(1,1)   NOT NULL,
    [EventId]        INT                 NOT NULL,
    [CategoryName]   VARCHAR(50)         NOT NULL,
    [MinAge]         INT                 NULL,
    [MaxAge]         INT                 NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([CategoryId] ASC),
    CONSTRAINT [FK_Categories_Events] FOREIGN KEY ([EventId])
        REFERENCES [dbo].[Events] ([EventId])
);
GO

/* ------------------------------------------------------------
   5. Enrolments  – links a Participant, an Event and a Category
   ------------------------------------------------------------ */
CREATE TABLE [dbo].[Enrolments]
(
    [EnrolmentId]     INT IDENTITY(1,1)  NOT NULL,
    [ParticipantId]   INT                NOT NULL,
    [EventId]         INT                NOT NULL,
    [CategoryId]      INT                NOT NULL,
    [DateEnrolled]    DATETIME           NOT NULL DEFAULT (GETDATE()),
    [EnrolmentStatus] VARCHAR(20)        NOT NULL DEFAULT ('Confirmed'),
    CONSTRAINT [PK_Enrolments] PRIMARY KEY CLUSTERED ([EnrolmentId] ASC),
    CONSTRAINT [CK_Enrolments_Status] CHECK ([EnrolmentStatus] IN ('Pending','Confirmed','Cancelled')),
    CONSTRAINT [UQ_Enrolments_OnePerEvent] UNIQUE ([ParticipantId], [EventId]),
    CONSTRAINT [FK_Enrolments_Users] FOREIGN KEY ([ParticipantId])
        REFERENCES [dbo].[Users] ([UserId]),
    CONSTRAINT [FK_Enrolments_Events] FOREIGN KEY ([EventId])
        REFERENCES [dbo].[Events] ([EventId]),
    CONSTRAINT [FK_Enrolments_Categories] FOREIGN KEY ([CategoryId])
        REFERENCES [dbo].[Categories] ([CategoryId])
);
GO

/* ------------------------------------------------------------
   6. Results  – one result per Enrolment (1-to-1)
   ------------------------------------------------------------ */
CREATE TABLE [dbo].[Results]
(
    [ResultId]         INT IDENTITY(1,1) NOT NULL,
    [EnrolmentId]      INT               NOT NULL,
    [FinishTime]       TIME              NOT NULL,
    [FinishPosition]   INT               NOT NULL,
    [DateCaptured]     DATETIME          NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Results] PRIMARY KEY CLUSTERED ([ResultId] ASC),
    CONSTRAINT [UQ_Results_Enrolment] UNIQUE ([EnrolmentId]),
    CONSTRAINT [FK_Results_Enrolments] FOREIGN KEY ([EnrolmentId])
        REFERENCES [dbo].[Enrolments] ([EnrolmentId])
);
GO

/* ============================================================
   SEED DATA
   Minimum required: 2 Organisers, 2 Participants, 3 Events,
   categories per event, and sample enrolments
   ============================================================ */

INSERT INTO [dbo].[Roles] ([RoleName])
VALUES ('Organiser'), ('Participant');
GO

INSERT INTO [dbo].[Users] ([FullName], [Email], [PasswordHash], [RoleId])
VALUES
    ('Kabelo Sithole',   'kabelo.sithole@raceday.co.za',   'PLACEHOLDER_HASH_1', 1),
    ('Ayanda Mthembu',   'ayanda.mthembu@raceday.co.za',   'PLACEHOLDER_HASH_2', 1),
    ('Refilwe Modise',   'refilwe.modise@raceday.co.za',   'PLACEHOLDER_HASH_3', 2),
    ('Jason van der Merwe', 'jason.vdm@raceday.co.za',     'PLACEHOLDER_HASH_4', 2);
GO

INSERT INTO [dbo].[Events] ([EventName], [EventDetails], [EventDate], [Venue], [DistanceKm], [EventType], [OrganiserId])
VALUES
    ('Two Oceans Marathon', 'Scenic ultra-marathon around the Cape Peninsula.', '2026-04-11', 'Cape Town', 56.0, 'Run', 1),
    ('Joburg City Cycle Classic', 'Road cycling race through central Johannesburg.', '2026-05-16', 'Johannesburg', 94.7, 'Cycle', 2),
    ('Krugersdorp Community Park Run', 'Weekly free 5km community run/walk.', '2026-09-19', 'Krugersdorp', 5.0, 'Walk', 1);
GO

INSERT INTO [dbo].[Categories] ([EventId], [CategoryName], [MinAge], [MaxAge])
VALUES
    (1, '56km Ultra Open', 20, 99),
    (1, '21km Half Marathon', 15, 99),
    (2, '94.7km Individual', 18, 99),
    (2, '47km Half Distance', 15, 99),
    (3, '5km Community', 5, 99);
GO

INSERT INTO [dbo].[Enrolments] ([ParticipantId], [EventId], [CategoryId], [EnrolmentStatus])
VALUES
    (3, 1, 1, 'Confirmed'),
    (4, 2, 3, 'Confirmed'),
    (3, 3, 5, 'Confirmed');
GO

INSERT INTO [dbo].[Results] ([EnrolmentId], [FinishTime], [FinishPosition])
VALUES
    (2, '03:04:52', 145);
GO

/* Quick verification – run these after the script to eyeball the data */
-- SELECT * FROM [dbo].[Users];
-- SELECT * FROM [dbo].[Events];
-- SELECT * FROM [dbo].[Enrolments];