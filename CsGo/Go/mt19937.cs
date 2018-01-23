using System;

namespace Go
{
    public class mt19937 : Random
    {
        private const int N = 624;
        private const int M = 397;
        private const uint MatrixA = 0x9908b0df;
        private const uint UpperMask = 0x80000000;
        private const uint LowerMask = 0x7fffffff;
        private const uint TemperingMaskB = 0x9d2c5680;
        private const uint TemperingMaskC = 0xefc60000;
        private const double FiftyThreeBitsOf1s = 9007199254740991.0;
        private const double Inverse53BitsOf1s = 1.0 / FiftyThreeBitsOf1s;
        private const double OnePlus53BitsOf1s = FiftyThreeBitsOf1s + 1;
        private const double InverseOnePlus53BitsOf1s = 1.0 / OnePlus53BitsOf1s;

        private static readonly uint[] _mag01 = { 0x0, MatrixA };
        private readonly uint[] _mt = new uint[N];
        private short _mti;

        public mt19937(int seed)
        {
            init((uint)seed);
        }

        public mt19937()
        {
            init((uint)system_tick.get_tick());
        }

        public mt19937(uint[] initKey)
        {
            init(initKey);
        }

        public virtual uint NextUInt32()
        {
            return GenerateUInt32();
        }

        public virtual uint NextUInt32(uint maxValue)
        {
            return (uint)(GenerateUInt32() / ((double)uint.MaxValue / maxValue));
        }

        public virtual uint NextUInt32(uint minValue, uint maxValue)
        {
            if (minValue >= maxValue)
            {
                throw new ArgumentOutOfRangeException();
            }
            return (uint)(GenerateUInt32() / ((double)uint.MaxValue / (maxValue - minValue)) + minValue);
        }

        public override int Next()
        {
            return Next(int.MaxValue);
        }

        public override int Next(int maxValue)
        {
            if (maxValue <= 1)
            {
                if (maxValue < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return 0;
            }
            return (int)(NextDouble() * maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            if (maxValue <= minValue)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (maxValue == minValue)
            {
                return minValue;
            }
            return Next(maxValue - minValue) + minValue;
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException();
            }
            int bufLen = buffer.Length;
            for (int idx = 0; idx < bufLen; ++idx)
            {
                buffer[idx] = (byte)Next(256);
            }
        }

        public override double NextDouble()
        {
            return compute53BitRandom(0, InverseOnePlus53BitsOf1s);
        }

        public double NextDouble(bool includeOne)
        {
            return includeOne ? compute53BitRandom(0, Inverse53BitsOf1s) : NextDouble();
        }

        public double NextDoublePositive()
        {
            return compute53BitRandom(0.5, Inverse53BitsOf1s);
        }

        public float NextSingle()
        {
            return (float)NextDouble();
        }

        public float NextSingle(bool includeOne)
        {
            return (float)NextDouble(includeOne);
        }

        public float NextSinglePositive()
        {
            return (float)NextDoublePositive();
        }

        protected uint GenerateUInt32()
        {
            uint y;
            if (_mti >= N)
            {
                short kk = 0;
                for (; kk < N - M; ++kk)
                {
                    y = (_mt[kk] & UpperMask) | (_mt[kk + 1] & LowerMask);
                    _mt[kk] = _mt[kk + M] ^ (y >> 1) ^ _mag01[y & 0x1];
                }
                for (; kk < N - 1; ++kk)
                {
                    y = (_mt[kk] & UpperMask) | (_mt[kk + 1] & LowerMask);
                    _mt[kk] = _mt[kk + (M - N)] ^ (y >> 1) ^ _mag01[y & 0x1];
                }
                y = (_mt[N - 1] & UpperMask) | (_mt[0] & LowerMask);
                _mt[N - 1] = _mt[M - 1] ^ (y >> 1) ^ _mag01[y & 0x1];
                _mti = 0;
            }
            y = _mt[_mti++];
            y ^= temperingShiftU(y);
            y ^= temperingShiftS(y) & TemperingMaskB;
            y ^= temperingShiftT(y) & TemperingMaskC;
            y ^= temperingShiftL(y);
            return y;
        }

        private static uint temperingShiftU(uint y)
        {
            return (y >> 11);
        }

        private static uint temperingShiftS(uint y)
        {
            return (y << 7);
        }

        private static uint temperingShiftT(uint y)
        {
            return (y << 15);
        }

        private static uint temperingShiftL(uint y)
        {
            return (y >> 18);
        }

        private void init(uint seed)
        {
            _mt[0] = seed & 0xffffffffU;
            for (_mti = 1; _mti < N; _mti++)
            {
                _mt[_mti] = (uint)(1812433253U * (_mt[_mti - 1] ^ (_mt[_mti - 1] >> 30)) + _mti);
                _mt[_mti] &= 0xffffffffU;
            }
        }

        private void init(uint[] key)
        {
            int i, j, k;
            init(19650218U);
            int keyLength = key.Length;
            i = 1; j = 0;
            k = (N > keyLength ? N : keyLength);
            for (; k > 0; k--)
            {
                _mt[i] = (uint)((_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 30)) * 1664525U)) + key[j] + j);
                _mt[i] &= 0xffffffffU;
                i++; j++;
                if (i >= N) { _mt[0] = _mt[N - 1]; i = 1; }
                if (j >= keyLength) j = 0;
            }
            for (k = N - 1; k > 0; k--)
            {
                _mt[i] = (uint)((_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 30)) * 1566083941U)) - i);
                _mt[i] &= 0xffffffffU;
                i++;
                if (i < N)
                {
                    continue;
                }
                _mt[0] = _mt[N - 1]; i = 1;
            }
            _mt[0] = 0x80000000U;
        }

        private double compute53BitRandom(double translate, double scale)
        {
            ulong a = (ulong)GenerateUInt32() >> 5;
            ulong b = (ulong)GenerateUInt32() >> 6;
            return ((a * 67108864.0 + b) + translate) * scale;
        }
    }
}
