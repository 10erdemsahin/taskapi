using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace AdventureGame.Tests
{
    internal static class TestUtil
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static void SetField(object target, string name, object value) => FindField(target, name).SetValue(target, value);

        public static T GetField<T>(object target, string name) => (T)FindField(target, name).GetValue(target);

        private static FieldInfo FindField(object target, string name)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, Flags);
                if (field != null) return field;
            }

            throw new MissingFieldException(target.GetType().Name, name);
        }

        /// <summary>Koşul gerçekleşene kadar bekler; süre (gerçek zaman) dolarsa testi düşürür.</summary>
        public static IEnumerator WaitFor(Func<bool> condition, float timeoutSeconds, string description)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail($"Zaman aşımı ({timeoutSeconds} sn): {description}");
                }

                yield return null;
            }
        }
    }
}
