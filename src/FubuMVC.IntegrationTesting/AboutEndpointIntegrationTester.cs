﻿using System.Diagnostics;
using System.Threading;
using FubuCore;
using FubuMVC.Core;
using FubuMVC.Core.Diagnostics;
using FubuMVC.Core.Http.Hosting;
using FubuMVC.Core.Runtime;
using HtmlTags;
using NUnit.Framework;

namespace FubuMVC.IntegrationTesting
{
    [TestFixture]
    public class AboutEndpointIntegrationTester
    {
        private EmbeddedFubuMvcServer server;

        [TestFixtureSetUp]
        public void SetUp()
        {
            server = FubuRuntime.Basic(_ => _.Mode = "development").RunEmbedded(port: PortFinder.FindPort(5500));
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            server.SafeDispose();
        }

        [Test]
        public void can_get_The_about_page_smoke_test()
        {
            TestHost.Scenario(_ =>
            {
                _.Get.Action<AboutFubuDiagnostics>(x => x.get_about());
                _.ContentShouldContain("Assemblies");
            });
        }


        [Test, Explicit]
        public void manual_test_the_auto_reloading_tag()
        {
            using (var server = FubuRuntime.Basic(_ => _.Mode = "development").RunEmbedded(port: 5601))
            {
                Process.Start("http://localhost:5601/reloaded");
                Thread.Sleep(20000);
            }

            using (var server = FubuRuntime.Basic(_ => _.Mode = "development").RunEmbedded(port: 5601))
            {
                Thread.Sleep(20000);
            }
        }
    }


    public class ReloadingEndpoint
    {
        private readonly AppReloaded _reloaded;
        private readonly FubuRuntime _runtime;

        public ReloadingEndpoint(AppReloaded reloaded, FubuRuntime runtime)
        {
            _reloaded = reloaded;
            _runtime = runtime;
        }

        public HtmlDocument get_reloaded()
        {
            var document = new HtmlDocument();
            document.Title = "Manual Test Harness for reloading";
            document.Add("h1").Text("Loaded at " + _reloaded.Timestamp);

            document.Add(new AutoReloadingTag(_runtime.Mode));

            return document;
        }
    }
}