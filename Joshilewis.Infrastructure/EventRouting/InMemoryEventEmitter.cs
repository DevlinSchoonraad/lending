﻿using System.Collections.Generic;
using Joshilewis.Cqrs;

namespace Joshilewis.Infrastructure.EventRouting
{
    public class InMemoryEventEmitter : IEventEmitter
    {
        private readonly Queue<Event> eventsToEmit;

        public InMemoryEventEmitter()
        {
            eventsToEmit = new Queue<Event>();
        }

        public void EmitEvent(Event @event)
        {
            eventsToEmit.Enqueue(@event);
        }

        public void EmitEvents(IEnumerable<Event> events)
        {
            foreach (var @event in events)
            {
                EmitEvent(@event);
            }
        }

        public Queue<Event> EmittedEvents => eventsToEmit;
    }
}
