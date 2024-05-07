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
        #region Identity

        public const string NAME = "Ultimate Trader Manager";

        public const string VERSION = "1.0";

        #endregion

        #region Enum
        public enum OpenTradeType
        {
            All,
            Buy,
            Sell
        }

        public enum TradingMode
        {
            Auto,
            Manual,
            Both
        }

        public enum AutoStrategyName
        {
            Trend_MA,
            HHLL,
            RSIMeanReversion,
            BreakoutTrading
        }

        public enum RiskBase
        {
            BaseEquity,
            BaseBalance,
            BaseMargin,
            BaseFixedBalance
        };

        public enum PositionSizeMode
        {
            Risk_Fixed,
            Risk_Auto,
        };

        public enum StopLossMode
        {
            SL_None,
            SL_Fixed,
            SL_Auto_ADR,
        };

        public enum TakeProfitMode
        {
            TP_None,
            TP_Fixed,
            TP_Auto_ADR,
            TP_Auto_RRR,
            TP_Multi
        };

        public enum TrailingMode
        {
            TL_None,
            TL_Fixed,
            TL_Fixed_BE,
            TL_Psar,
            TL_Pyramid,
        };
        #endregion

        #region Parameters of CBot

        #region Identity
        [Parameter(NAME + " " + VERSION, Group = "IDENTITY", DefaultValue = "https://haruperi.ltd/trading/")]
        public string ProductInfo { get; set; }

        [Parameter("Preset information", Group = "IDENTITY", DefaultValue = "XAUUSD Range5 | 01.01.2024 to 29.04.2024 | $1000")]
        public string PresetInfo { get; set; }
        #endregion

        #region Strategy

        [Parameter("Open Trade Type", Group = "STRATEGY", DefaultValue = OpenTradeType.All)]
        public OpenTradeType MyOpenTradeType { get; set; }

        [Parameter("Trading Mode", Group = "STRATEGY", DefaultValue = TradingMode.Both)]
        public TradingMode MyTradingMode { get; set; }

        [Parameter("Auto Strategy Name", Group = "STRATEGY", DefaultValue = AutoStrategyName.Trend_MA)]
        public AutoStrategyName MyAutoStrategyName { get; set; }
        #endregion

        #region Trading Hours
        [Parameter("Use Trading Hours", Group = "TRADING HOURS", DefaultValue = true)]
        public bool UseTradingHours { get; set; }

        [Parameter("Starting Hour", Group = "TRADING HOURS", DefaultValue = 02)]
        public int TradingHourStart { get; set; }

        [Parameter("Ending Hour", Group = "TRADING HOURS", DefaultValue = 23)]
        public int TradingHourEnd { get; set; }
        #endregion

        #region Risk Management Settings
        [Parameter("Position size mode", Group = "RISK MANAGEMENT", DefaultValue = PositionSizeMode.Risk_Fixed)]
        public PositionSizeMode MyPositionSizeMode { get; set; }

        [Parameter("Default Lot Size", Group = "RISK MANAGEMENT", DefaultValue = 0.01, MinValue = 0.01, Step = 0.01)]
        public double DefaultLotSize { get; set; }

        [Parameter("Cal Risk From ", Group = "RISK MANAGEMENT", DefaultValue = RiskBase.BaseBalance)]
        public RiskBase MyRiskBase { get; set; }

        [Parameter("Base Fixed Balance ", Group = "RISK MANAGEMENT", DefaultValue = 100)]
        public double MyBaseFixedBalance { get; set; }

        [Parameter("Max Risk % Per Trade", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 0.1, Step = 0.01)]
        public double MaxRiskPerTrade { get; set; }

        [Parameter("Risk Reward Ratio - 1:", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 0.1, Step = 0.01)]
        public double RiskRewardRatio { get; set; }

        [Parameter("Max Positions", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 1, Step = 1)]
        public int MaxPositions { get; set; }

        [Parameter("Max Buy Positions", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 1, Step = 1)]
        public int MaxBuyPositions { get; set; }

        [Parameter("Max Sell Positions", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 1, Step = 1)]
        public int MaxSellPositions { get; set; }

        [Parameter("Stop Loss Mode", Group = "RISK MANAGEMENT", DefaultValue = StopLossMode.SL_Fixed)]
        public StopLossMode MyStopLossMode { get; set; }

        [Parameter("Default StopLoss ", Group = "RISK MANAGEMENT", DefaultValue = 20, MinValue = 5, Step = 1)]
        public double DefaultStopLoss { get; set; }

        [Parameter("Use Fake StopLoss ", Group = "RISK MANAGEMENT", DefaultValue = true)]
        public bool UseFakeStopLoss { get; set; }

        [Parameter("Fake StopLoss ", Group = "RISK MANAGEMENT", DefaultValue = 200, MinValue = 100, Step = 5)]
        public double FakeStopLoss { get; set; }

        [Parameter("Take Profit Mode", Group = "RISK MANAGEMENT", DefaultValue = TakeProfitMode.TP_Fixed)]
        public TakeProfitMode MyTakeProfitMode { get; set; }

        [Parameter("Default Take Profit", Group = "RISK MANAGEMENT", DefaultValue = 21, MinValue = 5, Step = 1)]
        public double DefaultTakeProfit { get; set; }
        #endregion

        #region Trade Management
        [Parameter("Use Auto Trade Management", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool AutoTradeManagement { get; set; }

        [Parameter("Split Trades", Group = "TRADE MANAGEMENT", DefaultValue = true)]
        public bool SplitTrades { get; set; }

        [Parameter("How Many Split Trades", Group = "TRADE MANAGEMENT", DefaultValue = 2)]
        public double NumOfSplitTrades { get; set; }

        [Parameter("Use Pyramids for TP", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool DoPyramidsTrading { get; set; }

        [Parameter("Stop Bot On Equity Target", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool IsStopOnEquityTarget { get; set; }

        [Parameter("Equity Target", Group = "TRADE MANAGEMENT", DefaultValue = 100000)]
        public double EquityTarget { get; set; }

        [Parameter("Cost Ave Distance", Group = "TRADE MANAGEMENT", DefaultValue = 20)]
        public double CostAveDistance { get; set; }

        [Parameter("Pyramid Distance", Group = "TRADE MANAGEMENT", DefaultValue = 20)]
        public double PyramidDistance { get; set; }

        [Parameter("Cost Ave Distance Multiplier", Group = "TRADE MANAGEMENT", DefaultValue = 2)]
        public double CostAveMultiplier { get; set; }

        [Parameter("Pyramid Lot Divisor", Group = "TRADE MANAGEMENT", DefaultValue = 2)]
        public double PyramidLotDivisor { get; set; }

        [Parameter("Pyramid Stop Loss", Group = "TRADE MANAGEMENT", DefaultValue = 5)]
        public double PyramidStopLoss { get; set; }

        [Parameter("Use Trailing Stop ", Group = "TRADE MANAGEMENT", DefaultValue = TrailingMode.TL_Fixed_BE)]
        public TrailingMode MyTrailingMode { get; set; }

        [Parameter("Trail After (Pips) ", Group = "TRADE MANAGEMENT", DefaultValue = 10, MinValue = 1)]
        public double WhenToTrail { get; set; }

        [Parameter("Break-Even Losing Trades", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool BreakEvenLosing { get; set; }

        [Parameter("Cost Ave Take Profit", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool IsCostAveTakeProfit { get; set; }

        [Parameter("Commissions pips", Group = "TRADE MANAGEMENT", DefaultValue = 1, MinValue = 1)]
        public int CommissionsPips { get; set; }

        [Parameter("Pending Order Distance (pips)", Group = "TRADE MANAGEMENT", DefaultValue = 2, MinValue = 1)]
        public double PendingOrderDistance { get; set; }


        #endregion

        #region  Indicator Settings
        [Parameter("ADRPeriod", Group = "INDICATOR SETTINGS", DefaultValue = 10)]
        public int ADRPeriod { get; set; }

        [Parameter("ADR Divisor SL", Group = "INDICATOR SETTINGS", DefaultValue = 3)]
        public double ADR_SL { get; set; }

        [Parameter("WPRPeriod", Group = "INDICATOR SETTINGS", DefaultValue = 5)]
        public int WPRPeriod { get; set; }

        [Parameter("RSI Period", Group = "INDICATOR SETTINGS", DefaultValue = 14)]
        public int RSIPeriod { get; set; }

        [Parameter("RSI OSLevel", Group = "INDICATOR SETTINGS", DefaultValue = 30)]
        public int OSLevel { get; set; }

        [Parameter("RSI OBLevel", Group = "INDICATOR SETTINGS", DefaultValue = 70)]
        public int OBLevel { get; set; }

        [Parameter("Breakout Period", DefaultValue = 10, MinValue = 3)]
        public int BreakoutPeriod { get; set; }

        [Parameter("Min Acceleration Factor", Group = "Parabolic SAR", DefaultValue = 0.02, MinValue = 0, Step = 0.01)]
        public double MinAccFactor { get; set; }

        [Parameter("Max Acceleration Factor", Group = "Parabolic SAR", DefaultValue = 0.2, MinValue = 0, Step = 0.01)]
        public double MaxAccFactor { get; set; }
        #endregion

        #region EA Settings
        [Parameter("Max Slippage ", Group = "EA SETTINGS", DefaultValue = 1, MinValue = 1)]
        public int MaxSlippage { get; set; }

        [Parameter("Max Spread Allowed ", Group = "EA SETTINGS", DefaultValue = 3, MinValue = 1, Step = 0.1)]
        public double MaxSpread { get; set; }

        [Parameter("Bot Label", Group = "EA SETTINGS", DefaultValue = "RH Bot - ")]
        public string BotLabel { get; set; }
        #endregion

        #region Notification Settings

        [Parameter("Popup Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool PopupNotification { get; set; }

        [Parameter("Sound Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool SoundNotification { get; set; }

        [Parameter("Email Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool EmailNotification { get; set; }

        [Parameter("Email address", Group = "NOTIFICATION SETTINGS", DefaultValue = "notify@testmail.com")]
        public string EmailAddress { get; set; }

        [Parameter("Telegram Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool TelegramEnabled { get; set; }

        [Parameter("API Token", Group = "NOTIFICATION SETTINGS", DefaultValue = "")]
        public string TelegramToken { get; set; }

        [Parameter("Chat IDs (separate by comma)", Group = "NOTIFICATION SETTINGS", DefaultValue = "")]
        public string TelegramChatIDs { get; set; }
        #endregion

        #region Display Settings
        [Parameter("Buy", Group = "DISPLAY SETTINGS", DefaultValue = "#5335E5")]
        public Color BuyColor { get; set; }

        [Parameter("Sell", Group = "DISPLAY SETTINGS", DefaultValue = "#FC1D85")]
        public Color SellColor { get; set; }

        [Parameter("LineStyle", Group = "DISPLAY SETTINGS", DefaultValue = LineStyle.Solid)]
        public LineStyle HLineStyle { get; set; }

        [Parameter("Thickness", Group = "DISPLAY SETTINGS", DefaultValue = 1)]
        public int HLineThickness { get; set; }

        [Parameter("Color", Group = "DISPLAY SETTINGS", DefaultValue = "DarkGoldenrod")]
        public string HLineColor { get; set; }

        [Parameter("Transparency", Group = "DISPLAY SETTINGS", DefaultValue = 60, MinValue = 1, MaxValue = 100)]
        public int HLineTransparency { get; set; }

        [Parameter("Horizontal Alignment", Group = "DISPLAY SETTINGS", DefaultValue = HorizontalAlignment.Left)]
        public HorizontalAlignment PanelHorizontalAlignment { get; set; }

        [Parameter("Vertical Alignment", Group = "DISPLAY SETTINGS", DefaultValue = VerticalAlignment.Top)]
        public VerticalAlignment PanelVerticalAlignment { get; set; }

        [Parameter("Text Color", Group = "DISPLAY SETTINGS", DefaultValue = "Snow")]
        public string ColorText { get; set; }

        [Parameter("Show How To Use", Group = "DISPLAY SETTINGS", DefaultValue = true)]
        public bool ShowHowToUse { get; set; }
        #endregion

        #region Global variables

        private StackPanel contentPanel;
        private TextBlock ShowHeader, ShowADR, ShowCurrentADR, ShowADRPercent, ShowDrawdown, ShowLotsInfo, ShowTradesInfo, ShowTargetInfo, ShowSpread;
        private Grid PanelGrid;
        private ToggleButton buystoplimitbutton, sellstoplimitbutton;
        private Color hColour;
        private ChartHorizontalLine HorizontalLine;

        private bool _isPreChecksOk, _isSpreadOK, _isOperatingHours, _isUpSwing, _rsiBullishTrigger, _rsiBearishTrigger, buySLbool, sellSLbool, _isRecoveryTrade, _isPyramidTrade;
        private int _totalOpenOrders, _totalOpenBuy, _totalOpenSell, _totalPendingOrders, _totalPendingBuy, _totalPendingSell, _signalEntry, _signalExit, _breakoutSignal;
        private double _gridDistanceBuy, _gridDistanceSell, _atr, _adrCurrent, _adrOverall, _adrPercent, _nextBuyCostAveLevel, _nextSellCostAveLevel,
                        _nextBuyPyAddLevel, _nextSellPyrAddLevel, _PyramidSellStopLoss, _PyramidBuyStopLoss, WhenToTrailPrice,
                        _highestHigh, _lowestHigh, _highestLow, _lowestLow, _lastSwingHigh, _lastSwingLow, _breakoutBuy, _breakoutSell;
        double[] HTBarHigh, HTBarLow, HTBarClose, HTBarOpen, LTBarHigh, LTBarLow, LTBarClose, LTBarOpen = new double[5];
        int HTOldNumBars = 0, LTOldNumBars = 0;
        private string OrderComment, _recoverySTR, _pyramidSTR;

        private RelativeStrengthIndex _rsi;
        private WilliamsPctR _williamsPctR;
        private ParabolicSAR parabolicSAR;
        private AverageTrueRange _averageTrueRange;
        private MovingAverage _fastMA, _slowMA, _ltffastMA, _ltfslowMA, _htffastMA, _htfslowMA;

        private Bars _dailyBars;

        private const string Sign = "RBO";
        private TrendLevel LastBreakOutBuy { get; set; }
        private TrendLevel LastBreakOutSell { get; set; }
        private int _lastIndex { get; set; }

        #endregion

        #endregion

        #region Standard event handlers

        #region OnStart function
        protected override void OnStart()
        {
            CheckPreChecks();

            if (!_isPreChecksOk) Stop();

            LastBreakOutBuy = null;
            LastBreakOutSell = null;
            _lastIndex = -1;

            _recoverySTR = "Recovery";
            _pyramidSTR = "Pyramid";

            OrderComment = BotLabel + MyAutoStrategyName.ToString();
        }

        #endregion
        protected override void OnTick()
        {

        }

        protected override void OnStop()
        {

        }

        protected override void OnBar()
        {
            OnBarInitialization();

            CheckOperationHours();

            int index = Bars.ClosePrices.Count - 1;
            int signal = SupportResistanceSignal(index);

            if (signal == 1)
            {
                double volume = Symbol.NormalizeVolumeInUnits(Symbol.QuantityToVolumeInUnits(DefaultLotSize));

                var position = ExecuteMarketOrder(TradeType.Buy, SymbolName, volume, "SupportResistanceBot", DefaultStopLoss, DefaultTakeProfit);
            }

            if (signal == 0)
            {
                double volume = Symbol.NormalizeVolumeInUnits(Symbol.QuantityToVolumeInUnits(DefaultLotSize));

                var position = ExecuteMarketOrder(TradeType.Sell, SymbolName, volume, "SupportResistanceBot", DefaultStopLoss, DefaultTakeProfit);
            }
        }

        #endregion


        #region Custom Functions 

        #region CheckPreChecks
        private void CheckPreChecks()
        {
            _isPreChecksOk = true;

            //Slippage must be >= 0
            if (MaxSlippage < 0)
            {
                _isPreChecksOk = false;
                Print("Slippage must be a positive value");
                return;
            }
            //MaxSpread must be >= 0
            if (MaxSpread < 0)
            {
                _isPreChecksOk = false;
                Print("Maximum Spread must be a positive value");
                return;
            }
            //MaxRiskPerTrade is a % between 0 and 100
            if (MaxRiskPerTrade < 0 || MaxRiskPerTrade > 100)
            {
                _isPreChecksOk = false;
                Print("Maximum Risk Per Trade must be a percentage between 0 and 100");
                return;
            }
        }

        #endregion

        #region OnBar Initialization 
        private void OnBarInitialization()
        {
            _isPreChecksOk = false;
            _isSpreadOK = false;
            _isOperatingHours = false;

            _totalOpenOrders = 0;
            _totalOpenBuy = 0;
            _totalOpenSell = 0;
            _totalPendingBuy = 0;
            _totalPendingSell = 0;
            _totalPendingOrders = 0;
            _signalEntry = 0;
            _signalExit = 0;

            _isRecoveryTrade = false;
            _isPyramidTrade = false;
        }
        #endregion

        #region Check Operation Hours
        private void CheckOperationHours()
        {
            //If we are not using operating hours then IsOperatingHours is true and I skip the other checks
            if (!UseTradingHours)
            {
                _isOperatingHours = true;
                return;
            }

            //Check if the current hour is between the allowed hours of operations, if so IsOperatingHours is set true
            if (TradingHourStart == TradingHourEnd && Server.Time.Hour == TradingHourStart) _isOperatingHours = true;
            if (TradingHourStart < TradingHourEnd && Server.Time.Hour >= TradingHourStart && Server.Time.Hour <= TradingHourEnd) _isOperatingHours = true;
            if (TradingHourStart > TradingHourEnd && ((Server.Time.Hour >= TradingHourStart && Server.Time.Hour <= 23) || (Server.Time.Hour <= TradingHourEnd && Server.Time.Hour >= 0))) _isOperatingHours = true;
        }
        #endregion

        #endregion

        private int SupportResistanceSignal(int index)
        {
            if (index < BreakoutPeriod) return -1;

            int A = index - BreakoutPeriod;
            int B = index;

            if (LastBreakOutBuy == null)
            {
                if (ReadyResistance(index, BreakoutPeriod))
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
                if (ReadySupport(index, BreakoutPeriod))
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

            if (_lastIndex != index)
            {
                _lastIndex = index;
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