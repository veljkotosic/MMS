using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MMS.WpfApp.Controls;

public partial class ZoomBorder
{
    private UIElement? _child;
    private Point _origin;
    private Point _start;

    private static TranslateTransform GetTranslateTransform(UIElement element) =>
        (TranslateTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is TranslateTransform);

    private static ScaleTransform GetScaleTransform(UIElement element) =>
        (ScaleTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is ScaleTransform);

    public override UIElement Child
    {
        get => base.Child;
        set
        {
            if (value != Child)
            {
                Initialize(value);
            }
            
            base.Child = value;
        }
    }

    public void Initialize(UIElement element)
    {
        _child = element;
        if (_child == null)
        {
            return;
        }
        
        var group = new TransformGroup();
        group.Children.Add(new ScaleTransform());
        group.Children.Add(new TranslateTransform());
        _child.RenderTransform = group;
        _child.RenderTransformOrigin = new Point(0.0, 0.0);

        MouseWheel += child_MouseWheel;
        MouseLeftButtonDown += child_MouseLeftButtonDown;
        MouseLeftButtonUp += child_MouseLeftButtonUp;
        MouseMove += child_MouseMove;
        PreviewMouseRightButtonDown += (s, e) => Reset();
    }

    public void Reset()
    {
        if (_child == null)
        {
            return;
        }
        
        var st = GetScaleTransform(_child);
        st.ScaleX = 1.0;
        st.ScaleY = 1.0;

        var tt = GetTranslateTransform(_child);
        tt.X = 0.0;
        tt.Y = 0.0;
    }

    private void child_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_child == null)
        {
            return;
        }
        
        var st = GetScaleTransform(_child);
        var tt = GetTranslateTransform(_child);

        var zoom = e.Delta > 0 ? 0.2 : -0.2;
        
        if (!(e.Delta > 0) && (st.ScaleX < 0.4 || st.ScaleY < 0.4))
        {
            return;
        }

        var relativePoint = e.GetPosition(_child);
        var absoluteX = relativePoint.X * st.ScaleX + tt.X;
        var absoluteY = relativePoint.Y * st.ScaleY + tt.Y;

        st.ScaleX += zoom;
        st.ScaleY += zoom;

        tt.X = absoluteX - relativePoint.X * st.ScaleX;
        tt.Y = absoluteY - relativePoint.Y * st.ScaleY;
    }

    private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_child == null)
        {
            return;
        }
        
        var tt = GetTranslateTransform(_child);
        
        _start = e.GetPosition(this);
        _origin = new Point(tt.X, tt.Y);
        
        Cursor = Cursors.Hand;
        
        _child.CaptureMouse();
    }

    private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_child == null)
        {
            return;
        }
        
        _child.ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
    }

    private void child_MouseMove(object sender, MouseEventArgs e)
    {
        if (_child is not { IsMouseCaptured: true })
        {
            return;
        }
        
        var tt = GetTranslateTransform(_child);
        
        var v = _start - e.GetPosition(this);
        
        tt.X = _origin.X - v.X;
        tt.Y = _origin.Y - v.Y;
    }
}