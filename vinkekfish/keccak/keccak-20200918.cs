using System;

namespace vinkekfish
{
    public class Keccak_20200918: Keccak_abstract
    {
        public override Keccak_abstract Clone()
        {
            var result = new Keccak_20200918();

            for (int i = 0; i < S_len; i++)
                for (int j = 0; j < S_len; j++)
                    result.S[i, j] = S[i, j];

            return result;
        }

        public override void Clear(bool AllClear = true)
        {
            if (AllClear)
            {/*
                BytesBuilder.ToNull(zBytes);

                for (int i = 0; i < C.Length; i++)
                    C[i] = 0;

                for (int i = 0; i < B.GetLength(0); i++)
                    for (int j = 0; j < B.GetLength(1); j++)
                        B[i, j] = 0;
                        */
                for (int i = 0; i < S.GetLength(0); i++)
                    for (int j = 0; j < S.GetLength(1); j++)
                        S[i, j] = 0;

                // this.d = 0;
            }
        }
    }
}
