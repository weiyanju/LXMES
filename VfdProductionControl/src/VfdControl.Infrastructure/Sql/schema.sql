IF OBJECT_ID('dbo.Stations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Stations
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Stations PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Stations_IsActive DEFAULT 1,
        StationJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_Stations_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt DATETIMEOFFSET NULL
    );
END;

IF OBJECT_ID('dbo.StationSlots', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StationSlots
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_StationSlots PRIMARY KEY,
        StationId UNIQUEIDENTIFIER NOT NULL,
        SlotNumber INT NOT NULL,
        DisplayName NVARCHAR(120) NOT NULL,
        PortName NVARCHAR(32) NULL,
        VfdAddress TINYINT NOT NULL,
        VoltageMeterAddress TINYINT NOT NULL,
        CurrentMeterAddress TINYINT NOT NULL,
        BaudRate INT NOT NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_StationSlots_IsEnabled DEFAULT 1,
        ConfigJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_StationSlots_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_StationSlots_Stations FOREIGN KEY (StationId) REFERENCES dbo.Stations(Id)
    );
END;

IF OBJECT_ID('dbo.DeviceModels', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeviceModels
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_DeviceModels PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        DeviceType NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_DeviceModels_IsActive DEFAULT 1,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_DeviceModels_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt DATETIMEOFFSET NULL
    );
END;

IF OBJECT_ID('dbo.LogicalPoints', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LogicalPoints
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LogicalPoints PRIMARY KEY,
        DeviceModelId UNIQUEIDENTIFIER NOT NULL,
        LogicalKey NVARCHAR(100) NOT NULL,
        DisplayName NVARCHAR(200) NOT NULL,
        AccessMode NVARCHAR(50) NOT NULL,
        FunctionCode NVARCHAR(20) NOT NULL,
        RegisterAddress NVARCHAR(50) NOT NULL,
        DataType NVARCHAR(50) NOT NULL,
        Unit NVARCHAR(50) NULL,
        Description NVARCHAR(500) NULL,
        IsCustom BIT NOT NULL CONSTRAINT DF_LogicalPoints_IsCustom DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_LogicalPoints_IsActive DEFAULT 1,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_LogicalPoints_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_LogicalPoints_DeviceModels FOREIGN KEY (DeviceModelId) REFERENCES dbo.DeviceModels(Id)
    );
END;

IF OBJECT_ID('dbo.LogicalPointWriteOptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LogicalPointWriteOptions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LogicalPointWriteOptions PRIMARY KEY,
        LogicalPointId UNIQUEIDENTIFIER NOT NULL,
        Value NVARCHAR(100) NOT NULL,
        DisplayText NVARCHAR(200) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_LogicalPointWriteOptions_SortOrder DEFAULT 0,
        CONSTRAINT FK_LogicalPointWriteOptions_LogicalPoints FOREIGN KEY (LogicalPointId) REFERENCES dbo.LogicalPoints(Id)
    );
END;

IF OBJECT_ID('dbo.ProcessPlans', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcessPlans
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProcessPlans PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ProcessPlans_IsActive DEFAULT 1,
        PlanJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ProcessPlans_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt DATETIMEOFFSET NULL
    );
END;

IF OBJECT_ID('dbo.ProcessPlanVersions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcessPlanVersions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProcessPlanVersions PRIMARY KEY,
        ProcessPlanId UNIQUEIDENTIFIER NOT NULL,
        VersionNumber INT NOT NULL,
        IsExecutable BIT NOT NULL,
        StepsJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ProcessPlanVersions_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        PublishedAt DATETIMEOFFSET NULL,
        Remark NVARCHAR(500) NULL,
        CONSTRAINT FK_ProcessPlanVersions_ProcessPlans FOREIGN KEY (ProcessPlanId) REFERENCES dbo.ProcessPlans(Id)
    );
END;

IF OBJECT_ID('dbo.ProcessSteps', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcessSteps
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProcessSteps PRIMARY KEY,
        PlanVersionId UNIQUEIDENTIFIER NOT NULL,
        Sequence INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        StepType NVARCHAR(50) NOT NULL,
        TargetPointKey NVARCHAR(100) NULL,
        CommandValue NVARCHAR(200) NULL,
        CompareLeftPointKey NVARCHAR(100) NULL,
        CompareRightPointKey NVARCHAR(100) NULL,
        ToleranceType NVARCHAR(50) NULL,
        ToleranceValue DECIMAL(18,6) NULL,
        RuleType NVARCHAR(50) NULL,
        LowerLimit DECIMAL(18,6) NULL,
        UpperLimit DECIMAL(18,6) NULL,
        ExpectedValue NVARCHAR(200) NULL,
        FailureAction NVARCHAR(50) NOT NULL,
        MaxRetries INT NOT NULL CONSTRAINT DF_ProcessSteps_MaxRetries DEFAULT 0,
        AffectsFinalConclusion BIT NOT NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ProcessSteps_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT FK_ProcessSteps_ProcessPlanVersions FOREIGN KEY (PlanVersionId) REFERENCES dbo.ProcessPlanVersions(Id)
    );
END;

IF OBJECT_ID('dbo.StationSessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StationSessions
    (
        SessionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_StationSessions PRIMARY KEY,
        StationId UNIQUEIDENTIFIER NOT NULL,
        ProcessPlanVersionId UNIQUEIDENTIFIER NULL,
        OperatorCode NVARCHAR(64) NOT NULL,
        StartedAt DATETIMEOFFSET NOT NULL,
        EndedAt DATETIMEOFFSET NULL,
        Conclusion NVARCHAR(50) NULL,
        SessionJson NVARCHAR(MAX) NULL
    );
END;

IF OBJECT_ID('dbo.DeviceRuns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeviceRuns
    (
        DeviceRunId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_DeviceRuns PRIMARY KEY,
        SessionId UNIQUEIDENTIFIER NOT NULL,
        SlotId UNIQUEIDENTIFIER NOT NULL,
        Barcode NVARCHAR(100) NOT NULL,
        Conclusion NVARCHAR(50) NOT NULL,
        StartedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_DeviceRuns_StartedAt DEFAULT SYSDATETIMEOFFSET(),
        CompletedAt DATETIMEOFFSET NULL,
        RunJson NVARCHAR(MAX) NULL,
        CONSTRAINT FK_DeviceRuns_StationSessions FOREIGN KEY (SessionId) REFERENCES dbo.StationSessions(SessionId)
    );
END;

IF OBJECT_ID('dbo.StepRuns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StepRuns
    (
        StepRunId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_StepRuns PRIMARY KEY,
        DeviceRunId UNIQUEIDENTIFIER NOT NULL,
        ProcessStepId UNIQUEIDENTIFIER NULL,
        Sequence INT NOT NULL,
        StepName NVARCHAR(200) NOT NULL,
        StepType NVARCHAR(50) NULL,
        Conclusion NVARCHAR(50) NOT NULL,
        Message NVARCHAR(500) NULL,
        StartedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_StepRuns_StartedAt DEFAULT SYSDATETIMEOFFSET(),
        CompletedAt DATETIMEOFFSET NULL,
        StepJson NVARCHAR(MAX) NULL,
        CONSTRAINT FK_StepRuns_DeviceRuns FOREIGN KEY (DeviceRunId) REFERENCES dbo.DeviceRuns(DeviceRunId)
    );
END;

IF OBJECT_ID('dbo.MeasurementResults', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MeasurementResults
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MeasurementResults PRIMARY KEY,
        StepRunId UNIQUEIDENTIFIER NOT NULL,
        PointKey NVARCHAR(100) NOT NULL,
        Source NVARCHAR(50) NOT NULL,
        NumericValue DECIMAL(18,6) NULL,
        TextValue NVARCHAR(200) NULL,
        Unit NVARCHAR(50) NULL,
        Conclusion NVARCHAR(50) NULL,
        Message NVARCHAR(500) NULL,
        MeasurementJson NVARCHAR(MAX) NULL,
        CONSTRAINT FK_MeasurementResults_StepRuns FOREIGN KEY (StepRunId) REFERENCES dbo.StepRuns(StepRunId)
    );
END;

IF OBJECT_ID('dbo.ComparisonResults', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComparisonResults
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ComparisonResults PRIMARY KEY,
        StepRunId UNIQUEIDENTIFIER NOT NULL,
        LeftKey NVARCHAR(100) NULL,
        RightKey NVARCHAR(100) NULL,
        PrimaryValue DECIMAL(18,6) NULL,
        ReferenceValue DECIMAL(18,6) NULL,
        DifferenceValue DECIMAL(18,6) NULL,
        DifferencePercent DECIMAL(18,6) NULL,
        ToleranceType NVARCHAR(50) NULL,
        ToleranceValue DECIMAL(18,6) NULL,
        Conclusion NVARCHAR(50) NOT NULL,
        Message NVARCHAR(500) NULL,
        ComparisonJson NVARCHAR(MAX) NULL,
        CONSTRAINT FK_ComparisonResults_StepRuns FOREIGN KEY (StepRunId) REFERENCES dbo.StepRuns(StepRunId)
    );
END;

IF OBJECT_ID('dbo.CommandTraces', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommandTraces
    (
        TraceId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CommandTraces PRIMARY KEY,
        StepRunId UNIQUEIDENTIFIER NOT NULL,
        SlotId UNIQUEIDENTIFIER NOT NULL,
        CommandName NVARCHAR(100) NOT NULL,
        TargetPointKey NVARCHAR(100) NULL,
        RequestJson NVARCHAR(MAX) NOT NULL,
        ResponseJson NVARCHAR(MAX) NOT NULL,
        IsSuccess BIT NOT NULL,
        ErrorCode NVARCHAR(100) NULL,
        Message NVARCHAR(500) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_CommandTraces_StepRuns FOREIGN KEY (StepRunId) REFERENCES dbo.StepRuns(StepRunId)
    );
END;

IF COL_LENGTH('dbo.ProcessPlanVersions', 'StepsJson') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ProcessPlanVersions ALTER COLUMN StepsJson NVARCHAR(MAX) NULL;
END;

IF COL_LENGTH('dbo.StepRuns', 'Message') IS NULL
BEGIN
    ALTER TABLE dbo.StepRuns ADD Message NVARCHAR(500) NULL;
END;

IF COL_LENGTH('dbo.StepRuns', 'StepType') IS NULL
BEGIN
    ALTER TABLE dbo.StepRuns ADD StepType NVARCHAR(50) NULL;
END;

IF COL_LENGTH('dbo.MeasurementResults', 'PointKey') IS NULL AND COL_LENGTH('dbo.MeasurementResults', 'PointName') IS NOT NULL
BEGIN
    EXEC sp_rename 'dbo.MeasurementResults.PointName', 'PointKey', 'COLUMN';
END;

IF COL_LENGTH('dbo.MeasurementResults', 'NumericValue') IS NULL AND COL_LENGTH('dbo.MeasurementResults', 'Value') IS NOT NULL
BEGIN
    EXEC sp_rename 'dbo.MeasurementResults.Value', 'NumericValue', 'COLUMN';
END;
