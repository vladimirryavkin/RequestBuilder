using MahApps.Metro.Controls;
using RequestBuilder.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RequestBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public PrimaryViewModel ViewModel
        {
            get { return (PrimaryViewModel)DataContext; }
        }
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new PrimaryViewModel(Dispatcher);
            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.Width = e.NewSize.Width;
            ViewModel.Height = e.NewSize.Height;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Width = Width;
            ViewModel.Height = Height;
        }
    }
}