using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinTweaker.Models
{
    public abstract class NotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value))
                return false;
            field = value;
            Raise(name);
            return true;
        }

        protected void Raise([CallerMemberName] string name = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
