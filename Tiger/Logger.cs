using System;
using System.Collections.Generic;

namespace Tiger
{
    /// <summary>
    /// This class is the main logger used in Tiger to log information to the command line and to a logs file
    /// </summary>
    public static class Logger
    {
        public static bool verbouse = true;
        public static bool toFile = false;
        public static int buffer_length = 10;
        public static List<string> buffer = new List<string>();

        /// <summary>
        /// A class method who's main use is to log a message both to console and to a file
        /// </summary>
        /// <param name="message"> The message to be logged </param>
        public static void log(string message)
        {
            if (verbouse)
            {
                string TimeString = DateTime.Now.ToString("hh:mm:ss tt");
                Console.WriteLine($"[{TimeString}]: {message}");
            }
        }
    }
}

/*
Todo: allow for the logging to be done to a logs files 
*/