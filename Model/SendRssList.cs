using Telegram.Bot.Types.ReplyMarkups;

namespace TGBot_RssFeed_Polling.Model
{
    public class SendRssList
    {
        /// <summary>
        /// 標題
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 清單
        /// </summary>
        public List<IEnumerable<InlineKeyboardButton>> List { get; set; }
    }
}
