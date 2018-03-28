namespace LuisBot
{
    using Autofac;
    using Autofac.Integration.WebApi;
    using Microsoft.Bot.Builder.Azure;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Internals;
    using Microsoft.Bot.Builder.Luis;
    using Microsoft.Bot.Connector;
    using mStack.API.Bots;
    using mStack.API.Bots.ExactOnline.HoursReminder;
    using mStack.API.Bots.Jennifer;
    using System;
    using System.Reflection;
    using System.Web.Configuration;
    using System.Web.Http;

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            RegisterBotDependencies();

            Conversation.UpdateContainer(
                builder =>
                {
                    builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));

                    // Bot Storage: register state storage for your bot
                    var store = new InMemoryDataStore();
                    builder.Register(c => store)
                        .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                        .AsSelf()
                        .SingleInstance();
                }
            );

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        private void RegisterBotDependencies()
        {
            var builder = new ContainerBuilder();

            // register the required modules
            builder.RegisterModule(new DialogModule());
            builder.RegisterModule(new JenniferModule());

            // register other dialogs we use
            //builder.Register((c, p) => new AlarmRingDialog(p.TypedAs<string>(), c.Resolve<IAlarmService>(), c.Resolve<IAlarmRenderer>())).AsSelf().InstancePerDependency();

            // Get your HttpConfiguration.
            var config = GlobalConfiguration.Configuration;

            // Register your Web API controllers.
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            // OPTIONAL: Register the Autofac filter provider.
            builder.RegisterWebApiFilterProvider(config);

            // Set the dependency resolver to be Autofac.
            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            // Set the dependency resolver to be Autofac.
            //builder.Update(Conversation.Container);
            //config.DependencyResolver = new AutofacWebApiDependencyResolver(Conversation.Container);
        }        
    }
}
