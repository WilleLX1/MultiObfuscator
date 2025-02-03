using System;

namespace TestApplication
{
    class Program
    {
        public static void Main()
        {
            // Prompt for input
            Console.WriteLine("Enter the first number:");
            string num1 = Console.ReadLine();
            double number1;

            bool isValidNumber1 = double.TryParse(num1, out number1);
            if (!isValidNumber1)
            {
                Console.WriteLine("Please enter a valid number.");
                return;
            }

            Console.WriteLine("Enter the second number:");
            string num2 = Console.ReadLine();
            double number2;

            bool isValidNumber2 = double.TryParse(num2, out number2);
            if (!isValidNumber2)
            {
                Console.WriteLine("Please enter a valid number.");
                return;
            }

            // Determine which operation to perform
            Console.WriteLine("Choose an operation:");
            string operation = Console.ReadLine().ToLower();
            char op;

            switch (operation)
            {
                case "add":
                    op = '+';
                    break;
                case "subtract":
                    op = '-';
                    break;
                case "multiply":
                    op = '*';
                    break;
                case "divide":
                    if (number2 == 0)
                    {
                        Console.WriteLine("Cannot divide by zero!");
                        return;
                    }
                    op = '/';
                    break;
                default:
                    Console.WriteLine("Invalid operation. Please choose from add, subtract, multiply, or divide.");
                    return;
            }

            // Perform the calculation
            double result;
            switch (op)
            {
                case '+':
                    result = Add(number1, number2);
                    break;
                case '-':
                    result = Subtract(number1, number2);
                    break;
                case '*':
                    result = Multiply(number1, number2);
                    break;
                case '/':
                    result = Divide(number1, number2);
                    break;
                default:
                    throw new Exception("This should not happen.");
            }

            // Display the result
            if (result == (int)result)
            {
                Console.WriteLine($"Result: {(int)result}");
            }
            else
            {
                Console.WriteLine($"Result: {result}");
            }
        }

        private static double Add(double num1, double num2)
        {
            return num1 + num2;
        }

        private static double Subtract(double num1, double num2)
        {
            return num1 - num2;
        }

        private static double Multiply(double num1, double num2)
        {
            return num1 * num2;
        }

        private static double Divide(double num1, double num2)
        {
            return num1 / num2;
        }
    }
}