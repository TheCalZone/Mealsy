#if ANDROID
using Microsoft.Maui.Platform;
using Android.Views;
using Android.Widget;
using Android.Views.InputMethods;
using Microsoft.Maui.Handlers;
using RecipeApp.CustomViews;

public static class CustomEditorHandlerExtensions
{
    public static void MapCustomBehavior(this IPropertyMapper<CustomEditor, EditorHandler> mapper)
    {
        mapper.Add(nameof(CustomEditor.EnterPressedCommand), (handler, view) =>
        {
            if (handler.PlatformView is EditText editText)
            {
                editText.EditorAction += (sender, e) =>
                {
                    if (e.ActionId == ImeAction.Done)
                    {
                        // Execute the command when the Enter key is pressed
                        if (view.EnterPressedCommand?.CanExecute(null) ?? false)
                        {
                            view.EnterPressedCommand.Execute(null);
                        }
                        e.Handled = true;
                    }
                    else
                    {
                        e.Handled = false;
                    }
                };
            }
        });
    }
}
#endif

