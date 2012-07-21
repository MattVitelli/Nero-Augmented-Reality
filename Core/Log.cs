using System;
using System.IO;

namespace NeroOS.Core
{
    public class Log : IDisposable
    {
        StreamWriter LogWriter;

        static Log instance = null;

        public static Log GetInstance()
        {
            if (instance == null)
                instance = new Log();
            return instance;
        }

        public Log()
        {
            LogWriter = new StreamWriter("console.log", false, System.Text.Encoding.ASCII);
        }

        public void Dispose()
        {
            LogWriter.Close();
        }

        ~Log()
        {
            /*
            if(LogWriter != null && LogWriter.BaseStream != null)
                LogWriter.Close();
            if(LogFile != null)
                LogFile.Close();
        
             */
        }

        public void WriteLine(string _text)
        {
            Console.WriteLine(_text);
            LogWriter.WriteLine(_text);
        }

        public void WriteLine(string _text, object _arg0)
        {
            Console.WriteLine(_text, _arg0);
            LogWriter.WriteLine(_text, _arg0);
        }

        public void WriteLine(string _text, object _arg0, object _arg1)
        {
            Console.WriteLine(_text, _arg0, _arg1);
            LogWriter.WriteLine(_text, _arg0, _arg1);
        }

        public void WriteLine(string _text, object _arg0, object _arg1, object _arg2)
        {
            Console.WriteLine(_text, _arg0, _arg1, _arg2);
            LogWriter.WriteLine(_text, _arg0, _arg1, _arg2);
        }

        public void WriteLine(string _text, object _arg0, object _arg1, object _arg2, object _arg3)
        {
            Console.WriteLine(_text, _arg0, _arg1, _arg2, _arg3);
            LogWriter.WriteLine(_text, _arg0, _arg1, _arg2, _arg3);
        }

    }
}
