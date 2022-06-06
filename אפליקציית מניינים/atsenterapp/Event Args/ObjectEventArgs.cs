using System;

namespace atsenterapp
{
    public class ObjectEventArgs<T> : EventArgs
    {
        public T Value;
        public ObjectEventArgs(T value)
        {
            Value = value;
        }
        public ObjectEventArgs() { }
    }
}
