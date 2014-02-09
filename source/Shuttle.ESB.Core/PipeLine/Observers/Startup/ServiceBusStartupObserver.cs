using System;
using Shuttle.Core.Infrastructure;

namespace Shuttle.ESB.Core
{
	public class ServiceBusStartupObserver :
		IPipelineObserver<OnInitializeQueueFactories>,
		IPipelineObserver<OnCreateQueues>,
		IPipelineObserver<OnInitializeMessageHandlerFactory>,
		IPipelineObserver<OnInitializeMessageRouteProvider>,
		IPipelineObserver<OnInitializeForwardingRouteProvider>,
		IPipelineObserver<OnInitializePipelineFactory>,
		IPipelineObserver<OnInitializeSubscriptionManager>,
		IPipelineObserver<OnInitializeQueueManager>,
		IPipelineObserver<OnInitializeReceiveMessageStateService>,
		IPipelineObserver<OnInitializeTransactionScopeFactory>,
		IPipelineObserver<OnStartInboxProcessing>,
		IPipelineObserver<OnStartControlInboxProcessing>,
		IPipelineObserver<OnStartOutboxProcessing>,
		IPipelineObserver<OnStartDeferredMessageProcessing>,
		IPipelineObserver<OnStartWorker>
	{
		private readonly IServiceBus _bus;

		private readonly ILog _log;

		public ServiceBusStartupObserver(IServiceBus bus)
		{
			Guard.AgainstNull(bus, "bus");

			_bus = bus;
			_log = Log.For(this);
		}

		public void Execute(OnInitializeQueueFactories pipelineEvent)
		{
			_bus.Configuration.QueueManager.AttemptInitialization(_bus);
			_bus.Configuration.QueueManager.RegisterQueueFactory(new MemoryQueueFactory());

			foreach (var factory in _bus.Configuration.QueueManager.GetQueueFactories())
			{
				factory.AttemptInitialization(_bus);
			}
		}

		public void Execute(OnCreateQueues pipelineEvent)
		{
			if (ServiceBusConfiguration.ServiceBusSection != null 
				&&
				ServiceBusConfiguration.ServiceBusSection.CreateQueues)
			{
				_bus.Configuration.QueueManager.CreatePhysicalQueues(_bus.Configuration);
			}
		}

		public void Execute(OnInitializeMessageHandlerFactory pipelineEvent)
		{
			_bus.Configuration.MessageHandlerFactory.AttemptInitialization(_bus);
		}

		public void Execute(OnInitializePipelineFactory pipelineEvent)
		{
			_bus.Configuration.PipelineFactory.AttemptInitialization(_bus);
		}

		public void Execute(OnInitializeSubscriptionManager pipelineEvent)
		{
			if (!_bus.Configuration.HasSubscriptionManager)
			{
				_log.Information(ESBResources.NoSubscriptionManager);

				return;
			}

			_bus.Configuration.SubscriptionManager.AttemptInitialization(_bus);
		}

		public void Execute(OnInitializeReceiveMessageStateService pipelineEvent)
		{
			if (!_bus.Configuration.HasReceiveMessageStateService)
			{
				_log.Information(ESBResources.NoReceiveMessageStateService);

				return;
			}

			_bus.Configuration.ReceiveMessageStateService.AttemptInitialization(_bus);
		}

		public void Execute(OnInitializeTransactionScopeFactory pipelineEvent)
		{
			_bus.Configuration.TransactionScopeFactory.AttemptInitialization(_bus);
		}

		public void Execute(OnStartInboxProcessing pipelineEvent)
		{
			if (!_bus.Configuration.HasInbox)
			{
				return;
			}

			var inbox = _bus.Configuration.Inbox;

			if (inbox.WorkQueueStartupAction == QueueStartupAction.Purge)
			{
				var queue = inbox.WorkQueue as IPurge;

				if (queue != null)
				{
					_log.Information(string.Format(ESBResources.PurgingInboxWorkQueue, inbox.WorkQueue.Uri));

					queue.Purge();

					_log.Information(string.Format(ESBResources.PurgingInboxWorkQueueComplete, inbox.WorkQueue.Uri));
				}
				else
				{
					_log.Warning(string.Format(ESBResources.CannotPurgeQueue, inbox.WorkQueue.Uri));
				}
			}

			pipelineEvent.Pipeline.State.Add(
				"InboxThreadPool",
					 new ProcessorThreadPool(
						"InboxProcessor",
						inbox.ThreadCount,
						new InboxProcessorFactory(_bus)).Start());
		}

		public void Execute(OnStartControlInboxProcessing pipelineEvent)
		{
			if (!_bus.Configuration.HasControlInbox)
			{
				return;
			}

			pipelineEvent.Pipeline.State.Add(
				"ControlInboxThreadPool",
				new ProcessorThreadPool(
					"ControlInboxProcessor",
					_bus.Configuration.ControlInbox.ThreadCount,
					new ControlInboxProcessorFactory(_bus)).Start());
		}

		public void Execute(OnStartOutboxProcessing pipelineEvent)
		{
			if (!_bus.Configuration.HasOutbox)
			{
				return;
			}

			pipelineEvent.Pipeline.State.Add(
				"OutboxThreadPool",
				new ProcessorThreadPool(
					"OutboxProcessor",
					_bus.Configuration.Outbox.ThreadCount,
					new OutboxProcessorFactory(_bus)).Start());
		}

		public void Execute(OnStartDeferredMessageProcessing pipelineEvent)
		{
			if (!_bus.Configuration.HasDeferredMessageQueue || _bus.Configuration.IsWorker)
			{
				return;
			}

			pipelineEvent.Pipeline.State.Add(
				"DeferredMessageThreadPool",
				new ProcessorThreadPool(
					"DeferredMessageProcessor",
					1,
					new DeferredMessageProcessorFactory(_bus)).Start());
		}

		public void Execute(OnStartWorker pipelineEvent)
		{
			if (!_bus.Configuration.IsWorker)
			{
				return;
			}

			_bus.Send(new WorkerStartedEvent
						{
							InboxWorkQueueUri = _bus.Configuration.Inbox.WorkQueue.Uri.ToString(),
							DateStarted = DateTime.Now
						},
					 _bus.Configuration.Worker.DistributorControlInboxWorkQueue);
		}

		public void Execute(OnInitializeMessageRouteProvider pipelineEvent)
		{
			_bus.Configuration.MessageRouteProvider.AttemptInitialization(_bus);
		}

		public void Execute(OnInitializeForwardingRouteProvider pipelineEvent1)
		{
			_bus.Configuration.ForwardingRouteProvider.AttemptInitialization(_bus);
		}

		public void Execute(OnInitializeQueueManager pipelineEvent1)
		{
			_bus.Configuration.QueueManager.AttemptInitialization(_bus);
		}
	}
}