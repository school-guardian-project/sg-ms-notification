using ms_notification.Api.Application;
using ms_notification.Api.Domain.Enum;
using NUnit.Framework;

namespace ms_notification.Tests;

[TestFixture]
public class AlertDescriptionBuilderTests
{
    [Test]
    public void Build_AlBajar_IndicaLaBajadaYLaUbicacionDeLaParada()
    {
        var text = AlertDescriptionBuilder.Build(
            BoardingType.OFF_BOARD, "Juan Pérez", "Ruta Mañana", "Libertad", "Calle 12 #34-56");

        Assert.That(text, Is.EqualTo("Juan Pérez bajó del bus en la parada Libertad (Calle 12 #34-56) - Ruta Mañana"));
    }

    [Test]
    public void Build_AlSubir_IndicaLaSubida()
    {
        var text = AlertDescriptionBuilder.Build(
            BoardingType.ON_BOARD, "Juan Pérez", "Ruta Mañana", "Libertad", "Calle 12 #34-56");

        Assert.That(text, Does.StartWith("Juan Pérez subió al bus"));
    }

    [Test]
    public void Build_SinDireccion_ConservaElFormatoAnterior()
    {
        var text = AlertDescriptionBuilder.Build(
            BoardingType.ON_BOARD, "Juan Pérez", "Ruta Mañana", "Libertad", null);

        Assert.That(text, Is.EqualTo("Juan Pérez subió al bus en la parada Libertad - Ruta Mañana"));
        Assert.That(text, Does.Not.Contain("("));
    }

    [Test]
    public void Build_SinNombreDeEstudiante_UsaElValorPorDefecto()
    {
        var text = AlertDescriptionBuilder.Build(
            BoardingType.ON_BOARD, "   ", "Ruta Mañana", "Libertad", "Calle 12 #34-56");

        Assert.That(text, Does.StartWith("Estudiante subió al bus"));
    }

    [Test]
    public void Build_TextoLargo_NoSuperaElLimiteDeLaColumnaYConservaLaUbicacion()
    {
        var text = AlertDescriptionBuilder.Build(
            BoardingType.OFF_BOARD,
            "Juan Pérez",
            "Ruta de la mañana con recorrido extendido por el norte de la ciudad",
            "Libertad",
            "Calle 12 #34-56, barrio Centro");

        Assert.That(text.Length, Is.LessThanOrEqualTo(AlertDescriptionBuilder.MaxLength));
        Assert.That(text, Does.Contain("Calle 12 #34-56, barrio Centro"));
    }
}
