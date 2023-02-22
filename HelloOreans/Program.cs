// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.ConfigureServices(collection => collection.AddSingleton<IWithOutSeq, WithOutSeq>())
    .UseOrleans(builder => builder.UseLocalhostClustering());

using var host = hostBuilder.Build();
Console.WriteLine("启动程序");
await host.StartAsync();
Console.WriteLine("程序已启动");

#region 无顺序

var withOutSeq = host.Services.GetRequiredService<IWithOutSeq>();
foreach (var i in Enumerable.Range(0, 100))
{
    _ = Task.Run(async () => await withOutSeq.PlusAsync());
}

await Task.Delay(3000);
var num = await withOutSeq.GetNumAsync();
Console.WriteLine(num);

#endregion


#region 有顺序

var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
var grain = grainFactory.GetGrain<ISeq>(0);
foreach (var i in Enumerable.Range(0, 100))
{
    _ = Task.Run(async () => await grain.PlusAsync());
}

await Task.Delay(3000);
var num2 = await grain.GetNumAsync();
Console.WriteLine(num2);

#endregion

Console.ReadLine();
Console.WriteLine("停止程序");
await host.StopAsync();
Console.WriteLine("程序已停止");


public interface IWithOutSeq
{
    Task PlusAsync();

    Task<int> GetNumAsync();
}

public class WithOutSeq : IWithOutSeq
{
    public int Num { get; set; }

    public WithOutSeq()
    {
        Num = 0;
    }

    public Task PlusAsync()
    {
        Num += 1;
        return Task.CompletedTask;
    }

    public Task<int> GetNumAsync()
    {
        return Task.FromResult(Num);
    }
}


public interface ISeq : IGrainWithIntegerKey
{
    Task PlusAsync();

    Task<int> GetNumAsync();
}

public class Seq : Grain, ISeq
{
    public int Num { get; set; }

    public Seq()
    {
        Num = 0;
    }

    public Task PlusAsync()
    {
        Num += 1;
        return Task.CompletedTask;
    }

    public Task<int> GetNumAsync()
    {
        return Task.FromResult(Num);
    }
}