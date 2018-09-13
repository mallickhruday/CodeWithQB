using CodeWithQB.Core.Common;
using CodeWithQB.Core.DomainEvents;
using CodeWithQB.Core.Interfaces;
using CodeWithQB.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static CodeWithQB.Infrastructure.Data.DeserializedEventStore;
using static Newtonsoft.Json.JsonConvert;

namespace CodeWithQB.Infrastructure.Data
{
    public class EventStore : IEventStore
    {
        private readonly IConfiguration _configuration;
        private readonly IDateTime _dateTime;
        private readonly IBackgroundTaskQueue _queue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public Subject<EventStoreChanged> _subject = new Subject<EventStoreChanged>();

        public EventStore(
            IConfiguration configuration,
            IDateTime dateTime = default(IDateTime),
            IBackgroundTaskQueue queue = default(IBackgroundTaskQueue),
            IServiceScopeFactory serviceScopeFactory = default(IServiceScopeFactory)
            )
        {
            _configuration = configuration;
            _queue = queue;
            _serviceScopeFactory = serviceScopeFactory;
            _dateTime = dateTime;
            OnActivateAsync().GetAwaiter().GetResult();
        }

        public ConcurrentDictionary<string, ConcurrentBag<AggregateRoot>> Aggregates { get; set; }
        = new ConcurrentDictionary<string, ConcurrentBag<AggregateRoot>>();

        public async Task OnActivateAsync() {

            Dictionary<string,IEnumerable<object>> state = await GetStateAsync();

            if (state == null)
            {
                var types = new List<Type>() {
                    typeof(Address),
                    typeof(Card),
                    typeof(Dashboard),
                    typeof(DashboardCard),
                    typeof(Event),
                    typeof(Guest),
                    typeof(Mentee),
                    typeof(NotificationTemplate),
                    typeof(Product),
                    typeof(Role),
                    typeof(ShoppingCart),
                    typeof(ShoppingCartItem),
                    typeof(User)
                };

                foreach (var type in types)
                    Aggregates.TryAdd(type.AssemblyQualifiedName, new ConcurrentBag<AggregateRoot>(Query<AggregateRoot>(type.AssemblyQualifiedName)));
            }
            else
            {
                foreach (var item in state)
                    Aggregates.TryAdd(item.Key, new ConcurrentBag<AggregateRoot>(item.Value.Select(x => (AggregateRoot)x).ToList()));                             
            }

            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, IEnumerable<object>>> GetStateAsync() {

            var dateTime = _configuration?.GetValue<DateTime>("ViewAt");

            if (dateTime == default(DateTime) || dateTime == null)
                dateTime = _dateTime.UtcNow;

            using (var scope = _serviceScopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                var snapshot = context.Snapshots.OrderByDescending(x => x.AsOfDateTime)
                    .Where(x => x.AsOfDateTime < dateTime)
                    .FirstOrDefault();

                if (snapshot == null) return null;

                var result = new Dictionary<string, IEnumerable<object>>();

                foreach (var item in DeserializeObject<dynamic>(snapshot.Data))
                {
                    var aggregates = new List<object>();
                    foreach (var dataItem in ((JProperty)item).Value.ToList())
                    {
                        aggregates.Add(DeserializeObject(SerializeObject(dataItem), Type.GetType(((JProperty)item).Name)));
                    }

                    result.TryAdd(((JProperty)item).Name, aggregates);
                }

                return await Task.FromResult(result);
            }
        }

        public async Task PersistStateAsync()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                Dictionary<string, IEnumerable<AggregateRoot>> data = new Dictionary<string, IEnumerable<AggregateRoot>>();

                foreach (var item in Aggregates)
                    data.Add(item.Key, item.Value);

                context.Snapshots.Add(new Snapshot()
                {
                    AsOfDateTime = _dateTime.UtcNow,
                    Data = SerializeObject(data),
                });

                await context.SaveChangesAsync(default(CancellationToken));
            }
        }

        public void Dispose() {

        }

        public void Save(AggregateRoot aggregateRoot)
        {
            var type = aggregateRoot.GetType();
            Guid aggregateId = (Guid)type.GetProperty($"{type.Name}Id").GetValue(aggregateRoot, null);
            string aggregate = aggregateRoot.GetType().Name;
            
            foreach (var @event in aggregateRoot.DomainEvents)
            {
                Add(new StoredEvent()
                {
                    StoredEventId = Guid.NewGuid(),
                    Aggregate = aggregate,
                    Data = SerializeObject(@event),
                    StreamId = aggregateId,
                    DotNetType = @event.GetType().AssemblyQualifiedName,
                    Type = @event.GetType().Name,
                    CreatedOn = DateTime.UtcNow,
                    Sequence = Get().Count() + 1
                });
            }


            aggregateRoot.ClearEvents();

            Aggregates.TryGetValue(type.AssemblyQualifiedName, out ConcurrentBag<AggregateRoot> orginalAggregates);

            var newAggregates = new ConcurrentBag<AggregateRoot>() { aggregateRoot };

            foreach (var originalAggregate in orginalAggregates)
            {
                var originalId = (Guid)type.GetProperty($"{type.Name}Id").GetValue(originalAggregate, null);

                if (aggregateId != originalId)
                    newAggregates.Add(originalAggregate);
            }

            Aggregates.TryUpdate(type.AssemblyQualifiedName, newAggregates, orginalAggregates);
        }

        public ConcurrentDictionary<string, ConcurrentBag<AggregateRoot>> UpdateState<TAggregateRoot>(Type type, TAggregateRoot aggregateRoot,Guid aggregateId)
            where TAggregateRoot: AggregateRoot
        {
            Aggregates.TryGetValue(type.AssemblyQualifiedName, out ConcurrentBag<AggregateRoot> orginalAggregates);

            var newAggregates = new ConcurrentBag<AggregateRoot>() { aggregateRoot };

            foreach (var originalAggregate in orginalAggregates)
            {
                var originalId = (Guid)type.GetProperty($"{type.Name}Id").GetValue(originalAggregate, null);

                if (aggregateId != originalId)
                    newAggregates.Add(originalAggregate);
            }

            Aggregates.TryUpdate(type.AssemblyQualifiedName, newAggregates, orginalAggregates);

            return Aggregates;
        }

        public TAggregateRoot[] Query<TAggregateRoot>()
            where TAggregateRoot : AggregateRoot
        {
            var result = new List<TAggregateRoot>();
            var assemblyQualifiedName = typeof(TAggregateRoot).AssemblyQualifiedName;

            Aggregates.TryGetValue(assemblyQualifiedName, out ConcurrentBag<AggregateRoot> aggregates);

            foreach(var a in aggregates)
                result.Add(a as TAggregateRoot);

            return result.ToArray();
        }
        
        public TAggregateRoot[] Query<TAggregateRoot>(string assemblyQualifiedName)
            where TAggregateRoot : AggregateRoot
        {
            var result = new List<TAggregateRoot>();

            foreach (var grouping in Get()
                .Where(x => x.DotNetType == assemblyQualifiedName).GroupBy(x => x.StreamId))
            {
                var events = grouping.Select(x => x.Data as DomainEvent).ToArray();

                result.Add(Load<TAggregateRoot>(events.ToArray()));
            }

            return result.ToArray();
        }

        private T Load<T>(DomainEvent[] events)
            where T : AggregateRoot
        {
            var aggregate = (T)FormatterServices.GetUninitializedObject(typeof(T));

            foreach (var @event in events) aggregate.Apply(@event);

            aggregate.ClearEvents();

            return aggregate;
        }
        public List<DeserializedStoredEvent> Get()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                if (Events == null)
                    Events = new ConcurrentDictionary<Guid, DeserializedStoredEvent>(context.StoredEvents.Select(x => new DeserializedStoredEvent(x)).ToDictionary(x => x.StoredEventId));

                return Events.Select(x => x.Value)
                    .OrderBy(x => x.CreatedOn)
                    .ToList();
            }
        }


        protected void Add(StoredEvent @event)
        {
            Events.TryAdd(@event.StoredEventId, new DeserializedStoredEvent(@event));
            Persist(@event);
            _subject.OnNext(new EventStoreChanged(@event));
        }

        public void Persist(StoredEvent @event)
        {
            if (_queue == null) {
                using (var scope = _serviceScopeFactory.CreateScope())
                using (var context = scope.ServiceProvider.GetRequiredService<AppDbContext>())
                {
                    context.StoredEvents.Add(@event);
                    context.SaveChanges();
                }
            }

            _queue?.QueueBackgroundWorkItem(async token => await PersistStateAsync());
            
            _queue?.QueueBackgroundWorkItem(async token =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                using (var context = scope.ServiceProvider.GetRequiredService<AppDbContext>())
                {
                    context.StoredEvents.Add(@event);
                    context.SaveChanges();
                }

                await Task.CompletedTask;
            });

        }
        
        public void Subscribe(Action<EventStoreChanged> onNext) => _subject.Subscribe(onNext);        
    }
}
