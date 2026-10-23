namespace ms_notification.Api.Application;

/// <summary>
/// Los ids de AlertType que el dashboard cuenta como incidentes (no
/// boarding/alighting): Delay, Deviation, Other, Accident, Breakdown, etc.
/// La lista vive en un solo lugar para que el listado, la resolucion y la
/// consulta por id cuenten exactamente los mismos incidentes.
/// </summary>
internal static class IncidentTypeFilter
{
    internal static readonly byte[] AlertTypeIds = { 1, 2, 3, 4, 11, 13, 21, 23 };
}
