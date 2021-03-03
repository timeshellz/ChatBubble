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

using ChatBubbleClientWPF.ViewModels.ActiveDialogue;

namespace ChatBubbleClientWPF.Controls
{
    /// <summary>
    /// Interaction logic for DialogueInputPanel.xaml
    /// </summary>
    public partial class DialogueInputPanel : UserControl
    {
        public DialogueInputPanel()
        {
            InitializeComponent();
        }

        private void RoundedRectangleTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(this.DataContext is ActiveDialogueViewModel viewModel && e.Key == Key.Enter)
            {
                if (viewModel.SendMessageCommand.CanExecute(null))
                    viewModel.SendMessageCommand.Execute(null);
            }
        }
    }
}
