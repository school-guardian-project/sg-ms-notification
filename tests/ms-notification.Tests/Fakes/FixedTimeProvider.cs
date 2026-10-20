namespace ms_notification.Tests.Fakes;

/// <summary>
/// Reloj fijo para las pruebas de la ventana horaria: responde siempre la misma
/// hora, sin depender del reloj de la maquina que corre la suite. Se puede mover
/// a proposito para probar reglas que dependen del paso del tiempo.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now) => _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void SetUtcNow(DateTimeOffset now) => _now = now;

    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}
