namespace TGbot_RssFeed.Model
{
    public class Subscription
    {
        /// <summary> ID </summary>
        public int ID { get; set; }

        /// <summary> 訂閱序號 </summary>
        public string Num { get; set; }

        /// <summary> 使用者ID </summary>
        public string UserID { get; set; }

        /// <summary> 訂閱網站名稱 </summary>
        public string SubTitle { get; set; }

        /// <summary> 訂閱網址 </summary>
        public string SubUrl { get; set; }
    }
}
