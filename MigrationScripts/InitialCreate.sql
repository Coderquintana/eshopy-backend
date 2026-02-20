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
CREATE TABLE [Products] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Slug] nvarchar(128) NOT NULL,
    [Sku] nvarchar(64) NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Price] decimal(18,2) NOT NULL,
    [CurrencyCode] char(3) NOT NULL,
    [Status] tinyint NOT NULL,
    [StockOnHand] int NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Products_Price_Positive] CHECK ([Price] >= 0),
    CONSTRAINT [CK_Products_Stock_NonNegative] CHECK ([StockOnHand] >= 0)
);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'Catalogo de productos por tenant.';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products';
SET @description = N'Identificador unico del producto.';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'Id';
SET @description = N'Identificador del tenant propietario.';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'TenantId';
SET @description = N'Slug publico del producto (SEO).';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'Slug';
SET @description = N'SKU opcional del producto (normalizado a mayusculas).';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'Sku';
SET @description = N'Nombre visible del producto.';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'Name';
SET @description = N'Descripcion larga del producto.';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'Description';
SET @description = N'Precio unitario del producto.';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'Price';
SET @description = N'Codigo ISO 4217 de moneda.';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'CurrencyCode';
SET @description = N'Estado del producto (0=Draft,1=Active,2=Archived).';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'Status';
SET @description = N'Stock simple disponible.';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'StockOnHand';
SET @description = N'Fecha de creacion en UTC.';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'CreatedAtUtc';
SET @description = N'Fecha de ultima actualizacion en UTC.';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Products', 'COLUMN', N'UpdatedAtUtc';

CREATE INDEX [IX_Products_TenantId_Name] ON [Products] ([TenantId], [Name]);

CREATE INDEX [IX_Products_TenantId_Status] ON [Products] ([TenantId], [Status]);

CREATE INDEX [UQ_Products_TenantId_Sku] ON [Products] ([TenantId], [Sku]) WHERE [Sku] IS NOT NULL;

CREATE UNIQUE INDEX [UQ_Products_TenantId_Slug] ON [Products] ([TenantId], [Slug]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260207035710_InitialCreate', N'10.0.0-preview.3.25171.6');

COMMIT;
GO

