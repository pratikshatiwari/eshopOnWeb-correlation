using System;
using System.Linq; 
using os;
using sys;

namespace WebProject
{
    public class TestUnusedVariables
    {
        public void TestMethod()
        {
            // Used variables
            int usedVariable = 5;
            Console.WriteLine($"Used variable: {usedVariable}");

            // Unused variables
            int unusedVariable = 10; // Should be flagged
            string unusedString = "Hello, CodeQL!"; // Should be flagged

            // A variable declared but used in a conditional block
            int conditionallyUsedVariable = 20;
            if (conditionallyUsedVariable > 0) // This usage will prevent it from being flagged
            {
                Console.WriteLine("Conditionally used variable.");
            }
        }
    }
}
