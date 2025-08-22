-- Meeting Log tablosu ve trigger oluşturma SQL scripti

-- 1. MeetingLog tablosunu oluştur (eğer yoksa)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MeetingLogs' AND xtype='U')
BEGIN
    CREATE TABLE MeetingLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MeetingId INT NOT NULL,
        Action NVARCHAR(50) NOT NULL, -- 'INSERT', 'UPDATE', 'DELETE'
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        ChangedBy INT NULL, -- UserId
        ChangedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Details NVARCHAR(500) NULL
    );
END
GO

-- 2. Meeting tablosu için INSERT trigger
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Meeting_Insert')
    DROP TRIGGER TR_Meeting_Insert;
GO

CREATE TRIGGER TR_Meeting_Insert
ON Meetings
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO MeetingLogs (MeetingId, Action, NewValues, ChangedBy, ChangedAt, Details)
    SELECT 
        i.Id,
        'INSERT',
        CONCAT('Title: ', i.Title, ', Description: ', i.Description, ', StartDate: ', i.StartDate, ', EndDate: ', i.EndDate, ', UserId: ', i.UserId),
        i.UserId,
        GETUTCDATE(),
        'Yeni toplantı oluşturuldu'
    FROM inserted i;
END
GO

-- 3. Meeting tablosu için UPDATE trigger
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Meeting_Update')
    DROP TRIGGER TR_Meeting_Update;
GO

CREATE TRIGGER TR_Meeting_Update
ON Meetings
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO MeetingLogs (MeetingId, Action, OldValues, NewValues, ChangedBy, ChangedAt, Details)
    SELECT 
        i.Id,
        'UPDATE',
        CONCAT('Title: ', d.Title, ', Description: ', d.Description, ', StartDate: ', d.StartDate, ', EndDate: ', d.EndDate, ', IsCancelled: ', d.IsCancelled),
        CONCAT('Title: ', i.Title, ', Description: ', i.Description, ', StartDate: ', i.StartDate, ', EndDate: ', i.EndDate, ', IsCancelled: ', i.IsCancelled),
        i.UserId,
        GETUTCDATE(),
        CASE 
            WHEN i.IsCancelled = 1 AND d.IsCancelled = 0 THEN 'Toplantı iptal edildi'
            WHEN i.IsCancelled = 0 AND d.IsCancelled = 1 THEN 'Toplantı iptal durumu kaldırıldı'
            ELSE 'Toplantı güncellendi'
        END
    FROM inserted i
    INNER JOIN deleted d ON i.Id = d.Id;
END
GO

-- 4. Meeting tablosu için DELETE trigger
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Meeting_Delete')
    DROP TRIGGER TR_Meeting_Delete;
GO

CREATE TRIGGER TR_Meeting_Delete
ON Meetings
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO MeetingLogs (MeetingId, Action, OldValues, ChangedBy, ChangedAt, Details)
    SELECT 
        d.Id,
        'DELETE',
        CONCAT('Title: ', d.Title, ', Description: ', d.Description, ', StartDate: ', d.StartDate, ', EndDate: ', d.EndDate, ', UserId: ', d.UserId),
        d.UserId,
        GETUTCDATE(),
        'Toplantı silindi'
    FROM deleted d;
END
GO

-- 5. MeetingLogs tablosu için index oluştur (performans için)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MeetingLogs_MeetingId')
BEGIN
    CREATE INDEX IX_MeetingLogs_MeetingId ON MeetingLogs(MeetingId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MeetingLogs_ChangedAt')
BEGIN
    CREATE INDEX IX_MeetingLogs_ChangedAt ON MeetingLogs(ChangedAt DESC);
END
GO

PRINT 'Meeting Log tablosu ve trigger''lar başarıyla oluşturuldu.';
GO