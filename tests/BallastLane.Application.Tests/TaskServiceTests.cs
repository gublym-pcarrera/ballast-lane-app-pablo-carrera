using BallastLane.Application.Common;
using BallastLane.Application.Contracts;
using BallastLane.Application.Tasks;
using BallastLane.Domain.Tasks;

namespace BallastLane.Application.Tests;

public sealed class TaskServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid owner = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid other = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Create_TrimsTextAndKeepsTheCallerAsOwner()
    {
        var store = new InMemoryTasks();
        var service = CreateService(store);

        var created = await service.CreateAsync(owner, new UpsertTaskRequest("  Write notes  ", "  Details  ", "Todo", new DateOnly(2026, 6, 20)), CancellationToken.None);

        var saved = Assert.Single(store.Items);
        Assert.Equal(owner, saved.OwnerId);
        Assert.Equal("Write notes", created.Title);
        Assert.Equal("Details", created.Description);
        Assert.Equal("Todo", created.Status);
    }

    [Theory]
    [InlineData("   ", "Todo", "title")]
    [InlineData("Title", "1", "status")]
    [InlineData("Title", "todo", "status")]
    public async Task Create_RejectsInvalidInput(string title, string status, string field)
    {
        var service = CreateService(new InMemoryTasks());

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(owner, new UpsertTaskRequest(title, "", status, new DateOnly(2026, 6, 20)), CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey(field));
    }

    [Fact]
    public async Task Create_RequiresADueDate()
    {
        var service = CreateService(new InMemoryTasks());

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(owner, new UpsertTaskRequest("Title", "", "Done", null), CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("dueDate"));
    }

    [Theory]
    [InlineData("InProgress")]
    [InlineData("Done")]
    public async Task Create_RejectsAnyStatusOtherThanTodo(string status)
    {
        var service = CreateService(new InMemoryTasks());

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(owner, new UpsertTaskRequest("Title", "", status, new DateOnly(2026, 6, 20)), CancellationToken.None));

        Assert.Equal(TaskRules.NewTaskStatusMessage, Assert.Single(exception.Errors["status"]));
    }

    [Fact]
    public async Task CreateAndUpdate_RejectADueDateBeforeToday()
    {
        var service = CreateService(new InMemoryTasks());
        var created = await service.CreateAsync(owner, new UpsertTaskRequest("Today", "", "Todo", new DateOnly(2026, 6, 15)), CancellationToken.None);

        var createException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(owner, new UpsertTaskRequest("Yesterday", "", "Todo", new DateOnly(2026, 6, 14)), CancellationToken.None));
        var updateException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(owner, created.Id, new UpsertTaskRequest("Today", "", "Done", new DateOnly(2026, 6, 14)), CancellationToken.None));

        Assert.Equal(TaskRules.PastDueDateMessage, Assert.Single(createException.Errors["dueDate"]));
        Assert.Equal(TaskRules.PastDueDateMessage, Assert.Single(updateException.Errors["dueDate"]));
    }

    [Fact]
    public async Task List_ReturnsOnlyTheCallersTasksAndHonorsTheStatusFilter()
    {
        var store = new InMemoryTasks();
        var service = CreateService(store);
        await service.CreateAsync(owner, new UpsertTaskRequest("Later", "", "Todo", new DateOnly(2026, 6, 22)), CancellationToken.None);
        var activeTask = await service.CreateAsync(owner, new UpsertTaskRequest("Now", "", "Todo", new DateOnly(2026, 6, 16)), CancellationToken.None);
        await service.UpdateAsync(owner, activeTask.Id, new UpsertTaskRequest("Now", "", "InProgress", new DateOnly(2026, 6, 16)), CancellationToken.None);
        await service.CreateAsync(other, new UpsertTaskRequest("Hidden", "", "Todo", new DateOnly(2026, 6, 16)), CancellationToken.None);

        var all = await service.ListAsync(owner, null, CancellationToken.None);
        var active = await service.ListAsync(owner, "InProgress", CancellationToken.None);

        Assert.Equal(["Now", "Later"], all.Select(task => task.Title).ToArray());
        Assert.Equal("Now", Assert.Single(active).Title);
    }

    [Fact]
    public async Task GetUpdateAndDelete_HideTasksOwnedBySomeoneElse()
    {
        var service = CreateService(new InMemoryTasks());
        var created = await service.CreateAsync(owner, new UpsertTaskRequest("Mine", "", "Todo", new DateOnly(2026, 6, 20)), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(other, created.Id, CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(other, created.Id, new UpsertTaskRequest("Stolen", "", "Done", new DateOnly(2026, 6, 21)), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(other, created.Id, CancellationToken.None));

        var updated = await service.UpdateAsync(owner, created.Id, new UpsertTaskRequest("Mine still", "Edited", "Done", new DateOnly(2026, 6, 21)), CancellationToken.None);
        Assert.Equal("Mine still", updated.Title);
        Assert.Equal("Done", updated.Status);

        await service.DeleteAsync(owner, created.Id, CancellationToken.None);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(owner, created.Id, CancellationToken.None));
    }

    private static TaskService CreateService(InMemoryTasks tasks) =>
        new(tasks, new NoopUnitOfWork(), new FixedTimeProvider(Now));
}
