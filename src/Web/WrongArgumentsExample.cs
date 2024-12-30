using System;

namespace Example
{
    class Program
    {
        // Method expecting two arguments
        static void PrintMessage(string message, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Console.WriteLine(message);
            }
        }

        static void Main(string[] args)
        {
            // Correct call
            PrintMessage("Hello, World!", 3);

            // Incorrect call: wrong number of arguments (only one provided)
            PrintMessage("This will cause an error");

            // Incorrect call: wrong number of arguments (three provided)
            PrintMessage("Too many arguments", 2, "Extra argument");
        }
    }
}
