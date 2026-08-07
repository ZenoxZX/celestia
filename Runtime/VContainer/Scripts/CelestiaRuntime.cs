using System;
using JetBrains.Annotations;
using UnityEngine;
using VContainer.Unity;

namespace Celestia.VContainer
{
    [UsedImplicitly]
    public sealed class CelestiaRuntime : IStartable, ITickable, IDisposable
    {
        private readonly CelestiaConfig m_Config;
        private readonly WorldClock m_Clock;
        private readonly CelestialEngine m_Engine;
        private readonly ScheduleRunner m_Runner;
        private readonly ICelestiaLightProvider m_Lights;

        private CelestialLightRig m_Rig;

        public CelestiaRuntime(CelestiaConfig config,
            WorldClock clock,
            CelestialEngine engine,
            ScheduleRunner runner,
            ICelestiaLightProvider lights)
        {
            m_Config = config;
            m_Clock = clock;
            m_Engine = engine;
            m_Runner = runner;
            m_Lights = lights;
        }

        void IStartable.Start()
        {
            m_Engine.Bind();
            m_Runner.Bind();

            m_Rig = new CelestialLightRig(m_Engine, m_Lights.SunLight, m_Lights.MoonLight)
            {
                DriveShadows = m_Config.DriveShadows,
                DriveColor = m_Config.DriveColor,
                DriveSunSource = m_Config.DriveSunSource,
                ShadowType = m_Config.ShadowType
            };

            m_Rig.Bind();

            if (m_Config.PlayOnStart) m_Clock.Play();
            else m_Clock.Pause();
        }

        void ITickable.Tick()
        {
            m_Clock.Tick(Time.deltaTime);
        }

        void IDisposable.Dispose()
        {
            m_Rig?.Unbind();
            m_Runner.Unbind();
            m_Engine.Unbind();
        }
    }
}
