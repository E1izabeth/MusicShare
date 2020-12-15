using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

namespace MusicShare.Views
{
    class HidableCommandState : BindableObject
    {
        #region bool CanBeExecuted 

        public bool CanBeExecuted
        {
            get { return (bool)this.GetValue(CanBeExecutedProperty); }
            set { this.SetValue(CanBeExecutedProperty, value); }
        }

        // Using a BindableProperty as the backing store for CanBeExecuted. This enables animation, styling, binding, etc...
        public static readonly BindableProperty CanBeExecutedProperty =
            BindableProperty.Create("CanBeExecuted", typeof(bool), typeof(HidableCommandState), default(bool));

        #endregion
    }

    class HidableCommand : Command
    {
        // public bool CanBeExecuted { get; private set; } = true;
        public HidableCommandState State { get; } = new HidableCommandState();

        readonly Func<bool> _canExecute1 = null;
        readonly Func<object, bool> _canExecute2 = null;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public HidableCommand(Action execute) : base(execute) { _canExecute1 = () => true; this.Setup(); }
        public HidableCommand(Action<object> execute) : base(execute) { _canExecute2 = o => true; this.Setup(); }
        public HidableCommand(Action execute, Func<bool> canExecute) : base(execute, canExecute) { _canExecute1 = canExecute; this.Setup(); }
        public HidableCommand(Action<object> execute, Func<object, bool> canExecute) : base(execute, canExecute) { _canExecute2 = canExecute; this.Setup(); }

        private void Setup()
        {
            this.CanExecuteChanged += (sender, ea) =>
            {
                this.State.CanBeExecuted = _canExecute1?.Invoke() ?? _canExecute2?.Invoke(null) ?? false;
            };
            this.ChangeCanExecute();
        }
    }
}
