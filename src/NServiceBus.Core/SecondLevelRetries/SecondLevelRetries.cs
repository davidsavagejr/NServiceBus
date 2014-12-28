namespace NServiceBus.Features
{
    using System;
    using Config;
    using NServiceBus.SecondLevelRetries;

    /// <summary>
    /// Used to configure Second Level Retries.
    /// </summary>
    public class SecondLevelRetries : Feature
    {
        internal SecondLevelRetries()
        {
            EnableByDefault();
            DependsOn<ForwarderFaultManager>();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use SLR since it requires receive capabilities");

            Prerequisite(IsEnabledInConfig, "SLR was disabled in config");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var retryPolicy = context.Settings.GetOrDefault<Func<TransportMessage, TimeSpan>>("SecondLevelRetries.RetryPolicy");

            var secondLevelRetriesConfiguration = new SecondLevelRetriesConfiguration();
            if (retryPolicy != null)
            {
                secondLevelRetriesConfiguration.RetryPolicy = retryPolicy;
            }


            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig == null)
            {
                return;
            }

            secondLevelRetriesConfiguration.NumberOfRetries = retriesConfig.NumberOfRetries;

            if (retriesConfig.TimeIncrease != TimeSpan.MinValue)
            {
                secondLevelRetriesConfiguration.TimeIncrease = retriesConfig.TimeIncrease;
            }
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
    }
}