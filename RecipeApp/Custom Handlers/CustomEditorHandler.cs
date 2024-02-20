
#if ANDROID
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.AppCompat.Widget;
#endif
using Microsoft.Maui.Handlers;
using RecipeApp.CustomViews;
#if ANDROID
using Keycode = Android.Views.Keycode;
#endif

namespace RecipeApp.Custom_Handlers
{
    class CustomEditorHandler : EditorHandler
    {
#if ANDROID
        protected override void ConnectHandler(AppCompatEditText platformView)
        {
            base.ConnectHandler(platformView);
            platformView.EditorAction += OnEditorAction;
        }

        protected override void DisconnectHandler(AppCompatEditText platformView)
        {
            base.DisconnectHandler(platformView);
            platformView.EditorAction -= OnEditorAction;
        }

        private void OnEditorAction(object sender, Android.Widget.TextView.EditorActionEventArgs e)
        {
            var actionId = e.ActionId;
            var evt = e.Event;

            if (actionId == ImeAction.Done ||
               (actionId == ImeAction.ImeNull && evt?.KeyCode == Keycode.Enter && evt?.Action == KeyEventActions.Up))
            {
                if (VirtualView is CustomEditor customEditor)
                {
                    customEditor.InvokeEnterPressedCommand();
                    e.Handled = true;
                    return;
                }
            }

            if (actionId != ImeAction.ImeNull)
                VirtualView?.Completed();

            e.Handled = true;
        }
#endif
    }


}

