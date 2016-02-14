/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks. All Rights Reserved.
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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting
{
    internal interface INKScriptTypeInfo : IList<NKScriptTypeInfoMemberInfo>
    {
        bool hasSettableProperties { get; }
        Type pluginType { get; }
    }

    internal class NKScriptTypeInfo<T> : List<NKScriptTypeInfoMemberInfo>, INKScriptTypeInfo
    {
        private Type _pluginType;
        private bool _hasSettableProperties;

        private Dictionary<string, NKScriptTypeInfoMemberInfo> members = new Dictionary<string, NKScriptTypeInfoMemberInfo>();
        private static List<string> exclusion;

        bool INKScriptTypeInfo.hasSettableProperties
        {
            get
            {
                return _hasSettableProperties;
            }
        }

        Type INKScriptTypeInfo.pluginType
        {
            get
            {
                return _pluginType;
            }
        }

        static NKScriptTypeInfo()
        {
            var methods = NKScriptTypeInfo<T>.instanceMethods(typeof(NKScriptExport));
            // Add/remove any extra exclusions here
            exclusion = methods;
        }

        private delegate bool inclusionDelegate(string name, NKScriptTypeInfoMemberInfo member);

        public NKScriptTypeInfo(T plugin) : base()
        {
            T instance;
            _pluginType = typeof(T);
            if (_pluginType == typeof(Type))
            {
                _pluginType = plugin as Type;
                instance = default(T);
            }
            else
                instance = plugin;

            enumerateExcluding(exclusion, (name, member) =>
            {
                NKScriptExportProxy<T> cls = new NKScriptExportProxy<T>(instance);

                switch (member.memberType)
                {
                    case MemberType.Method:
                        if (name.Substring(0, 1) == "_")
                            return true;
                        if (cls.isExcludedFromScript(name))
                            return true;
                        member.name = cls.rewritescriptNameForKey(name);
                        return false;
                    case MemberType.Property:
                        if (name.Substring(0, 1) == "_")
                            return true;
                        if (cls.isExcludedFromScript(name))
                            return true;
                        member.name = cls.rewritescriptNameForKey(name);
                        return false;
                    case MemberType.Constructor:
                        if (cls.isExcludedFromScript(name))
                            return true;
                        member.name = cls.rewritescriptNameForKey(name);
                        return false;
                    default:
                        return false;
                }
            });
        }

        private void enumerateExcluding(List<string> excluding, inclusionDelegate callback)
        {
            var known = excluding;

            var t = _pluginType.GetTypeInfo();

            foreach (ConstructorInfo m in t.DeclaredConstructors)
            {
                if (m.IsPublic && !m.IsStatic)
                {
                    string name = m.Name;
                    NKScriptTypeInfoMemberInfo member = new NKScriptTypeInfoMemberInfo(m);
                    if (!known.Contains(name) && !callback(name, member))
                    {
                        this.Add(member);
                    }
                }
            }

            foreach (MethodInfo m in t.DeclaredMethods)
            {
                if (m.IsPublic && !m.IsSpecialName)
                {
                    string name = m.Name;
                    NKScriptTypeInfoMemberInfo member = new NKScriptTypeInfoMemberInfo(m);
                    if (!known.Contains(name) && !callback(name, member))
                    {
                        this.Add(member);
                    }
                }
            }

            foreach (PropertyInfo p in t.DeclaredProperties)
            {
                string name = p.Name;

                MethodInfo getter = p.GetMethod;
                if (!getter.IsPublic) getter = null;

                MethodInfo setter = p.SetMethod;
                if (!setter.IsPublic) setter = null;

                if ((getter != null) || (setter != null))
                {
                    NKScriptTypeInfoMemberInfo member = new NKScriptTypeInfoMemberInfo(getter, setter);
                    if (!known.Contains(name) && !callback(name, member))
                    {
                        this.Add(member);
                        this._hasSettableProperties = (member.setter != null);
                    }
                }
            }
        }


        private static List<String> instanceMethods(Type protocol)
        {
            var result = new List<String>();
            var t = protocol.GetTypeInfo();

            foreach (MethodInfo m in t.DeclaredMethods)
            {
                if (!m.IsPrivate)
                    result.Add(m.Name);
            }
            return result;
        }
    }


    internal enum MemberType
    {
        Method,
        Property,
        Constructor
    }

    internal class NKScriptTypeInfoMemberInfo
    {
        internal NKScriptTypeInfoMemberInfo(ConstructorInfo constructor)
        {
            _method = constructor;
            arity = constructor.GetParameters().Length;
            _getter = null;
            _setter = null;
            memberType = MemberType.Constructor;
        }

        internal NKScriptTypeInfoMemberInfo(MethodInfo method)
        {
            _method = method;
            arity = method.GetParameters().Length;
            _getter = null;
            _setter = null;
            memberType = MemberType.Method;
        }

        internal NKScriptTypeInfoMemberInfo(MethodInfo getter, MethodInfo setter)
        {
            _getter = getter;
            _setter = setter;
            _method = null;
            arity = 0;
            memberType = MemberType.Property;
        }

        internal MemberType memberType;
        private MethodBase _method;
        private MethodInfo _getter;
        private MethodInfo _setter;
        internal Int32 arity;
        internal string name;

        internal bool isMethod() { return (memberType == MemberType.Method); }
        internal bool isProperty() { return (memberType == MemberType.Property); }
        internal bool isConstructor() { return (memberType == MemberType.Constructor); }

        internal MethodBase method { get { return _method; } }
        internal MethodInfo getter { get { return _getter; } }
        internal MethodInfo setter { get { return _setter; } }

        internal string NKScriptingjsType
        {
            get
            {
                bool promise;
                Int32 _arity = arity;
                switch (this.memberType)
                {
                    case MemberType.Method:
                        promise = _method.Name.EndsWith("promiseObject");
                        _arity = this.arity;
                        break;
                    case MemberType.Constructor:
                        promise = false; // Initializers no longer default to promise
                        _arity = (arity < 0) ? arity : arity + 1;
                        break;
                    default:
                        promise = false;
                        arity = -1;
                        break;
                }
                if (!promise && (_arity < 0))
                    return "";
                else
                    return "#" + (_arity >= 0 ? _arity.ToString() : "") + (promise ? "p" : "a");
            }
        }
    }

}

namespace io.nodekit.NKScripting
{

    static class ListExtensions
    {
        public static NKScriptTypeInfoMemberInfo Item(this IList<NKScriptTypeInfoMemberInfo> list, string item)
        {
            return (list.Where(p => { return ((p.name == item)); }).FirstOrDefault());
        }

        public static bool ContainsProperty(this IList<NKScriptTypeInfoMemberInfo> list, string item)
        {
            return (list.Where(p => { return ((p.isProperty()) && (p.name == item)); }).Count() > 0);
        }

        public static bool ContainsMethod(this IList<NKScriptTypeInfoMemberInfo> list, string item)
        {
            return (list.Where(p => { return ((p.isMethod()) && (p.name == item)); }).Count() > 0);
        }
    }
}