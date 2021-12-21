using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	internal sealed class DefaultINotifyPropertyChangedPostponementObject : IDisposable
	{
		private readonly DefaultINotifyPropertyChanged obj;
		public DefaultINotifyPropertyChangedPostponementObject(DefaultINotifyPropertyChanged obj)
		{
			this.obj = obj;
			obj.PostponePropertyChangedInvocation();
		}
		public void Dispose()
		{
			obj.RemovePostponement();
		}
	}
}
