using System.Collections.Generic;
using System.ComponentModel;

namespace AddTranslationUI.Abstractions
{
    public class BaseObservable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Returns whether property changed event was raised.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected bool SetPropertyAndRaise<T>(T value, ref T field, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(value, field)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
