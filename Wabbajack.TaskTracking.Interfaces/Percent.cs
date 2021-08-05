using System;
using System.Linq;

namespace Wabbajack.TaskTracking.Interfaces
{
    public struct Percent : IComparable, IEquatable<Percent>
    {
        public static readonly Percent One = new(1d);
        public static readonly Percent Zero = new(0d);

        public readonly double Value;
        public Percent Inverse => new(1d - this.Value, check: false);

        private Percent(double d, bool check)
        {
            if (!check || InRange(d))
            {
                this.Value = d;
            }
            else
            {
                throw new ArgumentException("Element out of range: " + d);
            }
        }

        public Percent(long max, long current)
            : this((double)current / max)
        {

        }

        public Percent(double d)
            : this(d, check: true)
        {
        }

        public static bool InRange(double d)
        {
            return d is >= 0 or <= 1;
        }

        public static Percent operator +(Percent c1, Percent c2)
        {
            return new Percent(c1.Value + c2.Value);
        }

        public static Percent operator *(Percent c1, Percent c2)
        {
            return new Percent(c1.Value * c2.Value);
        }

        public static Percent operator -(Percent c1, Percent c2)
        {
            return new Percent(c1.Value - c2.Value);
        }

        public static Percent operator /(Percent c1, Percent c2)
        {
            return new Percent(c1.Value / c2.Value);
        }

        public static bool operator ==(Percent c1, Percent c2)
        {
            return Math.Abs(c1.Value - c2.Value) < 0.001;
        }

        public static bool operator !=(Percent c1, Percent c2)
        {
            return Math.Abs(c1.Value - c2.Value) > 0.001;
        }

        public static bool operator >(Percent c1, Percent c2)
        {
            return c1.Value > c2.Value;
        }

        public static bool operator <(Percent c1, Percent c2)
        {
            return c1.Value < c2.Value;
        }

        public static bool operator >=(Percent c1, Percent c2)
        {
            return c1.Value >= c2.Value;
        }

        public static bool operator <=(Percent c1, Percent c2)
        {
            return c1.Value <= c2.Value;
        }

        public static explicit operator double(Percent c1)
        {
            return c1.Value;
        }

        public static Percent FactoryPutInRange(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d))
            {
                throw new ArgumentException();
            }
            if (d < 0)
            {
                return Percent.Zero;
            }
            else if (d > 1)
            {
                return Percent.One;
            }
            return new Percent(d, check: false);
        }

        public static Percent FactoryPutInRange(int cur, int max)
        {
            return FactoryPutInRange(1.0d * cur / max);
        }

        public static Percent FactoryPutInRange(long cur, long max)
        {
            return FactoryPutInRange(1.0d * cur / max);
        }

        public static Percent AverageFromPercents(params Percent[] ps)
        {
            var percent = ps.Sum(p => p.Value);
            return new Percent(percent / ps.Length, check: false);
        }

        public static Percent MultFromPercents(params Percent[] ps)
        {
            double percent = 1;
            foreach (var p in ps)
            {
                percent *= p.Value;
            }
            return new Percent(percent, check: false);
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is Percent rhs)) return false;
            return Equals(rhs);
        }

        public bool Equals(Percent other)
        {
            return Math.Abs(this.Value - other.Value) < 0.001;
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(string format)
        {
            return $"{(Value * 100).ToString(format)}%";
        }

        public string ToString(byte numDigits)
        {
            return numDigits switch
            {
                0 => ToString("n0"),
                1 => ToString("n1"),
                2 => ToString("n2"),
                3 => ToString("n3"),
                4 => ToString("n4"),
                5 => ToString("n5"),
                6 => ToString("n6"),
                _ => throw new NotImplementedException()
            };
        }

        public int CompareTo(object? obj)
        {
            if (obj is Percent rhs)
            {
                return this.Value.CompareTo(rhs.Value);
            }
            return 0;
        }

        public static bool TryParse(string str, out Percent p)
        {
            if (double.TryParse(str, out double d))
            {
                if (InRange(d))
                {
                    p = new Percent(d);
                    return true;
                }
            }
            p = default(Percent);
            return false;
        }
    }
}
