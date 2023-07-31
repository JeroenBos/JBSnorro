using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Dynamic
{
	/// <summary> Allows to dynamically access non-public members on a certain type. </summary>
	public class NonPublicMembersDynamicWrapper : DynamicObject
	{
		private readonly object _obj;
		private Type type => _obj.GetType();

		/// <param name="obj"> The type on which non-public members can be accessed through this wrapper. </param>
		public NonPublicMembersDynamicWrapper(object obj)
		{
			Contract.Requires(obj != null);

			this._obj = obj;
		}
		public override IEnumerable<string> GetDynamicMemberNames()
		{
			return base.GetDynamicMemberNames();
		}
		public override DynamicMetaObject GetMetaObject(Expression parameter)
		{
			return base.GetMetaObject(parameter);
		}
		public override bool TryCreateInstance(CreateInstanceBinder binder, object?[]? args, [NotNullWhen(true)] out object? result)
		{
			return base.TryCreateInstance(binder, args, out result);
		}
		public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
		{
			return base.TryInvoke(binder, args, out result);
		}
		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
		{
			return base.TryGetIndex(binder, indexes, out result);
		}
		public override bool TryGetMember(GetMemberBinder binder, out object? result)
		{
			MemberInfo[] members = type.GetMember(binder.Name, BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (members.Length == 0)
			{
				result = null;
				return false;
			}
			if (members.Length == 1)
			{
				result = members[0] switch
				{
					PropertyInfo pi => pi.GetValue(this._obj, null),
					FieldInfo fi => fi.GetValue(this._obj),
					_ => throw new NotImplementedException("member not field or property")
				};
				if (result == null)
				{
					return true;
				}

				result = new NonPublicMembersDynamicWrapper(result);
				return true;
			}
			throw new InvalidOperationException($"Multiple members with name '{binder.Name}'");
		}

		// Handle static methods
		public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
		{
			MethodInfo? method = type.GetMethod(binder.Name, BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method == null)
			{
				result = null;
				return false;
			}

			result = method.Invoke(_obj, args);
			if (result != null)
				result = new NonPublicMembersDynamicWrapper(result);
			return true;
		}
		public override bool TryConvert(System.Dynamic.ConvertBinder binder, out object? result)
		{
			if (!binder.Type.IsAssignableFrom(this.type))
				return base.TryConvert(binder, out result); // let the base call throw the exception

			result = this._obj;
			return true;
		}
	}
}
