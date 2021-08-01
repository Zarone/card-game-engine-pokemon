using System;
using System.Collections.Generic;
using System.Threading;

public static class CardManipulation
{
    public static UnityEngine.Color Normal = UnityEngine.Color.white;
    public static UnityEngine.Color Selected = UnityEngine.Color.blue;
    public static UnityEngine.Color Unselected = UnityEngine.Color.gray;
    public static UnityEngine.Color PossibleMoveTo = new UnityEngine.Color(0, 1, 0, 0.52f);
    public static UnityEngine.Color Placeholder = new UnityEngine.Color(0.1f, 0.1f, 0.2f, 0.2f);
    public static string DefaultCard = "Background-01";

    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ??= new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)); }
        }
    }

    public static T[] Shuffle<T>(T[] items)
    {
        T[] list = items;
        int n = list.Length;
        while (n > 1)
        {
            n--;
            int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }

        return list;
    }
}
