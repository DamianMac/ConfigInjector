﻿using System;

namespace ConfigInjector.Exceptions
{
    [Serializable]
    public class MissingSettingException : ConfigurationException
    {
        public MissingSettingException(Type settingType) : base(string.Format("Setting {0} was not found", settingType.Name))
        {
        }
    }
}