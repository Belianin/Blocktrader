using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blocktrader.Domain;

namespace Blocktrader.Service.Files
{
    [Obsolete("Нет смысла париться, сделам на костлях раз потом уедем в веб")]
    public class BufferedTimestampManager : ITimestampManager
    {
        private readonly ITimestampManager innerManager;
        private readonly IDictionary<Ticker, MonthTimestamp> current;

        public BufferedTimestampManager(ITimestampManager innerManager)
        {
            this.innerManager = innerManager;
            current = new Dictionary<Ticker, MonthTimestamp>();
        }

        public async Task WriteAsync(CommonTimestamp commonTimestamp)
        {
            await innerManager.WriteAsync(commonTimestamp).ConfigureAwait(false);
        }

        public MonthTimestamp ReadTimestampsFromMonth(DateTime dateTime, Ticker ticker)
        {
            return innerManager.ReadTimestampsFromMonth(dateTime, ticker);
        }
    }
}