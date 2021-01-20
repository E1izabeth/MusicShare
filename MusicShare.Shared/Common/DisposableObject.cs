using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Interaction.Standard.Common
{

    public abstract class DisposableObject : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public DisposableObject()
        {
            this.IsDisposed = false;
        }

        public void Dispose()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }
            else
            {
                this.IsDisposed = true;
                this.DisposeImpl();
            }
        }

        protected abstract void DisposeImpl();
    }
}
