using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace MusicShare.Views
{
    [ContentProperty("Template")]
    public class TemplateCase : BindableObject
    {
        public Type DataType { get; set; }
        public DataTemplate Template { get; set; }
    }

    [ContentProperty("Templates")]
    public class TemplateSelector : BindableObject
    {
        public IList<TemplateCase> Templates { get; }
        public DataTemplate DefaultTemplate { get; set; }

        public TemplateSelector()
        {
            this.Templates = new List<TemplateCase>();
        }

        public DataTemplate SelectTemplate(object item, BindableObject container)
        {
            return item == null ? this.DefaultTemplate 
                : this.Templates.FirstOrDefault(t => t.DataType == item.GetType())?.Template ?? this.DefaultTemplate;
        }
    }
}
