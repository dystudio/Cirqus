﻿using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    public class BasicEventDispatcher : IEventDispatcher
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly List<IViewManager> _viewManagers;

        public BasicEventDispatcher(IAggregateRootRepository aggregateRootRepository, params IViewManager[] viewManagers)
            : this(aggregateRootRepository, (IEnumerable<IViewManager>)viewManagers)
        {
        }

        public BasicEventDispatcher(IAggregateRootRepository aggregateRootRepository, IEnumerable<IViewManager> viewManagers)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _viewManagers = viewManagers.ToList();
        }

        public void Initialize(IEventStore eventStore, bool purgeExitingViews = false)
        {
            foreach (var manager in _viewManagers)
            {
                manager.Initialize(new DefaultViewContext(_aggregateRootRepository), eventStore, purgeExistingViews: purgeExitingViews);
            }
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            var eventList = events.ToList();

            foreach (var view in _viewManagers)
            {
                view.Dispatch(new DefaultViewContext(_aggregateRootRepository), eventStore, eventList);
            }
        }

        class DefaultViewContext : IViewContext
        {
            readonly IAggregateRootRepository _aggregateRootRepository;

            public DefaultViewContext(IAggregateRootRepository aggregateRootRepository)
            {
                _aggregateRootRepository = aggregateRootRepository;
            }

            public TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
            {
                return _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, maxGlobalSequenceNumber: globalSequenceNumber);
            }
        }
    }
}