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

