using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;

namespace vinkekfish
{
    public class Keccak_20200918: Keccak_base_20200918
    {
        // TODO: сделать тесты; кажется, Clone не должен работать, т.к. C и B не клонируются
        public override Keccak_abstract Clone()
        {
            var result = new Keccak_20200918();

            for (int i = 0; i < S_len; i++)
                for (int j = 0; j < S_len; j++)
                    result.S[i, j] = S[i, j];

            return result;
        }
    }
}
