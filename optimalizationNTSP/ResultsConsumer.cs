using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Communications;
using MassTransit;

namespace optimalizationNTSP
{
    public class ResultsConsumer : IConsumer<IResultsInfo>, INotifyPropertyChanged
    {
        private List<Location> _bestTour;
        public ObservableCollection<Location> BestTour { get; set; }


        private readonly object _bestDistanceLock;

        private int? _taskId;
        public int? TaskId
        {
            get => _taskId;
            set
            {
                _taskId = value;
                OnPropertyChanged(nameof(TaskId));
            }
        }

        private double _bestDistance;
        public double BestDistance
        {
            get => _bestDistance;
            set
            {
                _bestDistance = value;
                OnPropertyChanged(nameof(BestDistance));
            }
        }

        private double _solutionCount;
        public double SolutionCount
        {
            get => _solutionCount;
            set
            {
                _solutionCount = value;
                OnPropertyChanged(nameof(SolutionCount));
            }
        }

        private double _computingProgress;
        public double ComputingProgress
        {
            get => _computingProgress;
            set
            {
                _computingProgress = value;
                OnPropertyChanged(nameof(ComputingProgress));
            }
        }

        private Canvas _placeTourCanvas;
        public Canvas PlaceTourCanvas
        {
            get => _placeTourCanvas;
            set
            {
                _placeTourCanvas = value;
                OnPropertyChanged(nameof(PlaceTourCanvas));
            }
        }

        private double _maxProgress;
        public double MaxProgress
        {
            get => _maxProgress;
            set
            {
                _maxProgress = value;
                OnPropertyChanged(nameof(MaxProgress));
            }
        }

        private double _xWidth;
        public double XWidth
        {
            get => _xWidth;
            set
            {
                _xWidth = value;
                OnPropertyChanged(nameof(XWidth));
            }
        }

        private double _yHeight;
        public double YHeight
        {
            get => _yHeight;
            set
            {
                _yHeight = value;
                OnPropertyChanged(nameof(YHeight));
            }
        }

        public ResultsConsumer()
        {
            SolutionCount = Double.MaxValue;
            BestDistance = Double.MaxValue;
            TaskId = 0;
            BestTour = new ObservableCollection<Location>();
            PlaceTourCanvas = new Canvas();
            YHeight = 600;
            XWidth = 900;
            ComputingProgress = 0;
            MaxProgress = Double.MaxValue;
            _bestDistanceLock = new object();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public Task Consume(ConsumeContext<IResultsInfo> ctx)
        {
            lock (_bestDistanceLock) //access by the problem of reader and writers
            {
                if (ctx.Message.BestDistance <= BestDistance)
                {
                    BestDistance = ctx.Message.BestDistance;
                    SolutionCount = ctx.Message.SolutionCount;
                    TaskId = ctx.Message.TaskId;
                    App.Current.Dispatcher.Invoke((Action) delegate // access by current thread as dispatcher
                    {
                        BestTour.Clear();
                        ctx.Message.BestTour.ToList().ForEach(BestTour.Add);
                        UpdateBestTour(ctx.Message.BestTour, PlaceTourCanvas);
                    });
                }
            }

            return Task.FromResult(0);
        }

        public void UpdateBestTour(List<Location> actualTour, Canvas canvas)
        {
            double minX = Double.MaxValue, maxX = Double.MinValue, 
                    minY = Double.MaxValue, maxY = Double.MinValue;

            foreach (var city in actualTour)
            {
                if (city.X < minX)
                    minX = city.X;
                if (city.Y < minY)
                    minY = city.Y;
                if (city.X > maxX)
                    maxX = city.X;
                if (city.Y > maxY)
                    maxY = city.Y;
            }

            canvas.Children.Clear();
            int counter = 0;
            Ellipse[] arrayOfEllipses = new Ellipse[actualTour.Count];
            foreach (var city in actualTour)
            {
                var dx = city.X - minX;
                var divX = dx / (maxX - minX);
                city.ViewX = divX * XWidth / 1.2;

                var dy = city.Y - minY;
                var divY = dy / (maxY - minY);
                city.ViewY = divY * YHeight / 1.2;

                Ellipse elips = new Ellipse()
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.Purple,

                    RenderTransform = new TranslateTransform(city.ViewX, city.ViewY)
                };

                arrayOfEllipses[counter] = elips;

                if (counter != 0)
                {
                    Point btn1Point = new Point(city.ViewX, city.ViewY);
                    Point btn2Point = new Point(actualTour[counter - 1].ViewX,
                                            actualTour[counter - 1].ViewY);
                    Line l = new Line();
                    l.Stroke = new SolidColorBrush(Colors.DarkCyan);
                    l.StrokeThickness = 2.0;
                    l.X1 = btn1Point.X + elips.Width / 2;
                    l.X2 = btn2Point.X + arrayOfEllipses[counter - 1].Width / 2;
                    l.Y1 = btn1Point.Y + elips.Height / 2;
                    l.Y2 = btn2Point.Y + arrayOfEllipses[counter - 1].Height / 2;
                    canvas.Children.Add(l);

                }
                if (counter == actualTour.Count - 1) // also draw line from first element to last element to create cycle
                {
                    Point btn1Point = new Point(city.ViewX, city.ViewY);
                    Point btn2Point = new Point(actualTour[0].ViewX,
                        actualTour[0].ViewY);
                    Line l = new Line();
                    l.Stroke = new SolidColorBrush(Colors.DarkCyan);
                    l.StrokeThickness = 2.0;
                    l.X1 = btn1Point.X + elips.Width / 2;
                    l.X2 = btn2Point.X + arrayOfEllipses[0].Width / 2;
                    l.Y1 = btn1Point.Y + elips.Height / 2;
                    l.Y2 = btn2Point.Y + arrayOfEllipses[0].Height / 2;
                    canvas.Children.Add(l);
                }

                canvas.Children.Add(elips);
                counter++;
            }
        }
    }
}
