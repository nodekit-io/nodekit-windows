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

namespace io.nodekit.NKScripting
{
    public interface INKScriptTypeInfo : IList<NKScriptTypeInfoMemberInfo>
    {
        bool hasSettableProperties { get; }
        Type pluginType { get; }
    }

    public class NKScriptTypeInfo<T> : List<NKScriptTypeInfoMemberInfo>, INKScriptTypeInfo where T : class
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

        private delegate bool inclusionDelegate(NKScriptTypeInfoMemberInfo member);

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

            enumerateExcluding(exclusion, (member) =>
            {
                var key = member.key;
                var name = member.name;

                NKScriptExportProxy<T> cls = new NKScriptExportProxy<T>(plugin);

                switch (member.memberType)
                {
                    case MemberType.Method:
                        if (name.Substring(0, 1) == "_")
                            return true;
                        if (cls.isExcludedFromScript(key))
                            return true;
                        member.name = cls.rewritescriptNameForKey(key, name);
                        return false;
                    case MemberType.Property:
                        if (name.Substring(0, 1) == "_")
                            return true;
                        if (cls.isExcludedFromScript(key))
                            return true;
                        member.name = cls.rewritescriptNameForKey(key, name);
                        return false;
                    case MemberType.Constructor:
                        if (cls.isExcludedFromScript(key))
                            return true;
                        member.name = cls.rewritescriptNameForKey(key, name);
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
                    NKScriptTypeInfoMemberInfo member = new NKScriptTypeInfoMemberInfo(m);
                    if (!known.Contains(member.name) && !callback(member))
                    {
                        this.Add(member);
                    }
                }
            }

            foreach (MethodInfo m in t.DeclaredMethods)
            {
                if (m.IsPublic && !m.IsSpecialName)
                {
                     NKScriptTypeInfoMemberInfo member = new NKScriptTypeInfoMemberInfo(m);
                    if (!known.Contains(member.name) && !callback(member))
                    {
                        this.Add(member);
                    }
                }
            }

            foreach (PropertyInfo p in t.DeclaredProperties)
            {
                NKScriptTypeInfoMemberInfo member = new NKScriptTypeInfoMemberInfo(p);
                if ((member.getter != null) || (member.setter != null))
                {
                    if (!known.Contains(member.name) && !callback(member))
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


    public enum MemberType
    {
        Method,
        Property,
        Constructor
    }

    public class NKScriptTypeInfoMemberInfo
    {
        internal NKScriptTypeInfoMemberInfo(ConstructorInfo constructor)
        {
            _method = constructor;
            name = constructor.Name;
            var param = constructor.GetParameters();
            key = param.Select(o => o.Name).Aggregate(name, (prod, next) => prod + ":" + next);
            arity = constructor.GetParameters().Length;
            _getter = null;
            _setter = null;
            isVoid = false;
            memberType = MemberType.Constructor;
        }

        internal NKScriptTypeInfoMemberInfo(MethodInfo method)
        {
            name = method.Name;
            var param = method.GetParameters();
            key = param.Select(o => o.Name).Aggregate(name, (prod, next) => prod + ":" + next);
            _method = method;
            arity = param.Length;
            _getter = null;
            _setter = null;
            isVoid = (method.ReturnType == typeof(void));
            memberType = MemberType.Method;
        }

        internal NKScriptTypeInfoMemberInfo(PropertyInfo prop )
        {
            name = prop.Name;
            key = name;
            _getter = prop.GetMethod;
            if (_getter != null && !_getter.IsPublic) _getter = null;

            _setter = prop.SetMethod;
            if (_setter != null && !_setter.IsPublic) _setter = null;
            _method = null;
            arity = 0;
            isVoid = false;
            memberType = MemberType.Property;
        }

        internal MemberType memberType;
        private MethodBase _method;
        private MethodInfo _getter;
        private MethodInfo _setter;
        internal Int32 arity;
        internal bool isVoid;
        internal string name;
        internal string key;

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
                Int32 _arity = arity;
                switch (this.memberType)
                {
                    case MemberType.Method:
                        break;
                    case MemberType.Constructor:
                        break;
                    default:
                        arity = -1;
                        break;
                }
                if (isVoid && (_arity < 0))
                    return "";
                else
                    return "#" + (_arity >= 0 ? _arity.ToString() : "") + (isVoid ? "a" : "s");
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
            return (list.Where(p => { return ((p.key == item)); }).FirstOrDefault());
        }

        public static NKScriptTypeInfoMemberInfo DefaultConstructor(this IList<NKScriptTypeInfoMemberInfo> list)
        {
            return (list.Where(p => { return ((p.isConstructor()) && (p.name == "")); }).FirstOrDefault());
        }

        public static bool ContainsProperty(this IList<NKScriptTypeInfoMemberInfo> list, string item)
        {
            return (list.Where(p => { return ((p.isProperty()) && (p.name == item)); }).Count() > 0);
        }

        public static bool ContainsMethod(this IList<NKScriptTypeInfoMemberInfo> list, string item)
        {
            return (list.Where(p => { return ((p.isMethod()) && (p.key == item)); }).Count() > 0);
        }

        public static bool ContainsConstructor(this IList<NKScriptTypeInfoMemberInfo> list, string item)
        {
            return (list.Where(p => { return ((p.isConstructor()) && (p.name == item)); }).Count() > 0);
        }
    }
}