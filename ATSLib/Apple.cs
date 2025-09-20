using System;
using log4net;

namespace ATSLib
{
    public class Apple
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Apple));

        public void DoSomething()
        {
            log.Info("Apple.DoSomething called.");
            // Your logic here
        }
    }
}