IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(120) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [Doctors] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(120) NOT NULL,
        [Headline] nvarchar(160) NULL,
        [Bio] nvarchar(2000) NULL,
        [MedicalCode] nvarchar(30) NULL,
        [Phone] nvarchar(20) NULL,
        [PhotoPath] nvarchar(260) NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Doctors] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [Patients] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(120) NOT NULL,
        [Mobile] nvarchar(11) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Patients] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [Services] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Services] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [Stages] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(60) NOT NULL,
        [Description] nvarchar(200) NULL,
        [Color] nvarchar(9) NOT NULL,
        [Icon] nvarchar(40) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Stages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [DoctorSecretaries] (
        [DoctorId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_DoctorSecretaries] PRIMARY KEY ([DoctorId], [UserId]),
        CONSTRAINT [FK_DoctorSecretaries_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DoctorSecretaries_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [TimeOffs] (
        [Id] int NOT NULL IDENTITY,
        [DoctorId] int NOT NULL,
        [Date] date NOT NULL,
        [Reason] nvarchar(200) NULL,
        CONSTRAINT [PK_TimeOffs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TimeOffs_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [WeeklySchedules] (
        [Id] int NOT NULL IDENTITY,
        [DoctorId] int NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [SlotMinutes] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_WeeklySchedules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WeeklySchedules_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [DoctorServices] (
        [DoctorId] int NOT NULL,
        [ServiceId] int NOT NULL,
        CONSTRAINT [PK_DoctorServices] PRIMARY KEY ([DoctorId], [ServiceId]),
        CONSTRAINT [FK_DoctorServices_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DoctorServices_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [Appointments] (
        [Id] int NOT NULL IDENTITY,
        [TrackingCode] nvarchar(8) NOT NULL,
        [DoctorId] int NOT NULL,
        [PatientId] int NOT NULL,
        [ServiceId] int NULL,
        [Date] date NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [StageId] int NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [Amount] bigint NULL,
        [IsPaid] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ConfirmedAt] datetime2 NULL,
        CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Appointments_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Appointments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Appointments_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Appointments_Stages_StageId] FOREIGN KEY ([StageId]) REFERENCES [Stages] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE TABLE [StageLogs] (
        [Id] int NOT NULL IDENTITY,
        [AppointmentId] int NOT NULL,
        [StageId] int NOT NULL,
        [ChangedBy] nvarchar(120) NULL,
        [Note] nvarchar(500) NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StageLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StageLogs_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StageLogs_Stages_StageId] FOREIGN KEY ([StageId]) REFERENCES [Stages] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_Date_StageId] ON [Appointments] ([Date], [StageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Appointments_DoctorId_Date_StartTime] ON [Appointments] ([DoctorId], [Date], [StartTime]) WHERE [Status] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_PatientId] ON [Appointments] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_ServiceId] ON [Appointments] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_StageId] ON [Appointments] ([StageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Appointments_TrackingCode] ON [Appointments] ([TrackingCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DoctorSecretaries_UserId] ON [DoctorSecretaries] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DoctorServices_ServiceId] ON [DoctorServices] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Patients_Mobile] ON [Patients] ([Mobile]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StageLogs_AppointmentId] ON [StageLogs] ([AppointmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StageLogs_StageId] ON [StageLogs] ([StageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TimeOffs_DoctorId_Date] ON [TimeOffs] ([DoctorId], [Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WeeklySchedules_DoctorId_DayOfWeek] ON [WeeklySchedules] ([DoctorId], [DayOfWeek]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015055333_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015055333_InitialCreate', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [MustChangePassword] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE TABLE [InventoryGroups] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(300) NULL,
        [Color] nvarchar(7) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_InventoryGroups] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE TABLE [InventoryFields] (
        [Id] int NOT NULL IDENTITY,
        [GroupId] int NOT NULL,
        [Name] nvarchar(60) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_InventoryFields] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryFields_InventoryGroups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [InventoryGroups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE TABLE [InventoryItems] (
        [Id] int NOT NULL IDENTITY,
        [GroupId] int NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Code] nvarchar(40) NULL,
        [Manufacturer] nvarchar(120) NULL,
        [Country] nvarchar(60) NULL,
        [Supplier] nvarchar(120) NULL,
        [Unit] nvarchar(30) NOT NULL,
        [PurchasePrice] bigint NULL,
        [SalePrice] bigint NULL,
        [MinStock] decimal(12,2) NOT NULL,
        [Storage] nvarchar(120) NULL,
        [Notes] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_InventoryItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryItems_InventoryGroups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [InventoryGroups] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE TABLE [InventoryBatches] (
        [Id] int NOT NULL IDENTITY,
        [ItemId] int NOT NULL,
        [BatchNo] nvarchar(60) NULL,
        [ExpiryDate] date NULL,
        [Quantity] decimal(12,2) NOT NULL,
        [UnitCost] bigint NULL,
        [ReceivedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_InventoryBatches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryBatches_InventoryItems_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [InventoryItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE TABLE [InventoryValues] (
        [ItemId] int NOT NULL,
        [FieldId] int NOT NULL,
        [Value] nvarchar(300) NOT NULL,
        CONSTRAINT [PK_InventoryValues] PRIMARY KEY ([ItemId], [FieldId]),
        CONSTRAINT [FK_InventoryValues_InventoryFields_FieldId] FOREIGN KEY ([FieldId]) REFERENCES [InventoryFields] ([Id]),
        CONSTRAINT [FK_InventoryValues_InventoryItems_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [InventoryItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE TABLE [InventoryMovements] (
        [Id] int NOT NULL IDENTITY,
        [ItemId] int NOT NULL,
        [BatchId] int NULL,
        [Type] int NOT NULL,
        [Quantity] decimal(12,2) NOT NULL,
        [BalanceAfter] decimal(12,2) NOT NULL,
        [UnitPrice] bigint NULL,
        [Reason] nvarchar(40) NULL,
        [Note] nvarchar(300) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_InventoryMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryMovements_InventoryBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [InventoryBatches] ([Id]),
        CONSTRAINT [FK_InventoryMovements_InventoryItems_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [InventoryItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE INDEX [IX_InventoryBatches_ItemId_ExpiryDate] ON [InventoryBatches] ([ItemId], [ExpiryDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE INDEX [IX_InventoryFields_GroupId] ON [InventoryFields] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE INDEX [IX_InventoryItems_Code] ON [InventoryItems] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE INDEX [IX_InventoryItems_GroupId] ON [InventoryItems] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE INDEX [IX_InventoryMovements_BatchId] ON [InventoryMovements] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE INDEX [IX_InventoryMovements_CreatedAt] ON [InventoryMovements] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE INDEX [IX_InventoryMovements_ItemId] ON [InventoryMovements] ([ItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    CREATE INDEX [IX_InventoryValues_FieldId] ON [InventoryValues] ([FieldId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191714_InventoryAndSecurity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120191714_InventoryAndSecurity', N'10.0.13');
END;

COMMIT;
GO

