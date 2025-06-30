-- =============================================
-- SPs del sistema
-- =============================================

-- =============================================
-- Procedimiento almacenado: sp_validar_credenciales
-- Descripción: Valida el login/email y contraseña de un usuario activo
-- =============================================
-- 1) Extender tabla usuarios
ALTER TABLE dbo.usuarios
ADD
    password_salt        UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_usuarios_salt DEFAULT NEWID(),
    failed_login_count   INT             NOT NULL
        CONSTRAINT DF_usuarios_failed_login DEFAULT 0,
    lockout_until        DATETIME2(0)    NULL,
    password_expiry      DATETIME2(0)    NULL;

-- 2) Procedimiento almacenado reforzado
-- =============================================
-- Procedimiento almacenado: sp_validar_credenciales
-- Descripción: Valida credenciales y retorna datos de usuario y perfil
-- =============================================
CREATE OR ALTER PROCEDURE dbo.sp_validar_credenciales_por_email
    @correo               NVARCHAR(255),          -- email para validación
    @password             NVARCHAR(100),          -- contraseña en texto plano
    @resultado            INT           OUTPUT,   -- 0=OK,1=no existe/inactivo,2=bloqueado,3=clave incorrecta,4=expirada
    @out_usuario_id       INT           OUTPUT,
    @out_perfil_id        INT           OUTPUT,
    @out_usuario_login    NVARCHAR(100) OUTPUT,   -- para mostrar el nombre de usuario
    @out_perfil_nombre    NVARCHAR(100) OUTPUT    -- para mostrar el perfil
WITH ENCRYPTION, RECOMPILE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @salt            UNIQUEIDENTIFIER,
        @stored_hash     NVARCHAR(512),
        @computed_hash   NVARCHAR(512),
        @now             DATETIME2(0) = SYSUTCDATETIME(),
        @failed_count    INT,
        @lock_until      DATETIME2(0),
        @expiry          DATETIME2(0);

    -- 1) Cargar datos por email
    SELECT 
        @out_usuario_id    = u.usuario_id,
        @salt              = u.password_salt,
        @stored_hash       = u.usuario_password_hash,
        @failed_count      = u.failed_login_count,
        @lock_until        = u.lockout_until,
        @expiry            = u.password_expiry,
        @out_perfil_id     = p.perfil_id,
        @out_usuario_login = u.usuario_login,
        @out_perfil_nombre = p.perfil_nombre
    FROM dbo.usuarios u
    JOIN dbo.perfiles p 
      ON u.perfil_id = p.perfil_id
    WHERE u.usuario_email  = @correo
      AND u.usuario_estado = 1
      AND p.perfil_estado   = 1;

    IF @out_usuario_id IS NULL
    BEGIN
        SET @resultado = 1;  -- no existe o inactivo
        RETURN;
    END

    IF @lock_until IS NOT NULL AND @lock_until > @now
    BEGIN
        SET @resultado = 2;  -- bloqueado
        RETURN;
    END

    IF @expiry IS NOT NULL AND @expiry < @now
    BEGIN
        SET @resultado = 4;  -- expirada
        RETURN;
    END

    -- 2) Calcular hash con salt
    SET @computed_hash = LOWER(
        CONVERT(NVARCHAR(512),
            HASHBYTES('SHA2_256',
                CONCAT(@password, @salt)
            ), 2
        )
    );

    IF @computed_hash = LOWER(@stored_hash)
    BEGIN
        UPDATE dbo.usuarios
        SET 
            failed_login_count = 0,
            lockout_until      = NULL,
            last_login_at      = @now,
            updated_at         = @now
        WHERE usuario_id = @out_usuario_id;

        SET @resultado = 0;  -- OK
    END
    ELSE
    BEGIN
        UPDATE dbo.usuarios
        SET 
            failed_login_count = @failed_count + 1,
            lockout_until      = CASE 
                                    WHEN @failed_count + 1 >= 5 
                                        THEN DATEADD(MINUTE, 15, @now)
                                    ELSE lockout_until
                                 END,
            updated_at = @now
        WHERE usuario_id = @out_usuario_id;

        SET @resultado = 3;  -- contraseña incorrecta
    END
END;
GO


