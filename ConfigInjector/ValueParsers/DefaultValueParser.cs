﻿using System;

namespace ConfigInjector.ValueParsers
{
    public class DefaultValueParser : IValueParser
    {
        public int SortOrder
        {
            get { return int.MaxValue; }
        }

        public bool CanParse(Type settingValueType)
        {
            return true; // YOLO!!
        }

        public object Parse(Type settingValueType, string settingValueString)
        {
            return Convert.ChangeType(settingValueString, settingValueType);
        }
    }
}