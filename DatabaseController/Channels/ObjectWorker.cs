namespace DatabaseController.Channels
{
    public class ObjectWorker : BackgroundService
    {
        private readonly ObjectQueue _objectQueue;
        private readonly IServiceScopeFactory _scopeFactory;

        public ObjectWorker(ObjectQueue objectQueue, IServiceScopeFactory scopeFactory)
        {
            _objectQueue = objectQueue;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var buffer = new List<Models.ModelObject>();
            var lastFlush = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                while (_objectQueue.Queue.Reader.TryRead(out var item))
                {
                    buffer.Add(item);
                }

                var elapsed = DateTime.UtcNow - lastFlush;

                if (buffer.Count >= 100 || (buffer.Count > 0 && elapsed.TotalMilliseconds >= 100))
                {
                    await SaveBatch(buffer, stoppingToken);
                    buffer.Clear();
                    lastFlush = DateTime.UtcNow;
                }

                await Task.Delay(10, stoppingToken);
            }

            if (buffer.Count > 0)
            {
                await SaveBatch(buffer, stoppingToken);
            }
        }

        private async Task SaveBatch(List<Models.ModelObject> batch, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();

            await db.Objects.AddRangeAsync(batch, stoppingToken);
            await db.SaveChangesAsync(stoppingToken);
        }
    }
}