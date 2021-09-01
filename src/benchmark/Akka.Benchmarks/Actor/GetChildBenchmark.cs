﻿// //-----------------------------------------------------------------------
// // <copyright file="GetChildBenchmark.cs" company="Akka.NET Project">
// //     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
// //     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// // </copyright>
// //-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Benchmarks.Configurations;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using FluentAssertions;

namespace Akka.Benchmarks.Actor
{
    /// <summary>
    /// Used to measure how quickly an <see cref="IActorContext.Child"/> call can be executed in the wild.
    /// </summary>
    [Config(typeof(MicroBenchmarkConfig))]
    public class GetChildBenchmark
    {
        #region classes
        public sealed class Child : UntypedActor
        {
            protected override void OnReceive(object message)
            {
                
            }

            protected override void PreStart()
            {
                if (Self.Path.Name.Length > 1)
                {
                    // recursively create children using the previous name segments
                    var nextName = new string(Self.Path.Name.Skip(1).ToArray());
                    Context.ActorOf(Props.Create(() => new Child()), nextName);
                }
            }
        }
        
        public sealed class ActorWithChild : UntypedActor
        {
            public sealed class Get
            {
                public Get(string name)
                {
                    Name = name;
                }

                public string Name { get; }
            }

            public sealed class Create
            {
                public Create(string name)
                {
                    Name = name;
                }

                public string Name { get; }
            }

            protected override void OnReceive(object message)
            {
                switch (message)
                {
                    case Get g:
                    {
                        var child = Context.Child(g.Name);
                        Sender.Tell(child);
                        break;
                    }
                    case Create c:
                    {
                        var child = Context.ActorOf(Props.Create(() => new Child()), c.Name);
                        Sender.Tell(child);
                        break;
                    }
                    default:
                        Unhandled(message);
                        break;
                }
            }
        }
        
        #endregion
        
        private TimeSpan _timeout;
        private ActorSystem _system;
        private IActorRef _parentActor;

        private ActorWithChild.Get _getMessage = new ActorWithChild.Get("food");
        private ActorWithChild.Create _createMessage = new ActorWithChild.Create("food");

        private ActorCell _cell;
        private RepointableActorRef _repointableActorRef;
        private LocalActorRef _localActorRef;

        private List<string> _rpChildQueryPath = new List<string>() { "food", "foo", "fo" };
        private List<string> _lclChildQueryPath = new List<string>() { "foo", "fo", "f" };
        
        [GlobalSetup]
        public async Task Setup()
        {
            _timeout = TimeSpan.FromMinutes(1);
            _system = ActorSystem.Create("system");
            _parentActor = _system.ActorOf(Props.Create(() => new ActorWithChild()), "parent");
            _localActorRef = (LocalActorRef)await _parentActor.Ask<IActorRef>(_createMessage, _timeout);
            
            _cell = _parentActor.As<ActorRefWithCell>().Underlying.As<ActorCell>();
            _repointableActorRef = (RepointableActorRef)_parentActor;
        }

        [Benchmark]
        public void ResolveChild()
        {
            _cell.TryGetSingleChild(_getMessage.Name, out var child);
        }
        
        [Benchmark]
        public void Resolve3DeepChildRepointableActorRef()
        {
            _repointableActorRef.GetChild(_rpChildQueryPath);
        }
        
        [Benchmark]
        public void Resolve3DeepChildLocalActorRef()
        {
            _localActorRef.GetChild(_lclChildQueryPath);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _system.Terminate().Wait();
        }
    }
}