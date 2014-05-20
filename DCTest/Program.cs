﻿using System;
using DeveloperCommands;

namespace DCTest
{
    [Category]
    class Program
    {
        [Convar("testbool")]
        public static bool TestBool = false;

        static void Main(string[] args)
        {
            Console.Title = "Devcom";
            Devcom.Load();
            var context = Context.DefaultAdmin;
            while(true)
            {
                Console.Write(context.Prompt);
                Devcom.SendCommand(context, Console.ReadLine());
            }
        }
    }
}
