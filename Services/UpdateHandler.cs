using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace TGBot_RssFeed_Polling.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly RssService _rssService;

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger, RssService rssService)
    {
        _botClient = botClient;
        _logger = logger;
        _rssService = rssService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;

        if (messageText == "/start" || messageText == "hello")
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Hello " + message.From.FirstName + " " + message.From.LastName + "",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }
        else if (messageText.Contains("/list"))
        {
            var result = await _rssService.GetUserRssList(message.Chat.Id);

            if (result.Any())
            {
                StringBuilder sb = new StringBuilder();
                result.ForEach(item => sb.AppendLine($"{item.Num}.{item.SubTitle}"));

                _ = await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: sb.ToString(),
                    cancellationToken: cancellationToken);
            }
            else
            {
                _ = await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"訂閱清單為空",
                    cancellationToken: cancellationToken);
            }
        }
        else if (messageText.Contains("/send"))
        {
            var result = await _rssService.SendRss(message.Chat.Id);

            foreach (var item in result)
            {
                InlineKeyboardMarkup inlineKeyboard = new(item.List);

                _ = await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: @$"{item.Title}⚡️",
                    replyMarkup: inlineKeyboard);
            }
        }
        else if (messageText.Split().ToList().Count >= 2)
        {
            var text = messageText.Split().ToList();

            //訂閱
            if (messageText.Contains("/sub"))
            {
                var result = await _rssService.AddRss(message.Chat.Id, text[1]);
                if (result)
                {
                    _ = await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"訂閱成功！",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    _ = await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"訂閱失敗，請檢查網址是否為RSS格式或重複訂閱",
                        cancellationToken: cancellationToken);
                }
            }
            else if (messageText.Contains("/del"))
            {
                int num;
                if (int.TryParse(text[1], out num))
                {
                    if (await _rssService.DelRss(message.Chat.Id, num))
                    {
                        _ = await _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"移除成功！",
                            cancellationToken: cancellationToken);
                        Console.WriteLine($"{message.Chat.Id}：移除成功");
                    }
                    else
                    {
                        _ = await _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"錯誤：移除失敗",
                            cancellationToken: cancellationToken);
                        Console.WriteLine($"{message.Chat.Id}：移除失敗");

                    }
                }
                else
                {
                    _ = await _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"錯誤：請輸入要移除的{"}編號{"}",
                            cancellationToken: cancellationToken);
                    Console.WriteLine($"{message.Chat.Id}：請輸入要移除的{"}編號{"}");

                }
            }
        }

        #region example
        //var action = messageText.Split(' ')[0] switch
        //{
        //    "/inline_keyboard" => SendInlineKeyboard(_botClient, message, cancellationToken),
        //    "/keyboard" => SendReplyKeyboard(_botClient, message, cancellationToken),
        //    "/remove" => RemoveKeyboard(_botClient, message, cancellationToken),
        //    "/photo" => SendFile(_botClient, message, cancellationToken),
        //    "/request" => RequestContactAndLocation(_botClient, message, cancellationToken),
        //    "/inline_mode" => StartInlineQuery(_botClient, message, cancellationToken),
        //    _ => Usage(_botClient, message, cancellationToken)
        //};
        //Message sentMessage = await action;
        //_logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

        //Send inline keyboard
        //You can process responses in BotOnCallbackQueryReceived handler
        #endregion

    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);

        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient _botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}
