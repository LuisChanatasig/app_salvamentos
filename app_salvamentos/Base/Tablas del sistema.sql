CREATE DATABASE app_autopiezas

USE app_autopiezas

--CREACION DE TABLAS PARA LA BASE--
-- =============================================
-- Tabla de Perfiles de Usuario
-- =============================================

CREATE TABLE dbo.perfiles (
    perfil_id           INT           IDENTITY(1,1) NOT NULL,
    perfil_nombre       NVARCHAR(100) NOT NULL,  -- Nombre único del perfil
    perfil_descripcion  NVARCHAR(500) NULL,      -- Descripción opcional
    perfil_estado       BIT           NOT NULL  -- 1=Activo, 0=Inactivo
        CONSTRAINT CK_perfiles_estado CHECK (perfil_estado IN (0,1)),
    created_at          DATETIME2(0) NOT NULL
        CONSTRAINT DF_perfiles_created_at DEFAULT SYSUTCDATETIME(),
    updated_at          DATETIME2(0) NOT NULL
        CONSTRAINT DF_perfiles_updated_at DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_perfiles PRIMARY KEY CLUSTERED (perfil_id),
    CONSTRAINT UQ_perfiles_nombre UNIQUE (perfil_nombre)
);

-- Índice no clúster para consultas por estado
CREATE INDEX IX_perfiles_estado
    ON dbo.perfiles (perfil_estado);


	-- =============================================
-- Tabla de Usuarios del Sistema
-- =============================================
CREATE TABLE dbo.usuarios (
    usuario_id            INT           IDENTITY(1,1) NOT NULL,
    usuario_login         NVARCHAR(100) NOT NULL,  -- Nombre de usuario único
    usuario_email         NVARCHAR(255) NOT NULL,  -- Correo electrónico único
    usuario_password_hash NVARCHAR(512) NOT NULL,  -- Hash de la contraseña
    perfil_id             INT           NOT NULL,  -- FK a perfiles.perfil_id
    usuario_estado        BIT           NOT NULL  -- 1=Activo, 0=Inactivo
        CONSTRAINT CK_usuarios_estado CHECK (usuario_estado IN (0,1)),
    created_at            DATETIME2(0)  NOT NULL
        CONSTRAINT DF_usuarios_created_at DEFAULT SYSUTCDATETIME(),
    updated_at            DATETIME2(0)  NOT NULL
        CONSTRAINT DF_usuarios_updated_at DEFAULT SYSUTCDATETIME(),
    last_login_at         DATETIME2(0)  NULL,      -- Fecha y hora del último acceso

    CONSTRAINT PK_usuarios PRIMARY KEY CLUSTERED (usuario_id),
    CONSTRAINT UQ_usuarios_login UNIQUE      (usuario_login),
    CONSTRAINT UQ_usuarios_email UNIQUE      (usuario_email),
    CONSTRAINT FK_usuarios_perfiles FOREIGN KEY (perfil_id)
        REFERENCES dbo.perfiles (perfil_id)
);

-- Índices de apoyo
CREATE INDEX IX_usuarios_estado
    ON dbo.usuarios (usuario_estado);

CREATE INDEX IX_usuarios_perfil
    ON dbo.usuarios (perfil_id);
-- Tabla para almacenar la información de los Asegurados (personas o entidades)
CREATE TABLE Asegurados (
    asegurado_id INT IDENTITY(1,1) PRIMARY KEY,
    nombre_completo NVARCHAR(250) NOT NULL, -- Nombre completo del Asegurado
    identificacion NVARCHAR(50) UNIQUE NULL,     -- Cédula, RUC, Pasaporte, etc. (opcional, pero útil)
    telefono NVARCHAR(20) NULL,
    email NVARCHAR(100) NULL,
    direccion NVARCHAR(500) NULL,
    asegurado_estado BIT NOT NULL DEFAULT 1, -- Estado del asegurado (1=Activo, 0=Inactivo)
    created_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by INT NOT NULL,
    updated_by INT NOT NULL,
    CONSTRAINT FK_Asegurados_CreatedBy FOREIGN KEY (created_by) REFERENCES Usuarios(usuario_id),
    CONSTRAINT FK_Asegurados_UpdatedBy FOREIGN KEY (updated_by) REFERENCES Usuarios(usuario_id)
);


-- Tabla para almacenar la información de los Vehículos
CREATE TABLE Vehiculos (
    vehiculo_id INT IDENTITY(1,1) PRIMARY KEY,
    placa NVARCHAR(20) NOT NULL UNIQUE,       -- Placa del vehículo (única)
    marca NVARCHAR(50) NULL,
    modelo NVARCHAR(50) NULL,
    transmision NVARCHAR(50) NULL,
    combustible NVARCHAR(50) NULL,
    cilindraje NVARCHAR(50) NULL,
    anio INT,                                 -- Año del vehículo
    numero_chasis NVARCHAR(100) UNIQUE NULL,       -- Número de Chasis (único, si aplica)
    numero_motor NVARCHAR(100) UNIQUE NULL,         -- Número de Motor (único, si aplica)
    tipo_vehiculo NVARCHAR(50) NULL,               -- Ej. Sedan, SUV, Camioneta, Pickup
    clase NVARCHAR(50) NULL,                  -- Clasificación general del vehículo (ej. 'Particular', 'Comercial', 'Motocicleta')
    color NVARCHAR(50) NULL,
    observaciones NVARCHAR(MAX) NULL,              -- Observaciones generales del vehículo
    vehiculo_estado BIT NOT NULL DEFAULT 1,   -- Estado del vehículo (1=Activo, 0=Inactivo)
    created_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by INT NOT NULL,
    updated_by INT NOT NULL,
    CONSTRAINT FK_Vehiculos_CreatedBy FOREIGN KEY (created_by) REFERENCES Usuarios(usuario_id),
    CONSTRAINT FK_Vehiculos_UpdatedBy FOREIGN KEY (updated_by) REFERENCES Usuarios(usuario_id)
);

-- Tabla para definir los posibles estados de un Caso
CREATE TABLE EstadosCaso (
    estado_id INT IDENTITY(1,1) PRIMARY KEY,
    nombre_estado NVARCHAR(50) NOT NULL UNIQUE, -- Nombre descriptivo del estado (ej. 'CREADO', 'EN ANÁLISIS', 'FINALIZADO')
    descripcion NVARCHAR(250),                  -- Descripción opcional del estado
    activo BIT NOT NULL DEFAULT 1,              -- Indica si el estado está activo para su uso
    created_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by INT NOT NULL,
    updated_by INT NOT NULL,
    CONSTRAINT FK_EstadosCaso_CreatedBy FOREIGN KEY (created_by) REFERENCES Usuarios(usuario_id),
    CONSTRAINT FK_EstadosCaso_UpdatedBy FOREIGN KEY (updated_by) REFERENCES Usuarios(usuario_id)
);

-- Tabla para almacenar la información de cada Caso/Siniestro
CREATE TABLE Casos (
    caso_id INT IDENTITY(1,1) PRIMARY KEY,
    numero_avaluo NVARCHAR(50) NOT NULL UNIQUE,   -- Número de Avalúo (único por caso)
    asegurado_id INT NOT NULL,                    -- Clave foránea a la tabla Asegurados
    vehiculo_id INT NOT NULL,                     -- Clave foránea a la nueva tabla Vehiculos (relación 1:1 con Caso)
    cliente NVARCHAR(100) NOT NULL DEFAULT 'ASEGURADORA DEL SUR MATRIZ', -- Cliente (valor por defecto fijo)
    numero_reclamo NVARCHAR(100) NULL,                 -- Número de Reclamo/Indemnización (alfanumérico)
    fecha_siniestro DATETIME2(0) DEFAULT SYSUTCDATETIME(), -- Fecha en la que se sucedió el siniestro (se recomienda UTC)
    caso_estado_id INT NOT NULL,                  -- Clave foránea a la tabla EstadosCaso
    created_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by INT NOT NULL,
    updated_by INT NOT NULL,
    CONSTRAINT FK_Casos_Asegurado FOREIGN KEY (asegurado_id) REFERENCES Asegurados(asegurado_id),
    CONSTRAINT FK_Casos_Vehiculo FOREIGN KEY (vehiculo_id) REFERENCES Vehiculos(vehiculo_id),
    CONSTRAINT FK_Casos_Estado FOREIGN KEY (caso_estado_id) REFERENCES EstadosCaso(estado_id),
    CONSTRAINT FK_Casos_CreatedBy FOREIGN KEY (created_by) REFERENCES Usuarios(usuario_id),
    CONSTRAINT FK_Casos_UpdatedBy FOREIGN KEY (updated_by) REFERENCES Usuarios(usuario_id)
);

-- Tabla para definir los tipos de documentos que se pueden subir
CREATE TABLE TiposDocumento (
    tipo_documento_id INT IDENTITY(1,1) PRIMARY KEY,
    nombre_tipo NVARCHAR(100) NOT NULL UNIQUE, -- Ej. 'Matrícula', 'Fotos Siniestro', 'Cédula Asegurado', 'Factura'
    ambito_documento NVARCHAR(20) NOT NULL,    -- NUEVA COLUMNA: 'ASEGURADO', 'CASO', 'GENERAL'
    descripcion NVARCHAR(250) NULL,
    activo BIT NOT NULL DEFAULT 1,
    created_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by INT NOT NULL,
    updated_by INT NOT NULL,
    CONSTRAINT FK_TiposDocumento_CreatedBy FOREIGN KEY (created_by) REFERENCES Usuarios(usuario_id),
    CONSTRAINT FK_TiposDocumento_UpdatedBy FOREIGN KEY (updated_by) REFERENCES Usuarios(usuario_id),
    CONSTRAINT CK_TiposDocumento_Ambito CHECK (ambito_documento IN ('ASEGURADO', 'CASO', 'GENERAL'))
);

-- Tabla para almacenar la metadata de los documentos subidos
CREATE TABLE Documentos (
    documento_id INT IDENTITY(1,1) PRIMARY KEY,
    tipo_documento_id INT NOT NULL,             -- Clave foránea a TiposDocumento
    nombre_archivo NVARCHAR(255) NOT NULL,      -- Nombre original del archivo
    contenido_binario VARBINARY(MAX) NOT NULL,  -- Contenido binario del archivo (imagen, PDF, etc.)
    observaciones NVARCHAR(MAX) NULL,           -- Observaciones o descripción del documento
    fecha_subida DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),

    -- Claves foráneas para vincular el documento a un Caso o Asegurado
    caso_id INT NULL,                           -- Clave foránea a la tabla Casos (NULL si es un documento de Asegurado)
    asegurado_id INT NULL,                      -- Clave foránea a la tabla Asegurados (NULL si es un documento de Caso)

    documento_estado BIT NOT NULL DEFAULT 1,    -- Estado del documento (1=Activo, 0=Inactivo/Eliminado lógicamente)
    created_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by INT NOT NULL,
    updated_by INT NOT NULL,

    CONSTRAINT FK_Documentos_TipoDocumento FOREIGN KEY (tipo_documento_id) REFERENCES TiposDocumento(tipo_documento_id),
    CONSTRAINT FK_Documentos_Caso FOREIGN KEY (caso_id) REFERENCES Casos(caso_id),
    CONSTRAINT FK_Documentos_Asegurado FOREIGN KEY (asegurado_id) REFERENCES Asegurados(asegurado_id),
    CONSTRAINT FK_Documentos_CreatedBy FOREIGN KEY (created_by) REFERENCES Usuarios(usuario_id),
    CONSTRAINT FK_Documentos_UpdatedBy FOREIGN KEY (updated_by) REFERENCES Usuarios(usuario_id),

    -- Restricción para asegurar que un documento esté vinculado a UN caso O UN asegurado
    CONSTRAINT CK_Documentos_CasoOAsegurado CHECK (caso_id IS NOT NULL OR asegurado_id IS NOT NULL)
);

-- Tipo de tabla para documentos asociados a un Asegurado
-- IMPORTANTE: Si este tipo de tabla ya está en uso por un SP o función,
-- necesitarás eliminar esos objetos primero antes de poder modificar el tipo.
IF TYPE_ID('dbo.DocumentoAseguradoTableType') IS NOT NULL
    DROP TYPE dbo.DocumentoAseguradoTableType;
GO

CREATE TYPE dbo.DocumentoAseguradoTableType AS TABLE (
    tipo_documento_id INT  NULL,
    nombre_archivo NVARCHAR(255) NULL,
    ruta_fisica NVARCHAR(MAX) NULL, -- CAMBIO: Ahora almacena la ruta física
    observaciones NVARCHAR(MAX) NULL
);
GO

-- Tipo de tabla para documentos asociados a un Caso
-- IMPORTANTE: Si este tipo de tabla ya está en uso por un SP o función,
-- necesitarás eliminar esos objetos primero antes de poder modificar el tipo.
IF TYPE_ID('dbo.DocumentoCasoTableType') IS NOT NULL
    DROP TYPE dbo.DocumentoCasoTableType;
GO

CREATE TYPE dbo.DocumentoCasoTableType AS TABLE (
    tipo_documento_id INT  NULL,
    nombre_archivo NVARCHAR(255)  NULL,
    ruta_fisica NVARCHAR(MAX)  NULL, -- CAMBIO: Ahora almacena la ruta física
    observaciones NVARCHAR(MAX) NULL
);
GO