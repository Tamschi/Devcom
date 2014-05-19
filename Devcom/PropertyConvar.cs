﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeveloperCommands
{
    public sealed class PropertyConvar : Convar
    {
        private readonly PropertyInfo _property;
        internal PropertyConvar(PropertyInfo property, string name, string desc, string cat, object defaultValue) : base(name, desc, cat, defaultValue)
        {
            if (property != null)
            {
                if (!property.GetGetMethod().IsStatic)
                {
                    throw new ArgumentException("Convar creation failed: The property '" + property.Name + "' is not static.");
                }
                _property = property;
                if (defaultValue != null)
                {
                    _property.SetValue(null, defaultValue);
                }
            }
            else
            {
                throw new ArgumentNullException("property");
            }
        }

        public override object Value
        {
            get { return _property.GetValue(null); }
            set
            {
                try
                {
                    _property.SetValue(null, Convert.ChangeType(value, _property.PropertyType));
                }
                catch
                {
                    _property.SetValue(null, null);
                }
            }
        }
    }
}