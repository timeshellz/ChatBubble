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

namespace ChatBubbleClientWPF.Tabs
{
    /// <summary>
    /// Interaction logic for MainTab.xaml
    /// </summary>
    public partial class MainTab : Page
    {
        public MainTab()
        {
            InitializeComponent();
        }

        private void EditDescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            ShiftEditingStyle();
        }

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            ShiftEditingStyle();
            if (DataContext is ViewModels.MainTabViewModel viewModel)
            {
                viewModel.IsEditing = false;

                viewModel.UserStatus = viewModel.OldStatus;
                viewModel.UserDescription = viewModel.OldDescription;
            }

        }

        void ShiftEditingStyle()
        {
            if (DataContext is ViewModels.MainTabViewModel viewModel)
            {
                if (viewModel.IsEditing)
                {
                    EditDescriptionButton.Style = Application.Current.Resources["SecondaryRoundedButtonStyle"] as Style;
                    CancelEditButton.Style = Application.Current.Resources["SecondaryRoundedButtonStyle"] as Style;
                    CancelEditButton.Visibility = Visibility.Hidden;

                    StatusTextBox.Style = Application.Current.Resources["SecondaryRoundedTextBoxStyle"] as Style;
                    SummaryTextBox.Style = Application.Current.Resources["SecondaryRoundedTextBoxStyle"] as Style;

                    StatusTextBox.IsReadOnly = true;
                    SummaryTextBox.IsReadOnly = true;
                }
                else
                {
                    EditDescriptionButton.Style = Application.Current.FindResource(typeof(Button)) as Style;
                    CancelEditButton.Style = Application.Current.FindResource(typeof(Button)) as Style;
                    CancelEditButton.Visibility = Visibility.Visible;

                    StatusTextBox.Style = Application.Current.FindResource(typeof(Controls.RoundedRectangleTextBox)) as Style;
                    SummaryTextBox.Style = Application.Current.FindResource(typeof(Controls.RoundedRectangleTextBox)) as Style;

                    StatusTextBox.IsReadOnly = false;
                    SummaryTextBox.IsReadOnly = false;
                }
            }
        }
    }
}
