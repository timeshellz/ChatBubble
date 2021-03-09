using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using ChatBubbleClientWPF.ViewModels.ActiveDialogue;

namespace ChatBubbleClientWPF.Utility
{
    class MessageLineTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is MessageLineViewModel)
            {
                MessageLineViewModel messageLine = item as MessageLineViewModel;

                if (messageLine is DateDisplayMessageLineViewModel)
                    return element.FindResource("DateDisplayMessageLine") as DataTemplate;
                else
                    return element.FindResource("GenericMessageLine") as DataTemplate;
            }

            return null;
        }
    }
}
