using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Communications;

namespace optimalizationNTSP
{
    /// <summary>
    /// Logika interakcji dla klasy BestTourWindow.xaml
    /// </summary>
    public partial class BestTourWindow : Window
    {
        Path path;
        private readonly ResultsConsumer _resultsConsumer;

        public BestTourWindow(ResultsConsumer resultsCons)
        {
            InitializeComponent();
            _resultsConsumer = resultsCons;
            this.DataContext = _resultsConsumer;

            Loaded += delegate
            {
                Height = _resultsConsumer.YHeight;
                Width = _resultsConsumer.XWidth;
                _resultsConsumer.PropertyChanged += ViewModelOnPropertyChanged;
                SizeChanged += BestTourWindow_SizeChanged;
            };
            Unloaded += delegate
            {
                _resultsConsumer.PropertyChanged -= ViewModelOnPropertyChanged;
                SizeChanged -= BestTourWindow_SizeChanged;
            };
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_resultsConsumer.YHeight):
                    Height = _resultsConsumer.YHeight;
                    break;
                case nameof(_resultsConsumer.XWidth):
                    Width = _resultsConsumer.XWidth;
                    break;
            }
        }

        private void BestTourWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _resultsConsumer.XWidth = e.NewSize.Width;
            _resultsConsumer.YHeight = e.NewSize.Height;

            if (_resultsConsumer.BestTour.Count != 0)
            {
                _resultsConsumer.UpdateBestTour(_resultsConsumer.BestTour.ToList(), _resultsConsumer.PlaceTourCanvas);
            }
        }
    }
}
