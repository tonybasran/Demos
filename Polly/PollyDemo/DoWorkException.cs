using System;

namespace PollyDemo
{
    public class DoWorkException : Exception
    {
        public DoWorkException(string message) : base(message)
        {
        }

        public DoWorkException()
        {
        }
    }
}