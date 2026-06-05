using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinWigApp.Application.DTOs
{
    public class YahooQuoteResult
    {
        public decimal CurrentPrice { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal DayLow { get; set; }
        public decimal DayHigh { get; set; }
        public long Volume { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }
    }
}
