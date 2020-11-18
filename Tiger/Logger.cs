using System;
using System.IO;
using System.Collections.Generic;

namespace Tiger
{   
    /// <summary>
    /// An enum of the logging levels offered by the logger
    /// </summary>
    public enum LoggerLevels
    {
        LowVerbouse = 0x1,
        MediumVerbouse = 0x2,
        HighVerbouse = 0x3,
        Disabled = 0xFF
    }

    /// <summary>
    /// This class is the main logger used in Tiger to log information to the command line and to a logs file
    /// </summary>
    public static class Logger
    {
        public static LoggerLevels logging_level = LoggerLevels.MediumVerbouse;
        public static bool toFile = false;
        public static int buffer_length = 10;
        public static List<string> buffer = new List<string>();

        /// <summary>
        /// A class method who's main use is to log a message both to console and to a file
        /// </summary>
        /// <param name="message"> The message to be logged </param>
        /// <param name="level"> The level that the message is to be logged at </param>
        public static void log(string message, LoggerLevels level)
        {
            if (logging_level != LoggerLevels.Disabled && logging_level <= level)
            {
                string TimeString = DateTime.Now.ToString("dd/MMM/yyyy hh:mm:ss tt");
                string logging_message = $"[{TimeString}]: { (level == LoggerLevels.HighVerbouse ? "" : (new String('\t', (int)level - 1) + "|-")) }{message}";
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