﻿using System;
using FubuMVC.Core.Registration;
using FubuMVC.Core.Registration.Nodes;

namespace FubuMVC.AntiForgery
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AntiForgeryTokenAttribute : ModifyChainAttribute
    {
        public AntiForgeryTokenAttribute()
        {
            Salt = Guid.NewGuid().ToString();
        }

        public string Salt { get; set; }

        public override void Alter(ActionCall call)
        {
            call.AddBefore(new AntiForgeryNode(Salt));
        }
    }
}