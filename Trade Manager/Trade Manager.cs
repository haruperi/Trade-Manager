using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class TradeManager : Robot
    {
        [Parameter("Lot Size", Group = "Volume", DefaultValue = 0.01, MinValue = 0.00001, MaxValue = 100, Step = 0.00001)]
        public double LotSize { get; set; }

        [Parameter("Stop Loss (pips)", Group = "Risk Management", DefaultValue = 50, MinValue = 1)]
        public int StopLossInPips { get; set; }

        [Parameter("Take Profit (pips)", Group = "Risk Management", DefaultValue = 50, MinValue = 1)]
        public int TakeProfitInPips { get; set; }

        [Parameter("Period", DefaultValue = 10, MinValue = 3)]
        public int Period { get; set; }

        [Parameter("Buy", Group = "Colors", DefaultValue = "DodgerBlue")]
        public Color BuyColor { get; set; }

        [Parameter("Sell", Group = "Colors", DefaultValue = "Red")]
        public Color SellColor { get; set; }

        private const string Sign = "TBO";
        private TrendLevel LastBreakOutBuy { get; set; }
        private TrendLevel LastBreakOutSell { get; set; }
        private int LastIndex { get; set; }

        private bool isPositionOpen = false;

        protected override void OnStart()
        {
            LastBreakOutBuy = null;
            LastBreakOutSell = null;
            LastIndex = -1;
        }

        protected override void OnBar()
        {
            int index = MarketSeries.Close.Count - 1;
            int signal = SupportResistanceSignal(index);

            if (!isPositionOpen && signal != -1)
            {
                TradeType tradeType = (signal == 0) ? TradeType.Buy : TradeType.Sell;

                double volume = Symbol.NormalizeVolumeInUnits(Symbol.QuantityToVolumeInUnits(LotSize));

                var position = ExecuteMarketOrder(tradeType, Symbol, volume, "SupportResistanceBot", StopLossInPips, TakeProfitInPips);

                if (position != null)
                {
                    isPositionOpen = true;
                }
            }
        }

        protected override void OnPositionClosed(Position position)
        {
            isPositionOpen = false;
        }

        private int SupportResistanceSignal(int index)
        {
            if (index < Period) return -1;

            int A = index - Period;
            int B = index;

            if (LastBreakOutBuy == null)
            {
                if (ReadyResistance(index, Period))
                    LastBreakOutBuy = new TrendLevel
                    {
                        Name = string.Format("{0}-Buy-{1}", Sign, A),
                        IndexA = A,
                        IndexB = B,
                        Price = Bars.HighPrices[A]
                    };
            }

            if (LastBreakOutSell == null)
            {
                if (ReadySupport(index, Period))
                    LastBreakOutSell = new TrendLevel
                    {
                        Name = string.Format("{0}-Sell-{1}", Sign, A),
                        IndexA = A,
                        IndexB = B,
                        Price = Bars.LowPrices[A]
                    };
            }

            if (LastBreakOutBuy != null) LastBreakOutBuy.IndexB = B;
            if (LastBreakOutSell != null) LastBreakOutSell.IndexB = B;
            DrawTrendLevel(LastBreakOutBuy, BuyColor);
            DrawTrendLevel(LastBreakOutSell, SellColor);

            int signal = -1;

            if (LastIndex != index)
            {
                LastIndex = index;
                signal = OnBarforindicator(index);
            }

            return signal;
        }

        private int OnBarforindicator(int index)
        {
            int B = index - 1;
            int C = index - 2;

            int signal = -1;

            if (LastBreakOutBuy != null)
            {
                if (Bars.ClosePrices[B] > LastBreakOutBuy.Price)
                {
                    LastBreakOutBuy.IndexB = B;
                    DrawTrendLevel(LastBreakOutBuy, BuyColor);

                    if (LastBreakOutSell != null)
                    {
                        int vPeriod = LastBreakOutBuy.IndexB - LastBreakOutBuy.IndexA - 1;
                        int newindexA = C;

                        for (int i = 0; i < vPeriod; i++)
                        {
                            if (Bars.LowPrices[C - i] < Bars.LowPrices[newindexA]) newindexA = C - i;
                        }

                        if (Bars.LowPrices[newindexA] > LastBreakOutSell.Price)
                        {
                            LastBreakOutSell = new TrendLevel
                            {
                                Name = string.Format("{0}-Sell-{1}", Sign, newindexA),
                                IndexA = newindexA,
                                IndexB = index,
                                Price = Bars.LowPrices[newindexA]
                            };
                            DrawTrendLevel(LastBreakOutSell, SellColor);
                            signal = 1; // Sell signal
                        }
                    }
                    LastBreakOutBuy = null;
                }
            }

            if (LastBreakOutSell != null)
            {
                if (Bars.ClosePrices[B] < LastBreakOutSell.Price)
                {
                    LastBreakOutSell.IndexB = B;
                    DrawTrendLevel(LastBreakOutSell, SellColor);

                    if (LastBreakOutBuy != null)
                    {
                        int vPeriod = LastBreakOutSell.IndexB - LastBreakOutSell.IndexA - 1;
                        int newindexA = C;

                        for (int i = 0; i < vPeriod; i++)
                        {
                            if (Bars.HighPrices[C - i] > Bars.HighPrices[newindexA]) newindexA = C - i;
                        }

                        if (Bars.HighPrices[newindexA] < LastBreakOutBuy.Price)
                        {
                            LastBreakOutBuy = new TrendLevel
                            {
                                Name = string.Format("{0}-Buy-{1}", Sign, newindexA),
                                IndexA = newindexA,
                                IndexB = index,
                                Price = Bars.HighPrices[newindexA]
                            };
                            DrawTrendLevel(LastBreakOutBuy, BuyColor);
                            signal = 0; // Buy signal
                        }
                    }
                    LastBreakOutSell = null;
                }
            }

            return signal;
        }

        private void DrawTrendLevel(TrendLevel tLine, Color color)
        {
            if (tLine == null) return;
            Chart.DrawTrendLine(tLine.Name, tLine.IndexA, tLine.Price, tLine.IndexB, tLine.Price, color, 1);
        }

        private bool ReadyResistance(int index, int period)
        {
            bool result = true;
            for (int i = 0; i < period; i++)
            {
                if (Bars.ClosePrices[index - i] > Bars.HighPrices[index - period])
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        private bool ReadySupport(int index, int period)
        {
            bool result = true;
            for (int i = 0; i < period; i++)
            {
                if (Bars.ClosePrices[index - i] < Bars.LowPrices[index - period])
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        private class TrendLevel
        {
            public string Name { get; set; }
            public int IndexA { get; set; }
            public int IndexB { get; set; }
            public double Price { get; set; }
        }
    }
}