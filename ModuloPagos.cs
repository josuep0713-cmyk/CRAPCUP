using System.Text.Json;

namespace CajaRural.Modulos;

public class ModuloPagos : IModulo
{
    public string Nombre => "pagos";
    public string Titulo => "Pagos";
    public string Icono => "💵";
    public int Orden => 6;
    public string[] Roles => new[] { "Administrador", "Secretaria", "Encargado de Préstamos" };

    public object? Ejecutar(string accion, JsonElement datos)
    {
        switch (accion)
        {
            case "listar":
                return Db.Tabla("SELECT p.id, p.prestamo_id, p.monto_pagado, p.fecha_pago FROM pagos p ORDER BY p.fecha_pago DESC;");

            case "listar_pendientes":
                return Db.Tabla("SELECT pr.id, pr.persona_id, per.nombre, pr.monto_prestado, pr.saldo_pendiente, pr.estado FROM prestamos pr INNER JOIN personas per ON pr.persona_id = per.id WHERE pr.estado IN ('Activo', 'Atrasado');");

            case "registrar":
                int prestamoId = datos.GetProperty("prestamo_id").GetInt32();
                decimal montoPagado = datos.GetProperty("monto_pagado").GetDecimal();

                if (montoPagado <= 0)
                {
                    throw new ErrorNegocio("El monto pagado debe ser mayor a cero.");
                }

                var prestamo = Db.Fila("SELECT id, saldo_pendiente, estado FROM prestamos WHERE id = @0;", prestamoId);
                if (prestamo == null)
                {
                    throw new ErrorNegocio("El préstamo especificado no existe.");
                }

                decimal saldoPendiente = Convert.ToDecimal(prestamo["saldo_pendiente"]);

                if (montoPagado > saldoPendiente)
                {
                    throw new ErrorNegocio("El monto pagado no puede superar el saldo pendiente.");
                }

                Db.Ejecutar("INSERT INTO pagos (prestamo_id, monto_pagado, fecha_pago) VALUES (@0, @1, GETDATE());", prestamoId, montoPagado);

                decimal nuevoSaldo = saldoPendiente - montoPagado;
                string nuevoEstado = nuevoSaldo == 0 ? "Finalizado" : Convert.ToString(prestamo["estado"]);

                Db.Ejecutar("UPDATE prestamos SET saldo_pendiente = @0, estado = @1 WHERE id = @2;", nuevoSaldo, nuevoEstado, prestamoId);
                Db.Ejecutar("UPDATE capital_disponible SET monto = monto + @0;", montoPagado);

                return new { mensaje = "Pago registrado correctamente", nuevo_saldo = nuevoSaldo, estado = nuevoEstado };

            default:
                throw new ErrorNegocio("Acción no válida.");
        }
    }
}