using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Logging;
using Nexus.Logging.Utils;
using Telegram.Bot;

namespace Nexus.Prophecy.Notifications.Telegram
{
    public class TelegramNotificator : INotificator, IDisposable
    {
        private readonly HashSet<long> channels;
        private readonly ITelegramBotClient client;
        private readonly ILog log;

        public TelegramNotificator(string token, ILog log, params long[] channels)
        {
            this.log = log;
            this.channels = new HashSet<long>(channels);
            client = new TelegramBotClient(token);
            
            try
            {
                client.StartReceiving();
            }
            catch (Exception e)
            {
                log?.Fatal($"TelegramLog failed to start: {e.Message}");
            }
        }

        public async Task<Result> NotifyAsync(Notification notification)
        {
            try
            {
                foreach (var channel in channels)
                {
                    await client.SendTextMessageAsync(channel, FormatNotification(notification))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                log?.Fatal($"Telegram is dead: {e.Message}");
                return Result.Fail(e.Message);
            }
            
            return Result.Ok();
        }

        public void Dispose()
        {
            client.StopReceiving();
        }

        private string FormatNotification(Notification notification)
        {
            return $"{FormatLogLevel(notification.LogLevel)} " +
                   $"{(notification.Context.Length > 0 ? string.Join("", notification.Context.Select(c => $"[{c}] ")) : string.Empty)}" +
                   $"{notification.Message}"; 
        }

        private string FormatLogLevel(LogLevel level) => level switch
        {
            LogLevel.Debug => $"🛠#debug",
            LogLevel.Info => $"💬#info",
            LogLevel.Warn => $"⚠#warn",
            LogLevel.Error => $"⛔#error",
            LogLevel.Fatal => $"💀#fatal",
            LogLevel.Important => $"‼️#important",
            _ => "#wtf"
        };
    }
}