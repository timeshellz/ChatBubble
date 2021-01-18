using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;

namespace ChatBubbleClientWPF.Utility
{
    interface IWindowFactory
    {
        event EventHandler WindowRendered;
        void OpenAssociatedWindow(ViewModels.BaseViewModel viewModel);
        void CloseWindow(FrameworkElement viewElement);
        void OnWindowRendered(object sender, EventArgs e);
    }

    class WindowFactory : IWindowFactory
    {
        ViewModelResolver viewModelResolver;
        public event EventHandler WindowRendered;

        internal WindowFactory()
        {
            viewModelResolver = new ViewModelResolver();
        }

        public void OpenAssociatedWindow(ViewModels.BaseViewModel viewModel)
        {
            foreach(Type elementType in viewModelResolver.GetAssociatedViews(viewModel.GetType()))
            {
                if(typeof(Window).IsAssignableFrom(elementType))
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {

                    

                    object newWindow = new object();

                    try
                    {
                        newWindow = Activator.CreateInstance(elementType, viewModel);
                    }
                    catch (MissingMethodException)
                    {
                        newWindow = Activator.CreateInstance(elementType);
                    }

                    try
                    {
                        ((Window)newWindow).DataContext = viewModel;
                        ((Window)newWindow).ContentRendered += OnWindowRendered;
                        ((Window)newWindow).Show();                       
                    }
                    catch { return; }

                    }));
                }
            }
        }

        public void CloseWindow(FrameworkElement viewElement)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                if (viewElement is Window window)
                {
                    window.Close();

                    viewElement = null;
                }
            }));
        }

        public void OnWindowRendered(object sender, EventArgs e)
        {
            WindowRendered?.Invoke(sender, e);

            ((Window)sender).Loaded -= OnWindowRendered;
        }
    }
}
