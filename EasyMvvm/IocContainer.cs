﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EasyMvvm
{
    public class IocCacheData
    {
        public IocCacheData(bool singleton)
        {
            Singleton = singleton;
        }
        public string Key { get; set; }
        public Type TargetType { get; set; }
        public object Instance { get; set; }
        public bool Singleton { private set; get; }
    }

    public class IocContainer
    {
        private Dictionary<string, IocCacheData> _cache = new Dictionary<string, IocCacheData>();

        public IocContainer Singleton<T>(string key = null)
        {
            if (string.IsNullOrEmpty(key))
                key = typeof(T).Name;

            Register<T>(key, true);
            return this;
        }

        public IocContainer PerRequest<T>(string key = null)
        {
            if (string.IsNullOrEmpty(key))
                key = typeof(T).Name;

            Register<T>(key, false);
            return this;
        }

        public T Get<T>(string key = null)
        {
            T result = default(T);
            if (string.IsNullOrEmpty(key))
                result = (T)Get(typeof(T));
            else
                result = (T)Get(key);
            return result;
        }

        public IocContainer Instance<T>(T obj)
        {
            string key = typeof(T).Name;
            _cache[key] = new IocCacheData(true)
            {
                TargetType = typeof(T),
                Key = key,
                Instance = obj
            };
            return this;
        }

        public object Get(Type type)
        {
            var key = GetDefaultName(type);
            var result = Get(key);
            return result;
        }

        public object Get(string key)
        {
            if (!_cache.Keys.Contains(key))
                return null;

            var data = _cache[key];

            if (data.Singleton && data.Instance != null)
                return data.Instance;

            var args = GetConstructorArgs(data.TargetType);
            var result = ActivateInstance(data.TargetType, args);

            //释放旧对象
            if (data.Instance is IDisposable)
                (data as IDisposable).Dispose();

            data.Instance = result;

            return result;
        }

        /// <summary>
        /// 反射对象，并注入相关接口
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object GenerateObj(Type type)
        {
            var args = GetConstructorArgs(type);
            var result = ActivateInstance(type, args);
            return result;
        }

        #region private

        private string GetDefaultName(Type type)
        {
            string name = type.Name;
            return name;
        }

        private void Register<T>(string key, bool singleton)
        {
            Type targetType = typeof(T);

            if (string.IsNullOrEmpty(key))
            {
                key = GetDefaultName(targetType);
            }
            _cache[key] = new IocCacheData(singleton)
            {
                TargetType = targetType,
                Key = key
            };
        }

        #endregion

        #region reflector

        private object[] GetConstructorArgs(Type type)
        {
            var args = new List<object>();
            var constructor = GetConstructor(type);

            if (constructor != null)
            {
                args.AddRange(constructor.GetParameters().Select(info => Get(info.ParameterType)));
            }

            return args.ToArray();
        }

        private static ConstructorInfo GetConstructor(Type type)
        {
            return (from c in type.GetTypeInfo().DeclaredConstructors.Where(c => c.IsPublic)
                    orderby c.GetParameters().Length descending
                    select c).FirstOrDefault();
        }

        public static object ActivateInstance(Type type, object[] args)
        {
            var instance = args != null && args.Length > 0 ? System.Activator.CreateInstance(type, args) : System.Activator.CreateInstance(type);
            return instance;
        }

        #endregion
    }
}
