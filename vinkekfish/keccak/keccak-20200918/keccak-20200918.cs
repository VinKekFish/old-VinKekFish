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

            // Очищаем C и B, чтобы не копировать какие-то значения, которые не стоит копировать, да и хранить тоже
            clearOnly_C_and_B();

            // Копировать всё состояние не обязательно. Но здесь, для надёжности, копируется всё
            for (int i = 0; i < State.LongLength; i++)
                    result.State[i] = State[i];

            return result;
        }
    }
}
