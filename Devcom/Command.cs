﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DeveloperCommands
{
    /// <summary>
    /// Represents a command that can be executed with Devcom.
    /// </summary>
    public class Command
    {
        private readonly string _name, _desc, _category, _paramHelpString;
        private readonly MethodInfo _method;
        private readonly ParameterInfo[] _paramList;
        private readonly bool _hasParamsArgument;
        private readonly int _numOptionalParams;
        private readonly Type _contextType;

        /// <summary>
        /// The name of the command.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// The description of the command.
        /// </summary>
        public string Description
        {
            get { return _desc; }
        }

        /// <summary>
        /// A template string generated by the engine that shows how the command is used.
        /// </summary>
        public string ParamHelpString
        {
            get { return _paramHelpString; }
        }

        /// <summary>
        /// The category under which the command may be accessed.
        /// </summary>
        public string Category
        {
            get { return _category; }
        }

        /// <summary>
        /// The full name of the command, including the category, in the format: CATEGORY.NAME
        /// </summary>
        public string QualifiedName
        {
            get { return Util.Qualify(_category, _name); }
        }

        /// <summary>
        /// The filter rules applies to the command.
        /// </summary>
        public ContextFilter Filter { get; set; }

        /// <summary>
        /// The base context type required by the command.
        /// </summary>
        public Type ContextType
        {
            get { return _contextType; }
        }

        internal Command(MethodInfo method, string name, string desc, string category, ContextFilter filter = null)
        {
            _method = method;
            Filter = filter;

            var pl = _method.GetParameters();

            // Examine parameters for optional/params arguments

            _numOptionalParams = pl.Count(pi => pi.IsOptional);

            if (pl.Any())
            {
                _contextType = pl[0].ParameterType;
                var type = typeof (Context);
                if (!_contextType.IsSubclassOf(type) && type != _contextType)
                {
                    throw new ArgumentException("Command creation failed: Method '" + method.Name + "' requires a DevcomContext as the first parameter.");
                }
                
                _hasParamsArgument = pl.Last().GetCustomAttributes<ParamArrayAttribute>().Any();
            }
            else
            {
                throw new ArgumentException("Command creation failed: Method '" + method.Name + "' requires a DevcomContext as the first parameter.");
            }

            // Check the filter against the minimum context type to make sure it lets it through
            if (filter != null && !ContextFilter.Test(_contextType, Filter))
            {
                throw new ArgumentException("Command creation failed: The base context type '"+ _contextType.Name +"' will always be rejected by the filter rules you specified.");
            }

            _paramList = pl;
            _name = name;
            _desc = desc;
            _category = category;

            _paramHelpString = _paramList.Length > 1
                ? _paramList.Where((p, i) => i > 0)
                .Select(p => "<" + p.Name + (p.IsOptional ? " (optional)>" : p.IsDefined(typeof(ParamArrayAttribute)) ? "...>" : ">"))
                .Aggregate((accum, pname) => accum + " " + pname)
                : "";
        }

        internal bool Run(Context context, params string[] args)
        {
            var currentContextType = context.GetType();

            // Check context type compatability
            if ((!currentContextType.IsSubclassOf(_contextType) && currentContextType != _contextType) || !ContextFilter.Test(currentContextType, Filter))
            {
                context.PostCommandNotFound(QualifiedName);
                return false;
            }

            int argc = args.Length;
            int paramc = _paramList.Length - 1;
            try
            {
                object[] boxed;
                if (_hasParamsArgument)
                {
                    boxed = new object[argc];
                }
                else if (argc < paramc - _numOptionalParams)
                {
                    context.Notify("Parameter count mismatch.");
                    return false;
                }

                boxed = Enumerable.Repeat(Type.Missing, paramc).ToArray();

                // Convert parameters to the proper types
                for (int i = 0; i < argc; i++)
                {
                    boxed[i] = Util.ChangeType(args[i], _paramList[(i >= paramc ? paramc - 1 : i) + 1].ParameterType);   
                }
                
                var argsFormatted = new List<object> { context };

                // Add all arguments except for any marked as 'params'
                argsFormatted.AddRange(boxed.Take(_hasParamsArgument ? paramc - 1 : paramc));

                // Insert params argument as an array (it needs to be represented as a single object)
                if (_hasParamsArgument)
                {
                    argsFormatted.Add(args.Where((o, i) => i >= paramc - 1).ToArray());
                }

                // Call the method with our parameters
                _method.Invoke(null, argsFormatted.ToArray());
            }
            catch(Exception ex)
            {
                if (SystemConvars.Throws)
                {
                    throw;
                }
                context.Notify("Error: " + ex);
                return false;
            }
            return true;
        }
    }
}
