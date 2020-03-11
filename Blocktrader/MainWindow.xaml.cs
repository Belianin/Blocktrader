﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Blocktrader.Domain;
using Blocktrader.Service;
using Blocktrader.Service.Files;
using Blocktrader.Utils.Logging;

namespace Blocktrader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TimeSpan updateInterval = TimeSpan.FromMinutes(10);
        private readonly BlocktraderService service;
        private readonly ITimestampManager timestampManager;

        private FilterSettings filterSettings = new FilterSettings();

        private Ticket currentTicket = Ticket.BtcUsd;
        private DateTime selectedDate = DateTime.Now;
        private int selectedTick = 0;
        private MonthTimestamp selectedTimestamp;
        private int precision = -1;

        private bool isUpdating = false;
        
        public MainWindow()
        {
            InitializeComponent();
            var log = new ColorConsoleLog();
            service = new BlocktraderService(log);
            timestampManager = new TimestampFileManager(log);

            TicketPicker.ItemsSource = (Ticket[]) Enum.GetValues(typeof(Ticket));
            DatePicker.SelectedDate = DateTime.Now;
            PrecPicker.Value = 0;

            var timer = new Timer(updateInterval.TotalMilliseconds) {AutoReset = true};
            timer.Elapsed += (s, e) => DownloadAsync().Wait();
            timer.Start();
            log.Info($"Blocktader initializated");
        }

        private async Task DownloadAsync()
        {
            isUpdating = true;
            var timestamp = await service.GetCurrentTimestampAsync().ConfigureAwait(false);
            await timestampManager.WriteAsync(timestamp);
            isUpdating = false;
        }

        private void Filter(object sender, RoutedEventArgs routedEventArgs)
        {
            if (float.TryParse(OrderSizeInput.Text, out var value))
            {
                filterSettings.MinSize = value;
            }

            OrderSizeInput.Text = filterSettings.MinSize.ToString(CultureInfo.CurrentCulture);
            Update();
        }
        
        private bool IsOk(Order order)
        {
            return order.Amount >= filterSettings.MinSize;
        }

        private void TicketPicker_OnSelected(object sender, RoutedEventArgs e)
        {
            currentTicket = (Ticket) TicketPicker.SelectedItem;

        }
        
        private void DatePicker_OnSelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePicker.SelectedDate != null)
                selectedDate = DatePicker.SelectedDate.Value;
        }

        private void DataGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            var grid = (DataGrid)sender;
            foreach (var item in grid.Columns)
            {
                if (item.Header.ToString() == "Count")
                {
                    item.DisplayIndex = grid.Columns.Count - 1;
                    break;
                }
            }
        }
        private void Update()
        {
            if (isUpdating)
                return;
            
            if (selectedDate.Month == DateTime.Now.Month || selectedTimestamp == null)
                selectedTimestamp = timestampManager.ReadTimestampsFromMonth(selectedDate, currentTicket);
            
            var currentDayTimestamp = selectedTimestamp.Info.Where(i => i.Key.Day == selectedDate.Day).Select(v => v.Value).ToArray();
            
            TimePicker.Maximum = currentDayTimestamp.Length;
            TimePicker.TickFrequency = 1;
            TimePicker.TickPlacement = TickPlacement.BottomRight;
            
            BitstampBidsGrid.ItemsSource = currentDayTimestamp[selectedTick][ExchangeTitle.Binance].OrderBook.Bids.Where(IsOk).OrderByDescending(b => b.Price).Flat(precision, true);
            BitstampAsksGrid.ItemsSource = currentDayTimestamp[selectedTick][ExchangeTitle.Binance].OrderBook.Asks.Where(IsOk).OrderBy(p => p.Price).Flat(precision, false);
            BitfinexBidsGrid.ItemsSource = currentDayTimestamp[selectedTick][ExchangeTitle.Bitfinex].OrderBook.Bids.Where(IsOk).OrderByDescending(b => b.Price).Flat(precision, true);
            BitfinexAsksGrid.ItemsSource = currentDayTimestamp[selectedTick][ExchangeTitle.Bitfinex].OrderBook.Asks.Where(IsOk).OrderBy(p => p.Price).Flat(precision, false);
            BinanceBidsGrid.ItemsSource = currentDayTimestamp[selectedTick][ExchangeTitle.Bitstamp].OrderBook.Bids.Where(IsOk).OrderByDescending(b => b.Price).Flat(precision, true);
            BinanceAsksGrid.ItemsSource = currentDayTimestamp[selectedTick][ExchangeTitle.Bitstamp].OrderBook.Asks.Where(IsOk).OrderBy(p => p.Price).Flat(precision, false);

            TimeTextBlock.Text = "Time: " + selectedDate.ToString();
            PriceTextBlock.Text = Math.Floor(currentDayTimestamp[selectedTick][ExchangeTitle.Binance].AveragePrice).ToString();

            InvalidateVisual();
        }

        private void PrecPickerChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            precision = (int) PrecPicker.Value - 1;
            Update();
        }

        private void TimePickerChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((int)TimePicker.Value != 0)
            { 
                selectedTick = (int) TimePicker.Value - 1;
                Update();
            }
            
        }

        private void PickerLeft6_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ShiftCount.Text, out var value))
            {
                TimePicker.Value -= value;
            }
            
        }

        private void PickerRight6_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ShiftCount.Text, out var value))
            {
                TimePicker.Value += value;
            }
        }
    }
}