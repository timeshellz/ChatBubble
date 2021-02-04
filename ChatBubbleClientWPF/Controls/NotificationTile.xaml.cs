using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChatBubbleClientWPF.Controls
{
    /// <summary>
    /// Interaction logic for PopUpTile.xaml
    /// </summary>
    public partial class NotificationTile : UserControl
    {
        public static readonly DependencyProperty NotificationFillProperty =
        DependencyProperty.Register("NotificationFill", typeof(Brush), typeof(NotificationTile), new PropertyMetadata(null));

        public Brush NotificationFill
        {
            get { return (Brush)GetValue(NotificationFillProperty); }
            set { SetValue(NotificationFillProperty, value); }
        }

        public NotificationTile()
        {
            InitializeComponent();
        }
    }
}
