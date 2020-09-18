using System;
using System.Collections.Generic;
using System.Text;

namespace vinkekfish.CSharp_help
{
    // https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-7-3
    class Help1
    {
        // fixed-поля
        unsafe struct S
        {
            public fixed int myFixedField[10];
        }

        static S s = new S();
        unsafe public void M()
        {
            int p = s.myFixedField[5];

            // stackalloc инициализаторы
            int* pArr = stackalloc int[3] {1, 2, 3};

            // Кортежи
            (double, int) t1 = (4.5, 3);

            // Инициализатор переменной out прямо в вызове функции
            func1(out int k);
        }

        public static void func1(out int k)
        {
             k = 0;
        }
    }
}
