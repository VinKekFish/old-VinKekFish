using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * Это отдельная библиотека, где разрешена оптимизация кода
 * Здесь никто ничего не обнуляет, только раундовые вычисления
 * Реализация Trheefish 1024 бита
 * */
namespace cryptoprime
{
    // Медленная реализация Threefish 1024 бита
    public unsafe static class threefish_slowly
    {
        // Функция Mix[d, j](x0, x1) => (y0, y1)
        // y0 = (x0 + x1)
        // y1 = x1 rol RC[(d mod 8), j]
        // y1 = y1 xor y0

        public static void Mix(ref ulong a, ref ulong b, byte r)
        {
            a += b;
            b = b << r | b >> (64-r);   // rol b, r
            b ^= a;
        }

        public static void Mix(ref ulong a, ref ulong b, int r, ulong k0, ulong k1)
        {
            b += k1;
            a += b + k0;
            b = b << r | b >> (64-r);   // rol b, r
            b ^= a;
        }

        // Функция перестановок
        public static readonly byte[] Pi = {0, 9, 2, 13, 6, 11, 4, 15, 10, 7, 12, 3, 14, 5, 8, 1};
        // Rotation constants
        public static readonly byte[,] RC = 
        {
            {24, 13,  8, 47,  8, 17, 22, 37},
            {38, 19, 10, 55, 49, 18, 23, 52},
            {33,  4, 51, 13, 34, 41, 59, 17},
            { 5, 20, 48, 41, 47, 28, 16, 25},

            {41,  9, 37, 31, 12, 47, 44, 30},
            {16, 34, 56, 51,  4, 53, 42, 41},
            {31, 44, 47, 46, 19, 42, 44, 25},
            { 9, 48, 35, 52, 23, 31, 37, 20}
        };

        // d - round mod 8
        // Mixing and word permutation, see page 10
        public static void MixID(ulong[] e, ulong[] result, ref byte d)
        {
            for (int i = 0; i < 16; i += 2)
            {
                Mix(ref e[i+0], ref e[i+1], RC[d & 0x07, i >> 1]);
            }

            // In e - f words (page 10 of skein 1.3)
            for (int i = 0; i < 16; i += 1)
            {
                result[i] = e[Pi[i]];
            }
        }
    }
}
