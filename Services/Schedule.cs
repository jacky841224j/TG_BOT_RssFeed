using Dapper;
using Microsoft.Data.Sqlite;
using Quartz;
using System.ServiceModel.Syndication;
using System.Xml;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TGbot_RssFeed.Model;


namespace TGBot_RssFeed_Polling.Services
{
    [DisallowConcurrentExecution]
    public class Schedule : IJob
    {
        private readonly ILogger<Schedule> _logger;
        private readonly SqliteConnection _sqlcon;
        private ITelegramBotClient botClient;
        TimeZoneInfo targetTimeZone;

        public Schedule(ITelegramBotClient botClient, SqliteConnection sqlcon, ILogger<Schedule> logger)
        {
            this.botClient = botClient;
            _sqlcon = sqlcon;
            _logger = logger;
            targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        }
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation($"啟動排程-{DateTime.Now}");

                using (_sqlcon)
                {
                    await _sqlcon.OpenAsync();
                    //讀取使用者清單
                    _logger.LogInformation("讀取使用者清單：");
                    var UserList = await _sqlcon.QueryAsync<User>("SELECT * FROM User");

                    foreach (var user in UserList.ToList())
                    {
                        var SubList = await _sqlcon.QueryAsync<Subscription>("SELECT * FROM Sub WHERE UserID = @UserID ", new { user.UserID });
                        var InlineList = new List<IEnumerable<InlineKeyboardButton>>();

                        foreach (var Sub in SubList.ToList())
                        {
                            string url = Sub.SubUrl;
                            XmlReader reader = XmlReader.Create(url);
                            SyndicationFeed feed = SyndicationFeed.Load(reader);
                            reader.Close();

                            //讀取前次更新時間
                            _logger.LogInformation("讀取前次更新時間：");
                            var time = await _sqlcon.QueryFirstOrDefaultAsync<DateTime>("SELECT UpdateTime FROM Time");

                            //判斷通知時間為
                            foreach (var item in
                                feed.Items.Where(o => (o.PublishDate.DateTime == DateTime.MinValue ? TimeZoneInfo.ConvertTimeFromUtc(o.LastUpdatedTime.UtcDateTime, targetTimeZone) : TimeZoneInfo.ConvertTimeFromUtc(o.PublishDate.UtcDateTime, targetTimeZone)) >= time).ToList())
                            {
                                InlineList.Add(new[] { InlineKeyboardButton.WithUrl(item.Title.Text, item.Links[0].Uri.ToString()) });
                            }

                            if (InlineList.Any())
                            {
                                InlineKeyboardMarkup inlineKeyboard = new(InlineList);
                                await botClient.SendTextMessageAsync(
                                    chatId: user.UserID,
                                    text: @$"{feed.Title.Text}⚡️",
                                    replyMarkup: inlineKeyboard
                                    );
                                InlineList.Clear();
                            }
                        }
                    }
                    //更新通知時間
                    _ = await _sqlcon.ExecuteAsync("Update Time SET UpdateTime = @UpdateTime", new { UpdateTime = TimeZoneInfo.ConvertTime(DateTime.Now, targetTimeZone) });


                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("排程執行錯誤：" + ex.Message);
            }
            return;
        }
    }
}