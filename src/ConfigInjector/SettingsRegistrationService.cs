﻿using System;
using System.Collections.Generic;
using System.Linq;
using ConfigInjector.Exceptions;
using ConfigInjector.SettingsConventions;
using ConfigInjector.TypeProviders;

namespace ConfigInjector
{
    /// <summary>
    ///     Stateful service for settings registration.
    /// </summary>
    internal class SettingsRegistrationService
    {
        private readonly bool _allowEntriesInWebConfigThatDoNotHaveSettingsClasses;
        private readonly Action<IConfigurationSetting> _registerAsSingleton;
        private readonly ISettingKeyConvention[] _settingKeyConventions;
        private readonly ISettingsReader _settingsReader;
        private readonly SettingValueConverter _settingValueConverter;
        private readonly ITypeProvider _typeProvider;

        private IConfigurationSetting[] _stronglyTypedSettings;

        public SettingsRegistrationService(ITypeProvider typeProvider,
            Action<IConfigurationSetting> registerAsSingleton,
            bool allowEntriesInWebConfigThatDoNotHaveSettingsClasses,
            SettingValueConverter settingValueConverter,
            ISettingsReader settingsReader,
            ISettingKeyConvention[] settingKeyConventions)
        {
            _typeProvider = typeProvider;
            _registerAsSingleton = registerAsSingleton;
            _allowEntriesInWebConfigThatDoNotHaveSettingsClasses = allowEntriesInWebConfigThatDoNotHaveSettingsClasses;
            _settingValueConverter = settingValueConverter;
            _settingsReader = settingsReader;
            _settingKeyConventions = settingKeyConventions;
        }

        public void RegisterConfigurationSettings()
        {
            _stronglyTypedSettings = LoadConfigurationSettings();

            if (!_allowEntriesInWebConfigThatDoNotHaveSettingsClasses)
            {
                AssertThatNoAdditionalSettingsExist();
            }

            foreach (var configurationSetting in _stronglyTypedSettings)
            {
                _registerAsSingleton(configurationSetting);
            }
        }

        private IConfigurationSetting[] LoadConfigurationSettings()
        {
            var configurationSettings = _typeProvider.Get()
                .Where(t => !t.IsInterface)
                .Where(t => !t.IsAbstract)
                .Where(t => typeof (IConfigurationSetting).IsAssignableFrom(t))
                .Select(GetConfigSettingFor)
                .ToArray();

            return configurationSettings;
        }

        internal IConfigurationSetting GetConfigSettingFor(Type type)
        {
            var settingValueStrings = GetPossibleKeysFor(type)
                .Select(k => _settingsReader.ReadValue(k))
                .Where(v => v != null)
                .ToArray();

            var matchingSettingCount = settingValueStrings.Count();
            if (matchingSettingCount == 0) throw new MissingSettingException(type);
            if (matchingSettingCount > 1) throw new AmbiguousSettingException(type, settingValueStrings);

            var settingValueString = settingValueStrings.Single();
            return ConstructSettingObject(type, settingValueString);
        }

        private IConfigurationSetting ConstructSettingObject(Type type, string settingValueString)
        {
            var settingType = type.GetProperty("Value").PropertyType;

            dynamic settingValue;
           
                settingValue = _settingValueConverter.ParseSettingValue(settingType, settingValueString);
                

            var setting = (IConfigurationSetting) Activator.CreateInstance(type);
            ((dynamic) setting).Value = settingValue;

            return setting;
        }

        private void AssertThatNoAdditionalSettingsExist()
        {
            var extraneousWebConfigEntries = _settingsReader.AllKeys
                .Where(s => !StronglyTypedSettingExistsFor(s))
                .ToArray();

            if (!extraneousWebConfigEntries.Any()) return;

            throw new ExtraneousSettingsException(extraneousWebConfigEntries);
        }

        private IEnumerable<string> GetPossibleKeysFor(Type type)
        {
            return _settingKeyConventions
                .Select(sc => sc.KeyFor(type))
                .Where(k => k != null)
                .Distinct();
        }

        private bool StronglyTypedSettingExistsFor(string key)
        {
            var possibleKeysForType = _stronglyTypedSettings.SelectMany(t => GetPossibleKeysFor(t.GetType()))
                .ToArray();
            return possibleKeysForType
                .Where(k => k == key)
                .Any();
        }
    }
}