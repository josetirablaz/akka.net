﻿using System;
using Akka.Actor;
using Akka.Event;

namespace TimeServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("TimeServer"))
            {
                Console.Title = "Server";
                var server = system.ActorOf<TimeServerActor>("time");
                Console.ReadLine();
                Console.WriteLine("Shutting down...");
                Console.WriteLine("Terminated");
            }
        }

        public class TimeServerActor : TypedActor, IHandle<string>
        {
            private readonly ILoggingAdapter _log = Context.GetLogger();

            public void Handle(string message)
            {
                if (message.ToLowerInvariant() == "gettime")
                {
                    var time =DateTime.Now.ToLongTimeString();
                    Sender.Tell(time, Self);
                }
                else
                {

                    _log.Error("Invalid command: {0}", message);
                    var invalid = "Unrecognized command";
                    Sender.Tell(invalid, Self);
                }
            }
        }
    }
}
