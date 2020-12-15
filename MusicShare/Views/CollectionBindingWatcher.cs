using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Markup;
using static Xamarin.Forms.BindableProperty;

namespace MusicShare.Views
{
    class CollectionBindingContext<TValue> : BindableObject
    {
        public ObservableCollection<TValue> Collection { get; private set; }

        public event Action<ObservableCollection<TValue>, NotifyCollectionChangedEventArgs> OnCollectionChanged = delegate { };

        public CollectionBindingContext()
        {
        }

        private void RaizeViewLocsCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this.InvokeAction(() => this.OnCollectionChanged(this.Collection, e));
        }

        private void OnUnderlyingCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.RaizeViewLocsCollectionChanged(e);
        }

        internal void HandleCollectionPropertyChanged(object oldValue, object newValue)
        {
            if (oldValue is ObservableCollection<TValue> oldCollection)
            {
                if (this.Collection != oldCollection)
                    throw new InvalidOperationException("Unexpected collection");

                this.Collection.CollectionChanged -= this.OnUnderlyingCollectionChanged;
                this.Collection = null;
                this.RaizeViewLocsCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            if (newValue is ObservableCollection<TValue> newCollection)
            {
                if (this.Collection != null)
                    throw new InvalidOperationException("Unexpected collection");

                this.Collection = newCollection;
                this.Collection.CollectionChanged += this.OnUnderlyingCollectionChanged;
                this.RaizeViewLocsCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, this.Collection));
            }
        }
    }

    class CollectionBindingWatcher
    {
        public static BindingPropertyChangedDelegate For<TContainer, TValue>(Func<TContainer, CollectionBindingContext<TValue>> contextGetter)
        {
            return new CollectionBindingWatcher<TContainer, TValue>(contextGetter).HandleCollectionPropertyChanged;
        }
    }

    class CollectionBindingWatcher<TContainer, TValue>
    {
        readonly Func<TContainer, CollectionBindingContext<TValue>> _contextGetter;

        public CollectionBindingWatcher(Func<TContainer, CollectionBindingContext<TValue>> contextGetter)
        {
            _contextGetter = contextGetter;
        }

        public void HandleCollectionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TContainer me)
            {
                var ctx = _contextGetter(me);
                if (ctx != null)
                {
                    bindable.InvokeAction(() => ctx.HandleCollectionPropertyChanged(oldValue, newValue));
                }
            }
        }
    }
}
