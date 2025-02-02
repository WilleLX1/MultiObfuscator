using System;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Simple Calculator App");
            Console.Write("Enter your name: ");
            string userName = Console.ReadLine();

            PrintWelcomeMessage(userName);

            Console.Write("Enter first number: ");
            int num1 = Convert.ToInt32(Console.ReadLine());

            Console.Write("Enter second number: ");
            int num2 = Convert.ToInt32(Console.ReadLine());

            int sum = AddNumbers(num1, num2);
            Console.WriteLine($"\nHello {userName}, the sum is: {sum}");

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void PrintWelcomeMessage(string name)
        {
            string greeting = $"Welcome, {name}!";
            Console.WriteLine($"\n{greeting}");
        }

        static int AddNumbers(int a, int b)
        {
            return a + b;
        }
    }
}