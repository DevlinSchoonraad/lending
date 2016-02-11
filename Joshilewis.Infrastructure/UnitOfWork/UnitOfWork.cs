﻿using System;
using System.Net;
using EventStore.ClientAPI;
using Joshilewis.Cqrs;
using Joshilewis.Infrastructure.EventRouting;
using Joshilewis.Infrastructure.EventStore;
using NHibernate;
using NHibernate.Context;

namespace Joshilewis.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        //private readonly static ILog Log = LogManager.GetLogger(typeof(UnitOfWork).FullName);

        private readonly ISessionFactory sessionFactory;
        private readonly IEventStoreConnection connection;
        private readonly Guid transactionId;
        private readonly IEventEmitter eventEmitter;
        private EventStoreEventRepository eventRepository;
        private readonly EventDispatcher eventDispatcher;

        public UnitOfWork(ISessionFactory sessionFactory, string eventStoreIpAddress, IEventEmitter eventEmitter,
            EventDispatcher eventDispatcher)
            : this(sessionFactory, (IEventStoreConnection) EventStoreConnection.Create(new IPEndPoint(IPAddress.Parse(eventStoreIpAddress), 1113)), eventEmitter, eventDispatcher)
        {
        }

        protected UnitOfWork(ISessionFactory sessionFactory, IEventStoreConnection eventStoreConnection, IEventEmitter eventEmitter,
            EventDispatcher eventDispatcher)
        {
            //Log.DebugFormat("Creating unit of work {0}", GetHashCode());
            this.sessionFactory = sessionFactory;
            this.eventEmitter = eventEmitter;
            this.eventDispatcher = eventDispatcher;
            connection = eventStoreConnection;
            transactionId = Guid.NewGuid();
        }

        public void Begin()
        {
            //Log.DebugFormat("Beginning unit of work {0}", GetHashCode());
            currentSession = sessionFactory.OpenSession();
            CurrentSessionContext.Bind(CurrentSession);
            CurrentSession.BeginTransaction();
            connection.ConnectAsync().Wait();
            eventRepository = new EventStoreEventRepository(eventEmitter, connection);
        }

        public void Commit()
        {
            eventRepository.Commit(transactionId);


            //Log.DebugFormat("Committing unit of work {0}", GetHashCode());
            currentSession.Transaction.Commit();

            currentSession.BeginTransaction();
            eventDispatcher.DispatchEvents();
            currentSession.Transaction.Commit();

            CurrentSessionContext.Unbind(sessionFactory);
        }

        public void RollBack()
        {
            //Log.DebugFormat("Rolling back unit of work {0}", GetHashCode());
            currentSession.Transaction.Rollback();
            CurrentSessionContext.Unbind(sessionFactory);
        }

        public void Dispose()
        {
            //Log.DebugFormat("Disposing {0}", GetHashCode());
            eventRepository.Dispose();
            connection.Close();
            connection.Dispose();
            currentSession.Transaction.Dispose();
            CurrentSession.Dispose();
        }

        private ISession currentSession;
        public ISession CurrentSession => currentSession;

        public IEventRepository EventRepository => eventRepository;
    }
}