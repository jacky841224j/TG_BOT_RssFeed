using Microsoft.Data.Sqlite;
using Quartz;
using Telegram.Bot;
using TGBot_RssFeed_Polling.Services;

var builder = WebApplication.CreateBuilder(args);
var apikey = builder.Configuration["BotConfiguration:BotToken"];
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient("telegram_bot_client")
        .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
        {
            TelegramBotClientOptions options = new(apikey);
            return new TelegramBotClient(options, httpClient);
        });

builder.Services.AddScoped(x =>
{
    string SavePath = Environment.CurrentDirectory + "/Repositories/RssFeed.db";
    return new SqliteConnection($"Data Source={SavePath}");
}); 
builder.Services.AddScoped<RssService>();
builder.Services.AddScoped<UpdateHandler>();
builder.Services.AddScoped<ReceiverService>();
builder.Services.AddHostedService<PollingService>();

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
_ = builder.Services.AddQuartz(q =>
{

    var jobKey = new JobKey("Schedule", "TG");

    q.AddJob<Schedule>(opts =>
    {
        opts.WithIdentity(jobKey);
        opts.StoreDurably();
    });

    q.AddTrigger(opts =>
    {
        opts.ForJob(jobKey);
        opts.WithIdentity("ScheduleTrigger", "TG");
        opts.WithSimpleSchedule(x => x.WithIntervalInMinutes(30).RepeatForever());
        opts.StartNow();
    });

});
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
