using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	/// <summary> Wraps around an enumerator that may have already been partially enumerated over,
	/// to become an enumerable which yields all elements up until a delegate function stops it. </summary>
	sealed class WrappedEnumerator<T> : IEnumerable<T>
	{
		/// <summary> The enumerator wrapped around. </summary>
		private readonly IEnumerator<T> enumerator;
		/// <summary> A function determining whether this enumerable should end before yielding its specified argument. </summary>
		private readonly Func<T, bool> finished;
		/// <summary> Gets whether this wrapper around the enumerator is exhausted, 
		/// which is true if the enumerator wrapped around is exhausted, 
		/// or when the function specified as constructor argument terminates this enumerable. </summary>
		internal bool WrapperExhausted { get; private set; }
		/// <summary> Gets whether the enumerator wrapped around is exhausted. </summary>
		internal bool WrappedEnumeratorExhausted { get; private set; }

		/// <summary> Creates a new wrapper around an enumerator and start iterating at the current instance of the specified enumerator until
		/// all elements have been enumerated over, or the specified predicate for the next-to-be-yielded element returns false (in which case it isn't yielded). </summary>
		/// <param name="enumerator"> The enumerator to wrap around. Must have a current. This instance will not dispose the enumerator. </param>
		/// <param name="finished"> A function taking an element and determining whether it is still considered to be part of this enumerable. </param>
		internal WrappedEnumerator(IEnumerator<T> enumerator, Func<T, bool> finished)
		{
			if (enumerator == null) throw new ArgumentNullException("enumerator");
			if (finished == null) throw new ArgumentNullException("finished");
			try
			{
				var dummy = enumerator.Current;
			}
			catch (InvalidOperationException ioe)
			{
				throw new ArgumentException("The enumerator is in an invalid state (i.e. there is no Enumerator.Current). ", ioe);
			}

			this.enumerator = enumerator;
			this.finished = finished;
		}

		/// <summary> Gets the enumerator that enumerates over the wrapped enumerator until its end or until the delegate terminates the enumeration. </summary>
		public IEnumerator<T> GetEnumerator()
		{
			while (true)
			{
				yield return enumerator.Current;

				if (!enumerator.MoveNext())
				{
					WrappedEnumeratorExhausted = true;
					WrapperExhausted = true;
					yield break;
				}
				else if (finished(enumerator.Current))
				{
					WrappedEnumeratorExhausted = false;
					WrapperExhausted = true;
					yield break;
				}
			}
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}

	#region Unfinished CachedWrapperEnumerator
	//Might be used in Split to make the return values more robust, but decided not to finish now, because I don't really need it 
	//and it started to take more work that I anticipated
	/*ublic enum SplitCachingOptions : byte
	 {
		 None,
		 FullyCached,
		 ChachedOnce,
	 }
	 private class CachedWrapperEnumerator<T> : IEnumerable<T>
	 {
		 /// <summary> The enumerator wrapped around. </summary>
		 private readonly IEnumerator<T> enumerator;
		 /// <summary> A function determining whether this enumerable should at at its specified argument. </summary>
		 private readonly Func<T, bool> finished;
		 /// <summary> The cache. Is empty when there has not been a need of caching yet. When cached, there is always one element in it.
		 /// Is null when this wrapper isn't allowed to cache (specified in constructor caching options argument). </summary>
		 private List<T> cache;
		 /// <summary> Gets whether the enumerator wrapped around is exhausted. </summary>
		 internal bool WrappedEnumeratorExhausted { get; private set; }
		 /// <summary> Gets whether this wrapper around the enumerator is exhausted, 
		 /// which is true if the enumerator wrapped around is exhausted, 
		 /// or when the function specified as constructor argument terminates this enumerable. </summary>
		 internal bool WrapperExhausted { get; private set; }

		 internal SplitCachingOptions CachingOption; 

		 internal void CacheIfNecessary()
		 {
			 if (cache != null) return; //caching has already happened, and therefore is not necessary to do again
			 if (!WrappedEnumeratorExhausted) throw new InvalidOperationException();//basic contract invariant check

			 this.cache = new List<T>(this);
			 this.WrapperExhausted = cache.Count == 0;
		 }
			
		 /// <summary> Creates a new wrapper around an enumerator and start iterating at the current instance of the specified enumerator until
		 /// all elements have been enumerated over, or the specified predicate for the next-to-be-yielded element returns false (in which case it isn't yielded). </summary>
		 /// <param name="enumerator"> The enumerator to wrap around. Must have a current. This instance will not dispose the enumerator. </param>
		 /// <param name="finished"> A function taking an element and determining whether it is still considered to be part of this enumerable. </param>
		 internal CachedWrapperEnumerator(IEnumerator<T> enumerator, Func<T, bool> finished, SplitCachingOptions cachingOption)
		 {
			 if (enumerator == null) throw new ArgumentNullException("enumerator");
			 if (finished == null) throw new ArgumentNullException("finished");
			 try
			 {
				 var dummy = enumerator.Current;
			 }
			 catch (InvalidOperationException ioe)
			 {
				 throw new ArgumentException("The enumerator is in an invalid state (i.e. there is no Enumerator.Current). ", ioe);
			 }

			 switch (cachingOption)
			 {
				 case SplitCachingOptions.None:
				 case SplitCachingOptions.ChachedOnce:
					 break;
				 case SplitCachingOptions.FullyCached:
					 this.cache = new List<T>();
					 break;
				 default:
					 throw new ArgumentOutOfRangeException("cachingOption");
			 }

			 this.enumerator = enumerator;
			 this.finished = finished;
		 }

		 public IEnumerator<T> GetEnumerator()
		 {
			 if (WrapperExhausted)
				 throw new InvalidOperationException("This enumerable cannot be enumerated over twice");
			 if (cache == null)
			 {
				 //no caching
				 //just yield them all, and throw if WrappedExhausted
			 }
			 else if(cache.Count == 0)
			 {
				 //not cached yet
				 //just yield them all(probably to the caching method), and throw if WrappedExhausted
			 }
			 {
				 for (int i = 0; i < cache.Count; i++)
					 yield return cache[i];
				 WrapperExhausted = true;
				 yield break;
			 }
			 else
			 {
				 while (true)
				 {
					 yield return enumerator.Current;

					 if (!enumerator.MoveNext())
					 {
						 WrappedEnumeratorExhausted = true;
						 WrapperExhausted = true;
						 break;
					 }
					 else if (finished(enumerator.Current))
					 {
						 WrapperExhausted = true;
						 break;
					 }
				 }
			 }
		 }
		 System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		 {
			 return this.GetEnumerator();
		 }
	 }*/
	#endregion
}

