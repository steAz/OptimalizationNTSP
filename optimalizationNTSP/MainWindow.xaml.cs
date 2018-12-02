using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using System.Windows.Threading;
using Communications;
using MassTransit;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace optimalizationNTSP
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IBusControl _bus;

        private readonly ResultsConsumer _resultsConsumer;
        private readonly DispatcherTimer _dTforUpdatingProgressBar;
        private Stopwatch _sw;
        private double _additionalSeconds;
        private BestTourWindow _bestTourWindow;
        private List<Task> _tasks;

        public MainWindow()
        {
            InitializeComponent();
         
            for (int i = 1; i <= 30; i++)
            {
                PhaseFirstTimeCb.Items.Add(i);
                PhaseSecTimeCb.Items.Add(i);
            }

            ExitAppBtn.Visibility = Visibility.Hidden;
            StopNNTSPbtn.Visibility = Visibility.Hidden;
            _dTforUpdatingProgressBar = new DispatcherTimer();
            _dTforUpdatingProgressBar.Tick += DispatcherTimerForUpdatingProgressBar_Tick;
            _dTforUpdatingProgressBar.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _resultsConsumer = new ResultsConsumer();
            this.DataContext = _resultsConsumer;
        }


        private void LoadFileBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.tsp)|*.tsp",
                InitialDirectory = Environment.CurrentDirectory
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var fileName = openFileDialog.FileName;
                FileTb.Text = Path.GetFullPath(fileName);
            }
        }


        private void StartComputingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Path.IsPathRooted(FileTb.Text))
            {
                MessageBox.Show("File path is not valid");
                return;
            }

            if (PhaseFirstTimeCb.SelectedItem == null || PhaseSecTimeCb.SelectedItem == null ||
                PhaseFirstUnitCb.SelectedItem == null || PhaseSecUnitCb.SelectedItem == null)
            {
                MessageBox.Show("Phases' durations are not filled correctly");
                return;
            }

            if (HowManyTasksCb.SelectedItem == null)
            {
                MessageBox.Show("There is not specified how many tasks perform computing");
                return;
            }

            if ((!((bool) TlpRb.IsChecked) && !((bool) ThreadPoolRb.IsChecked)))
            {
                MessageBox.Show("There is not specified which mechanism should be used to compute ");
                return;
            }

            RunComputingAsync();
        }


        private void RunComputingAsync()
        {
            _tasks = null;
            _tasks = new List<Task>();
            var numOfTasks = Int32.Parse(HowManyTasksCb.Text);
            var tspDataFilePath = FileTb.Text;
            var phaseFirstTime = Int32.Parse(PhaseFirstTimeCb.Text);
            var phaseSecTime = Int32.Parse(PhaseSecTimeCb.Text);
            var phaseFirstTimeInSeconds = SetPhaseTime(phaseFirstTime,
                PhaseFirstUnitCb.Text[0]);
            var phaseSecTimeInSeconds = SetPhaseTime(phaseSecTime,
                PhaseSecUnitCb.Text[0]);

            _resultsConsumer.ComputingProgress = 0;
            _resultsConsumer.MaxProgress = phaseFirstTimeInSeconds + phaseSecTimeInSeconds;
            _resultsConsumer.SolutionCount = Double.MaxValue;
            _resultsConsumer.BestDistance = Double.MaxValue;
            _resultsConsumer.TaskId = 0;
            _resultsConsumer.BestTour.Clear();
            _sw = new Stopwatch();
            _additionalSeconds = 0;
            _sw.Start();
            _dTforUpdatingProgressBar.Start();

            _bestTourWindow?.Close();
            _bestTourWindow = new BestTourWindow(_resultsConsumer);
            _bestTourWindow.Show();
            StopNNTSPbtn.Visibility = Visibility.Visible;
            ExitAppBtn.Visibility = Visibility.Hidden;

            _bus?.Stop();
            _bus = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                var host = sbc.Host(
                    new Uri(
                        "rabbitmq://luozycyv:HMETMyUNp2qMHslxNUihuSMWLb6NElPy@hound.rmq.cloudamqp.com/luozycyv?temporary=true"),
                    h => { });

                sbc.ReceiveEndpoint(host, ep => { ep.Instance(_resultsConsumer); });
            });
            _bus.Start();

            if (TlpRb.IsChecked != null && (bool) TlpRb.IsChecked)
                RunViaTPL(numOfTasks, phaseFirstTimeInSeconds, phaseSecTimeInSeconds, tspDataFilePath);
            else if (ThreadPoolRb.IsChecked != null && (bool) ThreadPoolRb.IsChecked)
            {
                RunViaThreadPool(numOfTasks, phaseFirstTimeInSeconds,
                                                                phaseSecTimeInSeconds, tspDataFilePath);
            }

            
        }


        private async Task<int> RunViaThreadPool(int numOfTasks, int phaseFirstTimeInSeconds, int phaseSecTimeInSeconds,
            string tspDataFilePath)
        {
            var handles = new ManualResetEvent[numOfTasks];
            for (var i = 0; i < numOfTasks; i++)
            {
                handles[i] = new ManualResetEvent(false);
                var currentHandle = handles[i];
                Action wrappedAction = () =>
                {
                    try
                    {
                        RunNTSP(_bus, Thread.CurrentThread.ManagedThreadId,
                            phaseFirstTimeInSeconds, phaseSecTimeInSeconds,
                            tspDataFilePath, numOfTasks);
                    }
                    finally
                    {
                        currentHandle.Set();
                    }
                };
                ThreadPool.QueueUserWorkItem(x => wrappedAction());
            }
            WaitHandle.WaitAll(handles);

            FinalWorkForThreadPool();
            return 0;
        }

        private void FinalWorkForThreadPool()
        {
            while (_sw.Elapsed.TotalSeconds < _resultsConsumer.MaxProgress + 1) // one sec for eventual mistake
            { }
            _sw.Reset();
            _dTforUpdatingProgressBar.Stop();
        }

        private async void RunViaTPL(int numOfTasks, int phaseFirstTimeInSeconds, int phaseSecTimeInSeconds,
            string tspDataFilePath)
        {
            var po = new ParallelOptions();
            po.MaxDegreeOfParallelism = Environment.ProcessorCount;

            ProgressInfoTBlock.Text = "Computing in progress ...";
            Parallel.For(0, numOfTasks, po, i =>
            {
                var task = Task.Factory.StartNew(() => RunNTSP(_bus, Task.CurrentId,
                    phaseFirstTimeInSeconds, phaseSecTimeInSeconds,
                    tspDataFilePath, numOfTasks));
                _tasks.Add(task);
            });
            await Task.Factory.ContinueWhenAll(_tasks.ToArray(), FinalWorkForTPL);

            ProgressInfoTBlock.Text = "Computing ended, but some more results may come ...";
            ExitAppBtn.Visibility = Visibility.Visible;
        }


        private void FinalWorkForTPL(Task[] tasks)
        {
            if (tasks.All(t => t.Status == TaskStatus.RanToCompletion))
            {
                while (_sw.Elapsed.TotalSeconds < _resultsConsumer.MaxProgress + 1) // one sec for eventual mistake
                { }
                _sw.Reset();
                _dTforUpdatingProgressBar.Stop();
            }
        }

        private static int RunNTSP(IBusControl bus, int? taskId,
            int phaseFirstTime, int phaseSecTime,
            string tspDataFilePath, int numOfTasks)
        {
            var release = new AlgorithmsInfo()
            {
                PhaseFirstTimeInSeconds = phaseFirstTime,
                PhaseSecTimeInSeconds = phaseSecTime,
                TaskId = taskId,
                TspDataFilePath = tspDataFilePath,
                NumberOfTasks =  numOfTasks
            };
            bus.Publish(release);
            
            return 0;
        }

        void Window_Closing(object sender, CancelEventArgs e)
        {
            _bus?.Stop();
        }

        public int SetPhaseTime(int phaseTime, char phaseUnit)
        {
            switch (phaseUnit)
            {
                case 's':
                    return phaseTime;
                case 'm':
                    return phaseTime * 60;
            }

            throw new ArgumentException("Wrong phase unit in AlgorithmsConsumer");
        }

        private void DispatcherTimerForUpdatingProgressBar_Tick(object sender, EventArgs e)
        {
            var nowSeconds = _sw.Elapsed.TotalSeconds;
            _resultsConsumer.ComputingProgress +=  nowSeconds -_additionalSeconds;
            _additionalSeconds = nowSeconds;
            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private void ExitAppBtn_Click(object sender, RoutedEventArgs e)
        {
            _bus?.Stop();
            _bestTourWindow.Close();
            this.Close();
        }

        private void StopNNTSPbtn_Click(object sender, RoutedEventArgs e)
        {
            _bus?.Stop();
            _bus = null;
            _resultsConsumer.ComputingProgress = 0;
            ExitAppBtn.Visibility = Visibility.Visible;
            _sw.Reset();
            _dTforUpdatingProgressBar.Stop();
            ProgressInfoTBlock.Text = "Computing was stopped";
        }
    }
}
