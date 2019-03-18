using System;
using System.Collections.Generic;

namespace PathRenderingLab.PathCompiler
{
    public struct OuterAngles : IComparable<OuterAngles>
    {
        public double Angle, DAngle, DDAngle;

        public OuterAngles(double angle, double dAngle, double ddAngle)
        {
            Angle = angle;
            DAngle = dAngle;
            DDAngle = ddAngle;
        }

        public int CompareTo(OuterAngles other)
        {
            var comparer = Comparer<double>.Default;
            var cmp = comparer.Compare(Angle, other.Angle);
            if (cmp == 0) cmp = comparer.Compare(DAngle, other.DAngle);
            if (cmp == 0) cmp = comparer.Compare(DDAngle, other.DDAngle);
            return cmp;
        }

        public override string ToString() => $"{Angle.ToDegrees()}:{DAngle}:{DDAngle}";
    }
}