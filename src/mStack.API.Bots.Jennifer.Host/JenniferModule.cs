using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Builder.Luis;
using mStack.API.Bots.Cache;
using mStack.API.Bots.ExactOnline.HoursReminder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace mStack.API.Bots.Jennifer
{
    public class JenniferModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            // Register the LUIS model
            builder.Register(c => new LuisService(new LuisModelAttribute(WebConfigurationManager.AppSettings["LuisAppId"], WebConfigurationManager.AppSettings["LuisAPIKey"]))).AsSelf().AsImplementedInterfaces().SingleInstance();

            // register the top level dialog
            builder.RegisterType<MainConversationDialog>().As<IDialog<object>>().InstancePerDependency();

            // register some singleton services
            builder.RegisterType<HoursReminderStore>().Keyed<IHoursReminderStore>(FiberModule.Key_DoNotSerialize).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<LuisService>().Keyed<ILuisService>(FiberModule.Key_DoNotSerialize).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ResolutionParser>().Keyed<IResolutionParser>(FiberModule.Key_DoNotSerialize).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<WesternCalendarPlus>().Keyed<ICalendarPlus>(FiberModule.Key_DoNotSerialize).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<StrictEntityToType>().Keyed<IEntityToType>(FiberModule.Key_DoNotSerialize).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<BotCache>().Keyed<IBotCache>(FiberModule.Key_DoNotSerialize).AsImplementedInterfaces().SingleInstance();

            // Register classes depening on the incoming messages
            builder.Register(c => new HoursReminderService(c.Resolve<IHoursReminderStore>(), c.Resolve<ResumptionCookie>())).Keyed<IHoursReminderService>(FiberModule.Key_DoNotSerialize).AsImplementedInterfaces().InstancePerMatchingLifetimeScope(DialogModule.LifetimeScopeTag);

        }
    }
}