using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace CajaRural;

// Ventana única de la aplicación: muestra las pantallas HTML/CSS de la carpeta wwwroot
// y atiende los mensajes que mandan (login, sesión, cerrar sesión...).
public class FrmShell : Form
{
    private readonly WebView2 web = new() { Dock = DockStyle.Fill };
    private int intentos;

    public FrmShell()
    {
        Text = "Caja Rural";
        ClientSize = new Size(1100, 720);
        MinimumSize = new Size(480, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(web);
        Load += async (_, _) => await Iniciar();
    }

    private async Task Iniciar()
    {
        try
        {
            await web.EnsureCoreWebView2Async();
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo iniciar el componente WebView2.\n" +
                            "Instale 'WebView2 Runtime' desde microsoft.com.\n\n" + ex.Message,
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
        }

        // La carpeta wwwroot se muestra como si fuera el sitio https://app.local
        string carpeta = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        web.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "app.local", carpeta, CoreWebView2HostResourceAccessKind.Allow);

        web.CoreWebView2.WebMessageReceived += AlRecibirMensaje;
        web.CoreWebView2.Navigate("https://app.local/login.html");
    }

    private void AlRecibirMensaje(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        using var doc = JsonDocument.Parse(e.WebMessageAsJson);
        var m = doc.RootElement;
        string accion = m.TryGetProperty("accion", out var a) ? a.GetString() ?? "" : "";

        switch (accion)
        {
            case "login":
                Login(m);
                break;

            case "sesion":
                Responder(new { accion = "sesion", usuario = Sesion.NombreUsuario, rol = Sesion.Rol });
                break;

            case "logout":
                AuthServicio.RegistrarBitacora(Sesion.IdUsuario, "Seguridad", "Cierre de sesión");
                Sesion.Cerrar();
                web.CoreWebView2.Navigate("https://app.local/login.html");
                break;
        }
    }

    private void Login(JsonElement m)
    {
        string usuario = (m.GetProperty("usuario").GetString() ?? "").Trim();
        string clave = m.GetProperty("clave").GetString() ?? "";
        string rolElegido = m.GetProperty("rol").GetString() ?? "";

        // En pantalla se llama "Presidente"; en la base de datos el rol se llama "Administrador"
        // (el vicepresidente también entra por "Presidente").
        string rolEsperado = rolElegido == "Presidente" ? "Administrador" : rolElegido;

        try
        {
            var r = AuthServicio.Autenticar(usuario, clave);

            if (r is { } ok && ok.Rol == rolEsperado)
            {
                intentos = 0;
                Sesion.Iniciar(ok.Id, usuario, ok.Rol);
                AuthServicio.RegistrarBitacora(ok.Id, "Seguridad", "Inicio de sesión");
                Responder(new { accion = "login", ok = true });
                return;
            }
        }
        catch (Exception ex)
        {
            Responder(new { accion = "login", ok = false,
                            mensaje = "No se pudo conectar con la base de datos: " + ex.Message });
            return;
        }

        // Mensaje único para no revelar si falló el usuario, la clave o el rol
        AuthServicio.RegistrarBitacora(null, "Seguridad", "Intento fallido", $"Usuario: {usuario}");
        intentos++;

        if (intentos >= 3)
        {
            MessageBox.Show("Demasiados intentos fallidos. La aplicación se cerrará.",
                "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
            return;
        }

        Responder(new { accion = "login", ok = false,
                        mensaje = $"Usuario, contraseña o rol incorrectos ({intentos}/3)." });
    }

    private void Responder(object datos) =>
        web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(datos));
}
