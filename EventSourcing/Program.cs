// See https://aka.ms/new-console-template for more information

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.EventSourcing;

const string connectionString = "Server=(LocalDb)\\MSSQLLocalDB;Database=Orleans;Trusted_Connection=True;TrustServerCertificate=True";
const string invariantNameSqlServerDotnetCore = "Microsoft.Data.SqlClient";
var hostBuilder = Host.CreateDefaultBuilder(args);
hostBuilder.UseOrleans(builder =>
{
    builder.UseLocalhostClustering();
    builder.AddStateStorageBasedLogConsistencyProviderAsDefault();
    builder.AddLogStorageBasedLogConsistencyProviderAsDefault();
    // builder.AddCustomStorageBasedLogConsistencyProviderAsDefault();
    builder.AddAdoNetGrainStorageAsDefault(options =>
    {
        options.ConnectionString = connectionString;
        options.Invariant = invariantNameSqlServerDotnetCore;
    });
});

using var host = hostBuilder.Build();
Console.WriteLine("启动程序");
await host.StartAsync();
Console.WriteLine("程序已启动");

var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
var user = grainFactory.GetGrain<IUser>(0);
await user.SetAsync("王俊", Gender.Man, 18);
await user.ChangeAsync(Gender.Women);
var userState = await user.GetAsync();
Console.WriteLine(JsonSerializer.Serialize(userState, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
var version = await user.GetVersionAsync();
Console.WriteLine(version);

Console.ReadLine();
Console.WriteLine("停止程序");
await host.StopAsync();
Console.WriteLine("程序已停止");

[GenerateSerializer]
public class UserState
{
    [Id(0)] public string? Name { get; set; }

    [Id(1)] public Gender Gender { get; set; }

    [Id(2)] public int? Age { get; set; }

    public void Apply(UserSetEvent @event)
    {
        Name = @event.Name;
        Gender = @event.Gender;
        Age = @event.Age;
    }

    public void Apply(UserChangedEvent @event)
    {
        Gender = @event.Gender;
    }
}

public abstract class UserEventBase
{
}

public class UserSetEvent : UserEventBase
{
    public string? Name { get; set; }

    public Gender Gender { get; set; }

    public int? Age { get; set; }
}

public class UserChangedEvent : UserEventBase
{
    public Gender Gender { get; set; }
}

public enum Gender
{
    Man,
    Women
}

public interface IUser : IGrainWithIntegerKey
{
    ValueTask SetAsync(string name, Gender gender, int age);

    ValueTask ChangeAsync(Gender gender);

    ValueTask<UserState> GetAsync();

    ValueTask<int> GetVersionAsync();
}

public class User : JournaledGrain<UserState, UserEventBase>, IUser
{
    public async ValueTask SetAsync(string name, Gender gender, int age)
    {
        RaiseEvent(new UserSetEvent()
        {
            Name = name,
            Gender = gender,
            Age = age
        });
        await ConfirmEvents();
    }

    public async ValueTask ChangeAsync(Gender gender)
    {
        RaiseEvent(new UserChangedEvent()
        {
            Gender = gender
        });
        await ConfirmEvents();
    }

    public ValueTask<UserState> GetAsync()
    {
        return ValueTask.FromResult(State);
    }

    public ValueTask<int> GetVersionAsync()
    {
        return ValueTask.FromResult(Version);
    }
}