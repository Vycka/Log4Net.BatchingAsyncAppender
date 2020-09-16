// Took ideas from:
//  * http://svn.apache.org/viewvc/logging/log4net/trunk/examples/net/2.0/Appenders/SampleAppendersApp/cs/src/Appender/AsyncAppender.cs?revision=1707180&view=co
//  * https://stackoverflow.com/questions/7044497/how-do-i-create-an-asynchronous-wrapper-for-log4net
//  * https://github.com/Vycka/LoadRunner/blob/master/src/Viki.LoadRunner.Engine/Core/Collector/Pipeline/
// And created created new high throughput async appender. 
//  * Fix feature not implemented intentionally to decrease overhead to the main thread (the one that produces logs)
//    - Fix feature as is doesn't make sense in async context anyway as it shouldn't be patched on producer thread if aiming for maximum performance, and in child thread  volatile data might be already lost.
//    - Someone might prove me wrong, but until then - see no point for it.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
using Viki.Log4net.Appenders.BatchingAsync.Pipeline;

namespace Viki.Log4net.Appenders.BatchingAsync
{
    // TODO, requires some disposing
    // Its WiP - not ready to be used..
    public class BatchingAsyncAppender : IBulkAppender, IOptionHandler, IAppenderAttachable
    {
        public string Name { get; set; }

        private Task _consumerTask;
        private CancellationTokenSource _cancellationTokenSource;

        private BatchingPipe<LoggingEvent> _pipe;
        

        public void ActivateOptions()
        {
            _pipe = new BatchingPipe<LoggingEvent>();
            _cancellationTokenSource = new CancellationTokenSource();
            _consumerTask = StartConsumer(_cancellationTokenSource.Token);
        }

        private Task StartConsumer(CancellationToken cancellationToken)
        {
            Task result = ConsumerLoop();
            return result;
        }

        private async Task ConsumerLoop()
        {
            while (_pipe.Available)
            {
                List<LoggingEvent> batch;
                while (_pipe.TryLockBatch(out batch))
                {
                    LoggingEvent[] localCopy = new LoggingEvent[batch.Count];
                    batch.CopyTo(localCopy);

                    m_appenderAttachedImpl.AppendLoopOnAppenders(localCopy);

                    _pipe.ReleaseBatch();
                }

                await Task.Delay(500);
            }

            // TODO: handle errors..
        }

        public void Close()
        {
            
            _pipe.ProducingCompleted();

            lock (this)
            {
                if (m_appenderAttachedImpl != null)
                {
                    m_appenderAttachedImpl.RemoveAllAppenders();
                }
            }
        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            _pipe.Produce(loggingEvent);
        }

        

        public void DoAppend(LoggingEvent[] loggingEvents)
        {
            _pipe.Produce(loggingEvents);
        }

        #region IAppenderAttachable Members

        public void AddAppender(IAppender newAppender)
        {
            if (newAppender == null)
            {
                throw new ArgumentNullException("newAppender");
            }
            lock (this)
            {
                if (m_appenderAttachedImpl == null)
                {
                    m_appenderAttachedImpl = new AppenderAttachedImpl();
                }
                m_appenderAttachedImpl.AddAppender(newAppender);
            }
        }

        public AppenderCollection Appenders
        {
            get
            {
                lock (this)
                {
                    if (m_appenderAttachedImpl == null)
                    {
                        return AppenderCollection.EmptyCollection;
                    }
                    else
                    {
                        return m_appenderAttachedImpl.Appenders;
                    }
                }
            }
        }

        public IAppender GetAppender(string name)
        {
            lock (this)
            {
                if (m_appenderAttachedImpl == null || name == null)
                {
                    return null;
                }

                return m_appenderAttachedImpl.GetAppender(name);
            }
        }

        public void RemoveAllAppenders()
        {
            lock (this)
            {
                if (m_appenderAttachedImpl != null)
                {
                    m_appenderAttachedImpl.RemoveAllAppenders();
                    m_appenderAttachedImpl = null;
                }
            }
        }

        public IAppender RemoveAppender(IAppender appender)
        {
            lock (this)
            {
                if (appender != null && m_appenderAttachedImpl != null)
                {
                    return m_appenderAttachedImpl.RemoveAppender(appender);
                }
            }
            return null;
        }

        public IAppender RemoveAppender(string name)
        {
            lock (this)
            {
                if (name != null && m_appenderAttachedImpl != null)
                {
                    return m_appenderAttachedImpl.RemoveAppender(name);
                }
            }
            return null;
        }

        private AppenderAttachedImpl m_appenderAttachedImpl;

        #endregion
    }
}