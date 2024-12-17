using System;

namespace TestCases
{
    public class EmptyCatchBlockExample
    {
        public void TriggerEmptyCatch()
        {
            try
            {
                int result = 10 / 0; // This will throw a DivideByZeroException
            }
            catch
            {
                // Empty catch block - this should be flagged by the custom CodeQL query
            }
        }

        public void ProperCatch()
        {
            try
            {
                int result = 10 / 0; // This will throw a DivideByZeroException
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message); // Proper error handling
            }
        }
    }
}
