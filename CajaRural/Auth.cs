using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace CajaRural;

// Datos del usuario que inició sesión (disponibles en toda la aplicación)
public static class Sesion
{
    public static int IdUsuario { get; private set; }
    public static string NombreUsuario { get; private set; } = "";
    public static string Rol { get; private set; } = "";

    public static void Iniciar(int id, string usuario, string rol)
    {
        IdUsuario = id; NombreUsuario = usuario; Rol = rol;
    }

    public static void Cerrar()
    {
        IdUsuario = 0; NombreUsuario = ""; Rol = "";
    }
}

public static class AuthServicio
{
    // Si tu instancia es SQL Express, cambia "Server=." por "Server=.\\SQLEXPRESS"
    private const string Cadena =
        "Server=.;Database=CajaRuralDB;Trusted_Connection=True;TrustServerCertificate=True;";

    // Devuelve el id y el rol si usuario y contraseña son correctos; si no, null
    public static (int Id, string Rol)? Autenticar(string usuario, string contrasena)
    {
        using var cn = new SqlConnection(Cadena);
        cn.Open();
        using var cmd = new SqlCommand(
            @"SELECT u.IdUsuario, u.ContrasenaHash, r.Nombre
              FROM Usuarios u JOIN Roles r ON r.IdRol = u.IdRol
              WHERE u.NombreUsuario = @u AND u.Activo = 1", cn);
        cmd.Parameters.AddWithValue("@u", usuario);

        using var rd = cmd.ExecuteReader();
        if (!rd.Read() || !Verificar(contrasena, rd.GetString(1))) return null;
        return (rd.GetInt32(0), rd.GetString(2));
    }

    // Llamar una vez al iniciar el programa: crea "admin" si aún no hay usuarios
    public static void CrearAdminInicial()
    {
        using var cn = new SqlConnection(Cadena);
        cn.Open();
        using var cuenta = new SqlCommand("SELECT COUNT(*) FROM Usuarios", cn);
        if ((int)cuenta.ExecuteScalar()! > 0) return;

        using var ins = new SqlCommand(
            @"INSERT INTO Usuarios (NombreCompleto, NombreUsuario, ContrasenaHash, IdRol)
              VALUES (@n, @u, @h, (SELECT IdRol FROM Roles WHERE Nombre = N'Administrador'))", cn);
        ins.Parameters.AddWithValue("@n", "Administrador del sistema");
        ins.Parameters.AddWithValue("@u", "admin");
        ins.Parameters.AddWithValue("@h", Hashear("Admin123*"));   // cambiar antes de entregar
        ins.ExecuteNonQuery();
    }

    // Deja constancia en la tabla Bitacora (quién hizo qué y cuándo)
    public static void RegistrarBitacora(int? idUsuario, string modulo, string accion, string? descripcion = null)
    {
        try
        {
            using var cn = new SqlConnection(Cadena);
            cn.Open();
            using var cmd = new SqlCommand(
                "INSERT INTO Bitacora (IdUsuario, Modulo, Accion, Descripcion) VALUES (@u, @m, @a, @d)", cn);
            cmd.Parameters.AddWithValue("@u", (object?)idUsuario ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@m", modulo);
            cmd.Parameters.AddWithValue("@a", accion);
            cmd.Parameters.AddWithValue("@d", (object?)descripcion ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
        catch { /* la bitácora nunca debe impedir que el sistema funcione */ }
    }

    // Contraseñas cifradas con PBKDF2 + sal (nunca texto plano)
    private static string Hashear(string contrasena)
    {
        byte[] sal = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, 100_000, HashAlgorithmName.SHA256, 32);
        return $"100000.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    private static bool Verificar(string contrasena, string guardado)
    {
        var p = guardado.Split('.');
        if (p.Length != 3) return false;
        byte[] sal = Convert.FromBase64String(p[1]);
        byte[] esperado = Convert.FromBase64String(p[2]);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, int.Parse(p[0]), HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(hash, esperado);
    }
}
