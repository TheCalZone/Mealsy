using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RecipeApp.CustomViews
{
    public class CustomEditor : Editor
    {
        public static readonly BindableProperty EnterPressedCommandProperty =
            BindableProperty.Create(nameof(EnterPressedCommand), typeof(ICommand), typeof(CustomEditor));

        public ICommand EnterPressedCommand
        {
            get => (ICommand)GetValue(EnterPressedCommandProperty);
            set => SetValue(EnterPressedCommandProperty, value);
        }

        // Method to invoke the EnterPressedCommand
        public void InvokeEnterPressedCommand()
        {
            if (EnterPressedCommand?.CanExecute(null) == true)
            {
                EnterPressedCommand.Execute(null);
            }
        }

    }

}
