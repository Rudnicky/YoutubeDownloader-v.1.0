using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace YoutubeDownloader.Behaviors
{
    public enum ScrollType { NOT_SET, NONE, BOTH, HORIZONTAL, VERTICAL };
    public class TouchScrollBehavior : DependencyObject
    {
        private static bool _isTargetUnloaded = false;
        private const uint SCROLL_THRESHOLD = 5;

        public static ScrollType GetTouchScroll(DependencyObject obj)
        {
            return (ScrollType)obj.GetValue(TouchScrollProperty);
        }

        public static void SetTouchScroll(DependencyObject obj, ScrollType value)
        {
            obj.SetValue(TouchScrollProperty, value);
        }

        public ScrollType TouchScroll
        {
            get { return (ScrollType)GetValue(TouchScrollProperty); }
            set { SetValue(TouchScrollProperty, value); }
        }
        public static readonly DependencyProperty TouchScrollProperty =
            DependencyProperty.RegisterAttached("TouchScroll", typeof(ScrollType), typeof(TouchScrollBehavior),
                new UIPropertyMetadata(ScrollType.NONE, TouchScrollChanged));

        static Dictionary<object, MouseCapture> _captures = new Dictionary<object, MouseCapture>();
        static List<ScrollViewer> FindScrollViewer(DependencyObject parent) { return FindScrollViewer(parent, new List<ScrollViewer>()); }

        static List<ScrollViewer> FindScrollViewer(DependencyObject parent, List<ScrollViewer> alreadyFound)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollViewer && !alreadyFound.Contains(child))
                {
                    alreadyFound.Add(child as ScrollViewer);
                }
            }

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (!(child is ScrollViewer))
                {
                    List<ScrollViewer> returnedList = FindScrollViewer(child, alreadyFound);
                    if (returnedList != null)
                    {
                        foreach (ScrollViewer sV in returnedList)
                        {
                            if (!alreadyFound.Contains(sV))
                            {
                                alreadyFound.Add(sV);
                            }
                        }
                    }
                }
            }
            return alreadyFound;
        }

        private static void TouchScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var target = d as Control;
                if (target != null)
                {
                    ScrollType value = (ScrollType)e.NewValue;
                    if (value != ScrollType.NONE && value != ScrollType.NOT_SET)
                    {
                        target.Loaded += TargetLoaded;
                    }
                    else
                    {
                        TargetUnloaded(target, new RoutedEventArgs());
                    }
                }
            }
            catch (Exception)
            {
                // TODO: log it
            }
        }

        private static void TargetUnloaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Target Unloaded");

            var target = sender as Control;
            if (target != null)
            {
                _captures.Remove(sender);

                target.Loaded -= TargetLoaded;
                target.Unloaded -= TargetUnloaded;
                target.PreviewMouseLeftButtonDown -= TargetPreviewMouseLeftButtonDown;
                target.PreviewMouseMove -= TargetPreviewMouseMove;
                target.PreviewMouseLeftButtonUp -= TargetPreviewMouseLeftButtonUp;
                _isTargetUnloaded = true;
            }
        }

        private static void TargetPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var target = sender as ScrollViewer;
            if (target != null)
            {
                _captures[sender] = new MouseCapture
                {
                    HorizontalOffset = target.HorizontalOffset,
                    VerticalOffset = target.VerticalOffset,
                    Point = e.GetPosition(target),
                };
                _isTargetUnloaded = false;
            }
        }

        private static void TargetLoaded(object sender, RoutedEventArgs e)
        {
            var target = sender as Control;
            if (target == null)
            {
                return;
            }

            if (target is ScrollViewer)
            {
                return;
            }

            List<ScrollViewer> viewers = FindScrollViewer(target);
            if (viewers == null || viewers.Count < 1)
            {
                return;
            }

            foreach (ScrollViewer sV in viewers)
            {
                ScrollType parentScrollSetting = GetTouchScroll(target);
                SetTouchScroll(sV, parentScrollSetting);

                sV.CanContentScroll = false;
                sV.Unloaded += TargetUnloaded;
                sV.PreviewMouseLeftButtonDown += TargetPreviewMouseLeftButtonDown;
                sV.PreviewMouseMove += TargetPreviewMouseMove;
                sV.PreviewMouseLeftButtonUp += TargetPreviewMouseLeftButtonUp;
                _isTargetUnloaded = false;
            }
        }

        private static void TargetPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as Control)?.ReleaseMouseCapture();

            _isTargetUnloaded = false;
        }

        private static void TargetPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_captures.ContainsKey(sender))
            {
                return;
            }

            if (_isTargetUnloaded)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _captures.Remove(sender);
                return;
            }

            var target = sender as ScrollViewer;
            if (target == null)
            {
                return;
            }

            var capture = _captures[sender];
            var point = e.GetPosition(target);
            var dy = point.Y - capture.Point.Y;
            var dx = point.X - capture.Point.X;

            if (Math.Abs(dy) > SCROLL_THRESHOLD || Math.Abs(dx) > SCROLL_THRESHOLD)
            {
                target.CaptureMouse();
            }

            if (GetTouchScroll(target) == ScrollType.VERTICAL || GetTouchScroll(target) == ScrollType.BOTH)
                target.ScrollToVerticalOffset(capture.VerticalOffset - dy);

            if (GetTouchScroll(target) == ScrollType.HORIZONTAL || GetTouchScroll(target) == ScrollType.BOTH)
                target.ScrollToHorizontalOffset(capture.HorizontalOffset - dx);
        }

        internal class MouseCapture
        {
            public double HorizontalOffset { get; set; }
            public double VerticalOffset { get; set; }
            public Point Point { get; set; }
        }
    }
}
