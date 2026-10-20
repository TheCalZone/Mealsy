using Android.Content;
using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using RecipeApp.Views;

namespace RecipeApp.Platforms.Android;

public class RecipePressHandler : LayoutHandler
{
    protected override LayoutViewGroup CreatePlatformView()
    {
        var view=new PressView(Context, () => (VirtualView as RecipePressLayout)?.Hold()) { CrossPlatformLayout=VirtualView };
        view.SetClipChildren(false);
        return view;
    }

    private sealed class PressView : LayoutViewGroup
    {
        private readonly GestureDetector detector;
        private bool held;
        public PressView(Context context, Action hold) : base(context)
        {
            detector=new GestureDetector(context,new HoldListener(()=>{held=true;PerformHapticFeedback(FeedbackConstants.LongPress);hold();}));
        }
        public override bool DispatchTouchEvent(MotionEvent motion)
        {
            if(motion.ActionMasked==MotionEventActions.Down)held=false;
            detector.OnTouchEvent(motion);
            if(held && motion.ActionMasked==MotionEventActions.Up)
            {
                // Cancel MAUI's pending tap: releasing a long press must not open
                // the recipe underneath the action sheet.
                using var cancel=MotionEvent.Obtain(motion);cancel.Action=MotionEventActions.Cancel;
                base.DispatchTouchEvent(cancel);held=false;return true;
            }
            return base.DispatchTouchEvent(motion);
        }
        protected override void Dispose(bool disposing){if(disposing)detector.Dispose();base.Dispose(disposing);}
        private sealed class HoldListener : GestureDetector.SimpleOnGestureListener
        {
            private readonly Action hold;
            public HoldListener(Action hold)=>this.hold=hold;
            public override bool OnDown(MotionEvent e)=>true;
            public override void OnLongPress(MotionEvent e)=>hold();
        }
    }
}
