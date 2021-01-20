using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MusicShare.ViewModels;
using Xamarin.Forms;

namespace MusicShare.Views
{
    public class CollectionViewAutoScroll
    {
        #region object SelectedItemToFollow 

        public static object GetSelectedItemToFollow(BindableObject obj)
        {
            return obj.GetValue(SelectedItemToFollowProperty);
        }

        public static void SetSelectedItemToFollow(BindableObject obj, object value)
        {
            obj.SetValue(SelectedItemToFollowProperty, value);
        }

        // Using a BindableProperty as the backing store for SelectedItemToFollow.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty SelectedItemToFollowProperty =
            BindableProperty.CreateAttached("SelectedItemToFollow", typeof(object), typeof(CollectionViewAutoScroll), default(object), propertyChanged: OnSelectedItemToFollowChanged);

        #endregion

        #region attached IEnumerable<object> VisibleItems 

        public static IEnumerable<object> GetVisibleItems(BindableObject obj)
        {
            return (IEnumerable<object>)obj.GetValue(VisibleItemsProperty);
        }

        public static void SetVisibleItems(BindableObject obj, IEnumerable<object> value)
        {
            obj.SetValue(VisibleItemsProperty, value);
        }

        // Using a BindableProperty as the backing store for VisibleItems.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty VisibleItemsProperty =
            BindableProperty.CreateAttached("VisibleItems", typeof(IEnumerable<object>), typeof(CollectionViewAutoScroll), default(IEnumerable<object>), propertyChanged: OnVisibleItemsChanged);

        #endregion

        private class Info : BindableObject
        {
            #region System.Collections.IEnumerable ItemsCollection 

            public System.Collections.IEnumerable ItemsCollection
            {
                get { return (System.Collections.IEnumerable)this.GetValue(ItemsCollectionProperty); }
                set { this.SetValue(ItemsCollectionProperty, value); }
            }

            // Using a BindableProperty as the backing store for ItemsCollection.  This enables animation, styling, binding, etc...
            public static readonly BindableProperty ItemsCollectionProperty =
                BindableProperty.Create("ItemsCollection", typeof(System.Collections.IEnumerable), typeof(Info), default(System.Collections.IEnumerable),
                    propertyChanged: CollectionBindingWatcher.For<Info>(c => c._itemsCollectionWatcher));

            #endregion

            #region double DesiredPageWidth 

            public double DesiredPageWidth
            {
                get { return (double)this.GetValue(DesiredPageWidthProperty); }
                set { this.SetValue(DesiredPageWidthProperty, value); }
            }

            // Using a BindableProperty as the backing store for DesiredPageWidth.  This enables animation, styling, binding, etc...
            public static readonly BindableProperty DesiredPageWidthProperty =
                BindableProperty.Create("DesiredPageWidth", typeof(double), typeof(Info), default(double), propertyChanged: OnDesiredPageWidthChanged);

            #endregion

            private static void OnDesiredPageWidthChanged(BindableObject obj, object oldValue, object newValue)
            {
                if (obj is Info o)
                    o.UpdateVisibleItems(o._firstVisibleItemIndex);
            }

            public ItemsView View { get; }

            private int _firstVisibleItemIndex = 0;
            private object[] _visibleItems = new object[0];
            private readonly CollectionBindingContext _itemsCollectionWatcher = new CollectionBindingContext();

            public Info(ItemsView view)
            {
                this.View = view;
                this.View.SizeChanged += this.OnViewSizeChanged;
                this.View.Scrolled += this.OnViewScrolled;
                _itemsCollectionWatcher.OnCollectionChanged += this.OnCollectionChanged;

                this.SetBinding(ItemsCollectionProperty, new Binding("ItemsSource", source: view));
                this.SetBinding(DesiredPageWidthProperty, new Binding("DesiredPageWidth", source: AppViewModel.Instance));
            }

            private void OnViewSizeChanged(object sender, EventArgs e)
            {
                this.UpdateVisibleItems(_firstVisibleItemIndex);
            }

            private static double _lastActualWidth;

            private void UpdateVisibleItems(int from)
            {
                if (this.View.Width > 0 && this.View.Width < double.PositiveInfinity)
                    _lastActualWidth = this.View.Width;

                _firstVisibleItemIndex = from;
                var visibleWidth = _lastActualWidth;
                var pageWidth = AppViewModel.Instance.DesiredPageWidth;
                var visiblePagesCount = visibleWidth / pageWidth - 0.5;

                _visibleItems = this.ItemsCollection.OfType<object>().Skip(from).TakeWhile((o, n) => n < visiblePagesCount).ToArray();
                CollectionViewAutoScroll.SetVisibleItems(this.View, _visibleItems);

                var firstItem = _visibleItems.FirstOrDefault();
                if (firstItem  != null && this.View is SelectableItemsView selectableView && selectableView.SelectedItem != firstItem)
                {
                    CollectionViewAutoScroll.SetSelectedItemToFollow(selectableView, firstItem);
                }
            }

            private void OnCollectionChanged(INotifyCollectionChanged collection, NotifyCollectionChangedEventArgs ea)
            {
                this.UpdateVisibleItems(_firstVisibleItemIndex);
            }

            private void OnViewScrolled(object sender, ItemsViewScrolledEventArgs e)
            {
                this.UpdateVisibleItems(e.FirstVisibleItemIndex);
            }

            private object _previouslyScrolledTo = null;

            internal void OnSelectedItemChagned(object newValue)
            {
                if (_previouslyScrolledTo != newValue && newValue != null)
                {
                    if (_visibleItems.Length > 0 && !_visibleItems.Contains(newValue))
                    {
                        this.Dispatcher.BeginInvokeOnMainThread(() => {
                            this.View.ScrollTo(newValue);
                        });
                    }
                }

                _previouslyScrolledTo = newValue;
            }
        }

        private static void OnVisibleItemsChanged(BindableObject obj, object oldValue, object newValue) { }

        private static Dictionary<ItemsView, Info> _views = new Dictionary<ItemsView, Info>();

        private static void OnSelectedItemToFollowChanged(BindableObject obj, object oldValue, object newValue)
        {
            if (obj is ItemsView view)
            {
                if (!_views.TryGetValue(view, out var info))
                    _views.Add(view, info = new Info(view));

                info.OnSelectedItemChagned(newValue);
            }
        }
    }
}
