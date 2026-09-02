using MiniMicaApp.Configuration;
using MiniMicaApp.Shell;

namespace MiniMicaApp.Views
{
    public partial class MainWindow : MiniMicaWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = AppOptions.DisplayName;
            Backdrop = AppOptions.DefaultBackdrop;
        }
    }
}
