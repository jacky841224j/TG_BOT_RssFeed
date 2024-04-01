using Dapper;
using Microsoft.Data.Sqlite;
using System.ServiceModel.Syndication;
using System.Xml;
using Telegram.Bot.Types.ReplyMarkups;
using TGbot_RssFeed.Model;
using TGBot_RssFeed_Polling.Model;

namespace TGBot_RssFeed_Polling.Services
{
    public class RssService
    {
        private readonly ILogger<RssService> _logger;
        private readonly SqliteConnection _sqlcon;

        public RssService(ILogger<RssService> logger, SqliteConnection sqlcon)
        {
            _logger = logger;
            _sqlcon = sqlcon;
        }

        /// <summary>
        /// 新增訂閱
        /// </summary>
        /// <param name="id">使用者ID</param>
        /// <param name="url">訂閱網址</param>
        public async Task<bool> AddRss(long id, string url)
        {
            try
            {
                _logger.LogInformation("新增訂閱...");

                //連線
                using (_sqlcon)
                {
                    await _sqlcon.OpenAsync();
                    _logger.LogInformation("搜尋UserID...");
                    //搜尋UserID
                    var UserID = await _sqlcon.QueryFirstOrDefaultAsync<string>("SELECT UserID FROM User WHERE UserID = @UserID", new { UserID = id, });

                    //若 UserID 不存在則新增
                    if (UserID == null)
                    {
                        _logger.LogInformation("UserID不存在，新增中...");
                        _ = await _sqlcon.ExecuteAsync("INSERT INTO User (UserID) values (@UserID)", new { UserID = id });
                    }

                    //搜尋訂閱數目
                    var User = await _sqlcon.QueryAsync<Subscription>("SELECT * FROM Sub WHERE UserID = @UserID", new { UserID = id, });
                    var count = User.Count();

                    var exits = User.Where(x => x.SubUrl == url).Any();
                    if (exits) throw new Exception("訂閱網址已存在");
                    
                    _logger.LogInformation("解析RSS...");
                    XmlReader reader = XmlReader.Create(url);
                    SyndicationFeed feed = SyndicationFeed.Load(reader);
                    var SubTitle = feed.Title.Text;

                    _logger.LogInformation("新增RSS...");
                    //新增資料
                    _ = await _sqlcon.ExecuteAsync("INSERT INTO Sub (UserID,Num,SubTitle,SubUrl) values (@UserID,@Num,@SubTitle,@SubUrl)", new { UserID = id, Num = count + 1, SubTitle, SubUrl = url });
                }

                _logger.LogInformation("RSS新增成功...");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("錯誤-新增訂閱(AddRss)：" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 取消訂閱
        /// </summary>
        /// <param name="user"></param>
        /// <param name="delID"></param>
        /// <returns></returns>
        public async Task<bool> DelRss(long user, int delID)
        {
            try
            {
                _logger.LogInformation("取消訂閱...");

                using (_sqlcon)
                {
                    await _sqlcon.OpenAsync();
                    _ = await _sqlcon.ExecuteAsync("DELETE FROM Sub WHERE UserID = @UserID AND Num = @Num ", new { UserID = user, Num = delID });
                }

                _logger.LogInformation("取消成功...");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("錯誤-取消訂閱(DelRss)：" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 取得使用者訂閱名單
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<List<Subscription>> GetUserRssList(long id)
        {
            var sublist = new List<Subscription>();
            try
            {
                _logger.LogInformation("取得使用者訂閱名單...");
                using (_sqlcon)
                {
                    await _sqlcon.OpenAsync();
                    sublist = (await _sqlcon.QueryAsync<Subscription>("SELECT * FROM Sub WHERE UserID = @UserID ", new { UserID = id })).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("錯誤-取得使用者訂閱名單(GetUserRssList)：" + ex.Message);
            }
            return sublist;
        }

        /// <summary>
        /// 傳送最近五筆RSS
        /// </summary>
        public async Task<List<SendRssList>> SendRss(long id)
        {
            var result  = new List<SendRssList>();

            try
            {
                _logger.LogInformation("傳送最近五筆RSS...");

                using (_sqlcon)
                {
                    await _sqlcon.OpenAsync();

                    _logger.LogInformation("讀取使用者訂閱清單...");
                    //讀取使用者訂閱清單
                    var SubList = await _sqlcon.QueryAsync<Subscription>("SELECT * FROM Sub WHERE UserID = @UserID ", new { UserID = id });

                    foreach (var Sub in SubList.ToList())
                    {
                        string url = Sub.SubUrl;
                        var InlineList = new List<IEnumerable<InlineKeyboardButton>>();
                        XmlReader reader = XmlReader.Create(url);
                        SyndicationFeed feed = SyndicationFeed.Load(reader);
                        reader.Close();

                        //判斷通知時間為
                        foreach (var item in
                            feed.Items.Take(5).ToList())
                        {
                            InlineList.Add(new[] { InlineKeyboardButton.WithUrl(item.Title.Text, item.Links[0].Uri.ToString()) });
                        }

                        result.Add(new SendRssList { 
                            Title = feed.Title.Text,
                            List = InlineList,
                        });
                    }
                }
                _logger.LogInformation("讀取完成...");
            }
            catch (Exception ex)
            {
                _logger.LogInformation("錯誤-傳送最近五筆RSS(SendRss)：" + ex.Message);
            }

            return result;
        }
    }
}
