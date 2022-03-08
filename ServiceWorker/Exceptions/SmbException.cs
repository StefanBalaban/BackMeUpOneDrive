using SMBLibrary;
using System;

namespace BackMeUp.ServiceWorker.Exceptions
{
    public class SmbException : Exception
    {
        public SmbException(string message, NTStatus status) : base(message)
        {
            Status = status;
        }

        public NTStatus Status { get; set; }
    }
}