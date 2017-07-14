using Moq.Language.Flow;
using System;
using System.Collections;
using System.Diagnostics;

namespace AutoNumber.Tests
{
    public static class MoqExtensions
    {
        public static void ReturnsInOrder<T, TResult>(this ISetup<T, TResult> setup,
            params object[] results) where T : class
        {
            var queue = new Queue(results);
            setup.Returns(() =>
            {
                var result = queue.Dequeue();
                if (result is Exception)
                {
                    throw result as Exception;
                }
                return (TResult)result;
            });
        }

        public static void WriteTrace(string s, object[] o)
        {
            Debug.WriteLine(s);
        }
    }
}
