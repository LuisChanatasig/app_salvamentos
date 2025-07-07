-- =============================================
-- SPs del sistema
-- =============================================

-- =============================================
-- Procedimiento almacenado: sp_validar_credenciales
-- Descripci�n: Valida el login/email y contrase�a de un usuario activo
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
-- Descripci�n: Valida credenciales y retorna datos de usuario y perfil
-- =============================================
CREATE OR ALTER PROCEDURE dbo.sp_validar_credenciales_por_identificador
    @correo                  NVARCHAR(255) = NULL, -- Email para validaci�n (opcional)
    @login                   NVARCHAR(100) = NULL, -- Login para validaci�n (opcional)
    @resultado               INT           OUTPUT,  -- 0=OK,1=no existe/inactivo,2=bloqueado,4=expirada (3=clave incorrecta se gestiona en C#)
    @out_usuario_id          INT           OUTPUT,
    @out_perfil_id           INT           OUTPUT,
    @out_usuario_login       NVARCHAR(100) OUTPUT,  -- Para mostrar el nombre de usuario
    @out_perfil_nombre       NVARCHAR(100) OUTPUT,  -- Para mostrar el perfil
    @out_password_hash_almacenado NVARCHAR(512) OUTPUT, -- Para devolver el hash BCrypt almacenado
    @out_password_salt_almacenado UNIQUEIDENTIFIER OUTPUT, -- Para devolver el salt almacenado
    @out_failed_login_count  INT           OUTPUT, -- Para devolver el contador de intentos fallidos
    @out_lockout_until       DATETIME2(0)  OUTPUT, -- Para devolver la fecha de bloqueo
    @out_password_expiry     DATETIME2(0)  OUTPUT  -- Para devolver la fecha de expiraci�n
WITH ENCRYPTION, RECOMPILE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @now DATETIME2(0) = SYSUTCDATETIME();
    DECLARE @usuario_estado_db BIT;

    -- Inicializar par�metros de salida
    SET @resultado = 0;
    SET @out_usuario_id = NULL;
    SET @out_perfil_id = NULL;
    SET @out_usuario_login = NULL;
    SET @out_perfil_nombre = NULL;
    SET @out_password_hash_almacenado = NULL;
    SET @out_password_salt_almacenado = NULL;
    SET @out_failed_login_count = NULL;
    SET @out_lockout_until = NULL;
    SET @out_password_expiry = NULL;

    -- 1) Validar que al menos un identificador (correo o login) sea proporcionado
    IF @correo IS NULL AND @login IS NULL
    BEGIN
        SET @resultado = 1; -- C�digo de error: No se proporcion� identificador
        RETURN;
    END

    -- 2) Cargar datos del usuario por email O login
    SELECT TOP 1 -- Usar TOP 1 por si acaso (aunque los campos deber�an ser �nicos)
        @out_usuario_id         = u.usuario_id,
        @out_password_salt_almacenado = u.password_salt,
        @out_password_hash_almacenado = u.usuario_password_hash,
        @out_failed_login_count = u.failed_login_count,
        @out_lockout_until      = u.lockout_until,
        @out_password_expiry    = u.password_expiry,
        @usuario_estado_db      = u.usuario_estado,
        @out_perfil_id          = p.perfil_id,
        @out_usuario_login      = u.usuario_login,
        @out_perfil_nombre      = p.perfil_nombre
    FROM dbo.usuarios u
    JOIN dbo.perfiles p
      ON u.perfil_id = p.perfil_id
    WHERE (@correo IS NOT NULL AND u.usuario_email = @correo) -- Busca por correo si se proporciona
       OR (@login IS NOT NULL AND u.usuario_login = @login);   -- O busca por login si se proporciona

    -- 3) Evaluar el estado del usuario encontrado
    IF @out_usuario_id IS NULL
    BEGIN
        SET @resultado = 1;  -- Usuario no existe
        RETURN;
    END

    -- Verificar si el usuario o su perfil est�n inactivos
    IF @usuario_estado_db = 0 OR EXISTS (SELECT 1 FROM dbo.perfiles WHERE perfil_id = @out_perfil_id AND perfil_estado = 0)
    BEGIN
        SET @resultado = 1; -- Usuario o perfil inactivo
        RETURN;
    END

    -- Verificar si el usuario est� bloqueado
    IF @out_lockout_until IS NOT NULL AND @out_lockout_until > @now
    BEGIN
        SET @resultado = 2;  -- Usuario bloqueado
        RETURN;
    END

    -- Verificar si la contrase�a ha expirado
    IF @out_password_expiry IS NOT NULL AND @out_password_expiry < @now
    BEGIN
        SET @resultado = 4;  -- Contrase�a expirada
        RETURN;
    END

    -- Si llegamos aqu�, el usuario existe, est� activo y no est� bloqueado/expirado.
    -- La verificaci�n de la contrase�a (BCrypt) y la actualizaci�n de contadores se har�n en C#.
    SET @resultado = 0; -- OK para continuar la verificaci�n en C#
END;

-- 3) Procedimiento almacenado reforzado
-- =============================================
-- Procedimiento almacenado: sp_IncrementarIntentosFallidosYBloquear
-- Descripci�n: Cuenta los intentos fallidos y bloquea el usuario
-- =============================================
CREATE or ALTER PROCEDURE sp_IncrementarIntentosFallidosYBloquear
    @usuario_id INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @now DATETIME2(0) = SYSUTCDATETIME();
    DECLARE @failed_count INT;

    SELECT @failed_count = failed_login_count
    FROM usuarios
    WHERE usuario_id = @usuario_id;

    -- Incrementar el contador de intentos fallidos
    SET @failed_count = ISNULL(@failed_count, 0) + 1;

    UPDATE usuarios
    SET
        failed_login_count = @failed_count,
        lockout_until      = CASE
                                WHEN @failed_count >= 5 -- L�mite de intentos fallidos
                                THEN DATEADD(MINUTE, 15, @now) -- Bloquear por 15 minutos
                                ELSE lockout_until
                              END,
        updated_at         = @now
    WHERE usuario_id = @usuario_id;
END;


-- 4) Procedimiento almacenado reforzado
-- =============================================
-- Procedimiento almacenado: sp_ResetearIntentosFallidosYActualizarLogin
-- Descripci�n: Si luego de varios intentos fallidos el usuario logra ingresar, se reinician los contadores
-- =============================================

CREATE OR ALTER PROCEDURE sp_ResetearIntentosFallidosYActualizarLogin
    @usuario_id INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @now DATETIME2(0) = SYSUTCDATETIME();

    UPDATE usuarios
    SET
        failed_login_count = 0,
        lockout_until      = NULL,
        last_login_at      = @now,
        updated_at         = @now
    WHERE usuario_id = @usuario_id;
END;
-- 5) Procedimiento almacenado reforzado
-- =============================================
-- Procedimiento almacenado: sp_ListarPerfiles
-- Descripci�n: lista los perfiles existentes en la base de datos 
-- =============================================
CREATE OR ALTER PROCEDURE sp_ListarPerfiles
    @incluir_inactivos BIT = 0 -- Par�metro opcional: 0 para solo activos (por defecto), 1 para incluir inactivos
AS
BEGIN
    SET NOCOUNT ON; -- Evita que se devuelvan recuentos de filas afectadas

    -- Declaraci�n de variables para mensajes de error
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    BEGIN TRY
        -- Selecciona los perfiles bas�ndose en el estado
        SELECT
            perfil_id,
            perfil_nombre,
            perfil_descripcion,
            perfil_estado,
            created_at,
            updated_at
        FROM
            perfiles
        WHERE
            perfil_estado = 1 -- Siempre selecciona activos
            OR @incluir_inactivos = 1 -- Si @incluir_inactivos es 1, tambi�n incluye los inactivos
        ORDER BY
            perfil_nombre; -- Ordena los resultados por el nombre del perfil

    END TRY
    BEGIN CATCH
        -- Si ocurre un error, captura los detalles
        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Relanza el error para que la aplicaci�n cliente pueda manejarlo
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);

        -- Opcional: Podr�as devolver un c�digo de error espec�fico aqu� si lo necesitas
        -- RETURN -1;
    END CATCH;
END;



-- 6) Procedimiento almacenado reforzado
-- =============================================
-- Procedimiento almacenado: sp_CrearNuevoUsuario
-- Descripci�n: Ingresa un nuevo usuario y valida su existencia mediante email y login
-- =============================================
CREATE OR ALTER PROCEDURE sp_CrearNuevoUsuario
    @usuario_login NVARCHAR(100),
    @usuario_email NVARCHAR(255),
    @usuario_password_hash NVARCHAR(512),
    @perfil_id INT,
    @password_salt UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON; -- Evita que se devuelvan recuentos de filas afectadas

    -- Declaraci�n de variables para mensajes de error o IDs
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;
    DECLARE @NuevoUsuarioID INT;

    BEGIN TRY
        -- 1. Validaci�n de Unicidad para usuario_login
        IF EXISTS (SELECT 1 FROM usuarios WHERE usuario_login = @usuario_login)
        BEGIN
            -- Lanza un error si el login ya existe
            RAISERROR('El nombre de usuario ya existe. Por favor, elija otro.', 16, 1);
            RETURN -1; -- Retorna un c�digo de error
        END

        -- 2. Validaci�n de Unicidad para usuario_email
        IF EXISTS (SELECT 1 FROM usuarios WHERE usuario_email = @usuario_email)
        BEGIN
            -- Lanza un error si el email ya existe
            RAISERROR('El correo electr�nico ya est� registrado. Por favor, utilice otro.', 16, 1);
            RETURN -2; -- Retorna un c�digo de error
        END

        -- Inicia una transacci�n para asegurar la atomicidad de la operaci�n
        BEGIN TRANSACTION;

        -- 3. Inserci�n del nuevo usuario
        INSERT INTO usuarios (
            usuario_login,
            usuario_email,
            usuario_password_hash,
            perfil_id,
            usuario_estado,           -- Se establece por defecto a 1 (activo)
            created_at,               -- Se establece a la fecha y hora actual
            updated_at,               -- Se establece a la fecha y hora actual
            last_login_at,            -- NULL al crear
            password_salt,
            failed_login_count,       -- 0 al crear
            lockout_until,            -- NULL al crear
            password_expiry           -- Se establece una fecha de expiraci�n inicial
        )
        VALUES (
            @usuario_login,
            @usuario_email,
            @usuario_password_hash,
            @perfil_id,
            1,                        -- usuario_estado: 1 (activo)
            SYSDATETIME(),            -- created_at: Fecha y hora actual
            SYSDATETIME(),            -- updated_at: Fecha y hora actual
            NULL,                     -- last_login_at: NULL
            @password_salt,
            0,                        -- failed_login_count: 0
            NULL,                     -- lockout_until: NULL
            DATEADD(month, 6, SYSDATETIME()) -- password_expiry: Por ejemplo, 6 meses desde la creaci�n
        );

        -- Obtiene el ID del usuario reci�n insertado
        SET @NuevoUsuarioID = SCOPE_IDENTITY();

        -- Confirma la transacci�n si todo fue exitoso
        COMMIT TRANSACTION;

        -- 4. Retorna el ID del nuevo usuario
        SELECT @NuevoUsuarioID AS NuevoUsuarioID;

    END TRY
    BEGIN CATCH
        -- Si ocurre un error, revierte la transacci�n
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Captura los detalles del error
        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Relanza el error para que la aplicaci�n cliente pueda manejarlo
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);

        RETURN -99; -- Retorna un c�digo de error gen�rico para errores de SP
    END CATCH;
END;


-- 7) Procedimiento almacenado reforzado
-- =============================================
-- Procedimiento almacenado: sp_ActualizarContrasenaUsuario
-- Descripci�n: Cambia la contrase�a esto debido a que no se puede quitar el hasheo
-- =============================================
CREATE OR ALTER PROCEDURE sp_ActualizarContrasenaUsuario
    @usuario_id INT,
    @nueva_password_hash NVARCHAR(512),
    @nuevo_password_salt UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON; -- Evita que se devuelvan recuentos de filas afectadas

    -- Declaraci�n de variables para mensajes de error
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    BEGIN TRY
        -- Inicia una transacci�n para asegurar la atomicidad de la operaci�n
        BEGIN TRANSACTION;

        -- 1. Verifica si el usuario existe
        IF NOT EXISTS (SELECT 1 FROM usuarios WHERE usuario_id = @usuario_id)
        BEGIN
            -- Lanza un error si el usuario no se encuentra
            RAISERROR('Usuario no encontrado. No se pudo actualizar la contrase�a.', 16, 1);
            RETURN -1; -- Retorna un c�digo de error
        END

        -- 2. Actualiza la contrase�a, el salt y las columnas relacionadas
        UPDATE usuarios
        SET
            usuario_password_hash = @nueva_password_hash,
            password_salt = @nuevo_password_salt,
            updated_at = SYSDATETIME(), -- Actualiza la fecha de �ltima modificaci�n
            failed_login_count = 0,      -- Reinicia el contador de intentos fallidos
            lockout_until = NULL,        -- Elimina cualquier bloqueo existente
            last_login_at = NULL,        -- Opcional: Podr�as querer resetear esto tambi�n
            password_expiry = DATEADD(month, 6, SYSDATETIME()) -- Establece una nueva fecha de expiraci�n para la contrase�a
        WHERE
            usuario_id = @usuario_id;

        -- Confirma la transacci�n si todo fue exitoso
        COMMIT TRANSACTION;

        -- Opcional: Podr�as devolver un mensaje de �xito o un c�digo de �xito
        SELECT 1 AS Resultado; -- Indica �xito

    END TRY
    BEGIN CATCH
        -- Si ocurre un error, revierte la transacci�n
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Captura los detalles del error
        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Relanza el error para que la aplicaci�n cliente pueda manejarlo
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);

        RETURN -99; -- Retorna un c�digo de error gen�rico para errores de SP
    END CATCH;
END;

-- 8) Procedimiento almacenado reforzado
-- =============================================
-- Procedimiento almacenado: sp_ListarUsuarios
-- Descripci�n: Lista todos los usuarios
-- =============================================
CREATE OR ALTER PROCEDURE sp_ListarUsuarios
    -- Par�metros de Ordenamiento (Opcionales)
    @sort_column NVARCHAR(50) = 'created_at', -- Columna por la que ordenar (ej. 'usuario_login', 'created_at')
    @sort_direction NVARCHAR(4) = 'DESC'      -- Direcci�n del ordenamiento ('ASC' o 'DESC')
AS
BEGIN
    SET NOCOUNT ON;

    -- Declaraci�n de variables para manejo de errores
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    BEGIN TRY
        -- Construir la cl�usula ORDER BY din�micamente
        DECLARE @order_by_clause NVARCHAR(MAX);
        SET @order_by_clause = ' ORDER BY ';

        -- Validar y construir la columna de ordenamiento
        IF @sort_column = 'usuario_id' SET @order_by_clause = @order_by_clause + 'u.usuario_id';
        ELSE IF @sort_column = 'usuario_login' SET @order_by_clause = @order_by_clause + 'u.usuario_login';
        ELSE IF @sort_column = 'usuario_email' SET @order_by_clause = @order_by_clause + 'u.usuario_email';
        ELSE IF @sort_column = 'perfil_id' SET @order_by_clause = @order_by_clause + 'u.perfil_id';
        ELSE IF @sort_column = 'perfil_nombre' SET @order_by_clause = @order_by_clause + 'p.perfil_nombre';
        ELSE IF @sort_column = 'usuario_estado' SET @order_by_clause = @order_by_clause + 'u.usuario_estado';
        ELSE IF @sort_column = 'last_login_at' SET @order_by_clause = @order_by_clause + 'u.last_login_at';
        ELSE IF @sort_column = 'failed_login_count' SET @order_by_clause = @order_by_clause + 'u.failed_login_count';
        ELSE IF @sort_column = 'lockout_until' SET @order_by_clause = @order_by_clause + 'u.lockout_until';
        ELSE IF @sort_column = 'password_expiry' SET @order_by_clause = @order_by_clause + 'u.password_expiry';
        ELSE -- Por defecto, si la columna no es v�lida o no se especifica
            SET @order_by_clause = @order_by_clause + 'u.created_at';

        -- A�adir la direcci�n del ordenamiento
        IF UPPER(@sort_direction) = 'DESC'
            SET @order_by_clause = @order_by_clause + ' DESC';
        ELSE
            SET @order_by_clause = @order_by_clause + ' ASC';

        -- Consulta principal para obtener todos los usuarios con ordenamiento din�mico
        DECLARE @select_sql NVARCHAR(MAX);
        SET @select_sql = '
            SELECT
                u.usuario_id,
                u.usuario_login,
                u.usuario_email,
                u.perfil_id,
                p.perfil_nombre,
                u.usuario_estado,
                u.created_at,
                u.updated_at,
                u.last_login_at,
                u.failed_login_count,
                u.lockout_until,
                u.password_expiry
            FROM usuarios u
            INNER JOIN perfiles p ON u.perfil_id = p.perfil_id'
            + @order_by_clause + ';'; -- Solo la cl�usula ORDER BY

        -- Ejecutar la consulta principal (usuarios)
        EXEC sp_executesql @select_sql;

    END TRY
    BEGIN CATCH
        -- Captura los detalles del error
        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Relanza el error para que la aplicaci�n cliente pueda manejarlo
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END;

-- 9) Procedimiento almacenado reforzado
-- =============================================
-- Procedimiento almacenado: sp_ObtenerUsuarioPorId
-- Descripci�n: Trae el usuario por su id
-- =============================================
CREATE OR ALTER PROCEDURE sp_ObtenerUsuarioPorId
    @usuario_id INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Declaraci�n de variables para manejo de errores
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    BEGIN TRY
        SELECT
            u.usuario_id,
            u.usuario_login,
            u.usuario_email,
            u.perfil_id,
            p.perfil_nombre,           -- Nombre del perfil desde la tabla perfiles
            u.usuario_estado,
            u.created_at,
            u.updated_at,
            u.last_login_at,
            u.failed_login_count,
            u.lockout_until,
            u.password_expiry
        FROM
            dbo.usuarios u
        INNER JOIN
            dbo.perfiles p ON u.perfil_id = p.perfil_id
        WHERE
            u.usuario_id = @usuario_id;
    END TRY
    BEGIN CATCH
        -- Captura los detalles del error
        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Relanza el error para que la aplicaci�n cliente pueda manejarlo
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END;
-- 10) Procedimiento almacenado reforzado
-- =============================================
-- Procedimiento almacenado: sp_EditarUsuario
-- Descripci�n: sp para editar el usuario
-- =============================================
CREATE OR ALTER PROCEDURE sp_EditarUsuario
    @usuario_id INT,
    @usuario_login NVARCHAR(100),
    @usuario_email NVARCHAR(255),
    @perfil_id INT,
    @usuario_estado BIT, -- Para permitir cambiar el estado del usuario
    @resultado INT OUTPUT -- 0=OK, -1=Usuario no encontrado, -2=Login duplicado, -3=Email duplicado, -99=Error interno
AS
BEGIN
    SET NOCOUNT ON;

    -- Declaraci�n de variables para manejo de errores
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    -- Inicializar resultado
    SET @resultado = 0;

    BEGIN TRY
        -- Inicia una transacci�n para asegurar la atomicidad de la operaci�n
        BEGIN TRANSACTION;

        -- 1. Verificar si el usuario existe
        IF NOT EXISTS (SELECT 1 FROM dbo.usuarios WHERE usuario_id = @usuario_id)
        BEGIN
            SET @resultado = -1; -- Usuario no encontrado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 2. Verificar duplicidad de usuario_login (excluyendo al usuario actual)
        IF EXISTS (SELECT 1 FROM dbo.usuarios WHERE usuario_login = @usuario_login AND usuario_id <> @usuario_id)
        BEGIN
            SET @resultado = -2; -- Login duplicado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 3. Verificar duplicidad de usuario_email (excluyendo al usuario actual)
        IF EXISTS (SELECT 1 FROM dbo.usuarios WHERE usuario_email = @usuario_email AND usuario_id <> @usuario_id)
        BEGIN
            SET @resultado = -3; -- Email duplicado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 4. Verificar si el perfil_id existe
        IF NOT EXISTS (SELECT 1 FROM dbo.perfiles WHERE perfil_id = @perfil_id)
        BEGIN
            SET @resultado = -4; -- Perfil no encontrado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 5. Actualizar los datos del usuario
        UPDATE dbo.usuarios
        SET
            usuario_login = @usuario_login,
            usuario_email = @usuario_email,
            perfil_id = @perfil_id,
            usuario_estado = @usuario_estado,
            updated_at = SYSDATETIME() -- Actualizar la fecha de �ltima modificaci�n
        WHERE
            usuario_id = @usuario_id;

        -- Confirma la transacci�n si todo fue exitoso
        COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH
        -- Si ocurre un error, revierte la transacci�n
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Captura los detalles del error
        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Establece un c�digo de error gen�rico para errores internos del SP
        SET @resultado = -99;

        -- Relanza el error para que la aplicaci�n cliente pueda manejarlo (opcional, si quieres el detalle del error)
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END;

-- 11) Procedimiento almacenado reforzado
-- =============================================
-- Procedimiento almacenado: sp_EliminarUsuario
-- Descripci�n: sp para eliminar el usuario
-- =============================================
CREATE OR ALTER PROCEDURE sp_EliminarUsuario
    @usuario_id INT,
    @resultado INT OUTPUT -- 0=OK, -1=Usuario no encontrado, -99=Error interno
AS
BEGIN
    SET NOCOUNT ON;

    -- Declaraci�n de variables para manejo de errores
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    -- Inicializar resultado
    SET @resultado = 0;

    BEGIN TRY
        -- Inicia una transacci�n para asegurar la atomicidad de la operaci�n
        BEGIN TRANSACTION;

        -- 1. Verificar si el usuario existe
        IF NOT EXISTS (SELECT 1 FROM dbo.usuarios WHERE usuario_id = @usuario_id)
        BEGIN
            SET @resultado = -1; -- Usuario no encontrado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 2. Realizar el "soft delete" (cambiar el estado a inactivo)
        UPDATE dbo.usuarios
        SET
            usuario_estado = 0,     -- Marcar como inactivo
            updated_at = SYSDATETIME(), -- Actualizar la fecha de �ltima modificaci�n
            lockout_until = NULL,   -- Opcional: limpiar bloqueo si estaba bloqueado
            failed_login_count = 0  -- Opcional: resetear intentos fallidos
        WHERE
            usuario_id = @usuario_id;

        -- Confirma la transacci�n si todo fue exitoso
        COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH
        -- Si ocurre un error, revierte la transacci�n
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Captura los detalles del error
        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Establece un c�digo de error gen�rico para errores internos del SP
        SET @resultado = -99;

        -- Relanza el error para que la aplicaci�n cliente pueda manejarlo
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END;
