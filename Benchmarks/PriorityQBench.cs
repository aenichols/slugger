using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectBooster.Identity.Data.Utilities;

public record UserEventLog(Guid Id, Guid UserId, int TypeId, string EventData, int RandI, DateTime CreatedDate);

public sealed class UserEventLogger : IDisposable
{
    private readonly PriorityQueue<UserEventLog, int> _queueedEvents = new();
    private readonly Task? _mainTask;
    private Int64 _maxRandI;

    private List<UserEventLog> GetUserEventLogsThreshold_H()
    {
        var lockTaken = false;
        List<UserEventLog> events = new();
        try
        {
            Monitor.TryEnter(_queueedEvents, ref lockTaken);
            if (!lockTaken)
            {
                return events;
            }

            var startingCount = _queueedEvents.Count;
            var array1 = new UserEventLog[startingCount + 1];

            // Dequeue 4 (8 possible) at a time- should should maximize the op number per thread.
            for (var i = 0; i <= startingCount; ++i)
            {
                array1[Convert.ToByte(_queueedEvents.TryDequeue(out var cEvent1, out _)) * i] = cEvent1!;
                //if (_queueedEvents.TryDequeue(out var cEvent1, out _))
                //{
                //    events.Add(cEvent1);
                //}
            }

            //// create four dynamic arrays
            //var arraySize = startingCount / 4 + 1;
            //var array1 = new UserEventLog[arraySize];
            //var array2 = new UserEventLog[arraySize];
            //var array3 = new UserEventLog[arraySize];
            //var array4 = new UserEventLog[arraySize];

            //// Dequeue 4 (8 possible) at a time- should should maximize the op number per thread.
            //for (var i = 1; i <= arraySize + 1; ++i)
            //{
            //    array1[Convert.ToInt16(_queueedEvents.TryDequeue(out var cEvent1, out _)) * i] = cEvent1!;
            //    array2[Convert.ToInt16(_queueedEvents.TryDequeue(out var cEvent2, out _)) * i] = cEvent2!;
            //    array3[Convert.ToInt16(_queueedEvents.TryDequeue(out var cEvent3, out _)) * i] = cEvent3!;
            //    array4[Convert.ToInt16(_queueedEvents.TryDequeue(out var cEvent4, out _)) * i] = cEvent4!;
            //}

            //events.AddRange(array1.Skip(1));
            //events.AddRange(array2.Skip(1));
            //events.AddRange(array3.Skip(1));
            //events.AddRange(array4.Skip(1));
        }
        finally
        {
            if (lockTaken)
            {
                // ensure event logs do not get stuck- may loose logs if DQ fails
                _queueedEvents.Clear();
                Monitor.Exit(_queueedEvents);
            }
        }

        return events;
    }

    private List<UserEventLog> GetUserEventLogsThreshold_L()
    {
        var lockTaken = false;
        List<UserEventLog> events = new();
        try
        {
            Monitor.TryEnter(_queueedEvents, ref lockTaken);
            if (!lockTaken)
            {
                return events;
            }

            var startingCount = _queueedEvents.Count;
            // Dequeue 4 (8 possible) at a time- should should maximize the op number per thread.
            for (var i = 0; i <= startingCount; ++i)
            {
                if (_queueedEvents.TryDequeue(out var cEvent1, out _))
                {
                    events.Add(cEvent1);
                }
            }
        }
        finally
        {
            if (lockTaken)
            {
                // ensure event logs do not get stuck- may loose logs if DQ fails
                _queueedEvents.Clear();
                Monitor.Exit(_queueedEvents);
            }
        }

        return events;
    }

    public void ProcessQueue_H()
    {
        try
        {
            foreach (var chunk in GetUserEventLogsThreshold_H().Chunk(1000))
            {
                // create intense math problem
                _maxRandI = chunk.Aggregate(0L, (c, x) => c + x.RandI);

                //// Create new _dbContext to avoid threading issues.
                //using var dbContext = CreateDbContext();

                //// disable default options that impact performance
                //// and are not needed.
                ////
                //// note: if this is too slow (>2s per 1000 records)- we should
                //// look into System.Data.SqlClient.SqlBulkCopy or
                //// EFCore.BulkExtensions.
                //dbContext.Database.AutoTransactionsEnabled = false;
                //dbContext.Database.AutoSavepointsEnabled = false;

                //dbContext.UserEventLogs.AddRange(chunk);
                //var savedC = dbContext.SaveChanges();
                //dbContext.Dispose();
            }
        }
        finally
        {
            // process queue every minute - might need to expand.
            // Thread.Sleep(60000);
        }
    }

    public void ProcessQueue_L()
    {
        try
        {
            foreach (var chunk in GetUserEventLogsThreshold_L().Chunk(1000))
            {
                // create intense math problem
                _maxRandI = chunk.Aggregate(0L, (c, x) =>  c + x.RandI);

                //// Create new _dbContext to avoid threading issues.
                //using var dbContext = CreateDbContext();

                //// disable default options that impact performance
                //// and are not needed.
                ////
                //// note: if this is too slow (>2s per 1000 records)- we should
                //// look into System.Data.SqlClient.SqlBulkCopy or
                //// EFCore.BulkExtensions.
                //dbContext.Database.AutoTransactionsEnabled = false;
                //dbContext.Database.AutoSavepointsEnabled = false;

                //dbContext.UserEventLogs.AddRange(chunk);
                //var savedC = dbContext.SaveChanges();
                //dbContext.Dispose();
            }
        }
        finally
        {
            // process queue every minute - might need to expand.
            // Thread.Sleep(60000);
        }
    }

    public void EnqueueEvent(UserEventLog eventLog)
    {
        var lockTaken = false;
        try
        {
            Monitor.TryEnter(_queueedEvents, ref lockTaken);
            if (!lockTaken)
            {
                return;
            }
            // Millisecond should be a large enough buffer to avoid duplicate priorities.
            _queueedEvents.Enqueue(eventLog, ((int)eventLog.TypeId + 1) * eventLog.CreatedDate.Millisecond);
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(_queueedEvents);
            }
        }
    }

    public void Dispose()
    {
        _mainTask?.Dispose();
    }
}
