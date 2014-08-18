using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.TestHelpers
{
    public class TestUnitOfWork : IDisposable
    {
        readonly RealUnitOfWork _unitOfWork = new RealUnitOfWork();
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IEventStore _eventStore;
        readonly IEventDispatcher _eventDispatcher;

        internal TestUnitOfWork(IAggregateRootRepository aggregateRootRepository, IEventStore eventStore, IEventDispatcher eventDispatcher)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _eventStore = eventStore;
            _eventDispatcher = eventDispatcher;
        }

        public TAggregateRoot Get<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregateRootFromCache = _unitOfWork.GetAggregateRootFromCache<TAggregateRoot>(aggregateRootId, long.MaxValue);
            if (aggregateRootFromCache != null)
            {
                return aggregateRootFromCache;
            }

            var aggregateRootInfo = _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, _unitOfWork);
            var aggregateRoot = aggregateRootInfo.AggregateRoot;

            _unitOfWork.AddToCache(aggregateRoot, long.MaxValue);

            aggregateRoot.UnitOfWork = _unitOfWork;
            aggregateRoot.SequenceNumberGenerator = new CachingSequenceNumberGenerator(aggregateRootInfo.LastSeqNo + 1);

            _unitOfWork.AddToCache(aggregateRoot, aggregateRootInfo.LastGlobalSeqNo);

            if (aggregateRootInfo.IsNew)
            {
                aggregateRoot.InvokeCreated();
            }

            return aggregateRoot;
        }

        /// <summary>
        /// Gets the events collected in the current unit of work
        /// </summary>
        public EventCollection EmittedEvents
        {
            get { return new EventCollection(_unitOfWork.EmittedEvents); }
        }

        /// <summary>
        /// Commits the current unit of work (i.e. emitted events from <see cref="EmittedEvents"/> are saved
        /// to the history and will be used to hydrate aggregate roots from now on.
        /// </summary>
        public void Commit()
        {
            var domainEvents = EmittedEvents.ToList();

            if (!domainEvents.Any()) return;

            _eventStore.Save(Guid.NewGuid(), domainEvents);

            _eventDispatcher.Dispatch(_eventStore, domainEvents);
        }

        public void Dispose()
        {
            if (!EmittedEvents.Any()) return;

            Console.WriteLine("Unit of work was disposed with {0} events", EmittedEvents.Count());
        }
    }
}