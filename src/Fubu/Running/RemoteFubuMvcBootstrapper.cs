﻿using System;
using FubuMVC.Core;
using FubuMVC.Core.Diagnostics.Packaging;
using FubuMVC.Core.Services;
using FubuMVC.Core.Services.Messaging;

namespace Fubu.Running
{
    public class RemoteFubuMvcBootstrapper : IApplicationLoader, IActivator, IListener<StartApplication>,
        IListener<RecycleApplication>, IDisposable
    {
        private readonly IApplicationSourceFinder _typeFinder;
        private readonly IFubuMvcApplicationActivator _activator;
        private readonly IMessaging _messaging;

        public RemoteFubuMvcBootstrapper()
            : this(new ApplicationSourceFinder(), new FubuMvcApplicationActivator(), new Messaging())
        {
        }

        public RemoteFubuMvcBootstrapper(IApplicationSourceFinder typeFinder, IFubuMvcApplicationActivator activator,
            IMessaging messaging)
        {
            _typeFinder = typeFinder;
            _activator = activator;
            _messaging = messaging;
        }

        public IDisposable Load()
        {
            EventAggregator.Messaging.AddListener(this);
            return this;
        }

        public void Dispose()
        {
            _activator.ShutDown();
        }

        public void Receive(StartApplication message)
        {
            Console.WriteLine("Trying to start application " + message);


            var chooser = new FubuRegistryChooser(_typeFinder, _messaging);
            chooser.Find(message,
                applicationType => _activator.Initialize(applicationType, message.PortNumber, message.PhysicalPath, message.HtmlHeadInjectedText));
        }

        public void Receive(RecycleApplication message)
        {
            _activator.Recycle();
        }

        void IActivator.Activate(IActivationLog log, IPerfTimer timer)
        {
        }
    }
}