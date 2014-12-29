namespace NServiceBus.Features
{
    using System;
    using Config;
    using NServiceBus.SecondLevelRetries;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    /// Used to configure Second Level Retries.
    /// </summary>
    public class SecondLevelRetries : Feature
    {
        internal SecondLevelRetries()
        {
            EnableByDefault();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use SLR since it requires receive capabilities");

            Prerequisite(IsEnabledInConfig, "SLR was disabled in config");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var  retryPolicy = GetRetryPolicy(context.Settings);

            context.Pipeline.Register<SecondLevelRetriesBehavior.Registration, SecondLevelRetriesBehavior>(
                builder => new SecondLevelRetriesBehavior(builder.Build<IDeferMessages>(), retryPolicy,builder.Build<BusNotifications>()));
        }

        bool IsEnabledInConfig(FeatureConfigurationContext context)
        {
            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();

            if (retriesConfig == null)
                return true;

            if (retriesConfig.NumberOfRetries == 0)
                return false;

            return retriesConfig.Enabled;
        }

        RetryPolicy GetRetryPolicy(ReadOnlySettings settings)
        {
            var customRetryPolicy = settings.GetOrDefault<Func<TransportMessage, TimeSpan>>("SecondLevelRetries.RetryPolicy");

            if (customRetryPolicy != null)
            {
                return new CustomRetryPolicy(customRetryPolicy);
            }

            var retriesConfig = settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig != null)
            {
                return new DefaultRetryPolicy(retriesConfig.NumberOfRetries, retriesConfig.TimeIncrease);
            }

            return new DefaultRetryPolicy(DefaultRetryPolicy.DefaultNumberOfRetries,DefaultRetryPolicy.DefaultTimeIncrease);
        }
    }
}