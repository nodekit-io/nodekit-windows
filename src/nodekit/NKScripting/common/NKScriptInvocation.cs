/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks. All Rights Reserved.
* Portions Copyright 2015 .NET Foundation
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting
{
    internal class NKScriptInvocation
    {
        public readonly object target;
        public readonly Type targetClass;
        private TaskScheduler queue;

        internal NKScriptInvocation(object obj, TaskScheduler queue)
        {
            target = obj;
            this.queue = queue;
            targetClass = obj.GetType();
            if (targetClass == typeof(Type))
            {
                targetClass = obj as Type;
                target =null;
            }
        }

        public static object construct(Type target, ConstructorInfo constructor, object[] args)
        {
            return constructor.Invoke(args);
        }

        public object call(MethodBase method, object[] args)
        {
            return method.Invoke(target, UnwrapArgs(method, args));
        }

        public Task<object> callAsync(MethodBase method, object[] args)
        {

            Func<object> work = () =>
            {
                return method.Invoke(target, UnwrapArgs(method, args));
            };

            if (queue != null)
                return Task.Factory.StartNew<object>(work, new System.Threading.CancellationToken(false), TaskCreationOptions.None, queue);
            else
                return Task.FromResult<object>(work.Invoke());
        }

        protected object[] UnwrapArgs(MethodBase m, object[] args)
        {
            ParameterInfo[] paramInfos = m.GetParameters();

            if (args != null && args.Length > paramInfos.Length)
                throw new ArgumentException(String.Format("Too many js arguments passed to plugin method {0};  expected {1} got {2}", m.Name, paramInfos.Length, args.Length));

            object[] newArgs = new object[paramInfos.Length];
            int k = 0;
            for (int i = 0; i < paramInfos.Length; i++)
            {
                ParameterInfo paramInfo = paramInfos[i];
                object argValue;
                if (k < args.Length && TryConvertArg(paramInfo, args[k], out argValue)) // If args[k] matches
                {
                    newArgs[i] = argValue;
                    k++;
                    continue;
                }

                if (TryGetOptionalArg(paramInfo, out argValue))
                {
                    newArgs[i] = argValue;
                }
                else
                {
                    throw new MissingMemberException();
                }
            }
            return newArgs;
        }

        /// <summary>
        /// Try to match an arg with a parameter.
        /// </summary>
        /// <param name="parameterInfo">The current expected parameter info.</param>
        /// <param name="arg">A passed in arg.</param>
        /// <param name="argValue">The actual arg value if arg matches paramInfo.</param>
        /// <returns>true if arg matches paramInfo.</returns>
        protected bool TryConvertArg(ParameterInfo parameterInfo, object arg, out object argValue)
        {
            try {
                argValue = ChangeType(parameterInfo, arg);
                return true;
            } catch (Exception)
            {
                argValue = null;
                return false;
            }
        }

        protected virtual bool TryGetOptionalArg(ParameterInfo paramInfo, out object argValue)
        {
            if (paramInfo.IsOut || paramInfo.IsOptional)
            {
                argValue = paramInfo.DefaultValue;
         
                return true;
            }

            argValue = null;
            return false;
        }

        protected static object ChangeType(ParameterInfo parameterInfo, object arg)
        {
            Type parameterType = parameterInfo.ParameterType;
            if (parameterType.IsByRef)
            {
                parameterType = parameterType.GetElementType();
            }
            return Convert.ChangeType(arg, parameterType, CultureInfo.InvariantCulture);
        }     
    }
}