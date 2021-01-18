using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChatBubbleClientWPF.Utility
{
    class ViewModelResolver
    {
        static Dictionary<Type, List<Type>> viewModelDictionary = new Dictionary<Type, List<Type>>();

        public void MapNewView(Type viewModelType, Type elementType, object[] elementArguments = null)
        {
            if (!typeof(ViewModels.BaseViewModel).IsAssignableFrom(viewModelType))
                return;

            if (typeof(FrameworkElement).IsAssignableFrom(elementType))
            {
                if (!viewModelDictionary.ContainsKey(viewModelType))
                    viewModelDictionary.Add(viewModelType, new List<Type>());

                viewModelDictionary[viewModelType].Add(elementType);
            }
        }

        public void RemoveView(Type viewModelType, Type elementType, int elementOccurence = 0)
        {
            if (!viewModelDictionary.ContainsKey(viewModelType))
                return;

            int i = 0;
            foreach(Type viewType in viewModelDictionary[viewModelType])
            {
                if (viewType == elementType && i == elementOccurence)
                {
                    viewModelDictionary[viewModelType].Remove(viewType);
                }
                else i++;
            }
        }

        public void RemoveView(Type viewModelType, Type viewType)
        {
            if(viewModelDictionary.ContainsKey(viewModelType))
                viewModelDictionary[viewModelType].Remove(viewType);
        }

        public List<Type> GetAssociatedViews(Type viewModelType)
        {
            return viewModelDictionary[viewModelType];
        }
    }
}
