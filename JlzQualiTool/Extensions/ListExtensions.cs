using System;

namespace QualiTool.Extensions
{
    using System.Collections.ObjectModel;

    public static class ListExtensions
    {

        public static ObservableCollection<T> Shuffle<T>(this ObservableCollection<T> list)
        {
            var rng = new Random();

            var returnValue = new ObservableCollection<T>(list);

            int remainingSize = returnValue.Count;
            while (remainingSize > 1)
            {
                var position = rng.Next(0, remainingSize);

                remainingSize--;
                returnValue.Swap(remainingSize, position);
            }

            return returnValue;
        }

        public static void Swap<T>(this ObservableCollection<T> list, int x, int y)
        {
            var temp = list[x];
            list[x] = list[y];
            list[y] = temp;
        }
    }
}
