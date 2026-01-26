using TimeTracker_Entevisual.Models;
using TimeTracker_Entevisual.Models.ViewModels;

namespace TimeTracker_Entevisual.Helpers
{
    public static class ActividadCardBuilder
    {
        public static ActividadCardVM Build(Actividad a, DateTime now, DateTime inicioMes, DateTime finMes)
        {
            var abierto = a.Tiempos.FirstOrDefault(t => t.Fin == null);
            var tieneSesiones = a.Tiempos.Any();

            var estado = (abierto != null)
                ? EstadoActividadVM.Corriendo
                : (tieneSesiones ? EstadoActividadVM.Pausada : EstadoActividadVM.SinIniciar);

            var acumSeg = CalcularAcumuladoMesSegundos(a, now, inicioMes, finMes);

            var ultimaSesionCerrada = a.Tiempos
                .Where(t => t.Fin != null)
                .OrderByDescending(t => t.Fin)
                .FirstOrDefault();

            var ultimaSesionDetalle = BuildUltimaSesionDetalle(ultimaSesionCerrada, now, out var ultimaSesionDurSeg);
            var ultimoRegistro = BuildUltimoRegistro(a, tieneSesiones, ultimaSesionCerrada, ultimaSesionDurSeg, now);

            return new ActividadCardVM
            {
                ActividadId = a.Id,
                TipoActividad = a.TipoActividad?.Descripcion ?? "—",
                Codigo = a.Codigo,
                Titulo = a.Titulo,
                Descripcion = a.Descripcion,
                Estado = estado,
                UsuarioNombre = a.Usuario != null ? $"{a.Usuario.Nombre} {a.Usuario.Apellido}" : null,

                AcumuladoMes = TimeFormatHelper.FormatoHHMMSS(acumSeg),
                TiempoActualInicio = (abierto != null) ? abierto.Inicio : null,

                UltimoRegistro = ultimoRegistro,
                UltimaSesionDetalle = (estado == EstadoActividadVM.Pausada) ? ultimaSesionDetalle : null
            };
        }

        private static long CalcularAcumuladoMesSegundos(Actividad a, DateTime now, DateTime inicioMes, DateTime finMes)
        {
            long acumSeg = 0;
            var tiemposDelMes = a.Tiempos.Where(t => t.Inicio >= inicioMes && t.Inicio < finMes);

            foreach (var t in tiemposDelMes)
            {
                if (t.Fin != null && t.DuracionSegundos != null) acumSeg += (long)t.DuracionSegundos.Value;
                else if (t.Fin != null) acumSeg += (long)(t.Fin.Value - t.Inicio).TotalSeconds;
                else acumSeg += (long)(now - t.Inicio).TotalSeconds;
            }

            return acumSeg;
        }

        private static string? BuildUltimaSesionDetalle(Tiempo? ultimaSesionCerrada, DateTime now, out long? durSeg)
        {
            durSeg = null;
            if (ultimaSesionCerrada?.Fin == null) return null;

            durSeg = ultimaSesionCerrada.DuracionSegundos != null
                ? (long)ultimaSesionCerrada.DuracionSegundos.Value
                : (long)(ultimaSesionCerrada.Fin.Value - ultimaSesionCerrada.Inicio).TotalSeconds;

            return $"Última sesión: {FormatoDuracionCorta(durSeg.Value)} • {HaceCuanto(ultimaSesionCerrada.Fin.Value, now)}";
        }

        private static string BuildUltimoRegistro(Actividad a, bool tieneSesiones, Tiempo? ultimaSesionCerrada, long? ultimaSesionDurSeg, DateTime now)
        {
            if (!tieneSesiones)
                return $"Último registro: Creada • {a.FechaCreacion:dd/MM/yyyy}";

            var ultimaMarca = a.Tiempos
                .Where(t => t.Fin != null)
                .SelectMany(t => t.MarcasTiempo)
                .OrderByDescending(m => m.Fecha)
                .FirstOrDefault();

            if (ultimaMarca != null)
                return $"Último registro: Nota — “{ultimaMarca.Descripcion}”";

            if (ultimaSesionCerrada?.Fin != null && ultimaSesionDurSeg.HasValue)
                return $"Último registro: Sesión — {FormatoDuracionCorta(ultimaSesionDurSeg.Value)} • {HaceCuanto(ultimaSesionCerrada.Fin.Value, now)}";

            return "Último registro: Sesión — —";
        }

        private static string FormatoDuracionCorta(long segundos)
        {
            if (segundos < 0) segundos = 0;
            var ts = TimeSpan.FromSeconds(segundos);
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes} min";
        }

        private static string HaceCuanto(DateTime fecha, DateTime now)
        {
            var diff = now - fecha;
            if (diff.TotalMinutes < 1) return "Recién";
            if (diff.TotalHours < 1) return $"Hace {Math.Max(1, (int)diff.TotalMinutes)} min";
            if (diff.TotalDays < 1) return $"Hace {(int)diff.TotalHours} h";
            if (diff.TotalDays < 30) return $"Hace {(int)diff.TotalDays} días";
            return fecha.ToString("dd/MM/yyyy");
        }
    }
}
