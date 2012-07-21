using System;

namespace NeroOS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (NeroPlatform game = new NeroPlatform())
            {
                game.Run();
            }
        }
    }
}

