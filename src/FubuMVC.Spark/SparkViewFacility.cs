using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using FubuCore;
using FubuMVC.Core.Registration;
using FubuMVC.Core.Runtime.Files;
using FubuMVC.Core.UI;
using FubuMVC.Core.View;
using FubuMVC.Core.View.Model;
using FubuMVC.Spark.Rendering;
using FubuMVC.Spark.SparkModel;
using HtmlTags;
using Spark;

namespace FubuMVC.Spark
{
    public class SparkViewFacility : ViewFacility<SparkTemplate>
    {
        private readonly SparkViewEngine _engine;

        public SparkViewFacility(SparkViewEngine engine)
        {
            _engine = engine;
        }

        public override Func<IFubuFile, SparkTemplate> CreateBuilder(SettingsCollection settings)
        {
            return file => new SparkTemplate(file, _engine);
        }

        public override FileSet FindMatching(SettingsCollection settings)
        {
            return settings.Get<SparkEngineSettings>().Search;
        }

        public override void Fill(ViewEngineSettings settings, BehaviorGraph graph)
        {
            configureNamespaces(graph);



            base.Fill(settings, graph);

            var bindingTemplates = graph.Files
                .FindFiles(FileSet.Shallow("bindings.xml"))
                .Select(x => new SparkTemplate(x, _engine)).ToArray();

            _engine.ViewFolder = new TemplateViewFolder(AllTemplates());
            _engine.DefaultPageBaseType = typeof (FubuSparkView).FullName;
            _engine.BindingProvider = new FubuBindingProvider(bindingTemplates);
            _engine.PartialProvider = new FubuPartialProvider(this);
        }

        private static void configureNamespaces(BehaviorGraph graph)
        {
            var sparkSettings = graph.Settings.Get<SparkSettings>();
            sparkSettings.SetAutomaticEncoding(true);

            sparkSettings.AddAssembly(typeof (HtmlTag).Assembly)
                .AddAssembly(typeof (IPartialInvoker).Assembly)
                .AddNamespace(typeof (IPartialInvoker).Namespace)
                .AddNamespace(typeof (VirtualPathUtility).Namespace) // System.Web
                .AddNamespace(typeof (SparkViewFacility).Namespace) // FubuMVC.Spark
                .AddNamespace(typeof (HtmlTag).Namespace); // HtmlTags 

            var namespaces = graph.Settings.Get<CommonViewNamespaces>();
            namespaces.Namespaces.Each(x => sparkSettings.AddNamespace(x));

            var engineSettings = graph.Settings.Get<SparkEngineSettings>();
            engineSettings.UseNamespaces.Each(ns => sparkSettings.AddNamespace(ns));
        }
    }
}