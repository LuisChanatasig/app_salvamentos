using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace app_salvamentos.Models;

public partial class AppAutopiezasContext : DbContext
{
    public AppAutopiezasContext()
    {
    }

    public AppAutopiezasContext(DbContextOptions<AppAutopiezasContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Perfile> Perfiles { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=app_autopiezas;User Id=sa;Password=Sur2o22--;Encrypt=False;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Perfile>(entity =>
        {
            entity.HasKey(e => e.PerfilId);

            entity.ToTable("perfiles");

            entity.HasIndex(e => e.PerfilEstado, "IX_perfiles_estado");

            entity.HasIndex(e => e.PerfilNombre, "UQ_perfiles_nombre").IsUnique();

            entity.Property(e => e.PerfilId).HasColumnName("perfil_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.PerfilDescripcion)
                .HasMaxLength(500)
                .HasColumnName("perfil_descripcion");
            entity.Property(e => e.PerfilEstado).HasColumnName("perfil_estado");
            entity.Property(e => e.PerfilNombre)
                .HasMaxLength(100)
                .HasColumnName("perfil_nombre");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");

            entity.HasIndex(e => e.UsuarioEstado, "IX_usuarios_estado");

            entity.HasIndex(e => e.PerfilId, "IX_usuarios_perfil");

            entity.HasIndex(e => e.UsuarioEmail, "UQ_usuarios_email").IsUnique();

            entity.HasIndex(e => e.UsuarioLogin, "UQ_usuarios_login").IsUnique();

            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.FailedLoginCount).HasColumnName("failed_login_count");
            entity.Property(e => e.LastLoginAt)
                .HasPrecision(0)
                .HasColumnName("last_login_at");
            entity.Property(e => e.LockoutUntil)
                .HasPrecision(0)
                .HasColumnName("lockout_until");
            entity.Property(e => e.PasswordExpiry)
                .HasPrecision(0)
                .HasColumnName("password_expiry");
            entity.Property(e => e.PasswordSalt)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("password_salt");
            entity.Property(e => e.PerfilId).HasColumnName("perfil_id");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UsuarioEmail)
                .HasMaxLength(255)
                .HasColumnName("usuario_email");
            entity.Property(e => e.UsuarioEstado).HasColumnName("usuario_estado");
            entity.Property(e => e.UsuarioLogin)
                .HasMaxLength(100)
                .HasColumnName("usuario_login");
            entity.Property(e => e.UsuarioPasswordHash)
                .HasMaxLength(512)
                .HasColumnName("usuario_password_hash");

            entity.HasOne(d => d.Perfil).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.PerfilId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_usuarios_perfiles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
