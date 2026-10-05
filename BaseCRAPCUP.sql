
IF DB_ID(N'CajaRuralDB') IS NULL
    CREATE DATABASE CajaRuralDB;
GO

USE CajaRuralDB;
GO


    IdRol        INT IDENTITY(1,1) PRIMARY KEY,
    Nombre       NVARCHAR(50)  NOT NULL UNIQUE,
    Descripcion  NVARCHAR(200) NULL
);
GO

CREATE TABLE Usuarios (
    IdUsuario       INT IDENTITY(1,1) PRIMARY KEY,
    NombreCompleto  NVARCHAR(150) NOT NULL,
    NombreUsuario   NVARCHAR(50)  NOT NULL UNIQUE,
    ContrasenaHash  NVARCHAR(200) NOT NULL,   
    IdRol           INT NOT NULL,
    Activo          BIT NOT NULL DEFAULT 1,
    FechaCreacion   DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (IdRol) REFERENCES Roles(IdRol)
);
GO

CREATE TABLE Socios (
    IdSocio        INT IDENTITY(1,1) PRIMARY KEY,
    Identidad      NVARCHAR(15)  NOT NULL UNIQUE,   
    Nombres        NVARCHAR(100) NOT NULL,
    Apellidos      NVARCHAR(100) NOT NULL,
    Telefono       NVARCHAR(20)  NULL,
    Direccion      NVARCHAR(250) NULL,
    FechaRegistro  DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Activo         BIT NOT NULL DEFAULT 1
);
GO


CREATE TABLE Productos (
    IdProducto  INT IDENTITY(1,1) PRIMARY KEY,
    Nombre      NVARCHAR(100) NOT NULL UNIQUE,
    Precio      DECIMAL(10,2) NOT NULL CHECK (Precio > 0),
    Activo      BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE Ventas (
    IdVenta      INT IDENTITY(1,1) PRIMARY KEY,
    Fecha        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    IdUsuario    INT NOT NULL,
    Total        DECIMAL(12,2) NOT NULL DEFAULT 0 CHECK (Total >= 0),
    Estado       NVARCHAR(20) NOT NULL DEFAULT N'Registrada'
                 CHECK (Estado IN (N'Registrada', N'Anulada')),
    Observacion  NVARCHAR(250) NULL,
    CONSTRAINT FK_Ventas_Usuarios FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario)
);
GO

CREATE TABLE DetalleVenta (
    IdDetalle       INT IDENTITY(1,1) PRIMARY KEY,
    IdVenta         INT NOT NULL,
    IdProducto      INT NOT NULL,
    Cantidad        INT NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario  DECIMAL(10,2) NOT NULL CHECK (PrecioUnitario > 0),
    Subtotal        AS (Cantidad * PrecioUnitario) PERSISTED,
    CONSTRAINT FK_DetalleVenta_Ventas    FOREIGN KEY (IdVenta)    REFERENCES Ventas(IdVenta),
    CONSTRAINT FK_DetalleVenta_Productos FOREIGN KEY (IdProducto) REFERENCES Productos(IdProducto)
);
GO

CREATE TABLE Prestamos (
    IdPrestamo          INT IDENTITY(1,1) PRIMARY KEY,
    IdSocio             INT NOT NULL,
    FechaSolicitud      DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    MontoSolicitado     DECIMAL(12,2) NOT NULL CHECK (MontoSolicitado > 0),
    MontoAprobado       DECIMAL(12,2) NULL CHECK (MontoAprobado > 0),
    TasaInteresMensual  DECIMAL(5,2) NOT NULL DEFAULT 0 CHECK (TasaInteresMensual >= 0),
    PlazoMeses          INT NOT NULL CHECK (PlazoMeses > 0),
    Estado              NVARCHAR(20) NOT NULL DEFAULT N'Solicitado'
                        CHECK (Estado IN (N'Solicitado', N'Aprobado', N'Rechazado', N'Desembolsado', N'Cerrado')),
    IdUsuarioRegistra   INT NOT NULL,   -- quien registro la solicitud
    IdUsuarioResuelve   INT NULL,       -- quien aprobo o rechazo
    FechaResolucion     DATETIME2 NULL,
    FechaDesembolso     DATETIME2 NULL,
    Observacion         NVARCHAR(250) NULL,
    CONSTRAINT FK_Prestamos_Socios    FOREIGN KEY (IdSocio)           REFERENCES Socios(IdSocio),
    CONSTRAINT FK_Prestamos_Registra  FOREIGN KEY (IdUsuarioRegistra) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT FK_Prestamos_Resuelve  FOREIGN KEY (IdUsuarioResuelve) REFERENCES Usuarios(IdUsuario)
);
GO

CREATE TABLE Cuotas (
    IdCuota           INT IDENTITY(1,1) PRIMARY KEY,
    IdPrestamo        INT NOT NULL,
    NumeroCuota       INT NOT NULL,
    FechaVencimiento  DATE NOT NULL,
    MontoCapital      DECIMAL(12,2) NOT NULL CHECK (MontoCapital >= 0),
    MontoInteres      DECIMAL(12,2) NOT NULL DEFAULT 0 CHECK (MontoInteres >= 0),
    MontoCuota        AS (MontoCapital + MontoInteres) PERSISTED,
    MontoPagado       DECIMAL(12,2) NOT NULL DEFAULT 0 CHECK (MontoPagado >= 0),
    Estado            NVARCHAR(20) NOT NULL DEFAULT N'Pendiente'
                      CHECK (Estado IN (N'Pendiente', N'Parcial', N'Pagada')),
    CONSTRAINT UQ_Cuotas_Prestamo_Numero UNIQUE (IdPrestamo, NumeroCuota),
    CONSTRAINT FK_Cuotas_Prestamos FOREIGN KEY (IdPrestamo) REFERENCES Prestamos(IdPrestamo)
);
GO


CREATE TABLE Pagos (
    IdPago       INT IDENTITY(1,1) PRIMARY KEY,
    IdPrestamo   INT NOT NULL,
    IdCuota      INT NOT NULL,
    FechaPago    DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Monto        DECIMAL(12,2) NOT NULL CHECK (Monto > 0),
    IdUsuario    INT NOT NULL,
    Observacion  NVARCHAR(250) NULL,
    CONSTRAINT FK_Pagos_Prestamos FOREIGN KEY (IdPrestamo) REFERENCES Prestamos(IdPrestamo),
    CONSTRAINT FK_Pagos_Cuotas    FOREIGN KEY (IdCuota)    REFERENCES Cuotas(IdCuota),
    CONSTRAINT FK_Pagos_Usuarios  FOREIGN KEY (IdUsuario)  REFERENCES Usuarios(IdUsuario)
);
GO


CREATE TABLE MovimientosCapital (
    IdMovimiento  INT IDENTITY(1,1) PRIMARY KEY,
    Fecha         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Tipo          NVARCHAR(10) NOT NULL CHECK (Tipo IN (N'Ingreso', N'Egreso')),
    Categoria     NVARCHAR(30) NOT NULL
                  CHECK (Categoria IN (N'Venta', N'Aporte', N'Desembolso', N'PagoPrestamo', N'Gasto', N'Otro')),
    Monto         DECIMAL(12,2) NOT NULL CHECK (Monto > 0),
    Descripcion   NVARCHAR(250) NULL,
    IdVenta       INT NULL,
    IdPrestamo    INT NULL,
    IdPago        INT NULL,
    IdUsuario     INT NOT NULL,
    CONSTRAINT FK_Mov_Ventas    FOREIGN KEY (IdVenta)    REFERENCES Ventas(IdVenta),
    CONSTRAINT FK_Mov_Prestamos FOREIGN KEY (IdPrestamo) REFERENCES Prestamos(IdPrestamo),
    CONSTRAINT FK_Mov_Pagos     FOREIGN KEY (IdPago)     REFERENCES Pagos(IdPago),
    CONSTRAINT FK_Mov_Usuarios  FOREIGN KEY (IdUsuario)  REFERENCES Usuarios(IdUsuario)
);
GO

CREATE TABLE Bitacora (
    IdBitacora   BIGINT IDENTITY(1,1) PRIMARY KEY,
    Fecha        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    IdUsuario    INT NULL,
    Modulo       NVARCHAR(50)  NOT NULL,
    Accion       NVARCHAR(50)  NOT NULL,
    Descripcion  NVARCHAR(400) NULL,
    CONSTRAINT FK_Bitacora_Usuarios FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario)
);
GO


CREATE INDEX IX_Ventas_Fecha            ON Ventas(Fecha);
CREATE INDEX IX_DetalleVenta_Venta      ON DetalleVenta(IdVenta);
CREATE INDEX IX_Prestamos_Socio         ON Prestamos(IdSocio);
CREATE INDEX IX_Prestamos_Estado        ON Prestamos(Estado);
CREATE INDEX IX_Cuotas_Vencimiento      ON Cuotas(FechaVencimiento, Estado);
CREATE INDEX IX_Pagos_Prestamo          ON Pagos(IdPrestamo);
CREATE INDEX IX_Movimientos_Fecha       ON MovimientosCapital(Fecha);
CREATE INDEX IX_Bitacora_Fecha          ON Bitacora(Fecha);
GO


CREATE VIEW vw_SaldoCapital AS
SELECT COALESCE(SUM(CASE WHEN Tipo = N'Ingreso' THEN Monto ELSE -Monto END), 0) AS SaldoDisponible
FROM MovimientosCapital;
GO


INSERT INTO Roles (Nombre, Descripcion) VALUES- de
(N'Administrador', N'Presidente o vicepresidente: acceso total, aprueba prestamos y ve todos los reportes'),
(N'Secretaria',    N'Registra socios, ventas, capital, prestamos y pagos'),
(N'Consulta',      N'Solo consulta reportes y el panel de control');
GO


USE CajaRuralDB;
SELECT name FROM sys.tables ORDER BY name;   -ben salir 11 tablas
SELECT * FROM Roles;                         
SELECT * FROM vw_SaldoCapital;              