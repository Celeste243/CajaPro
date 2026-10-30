USE CajaPro;
GO

-- 1) Guardar los productos que ya tenés
SELECT IdProductos, Codigo, Nombre, Categoria, Precio, Stock, Estado
INTO #ProductosViejos
FROM Productos;
GO

-- 2) Sacar las relaciones viejas (si las hubiera)
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))
    + '.' + QUOTENAME(OBJECT_NAME(parent_object_id))
    + ' DROP CONSTRAINT ' + QUOTENAME(name) + ';'
FROM sys.foreign_keys;
EXEC sp_executesql @sql;
GO

-- 3) Borrar las tablas viejas
DROP TABLE IF EXISTS DetalleVenta;
DROP TABLE IF EXISTS Ventas;
DROP TABLE IF EXISTS Clientes;
DROP TABLE IF EXISTS Productos;
GO

-- 4) Crear las tablas bien armadas
CREATE TABLE Clientes (
    IdCliente int IDENTITY(1,1) PRIMARY KEY,
    Nombre    nvarchar(100) NOT NULL,
    DNI       nvarchar(20)  NOT NULL,
    Telefono  nvarchar(30)  NULL,
    Email     nvarchar(100) NULL,
    Estado    bit NOT NULL DEFAULT 1
);

CREATE TABLE Productos (
    IdProductos int IDENTITY(1,1) PRIMARY KEY,
    Codigo      varchar(50)   NOT NULL UNIQUE,
    Nombre      nvarchar(100) NOT NULL,
    Categoria   nvarchar(50)  NULL,
    Precio      decimal(10,2) NOT NULL,
    Stock       int NOT NULL DEFAULT 0,
    Estado      bit NOT NULL DEFAULT 1
);

CREATE TABLE Ventas (
    IdVenta   int IDENTITY(1,1) PRIMARY KEY,
    Fecha     datetime NOT NULL DEFAULT GETDATE(),
    IdCliente int NOT NULL REFERENCES Clientes(IdCliente),
    FormaPago nvarchar(30) NULL,
    Total     decimal(12,2) NOT NULL
);

CREATE TABLE DetalleVenta (
    Id             int IDENTITY(1,1) PRIMARY KEY,
    IdVenta        int NOT NULL REFERENCES Ventas(IdVenta),
    IdProductos    int NOT NULL REFERENCES Productos(IdProductos),
    Cantidad       int NOT NULL,
    PrecioUnitario decimal(10,2) NOT NULL
);
GO

-- 5) Volver a cargar tus productos y crear Consumidor Final
INSERT INTO Productos (Codigo, Nombre, Categoria, Precio, Stock, Estado)
SELECT ISNULL(Codigo, 'SIN-CODIGO-' + CAST(IdProductos AS varchar(10))),
       ISNULL(RTRIM(Nombre), 'Sin nombre'),
       Categoria,
       ISNULL(Precio, 0),
       ISNULL(Stock, 0),
       ISNULL(Estado, 1)
FROM #ProductosViejos;

INSERT INTO Clientes (Nombre, DNI) VALUES (N'Consumidor Final', N'00000000');
GO

-- 6) Ver cómo quedó
SELECT * FROM Productos;
SELECT * FROM Clientes;