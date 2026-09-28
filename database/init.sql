IF DB_ID('VehicleStatusDb') IS NULL
BEGIN
    CREATE DATABASE VehicleStatusDb;
END
GO

USE VehicleStatusDb;
GO


-- ==============================
-- Vehicles
-- ==============================

IF OBJECT_ID('dbo.Vehicles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Vehicles
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,

        PlateNumber NVARCHAR(20) NOT NULL,

        Model NVARCHAR(100) NOT NULL,

        Status NVARCHAR(20) NOT NULL,

        CreateTime DATETIME2 NOT NULL
            CONSTRAINT DF_Vehicles_CreateTime
            DEFAULT SYSDATETIME(),

        CONSTRAINT UQ_Vehicles_PlateNumber
            UNIQUE (PlateNumber)
    );
END
GO


-- ==============================
-- Vehicle Status Records
-- ==============================

IF OBJECT_ID('dbo.VehicleStatusRecords', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleStatusRecords
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,

        VehicleId INT NOT NULL,

        Temperature DECIMAL(5,2) NOT NULL,

        Speed DECIMAL(6,2) NOT NULL,

        Battery DECIMAL(5,2) NOT NULL,

        RecordTime DATETIME2 NOT NULL
            CONSTRAINT DF_VehicleStatusRecords_RecordTime
            DEFAULT SYSDATETIME(),

        CONSTRAINT FK_VehicleStatusRecords_Vehicles
            FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id),

        CONSTRAINT CK_VehicleStatusRecords_Battery
            CHECK (Battery >= 0 AND Battery <= 100)
    );
END
GO


-- ==============================
-- Users
-- ==============================

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,

        Username NVARCHAR(50) NOT NULL,

        PasswordHash NVARCHAR(500) NOT NULL,

        Role NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Users_Role
            DEFAULT 'User',

        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Users_CreatedAt
            DEFAULT SYSDATETIME(),

        CONSTRAINT UQ_Users_Username
            UNIQUE (Username)
    );
END
GO