using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Celestia.VContainer
{
    public class CelestiaInstaller : IInstaller
    {
        private readonly CelestiaConfig m_Config;

        public CelestiaInstaller(CelestiaConfig config)
        {
            m_Config = config;
        }

        public void Install(IContainerBuilder builder)
        {
            if (m_Config == null)
            {
                Debug.LogError($"{nameof(CelestiaInstaller)} was given no {nameof(CelestiaConfig)}.");
                return;
            }

            builder.RegisterInstance(m_Config);

            if (m_Config.Preset != null) builder.RegisterInstance(m_Config.Preset);

            builder.Register<CelestiaLightProvider>(Lifetime.Singleton)
                .As<ICelestiaLightProvider>()
                .AsSelf();

            builder.Register(_ => CreateClock(), Lifetime.Singleton)
                .As<IWorldClock>()
                .AsSelf();

            builder.Register<CelestialEngine>(Lifetime.Singleton)
                .WithParameter(m_Config.Preset)
                .As<ICelestialSource>()
                .AsSelf();

            builder.Register<ScheduleRunner>(Lifetime.Singleton)
                .As<IScheduleRunner>()
                .AsSelf();

            builder.RegisterEntryPoint<CelestiaRuntime>()
                .AsSelf();
        }

        private WorldClock CreateClock() => new(m_Config.RealSecondsPerDay, m_Config.StartProgress, false)
        {
            TimeScale = m_Config.TimeScale
        };
    }
}
