using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace JBSnorro.Tests
{
	public class Foo<T> : DefaultINotifyPropertyChanged
	{
		private Bar sourceProperty;
		public Bar SourceProperty
		{
			get { return sourceProperty; }
			set { Set(ref sourceProperty, value); }
		}


		public class Bar : DefaultINotifyPropertyChanged
		{
			private T property;
			public T Property
			{
				get { return property; }
				set { Set(ref property, value); }
			}
		}
	}

	public class Giraffe : DefaultINotifyPropertyChanged
	{
		private int property;
		public int Property
		{
			get { return property; }
			set { Set(ref property, value); }
		}

		private int property2;
		public int Property2
		{
			get { return property2; }
			set { Set(ref property2, value); }
		}
	}

	[TestClass]
	public class EventBindingTests
	{
		private static PropertyChangedEventHandler emptyHandler = (sender, e) => { };
		[DebuggerHidden]
		private static void Bind(INotifyPropertyChanged source, PropertyChangedEventHandler handler)
		{
			EventBindingExtensions.Bind(source, nameof(Foo<int>.SourceProperty), nameof(Foo<int>.Bar.Property), handler);
		}

		[TestMethod]
		public void NotInvokedWhenSourcePropertyRemainsDefault()
		{
			int handlerInvokedCount = 0;
			PropertyChangedEventHandler handler = (sender, e) => { handlerInvokedCount++; };
			var foo = new Foo<int>();

			Bind(foo, handler);

			Contract.Assert(handlerInvokedCount == 0);
		}

		[TestMethod]
		public void NotInvokedWhenSourcePropertyChangesToDefaultWhilePropertyWasDefaultTest()
		{
			int handlerInvokedCount = 0;
			PropertyChangedEventHandler handler = (sender, e) => { handlerInvokedCount++; };
			var foo = new Foo<int>();

			Bind(foo, handler);

			Contract.Assert(handlerInvokedCount == 0);
		}


		[TestMethod]
		public void InvokedWhenSourcePropertyChangesToDefaultWhilePropertyWasNotDefaultTest()
		{
			int handlerInvokedCount = 0;
			PropertyChangedEventHandler handler = (sender, e) => { handlerInvokedCount++; };
			var foo = new Foo<int>() { SourceProperty = new Foo<int>.Bar() { Property = 1 } };

			Bind(foo, handler);         // invokes handler because SourceProperty is set from 0 to 1
			foo.SourceProperty = null;  // invokes handler because SourceProperty is set from 1 to 0

			Contract.Assert(handlerInvokedCount == 2);
		}

		[TestMethod]
		public void InvokedWhenSourcePropertyChangesToPropertyWithDefaultWhilePropertyWasNotDefault()
		{
			int handlerInvokedCount = 0;
			PropertyChangedEventHandler handler = (sender, e) => { handlerInvokedCount++; };
			var foo = new Foo<int>() { SourceProperty = new Foo<int>.Bar() { Property = 1 } };

			Bind(foo, handler);
			foo.SourceProperty = new Foo<int>.Bar();

			Contract.Assert(handlerInvokedCount == 2);
		}

		[TestMethod]
		public void InvokedWhenSourcePropertyChangesToPropertyWithNonDefaultWhilePropertyWasNotDefault()
		{
			int handlerInvokedCount = 0;
			PropertyChangedEventHandler handler = (sender, e) => { handlerInvokedCount++; };
			var foo = new Foo<int>() { SourceProperty = new Foo<int>.Bar() { Property = 1 } };

			Bind(foo, handler);
			foo.SourceProperty = new Foo<int>.Bar() { Property = 2 };

			Contract.Assert(handlerInvokedCount == 2);
		}

		[TestMethod]
		public void NotInvokedWhenSourcePropertyIsSetWithDefaultPropertyTest()
		{
			int handlerInvokedCount = 0;
			PropertyChangedEventHandler handler = (sender, e) => { handlerInvokedCount++; };
			var foo = new Foo<int>();

			Bind(foo, handler);
			foo.SourceProperty = new Foo<int>.Bar();

			Contract.Assert(handlerInvokedCount == 0);
		}

		[TestMethod]
		public void InvokedWhenSourcePropertyIsSetWithNonDefaultPropertyTest()
		{
			int handlerInvokedCount = 0;
			PropertyChangedEventHandler handler = (sender, e) => { handlerInvokedCount++; };
			var foo = new Foo<int>();

			Bind(foo, handler);

			var bar = new Foo<int>.Bar() { Property = 1 };
			foo.SourceProperty = bar;

			Contract.Assert(handlerInvokedCount == 1);
		}

		[TestMethod]
		public void PropertyOnCollectionChangedIsPropagated()
		{
			bool eventHandlerInvoked = false;
			var collection = new ProperObservableCollection<Giraffe>();
			collection.Bind(nameof(Giraffe.Property), (sender, e) =>
			{
				eventHandlerInvoked = true;
			});
			collection.Add(new Giraffe());

			Assert.IsFalse(eventHandlerInvoked);
			collection[0].Property = 0;
			Assert.IsFalse(eventHandlerInvoked);
			collection[0].Property2 = 1;
			Assert.IsFalse(eventHandlerInvoked);
			collection[0].Property = 1;
			Assert.IsTrue(eventHandlerInvoked);
		}


		[TestMethod]
		public void PropertyOnCollectionChangedIsNotPropagatedIfRemoved()
		{
			//Arrange
			bool eventHandlerInvoked = false;
			var collection = new ProperObservableCollection<Giraffe>();
			collection.Bind(nameof(Giraffe.Property), (sender, e) =>
			{
				eventHandlerInvoked = true;
			});
			var giraffe = new Giraffe();
			collection.Add(giraffe);

			//Act
			collection.Remove(giraffe);
			giraffe.Property = 1;

			//Assert
			Assert.IsFalse(eventHandlerInvoked);
		}

		[TestMethod]
		public void ReductionBindTakesOnFirstValueFromBeginning()
		{
			// Arrange
			const int expected = 1;
			var collection = new ProperObservableCollection<Giraffe>();
			collection.Add(new Giraffe() { Property = expected });
			int result = -1;
			void OnCollectivePropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				result = ((PropertyMutatedEventArgs<int>)e).NewValue;
			}

			// Act
			collection.BindCollective<Giraffe, int>(nameof(Giraffe.Property), OnCollectivePropertyChanged, Math.Min);

			Assert.AreEqual(expected, result);
		}
		[TestMethod]
		public void ReductionBindTakesOnFirstSpecifiedValue()
		{
			// Arrange
			const int expected = 2;
			var collection = new ProperObservableCollection<Giraffe>();
			collection.BindCollective<Giraffe, int>(nameof(Giraffe.Property), OnCollectivePropertyChanged, Math.Min);
			int result = -1;
			void OnCollectivePropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				result = ((PropertyMutatedEventArgs<int>)e).NewValue;
			}

			// Act
			collection.Add(new Giraffe() { Property = expected });

			Assert.AreEqual(expected, result);
		}
		[TestMethod]
		public void ReductionBindTriggersHandler()
		{
			// Arrange
			const int expected = 2;
			var collection = new ProperObservableCollection<Giraffe>();
			collection.BindCollective<Giraffe, int>(nameof(Giraffe.Property), OnCollectivePropertyChanged, Math.Min);
			int result = -1;
			void OnCollectivePropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				result = ((PropertyMutatedEventArgs<int>)e).NewValue;
			}

			// Act
			collection.Add(new Giraffe() { Property = expected + 1 });
			collection.Add(new Giraffe() { Property = expected });

			Assert.AreEqual(expected, result);
		}
		[TestMethod]
		public void TestBindRecursively()
		{
			//Arrange
			var root = new Foo<int>();
			root.SourceProperty = new Foo<int>.Bar();
			int handledCount = 0;
			EventBindingExtensions.BindRecursively<int>(root, propertyName: nameof(Foo<int>.Bar.Property), handle);

			//Act
			root.SourceProperty.Property = 2;
			void handle(object sender, PropertyMutatedEventArgs<int> e)
			{
				handledCount++;
			}

			//Assert
			Assert.AreEqual(1, handledCount);
		}

	}
}
