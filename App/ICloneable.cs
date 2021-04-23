using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricSketch
{
    public interface ICloneable<T> : ICloneable
    {
        new T Clone();
    }
}
