using System;

namespace vinkekfish_crypto
{
    public abstract class Keccak_abstract
    {
        public const int S_len = 5;

        // Это внутреннее состояние keccak
        protected ulong[,] S = new ulong[S_len, S_len];
        public abstract Keccak_abstract Clone();
        public abstract void Clear(bool AllClear = true);
    }
}
