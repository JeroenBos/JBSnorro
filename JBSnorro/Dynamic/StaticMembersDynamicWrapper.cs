using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Dynamic
{
	/// <summary> Allows to dynamically access static members on a certain type. </summary>
	public class StaticMembersDynamicWrapper : DynamicObject
	{
		private Type _type;
		/// <param name="type"> The type on which static members can be accessed. </param>
		public StaticMembersDynamicWrapper(Type type)
		{
			Contract.Requires(type != null);

			_type = type;
		}
		/// <param name="typeName"> The name of the type on which static members can be accessed. </param>
		public StaticMembersDynamicWrapper(string typeName) : this(Type.GetType(typeName)) { }
		// Handle static properties
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			PropertyInfo prop = _type.GetProperty(binder.Name, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (prop == null)
			{
				result = null;
				return false;
			}

			result = prop.GetValue(null, null);
			return true;
		}

		// Handle static methods
		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			MethodInfo method = _type.GetMethod(binder.Name, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (method == null)
			{
				result = null;
				return false;
			}

			result = method.Invoke(null, args);
			return true;
		}
	}
}
