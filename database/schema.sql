-- ==============================================================================
-- File: /database/schema.sql
-- Description: 3NF Database Schema for Campus Event Management System
-- ==============================================================================

-- 1. Create Students Entity
CREATE TABLE Students (
    StudentID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    -- CHECK constraint to ensure basic email format validity
    CONSTRAINT CHK_Student_Email CHECK (Email LIKE '%_@__%.__%') 
);

-- 2. Create Events Entity
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    EventDate DATETIME NOT NULL,
    Capacity INT NOT NULL,
    -- CHECK constraint to ensure event capacity is a valid positive number
    CONSTRAINT CHK_Event_Capacity CHECK (Capacity > 0)
);

-- 3. Create Registrations Entity (Junction Table)
CREATE TABLE Registrations (
    RegistrationID INT IDENTITY(1,1) PRIMARY KEY,
    StudentID INT NOT NULL,
    EventID INT NOT NULL,
    RegistrationDate DATETIME DEFAULT GETDATE(),
    
    -- Foreign Key rules with cascading deletes
    CONSTRAINT FK_Registrations_Students FOREIGN KEY (StudentID) 
        REFERENCES Students(StudentID) ON DELETE CASCADE,
        
    CONSTRAINT FK_Registrations_Events FOREIGN KEY (EventID) 
        REFERENCES Events(EventID) ON DELETE CASCADE,
        
    -- Unique constraint preventing a student from registering for the same event twice
    CONSTRAINT UQ_Student_Event UNIQUE (StudentID, EventID)
);

-- ==============================================================================
-- Non-Clustered Indexes on Foreign Key Columns
-- ==============================================================================

-- Index to optimize administrator queries retrieving all registrations for a specific event
CREATE NONCLUSTERED INDEX IX_Registrations_EventID 
    ON Registrations(EventID);

-- Index to optimize queries retrieving all events a specific student is attending
CREATE NONCLUSTERED INDEX IX_Registrations_StudentID 
    ON Registrations(StudentID);