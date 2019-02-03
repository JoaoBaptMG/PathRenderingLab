using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PathRenderingLab
{
    // Shameless rip from https://stackoverflow.com/a/41169463/3225867. Thanks Drew Noakes!
    internal sealed class ReferenceEqualityComparer : IEqualityComparer, IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Default = new ReferenceEqualityComparer();

        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}