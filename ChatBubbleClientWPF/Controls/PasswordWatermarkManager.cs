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
    public class PasswordWatermarkManager : DependencyObject
    {

        public static readonly DependencyProperty PasswordLengthProperty = DependencyProperty.RegisterAttached("PasswordLength", typeof(int),
            typeof(PasswordWatermarkManager), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static int GetPasswordLength(DependencyObject target)
        {
            return (int)target.GetValue(PasswordLengthProperty);
        }

        public static void SetPasswordLength(DependencyObject target, int value)
        {
            target.SetValue(PasswordLengthProperty, value);
        }

        public static readonly DependencyProperty WatermarkEnabledProperty = DependencyProperty.RegisterAttached("WatermarkEnabled", typeof(bool),
            typeof(PasswordWatermarkManager), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnWatermarkEnabledChanged));

        public static bool GetWatermarkEnabled(DependencyObject target)
        {
            return (bool)target.GetValue(WatermarkEnabledProperty);
        }

        public static void SetWatermarkEnabled(DependencyObject target, bool value)
        {
            target.SetValue(WatermarkEnabledProperty, value);
        }

        static void OnWatermarkEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                if ((bool)e.NewValue) passwordBox.PasswordChanged += OnPasswordChanged;
                else passwordBox.PasswordChanged -= OnPasswordChanged;
            }
        }

        static void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                SetPasswordLength(passwordBox, passwordBox.SecurePassword.Length);
            }
        }
    }
}
