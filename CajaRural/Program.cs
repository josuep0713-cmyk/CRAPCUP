namespace CajaRural;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try { AuthServicio.CrearAdminInicial(); }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo conectar con SQL Server:\n\n" + ex.Message,
                "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Application.Run(new FrmShell());
    }
}
