## TG RSS訂閱服務機器人 基本使用教學  :memo:

示範機器人(若不想自己部屬也可以直接使用)

```cmd
https://t.me/Tian_RSS_bot
```

使用方法：

一、直接執行

1.下載後將appsettings.Prod.json裡的APIkey換成自己的API Key後執行檔案即可使用

二、使用Docker執行

1.將程式pull下來後打包成Docker使用
```cmd
docker build -t 名稱 . --no-cache
```

## 機器人指令

⭐️訂閱網站
```cmd
/sub + rss網址
```

⭐️查詢訂閱清單
```cmd
/list 
```

⭐️刪除訂閱網站
```cmd
/del + 編號 
```

⭐️傳送RSS (傳送最新五筆RSS)
```cmd
/send
```

## 未來預計更新內容  :memo:
```cmd
1.設定通知間隔
2.設定文章排列方式
```
