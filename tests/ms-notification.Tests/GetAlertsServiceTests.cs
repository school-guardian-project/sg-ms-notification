using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto.Request;
using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Application.UseCase;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Controller;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Api.Infrastructure.Persistence.Entity;
using ms_notification.Tests.Fakes;
using NUnit.Framework;

namespace ms_notification.Tests;

[TestFixture]
public class GetAlertsServiceTests
{
    private TestNotificationContext _context = null!;
    private GetAlertsService _service = null!;
    private Guid _adminId;
    private Guid _schoolId;
    private Guid _otherSchoolId;
    private Guid _studentId;

    [SetUp]
    public void SetUp()
    {
        _context = new TestNotificationContext(new DbContextOptionsBuilder<NotificationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _adminId = Guid.NewGuid();
        _schoolId = Guid.NewGuid();
        _otherSchoolId = Guid.NewGuid();
        _studentId = AddProfile(_schoolId);
        var otherStudent = AddProfile(_otherSchoolId);
        _context.Set<SchoolAdminRefEntity>().Add(new SchoolAdminRefEntity
        {
            Id = Guid.NewGuid(), ProfileId = _adminId, SchoolId = _schoolId, Status = "Active"
        });
        _context.AlertTypes.AddRange(
            new AlertType { Id = 1, Name = "Bus Delay" },
            new AlertType { Id = 25, Name = "On Board" });
        AddAlert(_studentId, 1, "Demora en la ruta", 1);
        AddAlert(_studentId, 25, "Abordaje del estudiante", 2);
        AddAlert(otherStudent, 1, "Demora de otro colegio", 3);
        _context.SaveChanges();
        _service = new GetAlertsService(_context, new Students(), new FakeRouteDirectory(), new AlertMapper());
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public async Task AdminScopeIsAppliedBeforeCountAndPagination()
    {
        var result = await _service.ExecuteAsync(new AlertFilters { AdminProfileId = _adminId, Size = 1 });
        Assert.That(result.TotalElements, Is.EqualTo(2));
        Assert.That(result.TotalPages, Is.EqualTo(2));
        Assert.That(result.Content.Single().ProfileId, Is.EqualTo(_studentId));
        Assert.That(result.Content.Single().AlertTypeId, Is.EqualTo(25));
        Assert.That(result.Content.Single().AlertTypeName, Is.EqualTo("On Board"));

        var second = await _service.ExecuteAsync(new AlertFilters { AdminProfileId = _adminId, Size = 1, Page = 1 });
        Assert.That(second.Content.Single().AlertTypeId, Is.EqualTo(1));
    }

    [Test]
    public async Task AnotherSchoolParameterCannotExpandAdminScope()
    {
        var result = await _service.ExecuteAsync(new AlertFilters
        {
            AdminProfileId = _adminId, SchoolId = _otherSchoolId
        });
        Assert.That(result.TotalElements, Is.Zero);
        Assert.That(result.Content, Is.Empty);
    }

    [Test]
    public async Task MissingOrInactiveAssignmentDoesNotReturnGlobalAlerts()
    {
        var missing = await _service.ExecuteAsync(new AlertFilters { AdminProfileId = Guid.NewGuid() });
        Assert.That(missing.Content, Is.Empty);
        _context.Set<SchoolAdminRefEntity>().Single().Status = "Inactive";
        await _context.SaveChangesAsync();
        var inactive = await _service.ExecuteAsync(new AlertFilters { AdminProfileId = _adminId });
        Assert.That(inactive.Content, Is.Empty);
    }

    [Test]
    public async Task CatalogTypeAndTextFiltersCombineWithinTheSchool()
    {
        var result = await _service.ExecuteAsync(new AlertFilters
        {
            AdminProfileId = _adminId, AlertTypeId = 1, Search = "  DEMORA  "
        });
        Assert.That(result.TotalElements, Is.EqualTo(1));
        Assert.That(result.Content.Single().AlertTypeName, Is.EqualTo("Bus Delay"));
        Assert.That(result.Content.Single().ProfileId, Is.EqualTo(_studentId));
    }

    [Test]
    public async Task AdminSeesAlertsAcrossAllCampusesOfTheAssignedSchool()
    {
        var anotherCampusStudent = AddProfile(_schoolId);
        AddAlert(anotherCampusStudent, 1, "Otra sede del mismo colegio", 4);
        await _context.SaveChangesAsync();
        var result = await _service.ExecuteAsync(new AlertFilters { AdminProfileId = _adminId });
        Assert.That(result.TotalElements, Is.EqualTo(3));
        Assert.That(result.Content.First().ProfileId, Is.EqualTo(anotherCampusStudent));
    }

    [Test]
    public async Task AscendingTimestampSortUsesThePersistedDateTime()
    {
        var result = await _service.ExecuteAsync(new AlertFilters
        {
            AdminProfileId = _adminId, Sort = "timestamp,asc"
        });
        Assert.That(result.Content.Select(a => a.AlertTypeId), Is.EqualTo(new byte[] { 1, 25 }));
    }

    [Test]
    public async Task LegacyBoardingFilterStillWorksAndSuperadminCanReadAllSchools()
    {
        var boarding = await _service.ExecuteAsync(new AlertFilters
        {
            AdminProfileId = _adminId, AlertType = ms_notification.Api.Domain.Enum.BoardingType.ON_BOARD
        });
        Assert.That(boarding.Content.Single().AlertTypeId, Is.EqualTo(25));
        var all = await _service.ExecuteAsync(new AlertFilters());
        Assert.That(all.TotalElements, Is.EqualTo(3));
        var school = await _service.ExecuteAsync(new AlertFilters { SchoolId = _otherSchoolId });
        Assert.That(school.TotalElements, Is.EqualTo(1));
    }

    [Test]
    public async Task TypeOptionsExcludeUnusedAndOtherSchoolOnlyTypesAndRemoveDuplicates()
    {
        _context.AlertTypes.AddRange(
            new AlertType { Id = 2, Name = "Route Deviation" },
            new AlertType { Id = 3, Name = "Unused type" });
        AddAlert(AddProfile(_otherSchoolId), 2, "Other school only", 4);
        AddAlert(_studentId, 1, "Repeated school type", 5);
        await _context.SaveChangesAsync();

        var result = await Controller("1", _adminId).GetAlertTypes(CancellationToken.None);
        var types = JsonSerializer.SerializeToElement(((OkObjectResult)result).Value);

        Assert.That(types.EnumerateArray().Select(t => t.GetProperty("Id").GetByte()),
            Is.EqualTo(new byte[] { 1, 25 }));
        Assert.That(types.EnumerateArray().Select(t => t.GetProperty("Name").GetString()),
            Is.EqualTo(new[] { "Bus Delay", "On Board" }));
    }

    [Test]
    public async Task TypeOptionsIncludeUsedTypesFromEverySchoolCampus()
    {
        _context.AlertTypes.Add(new AlertType { Id = 2, Name = "Route Deviation" });
        AddAlert(AddProfile(_schoolId), 2, "Another campus", 4);
        await _context.SaveChangesAsync();

        var result = await Controller("1", _adminId).GetAlertTypes(CancellationToken.None);
        var types = JsonSerializer.SerializeToElement(((OkObjectResult)result).Value);

        Assert.That(types.EnumerateArray().Select(t => t.GetProperty("Id").GetByte()),
            Is.EquivalentTo(new byte[] { 1, 2, 25 }));
    }

    [Test]
    public async Task TypeOptionsAreEmptyWithoutAlertsOrAnActiveSchoolAssignment()
    {
        var missing = await Controller("1", Guid.NewGuid()).GetAlertTypes(CancellationToken.None);
        Assert.That(JsonSerializer.SerializeToElement(((OkObjectResult)missing).Value).GetArrayLength(), Is.Zero);

        _context.Set<SchoolAdminRefEntity>().Single().Status = "Inactive";
        await _context.SaveChangesAsync();
        var inactive = await Controller("1", _adminId).GetAlertTypes(CancellationToken.None);
        Assert.That(JsonSerializer.SerializeToElement(((OkObjectResult)inactive).Value).GetArrayLength(), Is.Zero);

        _context.Set<SchoolAdminRefEntity>().Single().Status = "Active";
        _context.Alerts.RemoveRange(_context.Alerts);
        await _context.SaveChangesAsync();
        var empty = await Controller("1", _adminId).GetAlertTypes(CancellationToken.None);
        Assert.That(JsonSerializer.SerializeToElement(((OkObjectResult)empty).Value).GetArrayLength(), Is.Zero);
    }

    [Test]
    public async Task TypeOptionsRejectMissingProfileAndCannotExpandAdminScope()
    {
        var missing = await Controller("1", null).GetAlertTypes(CancellationToken.None);
        Assert.That(((StatusCodeResult)missing).StatusCode, Is.EqualTo(403));

        var outside = await Controller("1", _adminId).GetAlertTypes(CancellationToken.None, _otherSchoolId);
        Assert.That(JsonSerializer.SerializeToElement(((OkObjectResult)outside).Value).GetArrayLength(), Is.Zero);
    }

    [Test]
    public async Task SuperadminTypeOptionsUsePersistedAlertsAndRespectTheSchoolParameter()
    {
        _context.AlertTypes.Add(new AlertType { Id = 3, Name = "Unused type" });
        await _context.SaveChangesAsync();

        var all = await Controller("5", null).GetAlertTypes(CancellationToken.None);
        var types = JsonSerializer.SerializeToElement(((OkObjectResult)all).Value);
        Assert.That(types.EnumerateArray().Select(t => t.GetProperty("Id").GetByte()),
            Is.EqualTo(new byte[] { 1, 25 }));

        var school = await Controller("5", null).GetAlertTypes(CancellationToken.None, _otherSchoolId);
        var scoped = JsonSerializer.SerializeToElement(((OkObjectResult)school).Value);
        Assert.That(scoped.EnumerateArray().Select(t => t.GetProperty("Id").GetByte()),
            Is.EqualTo(new byte[] { 1 }));
    }

    [Test]
    public async Task ControllerDerivesScopeFromAuthenticatedAdmin()
    {
        var controller = Controller("1", _adminId);
        var action = await controller.GetAlerts();
        var response = (AlertsResponse)((OkObjectResult)action.Result!).Value!;
        Assert.That(response.TotalElements, Is.EqualTo(2));
        Assert.That(response.Content.All(a => a.ProfileId == _studentId), Is.True);
    }

    [Test]
    public async Task ControllerRejectsMissingProfileAndInvalidPagination()
    {
        var missing = await Controller("1", null).GetAlerts();
        Assert.That(((StatusCodeResult)missing.Result!).StatusCode, Is.EqualTo(403));
        var invalid = await Controller("1", _adminId).GetAlerts(size: 0);
        Assert.That(invalid.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    private AlertController Controller(string role, Guid? profileId)
    {
        var claims = new List<Claim> { new("roleId", role) };
        if (profileId.HasValue) claims.Add(new Claim("sub", profileId.Value.ToString()));
        return new AlertController(_service, new Acknowledge(), new Details(), _context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            }
        };
    }

    private Guid AddProfile(Guid schoolId)
    {
        var campusId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        _context.Set<SchoolCampusRefEntity>().Add(new SchoolCampusRefEntity { Id = campusId, SchoolId = schoolId });
        _context.Set<ProfileRefEntity>().Add(new ProfileRefEntity { Id = profileId, CampuseId = campusId });
        return profileId;
    }

    private void AddAlert(Guid profileId, byte typeId, string description, int hour) =>
        _context.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(), ProfileId = profileId, RouteExecutionId = Guid.NewGuid(),
            AlertTypeId = typeId, Description = description,
            DateTime = new DateTime(2026, 10, 26, hour, 0, 0, DateTimeKind.Utc)
        });

    private sealed class TestNotificationContext(DbContextOptions<NotificationContext> options)
        : NotificationContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // InMemory needs keys to seed the cross-service read-only references.
            modelBuilder.Entity<ProfileRefEntity>().HasKey(p => p.Id);
            modelBuilder.Entity<SchoolCampusRefEntity>().HasKey(c => c.Id);
            modelBuilder.Entity<SchoolAdminRefEntity>().HasKey(a => a.Id);
        }
    }

    private sealed class Students : IStudentDirectory
    {
        public Task<string?> GetStudentNameAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<string?>("Ana");
        public Task<StudentInfo?> GetStudentByProfileIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<StudentInfo?>(new StudentInfo(id, "Ana", null, null, null));
    }

    private sealed class Acknowledge : IAcknowledgeAlertUseCase
    {
        public Task ExecuteAsync(Guid alertId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class Details : IGetAlertByIdUseCase
    {
        public Task<AlertListResponse?> ExecuteAsync(Guid alertId, CancellationToken ct = default) =>
            Task.FromResult<AlertListResponse?>(null);
    }
}
