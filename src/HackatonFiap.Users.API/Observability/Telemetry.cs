using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HackatonFiap.Users.API.Observability;

public static class Telemetry
{
    public const string ServiceName = "HackatonFiap.Users";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> LoginsTotal =
        Meter.CreateCounter<long>("users_logins_total", description: "Total de logins bem-sucedidos");
    public static readonly Counter<long> RegistrationsTotal =
        Meter.CreateCounter<long>("users_registrations_total", description: "Total de cadastros de doador");
}
