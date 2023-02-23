// See https://aka.ms/new-console-template for more information

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const string connectionString = "Server=(LocalDb)\\MSSQLLocalDB;Database=Orleans;Trusted_Connection=True;TrustServerCertificate=True";
const string invariantNameSqlServerDotnetCore = "Microsoft.Data.SqlClient";
var hostBuilder = Host.CreateDefaultBuilder();
hostBuilder.UseOrleans(builder =>
{
    builder.UseAdoNetClustering(options =>
    {
        options.ConnectionString = connectionString;
        options.Invariant = invariantNameSqlServerDotnetCore;
    });
    builder.AddAdoNetGrainStorageAsDefault(options =>
    {
        options.ConnectionString = connectionString;
        options.Invariant = invariantNameSqlServerDotnetCore;
    });
    builder.UseAdoNetReminderService(options =>
    {
        options.ConnectionString = connectionString;
        options.Invariant = invariantNameSqlServerDotnetCore;
    });
});
using var host = hostBuilder.Build();
Console.WriteLine("启动程序");
await host.StartAsync();
Console.WriteLine("程序已启动");

#region 测试程序

var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
var user = grainFactory.GetGrain<IUser>(0);
await user.SetAsync("王俊", Gender.Man, 18);
var userState = await user.GetAsync();
Console.WriteLine(JsonSerializer.Serialize(userState, new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
}));

#endregion

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
}

public enum Gender
{
    Man,
    Women
}

public interface IUser : IGrainWithIntegerKey
{
    ValueTask SetAsync(string name, Gender gender, int age);
    ValueTask<UserState> GetAsync();
}

public class User : Grain<UserState>, IUser
{
    public async ValueTask SetAsync(string name, Gender gender, int age)
    {
        State.Name = name;
        State.Gender = gender;
        State.Age = age;
        await WriteStateAsync();
    }

    public async ValueTask<UserState> GetAsync()
    {
        await ReadStateAsync();
        return State;
    }
}