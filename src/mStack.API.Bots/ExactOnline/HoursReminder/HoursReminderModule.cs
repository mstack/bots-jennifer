using Autofac;
using Microsoft.Bot.Builder.Internals.Fibers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.ExactOnline.HoursReminder
{
    public class HoursReminderModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterType<HoursReminderEngine>().Keyed<HoursReminderEngine>(FiberModule.Key_DoNotSerialize).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<HoursReminderStore>().Keyed<IHoursReminderStore>(FiberModule.Key_DoNotSerialize).AsImplementedInterfaces().SingleInstance();
            
        }
    }
}
