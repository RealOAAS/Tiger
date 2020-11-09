using System;
using System.IO;
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
                string TimeString = DateTime.Now.ToString("dd/MMM/yyyy hh:mm:ss tt");
                string logging_message = $"[{TimeString}]: {message}";
                Console.WriteLine(logging_message);

                if(toFile)
                {
                    buffer.Add(logging_message);
                    if (buffer.Count >= buffer_length) //Flush if the buffer is equal to its maximum length
                        flush();
                }
            }
        }

        /// <summary>
        /// A method used to flush the buffer of its contents and write them to a file
        /// </summary>
        public static void flush()
        {
            File.AppendAllLines("logs.log", buffer);
            buffer.Clear(); //remove all of the items previously in the buffer
        }
    }
}