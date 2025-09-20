using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Reflection;
using ATSLib;

class Program
{
    private static readonly ILog log = LogManager.GetLogger(typeof(Program));

    static void Main(string[] args)
    {
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

        log.Info("Application started.");
        log.Debug("Debug message.");
        log.Error("Error message.");

        Console.WriteLine("Check the console for log4net output.");

        Apple apple = new Apple();
        apple.DoSomething();
    }
}