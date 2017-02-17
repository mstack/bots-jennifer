﻿namespace LuisBot
{
    using Autofac;
    using Autofac.Integration.WebApi;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Internals;
    using Microsoft.Bot.Builder.Luis;
    using Microsoft.Bot.Connector;
    using mStack.API.Bots.ExactOnline.HoursReminder;
    using mStack.API.Bots.Jennifer;
    using System.Reflection;
    using System.Web.Configuration;
    using System.Web.Http;

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RegisterBotDependencies();
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
        }

        //private void RegisterBotDependencies()
        //{
        //    var builder = new ContainerBuilder();

        //    RedisStoreOptions redisOptions = new RedisStoreOptions()
        //    {
        //        Configuration = "localhost"
        //    };

        //    builder.Register(c => new RedisStore(redisOptions))
        //       .As<RedisStore>()
        //       .SingleInstance();

        //    builder.Register(c => new CachingBotDataStore(c.Resolve<RedisStore>(),
        //                                                  CachingBotDataStoreConsistencyPolicy.ETagBasedConsistency))
        //        .As<IBotDataStore<BotData>>()
        //        .AsSelf()
        //        .InstancePerLifetimeScope();

        //    builder.Update(Conversation.Container);

        //    //DependencyResolver.SetResolver(new AutofacDependencyResolver(Conversation.Container));
        //}
    }
}