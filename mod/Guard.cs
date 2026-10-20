using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace CivMindOverhaul
{
    internal static class Ref
    {
        private static readonly List<string> _resolved = new List<string>();
        private static readonly List<string> _failed = new List<string>();
        private static bool _primed;

        internal static FieldInfo Field(Type t, string name)
        {
            FieldInfo f = null;
            try
            {
                f = AccessTools.Field(t, name);
            }
            catch
            {
            }
            Track(f != null, t.Name + "." + name);
            return f;
        }

        internal static MethodInfo Method(Type t, string name)
        {
            MethodInfo m = null;
            try
            {
                m = AccessTools.Method(t, name);
            }
            catch
            {
            }
            Track(m != null, t.Name + "." + name);
            return m;
        }

        internal static MethodInfo Method(Type t, string name, Type[] args)
        {
            MethodInfo m = null;
            try
            {
                m = AccessTools.Method(t, name, args);
            }
            catch
            {
            }
            Track(m != null, t.Name + "." + name);
            return m;
        }

        internal static AccessTools.FieldRef<T, F> FieldRef<T, F>(string name)
        {
            AccessTools.FieldRef<T, F> r = null;
            try
            {
                r = AccessTools.FieldRefAccess<T, F>(name);
            }
            catch
            {
            }
            Track(r != null, typeof(T).Name + "." + name);
            return r;
        }

        private static void Track(bool ok, string label)
        {
            (ok ? _resolved : _failed).Add(label);
        }

        private static bool HoldsRefHandles(Type t)
        {
            try
            {
                FieldInfo[] fields = t.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                for (int i = 0; i < fields.Length; i++)
                {
                    Type ft = fields[i].FieldType;
                    if (ft == typeof(FieldInfo) || ft == typeof(MethodInfo))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        internal static void Report()
        {
            if (_primed)
            {
                return;
            }
            _primed = true;
            Type[] types;
            try
            {
                types = typeof(Ref).Assembly.GetTypes();
            }
            catch
            {
                return;
            }
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0 && !HoldsRefHandles(types[i]))
                {
                    continue;
                }
                try
                {
                    RuntimeHelpers.RunClassConstructor(types[i].TypeHandle);
                }
                catch (Exception e)
                {
                    _failed.Add(types[i].Name + " (static init: " + e.Message + ")");
                }
            }
            int total = _resolved.Count + _failed.Count;
            if (_failed.Count == 0)
            {
                UnityEngine.Debug.Log("[CivMind] reflection: " + _resolved.Count + "/" + total + " targets resolved");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[CivMind] reflection: " + _resolved.Count + "/" + total + " targets resolved — FAILED: " + string.Join(", ", _failed.ToArray()));
            }
        }
    }

    internal static class First
    {
        private static readonly HashSet<string> _seen = new HashSet<string>();

        internal static void Fire(string id)
        {
            if (_seen.Add(id))
            {
                UnityEngine.Debug.Log("[CivMind] first fire: " + id);
            }
        }
    }

    internal static class Once
    {
        private static readonly HashSet<string> _done = new HashSet<string>();

        internal static bool PerInit(string key)
        {
            return _done.Add(key);
        }
    }
}
