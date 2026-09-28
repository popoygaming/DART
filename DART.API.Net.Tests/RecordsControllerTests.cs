using System.Net;
using System.Net.Http.Json;
using DART.API.Net.Data;
using DART.API.Net.DTOs;
using DART.API.Net.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DART.API.Net.Tests;

public class RecordsControllerTests
{
    [Theory]
    [InlineData("Birth")]
    [InlineData("Marriage")]
    [InlineData("Death")]
    public async Task Create_RecordType_Succeeds(string recordType)
    {
        using var factory = new TestWebApplicationFactory();
        await factory.SeedUsersAsync();
        using var client = TestWebApplicationFactory.CreateAuthenticatedClient(factory, 2, AppRoles.Staff);

        var request = new CreateRecordRequest
        {
            RecordType = recordType,
            RegistryNumber = $"RN-{Guid.NewGuid():N}"[..20],
            Name = "Juan Dela Cruz"
        };

        var response = await client.PostAsJsonAsync("/api/records", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RecordResponse>();
        Assert.NotNull(payload);
        Assert.Equal(recordType, payload.RecordType);
        Assert.Equal(2, payload.CreatedBy);
    }

    [Fact]
    public async Task Create_InvalidRecordType_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.SeedUsersAsync();
        using var client = TestWebApplicationFactory.CreateAuthenticatedClient(factory, 1, AppRoles.Admin);

        var request = new CreateRecordRequest
        {
            RecordType = "InvalidType",
            RegistryNumber = "123456",
            Name = "Maria Santos"
        };

        var response = await client.PostAsJsonAsync("/api/records", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingRequiredFields_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.SeedUsersAsync();
        using var client = TestWebApplicationFactory.CreateAuthenticatedClient(factory, 1, AppRoles.Admin);

        var request = new CreateRecordRequest
        {
            RecordType = "Birth",
            RegistryNumber = " ",
            Name = " "
        };

        var response = await client.PostAsJsonAsync("/api/records", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingRecord_ReturnsOk()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.SeedUsersAsync();
        using var client = TestWebApplicationFactory.CreateAuthenticatedClient(factory, 1, AppRoles.Admin);

        var created = await CreateRecordAsync(client, "Birth", "REG-1001", "Juan Dela Cruz");

        var response = await client.GetAsync($"/api/records/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RecordResponse>();
        Assert.NotNull(payload);
        Assert.Equal(created.Id, payload.Id);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNotFound()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.SeedUsersAsync();
        using var client = TestWebApplicationFactory.CreateAuthenticatedClient(factory, 1, AppRoles.Admin);

        var response = await client.GetAsync("/api/records/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Request_IsRejected()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.SeedUsersAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/records");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_Record_ChangesUpdatedAt_AndPreservesCreatedFields()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.SeedUsersAsync();
        using var client = TestWebApplicationFactory.CreateAuthenticatedClient(factory, 2, AppRoles.Staff);

        var created = await CreateRecordAsync(client, "Birth", "REG-2001", "Pedro Cruz");

        var updateRequest = new UpdateRecordRequest
        {
            RecordType = "Death",
            RegistryNumber = "REG-2002",
            Name = "Pedro Cruz Updated"
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/records/{created.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<RecordResponse>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Death", updated.RecordType);
        Assert.Equal(created.CreatedBy, updated.CreatedBy);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt >= created.UpdatedAt);
    }

    [Fact]
    public async Task Search_ByRegistryNumber_AndPartialName_AndRecordType_Works()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.SeedUsersAsync();
        using var client = TestWebApplicationFactory.CreateAuthenticatedClient(factory, 1, AppRoles.Admin);

        await CreateRecordAsync(client, "Birth", "REG-3001", "Juan Dela Cruz");
        await CreateRecordAsync(client, "Marriage", "REG-3002", "Juan Santos");
        await CreateRecordAsync(client, "Death", "REG-3003", "Maria Juanita");

        var byRegistry = await client.GetFromJsonAsync<PagedRecordsResponse>("/api/records?registryNumber=REG-3002&page=1&pageSize=20");
        Assert.NotNull(byRegistry);
        Assert.Single(byRegistry.Items);
        Assert.Equal("REG-3002", byRegistry.Items[0].RegistryNumber);

        var byName = await client.GetFromJsonAsync<PagedRecordsResponse>("/api/records?name=Juan&page=1&pageSize=20");
        Assert.NotNull(byName);
        Assert.True(byName.Items.Count >= 3);

        var byType = await client.GetFromJsonAsync<PagedRecordsResponse>("/api/records?recordType=Birth&page=1&pageSize=20");
        Assert.NotNull(byType);
        Assert.Contains(byType.Items, x => x.RecordType == "Birth");

        var combined = await client.GetFromJsonAsync<PagedRecordsResponse>("/api/records?name=Juan&recordType=Marriage&page=1&pageSize=20");
        Assert.NotNull(combined);
        Assert.Single(combined.Items);
        Assert.Equal("Marriage", combined.Items[0].RecordType);
    }

    [Fact]
    public async Task Search_InvalidRecordType_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.SeedUsersAsync();
        using var client = TestWebApplicationFactory.CreateAuthenticatedClient(factory, 1, AppRoles.Admin);

        var response = await client.GetAsync("/api/records?recordType=Invalid&page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_And_Staff_CanManageRecords()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.SeedUsersAsync();

        using var adminClient = TestWebApplicationFactory.CreateAuthenticatedClient(factory, 1, AppRoles.Admin);
        var adminCreate = await adminClient.PostAsJsonAsync("/api/records", new CreateRecordRequest
        {
            RecordType = "Birth",
            RegistryNumber = "REG-4001",
            Name = "Admin Created"
        });

        using var staffClient = TestWebApplicationFactory.CreateAuthenticatedClient(factory, 2, AppRoles.Staff);
        var staffCreate = await staffClient.PostAsJsonAsync("/api/records", new CreateRecordRequest
        {
            RecordType = "Marriage",
            RegistryNumber = "REG-4002",
            Name = "Staff Created"
        });

        Assert.Equal(HttpStatusCode.Created, adminCreate.StatusCode);
        Assert.Equal(HttpStatusCode.Created, staffCreate.StatusCode);
    }

    private static async Task<RecordResponse> CreateRecordAsync(HttpClient client, string recordType, string registryNumber, string name)
    {
        var response = await client.PostAsJsonAsync("/api/records", new CreateRecordRequest
        {
            RecordType = recordType,
            RegistryNumber = registryNumber,
            Name = name
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<RecordResponse>();
        return payload!;
    }
}
