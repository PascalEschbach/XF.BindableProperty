using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq {

	public static class LinqExtension {

		public static void ForEach<T>( this IEnumerable<T> source, Action<T> action ) {
			foreach( T item in source )
				action( item );
		}
		public static IEnumerable<R> ForEach<T, R>( this IEnumerable<T> source, Func<T, R> func ) {
			List<R> results = new List<R>();
			foreach( T item in source )
				results.Add( func( item ) );

			return results;
		}

		public static TSource[] ToArray<TSource>( this IEnumerable<TSource> source, Func<TSource, int, bool> predicate )
			=> source.Where( predicate ).ToArray();
		public static TSource[] ToArray<TSource>( this IEnumerable<TSource> source, Func<TSource, bool> predicate )
			=> source.Where( predicate ).ToArray();
		public static TResult[] ToArray<TSource, TResult>( this IEnumerable<TSource> source, Func<TSource, TResult> selector )
			=> source.Select( selector ).ToArray();
		public static TResult[] ToArray<TSource, TResult>( this IEnumerable<TSource> source, Func<TSource, int, TResult> selector )
			=> source.Select( selector ).ToArray();
	}
}
