using System;
using NUnit.Framework;

namespace kevoreedotnetmodelgenerator2
{

	[TestFixture]
	public class EmptyClass
	{
		public EmptyClass ()
		{
		}

		[Test]
		public void Test()
		{
			new MyClass ().loadDll (@"/home/mleduc/dev/dotnet/kevoree-dotnet-comp-yield-version/bin/Release/YieldVersion.dll");
		}
	}
}

