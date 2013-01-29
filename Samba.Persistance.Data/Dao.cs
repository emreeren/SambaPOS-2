using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Samba.Infrastructure.Data;
using Samba.Infrastructure.Data.Serializer;

namespace Samba.Persistance.Data
{
    public static class Dao
    {
        private static readonly IDictionary<Type, IDictionary<int, Dictionary<int, IEntity>>> Cache = new Dictionary<Type, IDictionary<int, Dictionary<int, IEntity>>>();

        private static void AddToCache<T>(int key, T item) where T : class,IEntity
        {
            if (item == null) return;
            var type = typeof(T);
            if (!Cache.ContainsKey(type)) Cache.Add(type, new Dictionary<int, Dictionary<int, IEntity>>());
            if (!Cache[type].ContainsKey(key)) Cache[type].Add(key, new Dictionary<int, IEntity>());
            try
            {
                Cache[type][key].Add(item.Id, item);
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#else
                Cache[type][key].Clear();
#endif
            }
        }

        private static T GetFromCache<T>(Expression<Func<T, bool>> predictate, int key)
        {
            try
            {
                if (Cache.ContainsKey(typeof(T)))
                    if (Cache[typeof(T)].ContainsKey(key))
                        return Cache[typeof(T)][key].Values.Cast<T>().SingleOrDefault(predictate.Compile());
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#else
                ResetCache();
                return default(T);
#endif
            }
            return default(T);
        }

        public static void ResetCache()
        {
            foreach (var arrayList in Cache.Values)
            {
                foreach (var item in arrayList.Values)
                {
                    item.Clear();
                }
                arrayList.Clear();
            }
            Cache.Clear();
        }

        public static TResult Single<TSource, TResult>(int id, Expression<Func<TSource, TResult>> expression) where TSource : class,IEntity
        {
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                return workspace.Single(id, expression);
            }
        }

        public static T Single<T>(Expression<Func<T, bool>> predictate, params Expression<Func<T, object>>[] includes) where T : class
        {
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                var result = workspace.Single(predictate, includes);
                return result;
            }
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public static T SingleWithCache<T>(Expression<Func<T, bool>> predictate, params Expression<Func<T, object>>[] includes) where T : class, IEntity
        {
            var lpredict = predictate;
            var key = ObjectCloner.DataHash(includes);
            var ci = GetFromCache(lpredict, key);
            if (ci != null) return ci;

            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                var result = workspace.Single(lpredict, includes);
                AddToCache(key, result);
                return result;
            }
        }

        public static IEnumerable<string> Distinct<T>(Expression<Func<T, string>> expression) where T : class
        {
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                return workspace.Distinct(expression).ToList();
            }
        }

        public static IEnumerable<T> Query<T>(Expression<Func<T, bool>> predictate, params Expression<Func<T, object>>[] includes) where T : class
        {
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                return workspace.Query(predictate, includes).ToList();
            }
        }

        public static IEnumerable<T> Query<T>(params Expression<Func<T, object>>[] includes) where T : class
        {
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                return workspace.Query(null, includes).ToList();
            }
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(Expression<Func<TSource, TResult>> expression,
                                                  Expression<Func<TSource, bool>> predictate) where TSource : class
        {
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                return workspace.Select(expression, predictate).ToList();
            }
        }

        public static IDictionary<int, T> BuildDictionary<T>() where T : class,IEntity
        {
            IDictionary<int, T> result = new Dictionary<int, T>();
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                var values = workspace.Query<T>(null);
                foreach (var value in values)
                {
                    result.Add(value.Id, value);
                }
            }

            return result;
        }

        public static int Count<T>(Expression<Func<T, bool>> predictate) where T : class
        {
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                return workspace.Count(predictate);
            }
        }

        public static decimal Sum<T>(Expression<Func<T, decimal>> func, Expression<Func<T, bool>> predictate) where T : class
        {
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                return workspace.Sum(func, predictate);
            }
        }

        public static T Last<T>(Expression<Func<T, bool>> predictate, params Expression<Func<T, object>>[] includes) where T : class ,IEntity
        {
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                return workspace.Last(predictate, includes);
            }
        }

        public static IEnumerable<T> Last<T>(int recordCount) where T : class,IEntity
        {
            using (var workspace = WorkspaceFactory.CreateReadOnly())
            {
                return workspace.Last<T>(recordCount).OrderBy(x => x.Id);
            }
        }
    }
}
