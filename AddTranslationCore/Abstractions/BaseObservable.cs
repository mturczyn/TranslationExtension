using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AddTranslationCore.Abstractions
{
    public abstract class BaseObservable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Returns whether property changed event was raised.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">Value to be set.</param>
        /// <param name="backingField">Underlying field, which backs the property.</param>
        /// <param name="propertyName">Name of property.</param>
        /// <returns>If <see cref="PropertyChanged"/> event was raised for property.</returns>
        protected bool Set<T>(T value, ref T backingField, [CallerMemberName]string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(value, backingField)) return false;
            backingField = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
