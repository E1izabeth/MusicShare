using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace MusicShare.Views
{
    public class ContentPresenter : ContentView
    {
        #region DataTemplate ItemTemplate

        public object ItemTemplate
        {
            get { return (object)this.GetValue(ItemTemplateProperty); }
            set { this.SetValue(ItemTemplateProperty, value); }
        }

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create("ItemTemplate", typeof(object), typeof(ContentPresenter), null, propertyChanged: OnItemTemplateChanged);

        #endregion

        private static void OnItemTemplateChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            var cp = (ContentPresenter)bindable;
            cp.UpdateContent();
        }

        #region object DataItem 

        public object DataItem
        {
            get { return (object)this.GetValue(DataItemProperty); }
            set { this.SetValue(DataItemProperty, value); }
        }

        // Using a BindableProperty as the backing store for DataItem. This enables animation, styling, binding, etc...
        public static readonly BindableProperty DataItemProperty =
            BindableProperty.Create("DataItem", typeof(object), typeof(ContentPresenter), default(object), propertyChanged: OnDataItemChanged);

        #endregion

        #region bool UseDataItem 

        public bool UseDataItem
        {
            get { return (bool)this.GetValue(UseDataItemProperty); }
            set { this.SetValue(UseDataItemProperty, value); }
        }

        // Using a BindableProperty as the backing store for UseDataItem. This enables animation, styling, binding, etc...
        public static readonly BindableProperty UseDataItemProperty =
            BindableProperty.Create("UseDataItem", typeof(bool), typeof(ContentPresenter), default(bool), propertyChanged: OnDataItemChanged);

        #endregion

        private static void OnDataItemChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            var cp = (ContentPresenter)bindable;
            cp.UpdateContent();
        }

        public ContentPresenter()
        {
            this.BindingContextChanged += (sender, ea) => this.UpdateContent();
        }

        void UpdateContent()
        {
            var value = this.ItemTemplate;
            var content = default(View);
            try
            {
                var data = this.UseDataItem ? this.DataItem : this.BindingContext;

                if (value is DataTemplateSelector dataTemplateSelector)
                {
                    var template = dataTemplateSelector.SelectTemplate(data, this);
                    content = (View)template?.CreateContent() ?? new Label() { Text = "<NULL>" };
                    // this.Content.BindingContext = this.DataItem;
                }
                else if (value is TemplateSelector templateSelector)
                {
                    var template = templateSelector.SelectTemplate(data, this);
                    content = (View)template?.CreateContent() ?? new Label() { Text = "<NULL>" };
                    // this.Content.BindingContext = this.DataItem;
                }
                else if (value is DataTemplate dataTemplate)
                {
                    content = (View)dataTemplate.CreateContent();
                    // this.Content.BindingContext = this.DataItem;
                }
                else
                {
                    content = null;
                }

                if (content != null)
                    content.BindingContext = data;

                this.Content = content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"Failed to update content based on {value} and value of {this.BindingContext} while view is {content?.ToString() ?? "<NULL>"}: {ex}");
            }
        }
    }

}
