-- RaceDay Database Script
-- PROG6212 Part 1 - System Planning and Database

IF DB_ID('RaceDayDB') IS NULL
    CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- Drop tables if they already exist so the script can be run again without errors
DROP TABLE IF EXISTS Result;
DROP TABLE IF EXISTS Enrolment;
DROP TABLE IF EXISTS Category;
DROP TABLE IF EXISTS Event;
DROP TABLE IF EXISTS Profile;
DROP TABLE IF EXISTS [User];
GO

 
-- TABLE: User
CREATE TABLE [User] (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- TABLE: Profile
CREATE TABLE Profile (
    ProfileId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    Phone NVARCHAR(20) NULL,
    City NVARCHAR(100) NULL,
    Province NVARCHAR(100) NULL,
    ProfilePictureUrl NVARCHAR(255) NULL
     CONSTRAINT FK_Profile_User FOREIGN KEY (UserId) REFERENCES [User](UserId)
);
GO

-- TABLE: Event
CREATE TABLE Event (
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(1000) NULL,
    EventDate DATETIME NOT NULL,
    Location NVARCHAR(150) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    EventType NVARCHAR(20) NOT NULL CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    BannerImageUrl NVARCHAR(255) NULL,
    CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserId) REFERENCES [User](UserId)
);
GO

-- TABLE: Category
CREATE TABLE Category (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_Category_Event FOREIGN KEY (EventId) REFERENCES Event(EventId)
);
GO

-- TABLE: Enrolment
CREATE TABLE Enrolment (
    EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId INT NOT NULL,
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Confirmed')),
    CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (ParticipantId) REFERENCES [User](UserId),
    CONSTRAINT FK_Enrolment_Event FOREIGN KEY (EventId) REFERENCES Event(EventId),
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryId) REFERENCES Category(CategoryId)
);
GO

-- TABLE: Result
CREATE TABLE Result (
    ResultId INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT NOT NULL UNIQUE,
    FinishTime TIME NOT NULL,
    Position INT NOT NULL,
    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES Enrolment(EnrolmentId)
);
GO

 
-- SEED DATA

-- Users: 2 Organisers, 2 Participants
INSERT INTO [User] (FullName, Email, PasswordHash, Role) VALUES
('Thabo Nkosi', 'thabo.nkosi@raceday.co.za', 'hashed_password_1', 'Organiser'),
('Lerato Dube', 'lerato.dube@raceday.co.za', 'hashed_password_2', 'Organiser'),
('Sipho Zulu', 'sipho.zulu@raceday.co.za', 'hashed_password_3', 'Participant'),
('Amahle Khumalo', 'amahle.khumalo@raceday.co.za', 'hashed_password_4', 'Participant');
GO

-- Profiles for each user
INSERT INTO Profile (UserId, Phone, City, Province, ProfilePictureUrl) VALUES
(1, '0821234567', 'Pietermaritzburg', 'KwaZulu-Natal', NULL),
(2, '0837654321', 'Cape Town', 'Western Cape', NULL),
(3, '0731122334', 'Durban', 'KwaZulu-Natal', NULL),
(4, '0745566778', 'Johannesburg', 'Gauteng', NULL);
GO

-- Events: 3 events, one per Organiser (and a second for Thabo)
INSERT INTO Event (OrganiserId, Name, Description, EventDate, Location, Distance, EventType, BannerImageUrl) VALUES
(1, 'Comrades Marathon', 'Iconic ultra-marathon between Pietermaritzburg and Durban.', '2026-06-14', 'Pietermaritzburg', 89.00, 'Run', NULL),
(2, 'Cape Town Cycle Tour', 'Scenic cycling event around the Cape Peninsula.', '2026-03-08', 'Cape Town', 109.00, 'Cycle', NULL),
(1, 'Durban Park Run', 'Community 5km run/walk event.', '2026-09-20', 'Durban', 5.00, 'Walk', NULL);
GO

-- Categories per event
INSERT INTO Category (EventId, Name) VALUES
(1, 'Senior'),
(1, 'Under 20'),
(2, '109km'),
(2, '56km'),
(3, '5km Open');
GO

-- Enrolments: Participants entering events
INSERT INTO Enrolment (ParticipantId, EventId, CategoryId, Status) VALUES
(3, 1, 1, 'Confirmed'),
(4, 2, 3, 'Confirmed'),
(3, 3, 5, 'Pending');
GO

-- Sample result for a completed enrolment
INSERT INTO Result (EnrolmentId, FinishTime, Position) VALUES
(1, '08:45:12', 47);
GO