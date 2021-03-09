using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Threading;

namespace ChatBubbleClientWPF.Utility
{
    interface IPageFactory
    {
        event EventHandler WindowRendered;
        Page GetAssociatedPage(ViewModels.BaseViewModel viewModel);
    }

    class PageFactory : IPageFactory
    {
        ViewModelResolver viewModelResolver;
        public event EventHandler WindowRendered;

        internal PageFactory()
        {
            viewModelResolver = new ViewModelResolver();
        }

        public Page GetAssociatedPage(ViewModels.BaseViewModel associatedViewModel)
        {
            foreach (Type elementType in viewModelResolver.GetAssociatedViews(associatedViewModel.GetType()))
            {
                if (typeof(Page).IsAssignableFrom(elementType))
                {
                    object newPage = null;

                    {
                        newPage = new object();
                        newPage = Activator.CreateInstance(elementType);

                        try
                        {
                            ((Page)newPage).DataContext = associatedViewModel;
                        }
                        catch { return null; }

                    }

                    return (Page)newPage;
                }              
            }

            return null;
        }
    }
}
