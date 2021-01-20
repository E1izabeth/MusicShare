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
    class CollectionBindingContext : BindableObject
    {
        public INotifyCollectionChanged Collection { get; private set; }

        public event Action<INotifyCollectionChanged, NotifyCollectionChangedEventArgs> OnCollectionChanged = delegate { };

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
            if (oldValue is INotifyCollectionChanged oldCollection)
            {
                if (this.Collection != oldCollection)
                    throw new InvalidOperationException("Unexpected collection");

                this.Collection.CollectionChanged -= this.OnUnderlyingCollectionChanged;
                this.Collection = null;
                this.RaizeViewLocsCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            if (newValue is INotifyCollectionChanged newCollection)
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
        public static BindingPropertyChangedDelegate For<TContainer>(Func<TContainer, CollectionBindingContext> contextGetter)
        {
            return new CollectionBindingWatcher<TContainer>(contextGetter).HandleCollectionPropertyChanged;
        }
    }

    class CollectionBindingWatcher<TContainer>
    {
        readonly Func<TContainer, CollectionBindingContext> _contextGetter;

        public CollectionBindingWatcher(Func<TContainer, CollectionBindingContext> contextGetter)
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
