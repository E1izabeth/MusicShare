using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace MusicShare.Views
{
    [ContentProperty("Setters")]
    public class PropertyTriggerAction : TriggerAction<BindableObject>
    {
        public IList<Setter> Setters { get; }
        public IList<BindableSetter> BindableSetters { get; }

        public PropertyTriggerAction()
        {
            this.Setters = new List<Setter>();
            this.BindableSetters = new List<BindableSetter>();
        }

        protected override void Invoke(BindableObject sender)
        {
            foreach (var setter in this.Setters)
            {
                BindableObject target = sender;
                if (!string.IsNullOrEmpty(setter.TargetName) && target is Element element)
                {
                    try
                    {
                        target = element.FindByName(setter.TargetName) as BindableObject;
                        if (target==null)
                        {
                            System.Diagnostics.Debug.Print(
    $"PropertyTriggerAction failed to apply property {setter.Property.PropertyName} setter with value {setter.Value} " +
    $"for target having name '{setter.TargetName}' because target not found");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print(
    $"PropertyTriggerAction failed to apply property {setter.Property.PropertyName} setter with value {setter.Value} " +
    $"for target having name '{setter.TargetName}' because failed to find such target: {ex.Message}");
                        System.Diagnostics.Debug.Print(ex.ToString());
                        continue;
                    }
                }

                target.SetValue(setter.Property, setter.Value);
            }

            foreach (var setter in this.BindableSetters)
            {
                setter.Apply();
            }
        }
    }

    public class BindableSetter : BindableObject
    {
        #region object Source 

        public object Source
        {
            get { return (object)this.GetValue(SourceProperty); }
            set { this.SetValue(SourceProperty, value); }
        }

        // Using a BindableProperty as the backing store for Source. This enables animation, styling, binding, etc...
        public static readonly BindableProperty SourceProperty =
            BindableProperty.Create("Source", typeof(object), typeof(BindableSetter), default(object));

        #endregion

        #region object Target 

        public object Target
        {
            get { return (object)this.GetValue(TargetProperty); }
            set { this.SetValue(TargetProperty, value); }
        }

        // Using a BindableProperty as the backing store for Target. This enables animation, styling, binding, etc...
        public static readonly BindableProperty TargetProperty =
            BindableProperty.Create("Target", typeof(object), typeof(BindableSetter), default(object));

        #endregion

        public BindableSetter()
        {
        }

        public void Apply()
        {
            this.Target = this.Source;
        }
    }
}
