﻿using System;

namespace Enum
{
    enum University
    {
       Lipia,
       UIM,
       Al_Azhar
    }

    enum Months
    {
        January,
            February,
            Maret,
            April,
            May,
            June,
            July,
            Agustus,
            September,
            Oktober,
          //  November=2, diberikan nilai default
          November,
            Desember

    }
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Penggunaan Enum");
            University myVar = University.Lipia;
            Console.WriteLine(myVar);

            int myMonts = (int) Months.Desember;
            Console.WriteLine(myMonts);

            switch (myVar)
            {
                case University.Lipia:
                    Console.WriteLine("Lipia");
                    break;
                case University.Al_Azhar:
                    Console.WriteLine("Al-Azhar");
                    break;

                case University.UIM:
                    Console.WriteLine("UIM");
                    break;
            }
        }
    }
}
