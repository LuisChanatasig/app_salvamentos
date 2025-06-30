-- 1) Insertar un perfil de prueba
INSERT INTO dbo.perfiles (perfil_nombre, perfil_descripcion, perfil_estado)
VALUES ('TestProfile', 'Perfil de prueba para login', 1);

-- Recuperar el ID del perfil recién creado
DECLARE @perfil_id INT = SCOPE_IDENTITY();


-- 2) Generar salt, hash y crear usuario de prueba
DECLARE 
    @plain_password NVARCHAR(100) = 'Password123!',
    @salt           UNIQUEIDENTIFIER = NEWID(),
    @hash           NVARCHAR(512);

-- Calcular SHA2_256(password + salt)
SELECT @hash = LOWER(CONVERT(NVARCHAR(512), 
                 HASHBYTES('SHA2_256', CONCAT(@plain_password, @salt)), 2));

INSERT INTO dbo.usuarios (
    usuario_login,
    usuario_email,
    usuario_password_hash,
    password_salt,
    perfil_id,
    usuario_estado,
    password_expiry
)
VALUES (
    'testuser',                           -- login
    'testuser@example.com',               -- email
    @hash,                                -- hash calculado
    @salt,                                -- salt generado
    @perfil_id,                           -- FK al perfil de prueba
    1,                                    -- activo
    DATEADD(DAY, 90, SYSUTCDATETIME())    -- expira en 90 días
);


-- 3) Probar sp_validar_credenciales
DECLARE 
    @resultado       INT,
    @out_usuario_id  INT,
    @out_perfil_id   INT;

EXEC dbo.sp_validar_credenciales
    @usuario        = 'testuser',
    @password       = 'Password123!',
    @resultado      = @resultado      OUTPUT,
    @out_usuario_id = @out_usuario_id OUTPUT,
    @out_perfil_id  = @out_perfil_id  OUTPUT;

-- Mostrar el resultado de la validación
SELECT 
    @resultado      AS resultado,       -- 0=OK
    @out_usuario_id AS usuario_id,     
    @out_perfil_id  AS perfil_id;
