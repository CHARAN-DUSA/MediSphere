-- Incremental migration: Email audit logging + secure password reset tokens
-- Run against MediSphereDb AFTER the main schema (Users/Patients/Doctors) exists.

USE MediSphereDb;
GO

-- Step 1: Confirm you are on the correct database and find the user table name
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('Users', 'AppUsers', 'AspNetUsers')
ORDER BY TABLE_NAME;
GO

-- If the query above returns NO rows, stop here.
-- Your base schema is missing. Start the API once (F5) so EF migrations run,
-- or run: Update-Database -StartupProject MediSphere.API

DECLARE @UserTable SYSNAME;

SELECT TOP 1 @UserTable = t.TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_SCHEMA = 'dbo'
  AND t.TABLE_NAME IN ('Users', 'AppUsers')
ORDER BY CASE t.TABLE_NAME WHEN 'Users' THEN 0 ELSE 1 END;

IF @UserTable IS NULL
BEGIN
    RAISERROR('No user table found (expected dbo.Users or dbo.AppUsers). Apply InitialCreate migrations first.', 16, 1);
    RETURN;
END;

PRINT 'Using user table: dbo.' + @UserTable;
GO

-- Step 2: EmailLogs
IF OBJECT_ID(N'[dbo].[EmailLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmailLogs] (
        [Id]           INT            IDENTITY(1,1) NOT NULL,
        [Recipient]    NVARCHAR(MAX)  NOT NULL,
        [Subject]      NVARCHAR(MAX)  NOT NULL,
        [Status]       NVARCHAR(20)   NOT NULL,
        [ErrorMessage] NVARCHAR(MAX)  NULL,
        [SentAt]       DATETIME2      NULL,
        [CreatedAt]    DATETIME2      NOT NULL,
        [UpdatedAt]    DATETIME2      NULL,
        CONSTRAINT [PK_EmailLogs] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_EmailLogs_CreatedAt] ON [dbo].[EmailLogs] ([CreatedAt]);
    PRINT 'Created table EmailLogs';
END
ELSE
    PRINT 'EmailLogs already exists — skipped';
GO

-- Step 3: PasswordResetTokens (FK uses whichever user table exists)
DECLARE @UserTable2 SYSNAME;
DECLARE @Sql NVARCHAR(MAX);

SELECT TOP 1 @UserTable2 = t.TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_SCHEMA = 'dbo'
  AND t.TABLE_NAME IN ('Users', 'AppUsers')
ORDER BY CASE t.TABLE_NAME WHEN 'Users' THEN 0 ELSE 1 END;

IF @UserTable2 IS NULL
BEGIN
    RAISERROR('Cannot create PasswordResetTokens: user table still missing.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'[dbo].[PasswordResetTokens]', N'U') IS NULL
BEGIN
    SET @Sql = N'
    CREATE TABLE [dbo].[PasswordResetTokens] (
        [Id]         INT            IDENTITY(1,1) NOT NULL,
        [UserId]     INT            NOT NULL,
        [TokenHash]  NVARCHAR(MAX)  NOT NULL,
        [ExpiresAt]  DATETIME2      NOT NULL,
        [IsUsed]     BIT            NOT NULL DEFAULT 0,
        [CreatedAt]  DATETIME2      NOT NULL,
        [UpdatedAt]  DATETIME2      NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_Users_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[' + @UserTable2 + N'] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_PasswordResetTokens_UserId_IsUsed]
        ON [dbo].[PasswordResetTokens] ([UserId], [IsUsed]);';

    EXEC sp_executesql @Sql;
    PRINT 'Created table PasswordResetTokens';
END
ELSE
    PRINT 'PasswordResetTokens already exists — skipped';
GO
