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


